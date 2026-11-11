using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalFinanceApp.Models;
using System.Security.Claims;

namespace PersonalFinanceApp.Controllers
{
    [ApiController]
    [Route("api/banking")]
    [Route("api/sepay")]
    [Route("api/webhook")]
    [Route("webhook")]
    public class BankingWebhookController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BankingWebhookController> _logger;

        public BankingWebhookController(ApplicationDbContext context, ILogger<BankingWebhookController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /api/banking/test hoặc /api/banking/webhook (Kiểm tra trạng thái cổng Webhook)
        [HttpGet]
        [HttpGet("test")]
        [HttpGet("webhook")]
        public IActionResult Test()
        {
            return Ok(new
            {
                status = "online",
                service = "CashFlow Open Banking Gateway (SePAY / OpenBanking Ready)",
                timestamp = DateTime.UtcNow,
                message = "Cổng Webhook đang hoạt động tốt và sẵn sàng nhận dữ liệu giao dịch ngân hàng!"
            });
        }

        // GET: /api/banking/live-sync (Endpoint thời gian thực phục vụ Auto-Sync số dư không cần F5)
        [HttpGet("live-sync")]
        public async Task<IActionResult> LiveSync([FromQuery] int? lastTxId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Ok(new { isAuthenticated = false });
                }

                // Lấy danh sách ví của user
                var wallets = await _context.Wallets
                    .Where(w => w.UserId == userId)
                    .Select(w => new
                    {
                        id = w.Id,
                        name = w.Name,
                        balance = w.InitialBalance,
                        formattedBalance = w.InitialBalance.ToString("N0") + " đ",
                        walletType = w.WalletType,
                        bankName = w.BankName,
                        accountNumber = w.AccountNumber
                    })
                    .ToListAsync();

                decimal totalBalance = wallets.Sum(w => w.balance);

                // Tính tổng thu nhập và chi tiêu của user
                var incomeSum = await _context.Transactions
                    .Where(t => t.UserId == userId && t.Category != null && t.Category.Type == 1)
                    .SumAsync(t => (decimal?)t.Amount) ?? 0m;

                var expenseSum = await _context.Transactions
                    .Where(t => t.UserId == userId && t.Category != null && t.Category.Type == 0)
                    .SumAsync(t => (decimal?)t.Amount) ?? 0m;

                // Lấy transaction ID lớn nhất hiện tại của user
                int currentMaxTxId = await _context.Transactions
                    .Where(t => t.UserId == userId)
                    .Select(t => (int?)t.Id)
                    .MaxAsync() ?? 0;

                var newTransactions = new List<object>();

                // Nếu client đã có lastTxId và server có ID mới hơn -> gom các giao dịch mới nổ
                if (lastTxId.HasValue && lastTxId.Value > 0 && currentMaxTxId > lastTxId.Value)
                {
                    var rawNewTxs = await _context.Transactions
                        .Include(t => t.Category)
                        .Include(t => t.Wallet)
                        .Where(t => t.UserId == userId && t.Id > lastTxId.Value)
                        .OrderByDescending(t => t.Id)
                        .Take(10)
                        .ToListAsync();

                    foreach (var t in rawNewTxs)
                    {
                        bool isIncome = t.Category?.Type == 1;
                        newTransactions.Add(new
                        {
                            id = t.Id,
                            amount = t.Amount,
                            formattedAmount = (isIncome ? "+" : "-") + t.Amount.ToString("N0") + " đ",
                            isIncome,
                            note = t.Note ?? "",
                            categoryName = t.Category?.Name ?? (isIncome ? "Thu nhập từ ngân hàng" : "Chi tiêu qua ngân hàng"),
                            categoryColor = t.Category?.Color ?? (isIncome ? "#20c997" : "#ff6b81"),
                            categoryIcon = t.Category?.Icon ?? (isIncome ? "fa-wallet" : "fa-tag"),
                            walletId = t.WalletId,
                            walletName = t.Wallet?.Name ?? "Ví",
                            walletIcon = t.Wallet?.Icon ?? "fa-wallet",
                            date = t.TransactionDate.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
                        });
                    }
                }

                return Ok(new
                {
                    isAuthenticated = true,
                    latestTxId = currentMaxTxId,
                    hasNew = newTransactions.Count > 0,
                    newTransactions,
                    wallets,
                    totalBalance,
                    formattedTotalBalance = totalBalance.ToString("N0") + " đ",
                    totalIncome = incomeSum,
                    formattedTotalIncome = "+" + incomeSum.ToString("N0") + " đ",
                    totalExpense = expenseSum,
                    formattedTotalExpense = expenseSum.ToString("N0") + " đ",
                    netBalance = incomeSum - expenseSum,
                    formattedNetBalance = (incomeSum - expenseSum >= 0 ? "+" : "") + (incomeSum - expenseSum).ToString("N0") + " đ"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tại LiveSync endpoint");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // POST: /api/banking/webhook, /api/banking, /api/sepay/webhook, /api/sepay, /webhook (Cổng nhận Webhook từ SePAY)
        [HttpPost]
        [HttpPost("webhook")]
        [HttpPost("webhooks")]
        public async Task<IActionResult> ReceiveWebhook([FromBody] SepayWebhookPayload? payload)
        {
            try
            {
                _logger.LogInformation("Nhận webhook ngân hàng: {@Payload}", payload);

                if (payload == null)
                {
                    return Ok(new
                    {
                        success = false,
                        isTestAcknowledged = true,
                        message = "Máy chủ CashFlow đã nhận được tín hiệu Webhook (Payload rỗng hoặc ping test)."
                    });
                }

                // Xác định số tài khoản ngân hàng
                var rawAccount = (payload.AccountNumber ?? "").Trim();
                if (string.IsNullOrEmpty(rawAccount))
                {
                    return Ok(new
                    {
                        success = false,
                        isTestAcknowledged = true,
                        message = "Máy chủ CashFlow đã nhận được tín hiệu Webhook (Thiếu số tài khoản để khớp ví)."
                    });
                }

                // Tìm ví liên kết khớp với số tài khoản
                var wallet = await _context.Wallets
                    .FirstOrDefaultAsync(w => w.AccountNumber != null &&
                        (w.AccountNumber.Trim() == rawAccount || rawAccount.EndsWith(w.AccountNumber.Trim())));

                if (wallet == null)
                {
                    _logger.LogWarning("Không tìm thấy ví nào liên kết với số tài khoản {Account}", rawAccount);
                    // Trả về 200 OK kèm thông điệp rõ ràng để công cụ 'Gửi thử' của SePAY thành công, không báo lỗi 404
                    return Ok(new
                    {
                        success = false,
                        isTestAcknowledged = true,
                        message = $"Máy chủ CashFlow đã nhận Webhook thành công! Số tài khoản '{rawAccount}' chưa khớp với ví nào trong hệ thống."
                    });
                }

                // Kiểm tra công tắc: Quyền tự động theo dõi biến động số dư
                if (!wallet.IsSyncEnabled)
                {
                    _logger.LogInformation("Ví {WalletName} đã TẮT quyền theo dõi biến động số dư. Bỏ qua.", wallet.Name);
                    return Ok(new
                    {
                        success = false,
                        message = $"Ví '{wallet.Name}' hiện đang TẮT công tắc theo dõi biến động số dư. Giao dịch không được tự động ghi nhận theo chính sách bảo mật."
                    });
                }

                // Xác định số tiền và loại giao dịch (In/Out)
                decimal amount = 0;
                string transferType = (payload.TransferType ?? "").ToLower();

                if (payload.TransferAmount.HasValue && payload.TransferAmount.Value > 0)
                {
                    amount = payload.TransferAmount.Value;
                }
                else if (payload.AmountIn.HasValue && payload.AmountIn.Value > 0)
                {
                    amount = payload.AmountIn.Value;
                    transferType = "in";
                }
                else if (payload.AmountOut.HasValue && payload.AmountOut.Value > 0)
                {
                    amount = payload.AmountOut.Value;
                    transferType = "out";
                }

                if (amount <= 0)
                {
                    return Ok(new
                    {
                        success = false,
                        isTestAcknowledged = true,
                        message = "Máy chủ CashFlow đã nhận tín hiệu Webhook (Số tiền giao dịch kiểm tra = 0 đ)."
                    });
                }

                // Lấy Danh mục tương ứng
                var inCategory = await _context.Categories.FirstOrDefaultAsync(c => c.Name == "Thu nhập từ ngân hàng")
                    ?? await _context.Categories.FirstOrDefaultAsync(c => c.Type == 1);

                var outCategory = await _context.Categories.FirstOrDefaultAsync(c => c.Name == "Chi tiêu qua ngân hàng")
                    ?? await _context.Categories.FirstOrDefaultAsync(c => c.Type == 0);

                var content = !string.IsNullOrWhiteSpace(payload.Content)
                    ? payload.Content.Trim()
                    : (!string.IsNullOrWhiteSpace(payload.TransactionContent)
                        ? payload.TransactionContent.Trim()
                        : (!string.IsNullOrWhiteSpace(payload.Description) ? payload.Description.Trim() : "Giao dịch ngân hàng tự động"));

                var bankTag = !string.IsNullOrWhiteSpace(payload.Gateway) ? payload.Gateway : (wallet.BankName ?? "Ngân hàng");

                if (transferType == "in" || transferType == "incoming")
                {
                    // TIỀN VÀO (+) -> Tăng số dư ví + Tạo Transaction Thu Nhập
                    wallet.InitialBalance += amount;

                    var tx = new Transaction
                    {
                        Amount = amount,
                        TransactionDate = DateTime.UtcNow,
                        Note = $"[{bankTag}] {content}",
                        WalletId = wallet.Id,
                        CategoryId = inCategory!.Id,
                        UserId = wallet.UserId
                    };
                    _context.Transactions.Add(tx);
                }
                else
                {
                    // TIỀN RA (-) -> Giảm số dư ví + Tạo Transaction Chi Tiêu
                    wallet.InitialBalance -= amount;

                    var tx = new Transaction
                    {
                        Amount = amount,
                        TransactionDate = DateTime.UtcNow,
                        Note = $"[{bankTag}] {content}",
                        WalletId = wallet.Id,
                        CategoryId = outCategory!.Id,
                        UserId = wallet.UserId
                    };
                    _context.Transactions.Add(tx);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Giao dịch ngân hàng xử lý thành công cho ví {WalletName}, số tiền: {Amount}", wallet.Name, amount);

                return Ok(new
                {
                    success = true,
                    message = $"Đã ghi nhận giao dịch thành công! Số dư mới của ví '{wallet.Name}' là {wallet.InitialBalance:N0} đ.",
                    walletId = wallet.Id,
                    walletName = wallet.Name,
                    newBalance = wallet.InitialBalance,
                    transferType,
                    amount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý webhook ngân hàng.");
                return StatusCode(500, new { success = false, message = "Lỗi máy chủ nội bộ: " + ex.Message });
            }
        }
    }

    public class SepayWebhookPayload
    {
        public long? Id { get; set; }
        public string? Gateway { get; set; }
        public string? TransactionDate { get; set; }
        public string? AccountNumber { get; set; }
        public string? SubAccount { get; set; }
        public string? Code { get; set; }
        public string? Content { get; set; }
        public string? Description { get; set; }
        public string? TransactionContent { get; set; }
        public string? ReferenceCode { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? TransferType { get; set; } // "in" hoặc "out"
        public decimal? TransferAmount { get; set; }
        public decimal? AmountIn { get; set; }
        public decimal? AmountOut { get; set; }
        public decimal? Accumulated { get; set; }
    }
}
