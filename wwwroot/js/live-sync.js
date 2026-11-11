/**
 * CashFlow Real-time Live Sync Engine
 * Tự động đồng bộ số dư ví và sổ giao dịch theo thời gian thực (Zero F5)
 * Hỗ trợ Webhook ngân hàng (SePAY / Open Banking / Sandbox Simulator)
 */

(function () {
    let lastKnownTxId = 0;
    let isInitialized = false;
    let isPolling = false;

    // Âm thanh thông báo nhẹ nhàng sinh bằng Web Audio API (không cần tải file audio ngoài)
    function playLiveChime() {
        try {
            const AudioContext = window.AudioContext || window.webkitAudioContext;
            if (!AudioContext) return;
            const ctx = new AudioContext();
            const now = ctx.currentTime;

            // Nốt 1: D5 (587.33 Hz)
            const osc1 = ctx.createOscillator();
            const gain1 = ctx.createGain();
            osc1.type = "sine";
            osc1.frequency.setValueAtTime(587.33, now);
            gain1.gain.setValueAtTime(0.12, now);
            gain1.gain.exponentialRampToValueAtTime(0.001, now + 0.25);
            osc1.connect(gain1);
            gain1.connect(ctx.destination);
            osc1.start(now);
            osc1.stop(now + 0.25);

            // Nốt 2: A5 (880 Hz) - vang cao hơn tạo tiếng chuông ngân
            const osc2 = ctx.createOscillator();
            const gain2 = ctx.createGain();
            osc2.type = "sine";
            osc2.frequency.setValueAtTime(880, now + 0.1);
            gain2.gain.setValueAtTime(0.18, now + 0.1);
            gain2.gain.exponentialRampToValueAtTime(0.001, now + 0.45);
            osc2.connect(gain2);
            gain2.connect(ctx.destination);
            osc2.start(now + 0.1);
            osc2.stop(now + 0.45);
        } catch (e) {
            // Trình duyệt chưa cấp quyền phát audio tự động -> bỏ qua êm đềm
        }
    }

    // Hiển thị Toast nổi góc màn hình
    function showLiveToast(tx) {
        const container = document.getElementById("liveSyncToastContainer");
        if (!container) return;

        const toastId = "liveToast_" + Date.now() + "_" + Math.floor(Math.random() * 1000);
        const isIncome = tx.isIncome;
        const bgClass = isIncome ? "bg-success" : "bg-danger";
        const icon = isIncome ? "fa-arrow-trend-up" : "fa-arrow-trend-down";
        const typeLabel = isIncome ? "Tiền Vào Ngân Hàng (+)" : "Tiền Ra Ngân Hàng (-)";

        const toastHtml = `
            <div id="${toastId}" class="toast align-items-center text-white ${bgClass} border-0 shadow-lg rounded-4 mb-2" role="alert" aria-live="assertive" aria-atomic="true" data-bs-delay="6500">
                <div class="d-flex p-2">
                    <div class="toast-body d-flex align-items-center gap-2">
                        <div class="p-2 bg-white bg-opacity-25 rounded-circle text-center" style="width: 38px; height: 38px; display: flex; align-items: center; justify-content: center; flex-shrink: 0;">
                            <i class="fa-solid ${icon} fa-lg"></i>
                        </div>
                        <div style="min-width: 0;">
                            <div class="small text-uppercase fw-bold text-white-75" style="letter-spacing: 0.5px; font-size: 0.72rem;">
                                ⚡ ${typeLabel} &bull; ${tx.walletName}
                            </div>
                            <div class="fs-6 fw-bold mb-0">
                                ${tx.formattedAmount}
                            </div>
                            <div class="small text-white-50 text-truncate" style="max-width: 270px; font-size: 0.76rem;">
                                ${tx.note || tx.categoryName}
                            </div>
                        </div>
                    </div>
                    <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Đóng"></button>
                </div>
            </div>
        `;

        container.insertAdjacentHTML("beforeend", toastHtml);
        const toastElem = document.getElementById(toastId);
        if (window.bootstrap && window.bootstrap.Toast) {
            const bsToast = new window.bootstrap.Toast(toastElem);
            bsToast.show();
            toastElem.addEventListener("hidden.bs.toast", () => toastElem.remove());
        }
    }

    // Tự động cập nhật số dư trên trang Quản lý Ví (/Wallets)
    function updateWalletsPage(data) {
        if (!data.wallets) return;

        data.wallets.forEach(w => {
            const balanceSpan = document.getElementById(`balance-val-${w.id}`);
            if (balanceSpan) {
                const oldText = balanceSpan.innerText.trim();
                const newText = w.formattedBalance;
                if (oldText !== newText) {
                    balanceSpan.innerText = newText;
                    balanceSpan.classList.remove("text-success", "text-danger", "live-pulse-green", "live-pulse-red");
                    if (w.balance < 0) {
                        balanceSpan.classList.add("text-danger", "live-pulse-red");
                    } else {
                        balanceSpan.classList.add("text-success", "live-pulse-green");
                    }
                    setTimeout(() => {
                        balanceSpan.classList.remove("live-pulse-green", "live-pulse-red");
                    }, 2000);
                }
            }
        });

        // Cập nhật thẻ Tổng Tài Sản Khả Dụng
        const totalAvailableElem = document.getElementById("total-available-balance");
        if (totalAvailableElem) {
            const oldTotal = totalAvailableElem.innerText.trim();
            const newTotal = data.formattedTotalBalance;
            if (oldTotal !== newTotal) {
                totalAvailableElem.innerText = newTotal;
                totalAvailableElem.classList.remove("text-success", "text-danger", "live-pulse-green", "live-pulse-red");
                if (data.totalBalance < 0) {
                    totalAvailableElem.classList.add("text-danger", "live-pulse-red");
                } else {
                    totalAvailableElem.classList.add("text-success", "live-pulse-green");
                }
                setTimeout(() => {
                    totalAvailableElem.classList.remove("live-pulse-green", "live-pulse-red");
                }, 2000);
            }
        }
    }

    // Tự động cập nhật trang Sổ Giao Dịch (/Transactions)
    function updateTransactionsPage(data) {
        // Cập nhật 3 thẻ thống kê
        const incElem = document.getElementById("live-total-income");
        if (incElem) incElem.innerText = data.formattedTotalIncome;

        const expElem = document.getElementById("live-total-expense");
        if (expElem) expElem.innerText = data.formattedTotalExpense;

        const netElem = document.getElementById("live-net-balance");
        if (netElem) {
            netElem.innerText = data.formattedNetBalance;
            netElem.classList.remove("text-primary", "text-danger");
            netElem.classList.add(data.netBalance >= 0 ? "text-primary" : "text-danger");
        }

        // Chèn giao dịch mới trực tiếp vào đầu bảng danh sách
        const tbody = document.getElementById("transactions-table-body");
        if (tbody && data.newTransactions && data.newTransactions.length > 0) {
            data.newTransactions.forEach(tx => {
                // Kiểm tra trùng lặp
                if (document.getElementById(`tx-row-${tx.id}`)) return;

                const isIncome = tx.isIncome;
                const amountClass = isIncome ? "amount-income" : "amount-expense";
                const catColor = tx.categoryColor || (isIncome ? "#20c997" : "#ff6b81");
                const catIcon = tx.categoryIcon || (isIncome ? "fa-wallet" : "fa-tag");

                const rowHtml = `
                    <tr id="tx-row-${tx.id}" class="live-row-enter">
                        <td>
                            <div class="d-flex align-items-center">
                                <div class="transaction-badge-icon" style="background: color-mix(in srgb, ${catColor} 15%, white); color: ${catColor};">
                                    <i class="fa-solid ${catIcon}"></i>
                                </div>
                                <div>
                                    <div class="fw-bold text-dark">${tx.categoryName}</div>
                                    ${tx.note ? `<small class="text-muted">${tx.note}</small>` : ""}
                                </div>
                            </div>
                        </td>
                        <td>
                            <span class="badge-wallet">
                                <i class="fa-solid ${tx.walletIcon || "fa-wallet"} me-1"></i>
                                ${tx.walletName}
                            </span>
                        </td>
                        <td>
                            <small class="text-muted">
                                <i class="fa-regular fa-clock me-1"></i>
                                ${tx.date}
                            </small>
                        </td>
                        <td class="text-end">
                            <span class="${amountClass}">
                                ${tx.formattedAmount}
                            </span>
                        </td>
                        <td class="text-end text-nowrap">
                            <a href="/Transactions/Edit/${tx.id}" class="btn btn-sm btn-outline-secondary me-1" title="Chỉnh sửa">
                                <i class="fa-solid fa-pen-to-square"></i>
                            </a>
                            <a href="/Transactions/Delete/${tx.id}" class="btn btn-sm btn-outline-danger" title="Xóa">
                                <i class="fa-solid fa-trash-can"></i>
                            </a>
                        </td>
                    </tr>
                `;
                tbody.insertAdjacentHTML("afterbegin", rowHtml);
            });
        }
    }

    // Hàm gọi polling ngầm
    async function pollLiveSync() {
        if (isPolling) return;
        isPolling = true;

        try {
            const url = `/api/banking/live-sync?lastTxId=${lastKnownTxId}`;
            const resp = await fetch(url, {
                method: "GET",
                headers: { "Accept": "application/json" }
            });

            if (!resp.ok) return;

            const data = await resp.json();
            if (!data.isAuthenticated) return;

            if (!isInitialized) {
                // Khởi tạo ban đầu: Ghi nhận mốc transaction ID hiện có
                lastKnownTxId = data.latestTxId || 0;
                isInitialized = true;
                return;
            }

            // Luôn cập nhật số dư hiển thị nếu có biến động
            updateWalletsPage(data);
            updateTransactionsPage(data);

            // Nếu có giao dịch mới nổ từ SePAY / Simulator
            if (data.hasNew && data.newTransactions && data.newTransactions.length > 0) {
                lastKnownTxId = data.latestTxId;
                playLiveChime();
                data.newTransactions.forEach(tx => {
                    showLiveToast(tx);
                });
            } else if (data.latestTxId > lastKnownTxId) {
                lastKnownTxId = data.latestTxId;
            }
        } catch (err) {
            // Lỗi mạng tạm thời hoặc server đang sleep -> bỏ qua tiếp tục chờ vòng sau
        } finally {
            isPolling = false;
        }
    }

    // Khởi chạy khi DOM đã sẵn sàng
    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", () => {
            pollLiveSync();
            setInterval(pollLiveSync, 3000);
        });
    } else {
        pollLiveSync();
        setInterval(pollLiveSync, 3000);
    }
})();
