# PRODUCT BACKLOG - DỰ ÁN CASHFLOW
**Hệ thống Quản lý Chi tiêu Cá nhân (Personal Finance Expense Management Web App)**

* **Học phần**: Công nghệ phần mềm nâng cao (CNPMNC)
* **Đơn vị**: Khoa Công nghệ Thông tin – Trường Đại học Ngoại ngữ - Tin học TP.HCM (HUFLIT)
* **Giảng viên hướng dẫn**: ThS. Nguyễn Tấn Khoa
* **Nhóm thực hiện (Nhóm 12)**:
  * Nguyễn Hoàng Phúc – MSSV: 24DH111453 (Product Owner / Full-stack Developer)
  * Đào Tuấn Phúc – MSSV: 24DH113351 (Scrum Master / Full-stack Developer)
* **Kho lưu trữ mã nguồn**: [Weii-hub/Personal-Finance-Expense-Management-App-Web](https://github.com/Weii-hub/Personal-Finance-Expense-Management-App-Web)
* **Môi trường triển khai thực tế**: Render Cloud (Docker Container) + Supabase Cloud (PostgreSQL 15 via Session Pooler Port 5432)

---

## 1. QUY ƯỚC & ĐỊNH NGHĨA (AGILE / SCRUM STANDARDS)

### 1.1. Cấu trúc một Hạng mục Backlog (Product Backlog Item - PBI)
Mỗi hạng mục trong Product Backlog được chuẩn hóa với các trường thông tin:
* **Mã PBI**: Định danh duy nhất theo cấu trúc `PBI-[STT]`.
* **Phân hệ (Epic)**: Nhóm chức năng nghiệp vụ tương ứng trong hệ thống.
* **User Story**: Phát biểu theo mẫu chuẩn: *"Là một [Đối tượng người dùng], tôi muốn [Hành động / Thao tác], để [Giá trị nghiệp vụ / Mục đích]"*.
* **Tiêu chí nghiệm thu (Acceptance Criteria - AC)**: Đặc tả kiểm thử theo chuẩn **Gherkin (Given - When - Then)** với các kịch bản thực tế, điều kiện biên và dữ liệu kiểm thử cụ thể.
* **Đặc tả kỹ thuật (Technical Scope)**: Các Controller, Action, Model, bảng Cơ sở dữ liệu và ràng buộc liên quan.
* **Độ ưu tiên (MoSCoW)**:
  * `Must Have` (Bắt buộc): Tính năng sống còn của hệ thống.
  * `Should Have` (Nên có): Tính năng quan trọng, hoàn thiện trải nghiệm nghiệp vụ.
  * `Could Have` (Có thể có): Tính năng nâng cao, tối ưu hóa hệ thống.
* **Ước lượng (Story Points - SP)**: Tính theo dãy số Fibonacci (1, 2, 3, 5, 8).
* **Sprint**: Giai đoạn phát triển tương ứng (Sprint 1, Sprint 2, Sprint 3).
* **Issue GitHub**: Mã định danh liên kết trực tiếp tới Issue trên GitHub Repository.

### 1.2. Tiêu chuẩn Hoàn thành (Definition of Done - DoD)
Một PBI chỉ được chuyển sang trạng thái **Done** khi thỏa mãn toàn bộ các điều kiện sau:
1. Logic nghiệp vụ và giao diện được cài đặt đầy đủ theo đặc tả.
2. Xác thực dữ liệu 2 lớp: Client-side (HTML5/JavaScript) và Server-side (ASP.NET Core `ModelState` & Data Annotations).
3. Đảm bảo tính toàn vẹn dữ liệu (Data Integrity): có ràng buộc khóa ngoại, transaction CSDL nếu cần, xử lý đầy đủ các trường hợp biên (số dư âm, trùng khóa, null).
4. Phân quyền và cô lập dữ liệu theo người dùng (`UserId` từ `ClaimsPrincipal`).
5. Biên dịch thành công với `dotnet build`: **0 Warning(s), 0 Error(s)**.
6. Đã kiểm thử chức năng (Manual Testing) trên môi trường phát triển cục bộ.
7. Mã nguồn được commit theo chuẩn Conventional Commits, tạo Pull Request và merge vào nhánh `main`.
8. Được tự động build và deploy thành công trên Render Cloud qua Dockerfile.

---

## 2. TỔNG QUAN TIẾN ĐỘ SPRINT & STORY POINTS

| Sprint | Mục tiêu Sprint | Issues phụ trách | Tổng Story Points | Trạng thái |
| :--- | :--- | :--- | :---: | :---: |
| **Sprint 1** | Khởi tạo cấu trúc đồ án, CSDL Supabase, giao diện dùng chung và phân hệ Quản lý Đa Ví. | #1, #2, #3 | **13 SP** | **100% Hoàn thành** |
| **Sprint 2** | Xây dựng Danh mục Thu/Chi, Sổ ghi chép giao dịch và logic tự động tính toán số dư ví. | #4, #5, #6 | **13 SP** | **100% Hoàn thành** |
| **Sprint 3** | Báo cáo Chart.js, Đặt ngân sách 80%, Mục tiêu tiết kiệm, Xác thực Identity, Phân hệ Admin và Tối ưu Mobile. | #7, #8, #9, #10, #11, #12, #13, #14, #15 | **34 SP** | **100% Hoàn thành** |
| **Sprint 4** | Mở rộng FinTech: Phân loại Ví kép (Tiền mặt vs Ngân hàng), Bảo mật Mã PIN 6 số, Open Banking Webhook & Sandbox Simulator. | #19, #20, #21, #22 | **18 SP** | **100% Hoàn thành** |
| **TỔNG CỘNG** | **Toàn bộ hệ thống CashFlow hoàn thiện và đưa vào vận hành thực tế** | **19/19 Issues** | **78 SP** | **100% Hoàn thành** |

---

## 3. CHI TIẾT PRODUCT BACKLOG

### PHÂN HỆ 1: QUẢN LÝ VÍ TIỀN (WALLETS MANAGEMENT)

#### PBI-01: Khởi tạo và Quản lý Đa Ví tiền
* **Phân hệ**: Quản lý Ví tiền (Wallets)
* **GitHub Issue**: Resolves #1, #2, #3
* **Độ ưu tiên**: Must Have | **Điểm ước lượng**: 5 SP | **Sprint**: Sprint 1 | **Trạng thái**: Done
* **User Story**:
  > Là một người dùng cá nhân, tôi muốn tạo nhiều ví tiền độc lập (ví dụ: Ví Tiền mặt, Tài khoản MBBank, Ví điện tử MoMo) với số dư ban đầu tùy chỉnh, để tôi có thể theo dõi và quản lý riêng biệt dòng tiền trong từng tài khoản.
* **Đặc tả kỹ thuật**:
  * Entity: `Wallet` (`Id`, `Name`, `InitialBalance`, `Icon`, `UserId`). Bảng CSDL: `"Wallets"`.
  * Controller: `WalletsController` (`Index`, `Create`).
  * Giao diện: `Views/Wallets/Index.cshtml`, `Views/Wallets/Create.cshtml`.
  * Hỗ trợ chọn icon trực quan với FontAwesome (`fa-wallet`, `fa-building-columns`, `fa-mobile-screen`...).
* **Tiêu chí nghiệm thu (Acceptance Criteria)**:
  * **AC-01.1 (Tạo ví hợp lệ)**:
    * *Given*: Người dùng đã đăng nhập và đang ở trang `/Wallets/Create`.
    * *When*: Nhập tên ví `"Tài khoản Techcombank"`, số dư ban đầu `2.000.000`, chọn icon `fa-building-columns` và nhấn *"Tạo Ví"*.
    * *Then*: Hệ thống lưu ví vào CSDL gán đúng `UserId` của tài khoản; chuyển hướng về `/Wallets/Index` hiển thị thông báo thành công và thẻ ví Techcombank có số dư `2.000.000 đ`.
  * **AC-01.2 (Kiểm tra dữ liệu rỗng)**:
    * *Given*: Người dùng để trống trường Tên ví hoặc nhập khoảng trắng.
    * *When*: Nhấn nút *"Tạo Ví"*.
    * *Then*: Hệ thống chặn gửi form, hiển thị thông báo lỗi: *"Tên ví không được để trống"*.

#### PBI-02: Chỉnh sửa thông tin ví & Khóa sửa số dư trực tiếp (Bảo vệ toàn vẹn dữ liệu)
* **Phân hệ**: Quản lý Ví tiền (Wallets)
* **GitHub Issue**: Resolves #3, Update Security Fix (`5ff112b`)
* **Độ ưu tiên**: Must Have | **Điểm ước lượng**: 3 SP | **Sprint**: Sprint 1 (Cập nhật Sprint 3) | **Trạng thái**: Done
* **User Story**:
  > Là một người dùng, tôi muốn chỉnh sửa tên gọi và biểu tượng icon của ví; đồng thời hệ thống khóa ô nhập số dư trực tiếp, để đảm bảo tính toàn vẹn dữ liệu kế toán và ngăn ngừa hiện tượng cộng dồn số dư ảo khi xóa giao dịch quá khứ.
* **Đặc tả kỹ thuật**:
  * Controller: `WalletsController` (`Edit [HttpGet]`, `Edit [HttpPost]`).
  * Giao diện: `Views/Wallets/Edit.cshtml`.
  * Backend loại bỏ binding cập nhật `InitialBalance` từ form POST; hiển thị số dư dưới dạng thẻ Read-only trực quan kèm nhãn trạng thái và nút chuyển hướng nhanh sang *"Nạp tiền / Ghi thu"*.
* **Tiêu chí nghiệm thu (Acceptance Criteria)**:
  * **AC-02.1 (Sửa tên và icon thành công)**:
    * *Given*: Người dùng sở hữu ví có tên `"Ví cũ"`.
    * *When*: Vào trang sửa ví, đổi tên thành `"Ví Tiêu Vặt"`, đổi icon sang `fa-piggy-bank` và nhấn *"Lưu Thay Đổi"*.
    * *Then*: Tên ví và icon được cập nhật; số dư của ví giữ nguyên giá trị thực tế trong CSDL.
  * **AC-02.2 (Bảo vệ số dư khỏi sửa trực tiếp)**:
    * *Given*: Người dùng xem trang `/Wallets/Edit/{id}`.
    * *Then*: Ô số dư không cho phép nhập liệu bằng tay; có ghi chú giải thích nguyên tắc toàn vẹn dữ liệu và nút bấm *"Nạp tiền / Ghi thu"* dẫn sang trang tạo giao dịch thu.

#### PBI-03: Xóa ví an toàn với Ràng buộc bảo vệ dữ liệu 2 lớp (Two-layer Constraint)
* **Phân hệ**: Quản lý Ví tiền (Wallets)
* **GitHub Issue**: Resolves #3
* **Độ ưu tiên**: Must Have | **Điểm ước lượng**: 5 SP | **Sprint**: Sprint 1 | **Trạng thái**: Done
* **User Story**:
  > Là một người dùng, tôi muốn xóa những chiếc ví không còn sử dụng; nhưng hệ thống phải kiểm tra và ngăn chặn nếu ví đó đang chứa các giao dịch chi tiêu hoặc là chiếc ví duy nhất còn lại, để không làm hỏng lịch sử báo cáo tài chính.
* **Đặc tả kỹ thuật**:
  * Controller: `WalletsController` (`Delete [HttpGet]`, `DeleteConfirmed [HttpPost]`).
  * Giao diện: `Views/Wallets/Delete.cshtml`.
  * Xử lý ràng buộc: Kiểm tra `_context.Transactions.Any(t => t.WalletId == id)` và `_context.Wallets.Count(w => w.UserId == userId) <= 1`.
* **Tiêu chí nghiệm thu (Acceptance Criteria)**:
  * **AC-03.1 (Chặn xóa ví có giao dịch)**:
    * *Given*: Ví `"Tiền mặt"` đã phát sinh ít nhất 1 giao dịch trong bảng `Transactions`.
    * *When*: Người dùng nhấn nút Xóa ví và xác nhận xóa.
    * *Then*: Hệ thống từ chối xóa, rollback dữ liệu và hiển thị thông báo lỗi: *"Không thể xóa ví này vì đang chứa [X] giao dịch. Vui lòng chuyển hoặc xóa các giao dịch trước!"*.
  * **AC-03.2 (Chặn xóa chiếc ví duy nhất)**:
    * *Given*: Tài khoản người dùng hiện chỉ có đúng 1 chiếc ví duy nhất.
    * *When*: Người dùng thực hiện thao tác xóa ví.
    * *Then*: Hệ thống chặn xóa và hiển thị cảnh báo: *"Bạn phải duy trì ít nhất một ví tiền trong tài khoản để thực hiện giao dịch."*.
  * **AC-03.3 (Xóa ví hợp lệ)**:
    * *Given*: Người dùng có trên 1 ví và chiếc ví định xóa chưa từng có giao dịch nào.
    * *When*: Người dùng xác nhận xóa.
    * *Then*: Ví được xóa thành công khỏi CSDL, danh sách ví cập nhật lại ngay lập tức.

---

### PHÂN HỆ 2: QUẢN LÝ DANH MỤC THU / CHI (CATEGORIES MANAGEMENT)

#### PBI-04: Quản lý Phân loại Danh mục Thu nhập & Chi tiêu
* **Phân hệ**: Quản lý Danh mục (Categories)
* **GitHub Issue**: Resolves #4, #12
* **Độ ưu tiên**: Must Have | **Điểm ước lượng**: 5 SP | **Sprint**: Sprint 2 | **Trạng thái**: Done
* **User Story**:
  > Là một người dùng, tôi muốn xem, tạo mới, chỉnh sửa và phân loại các danh mục thành 2 nhóm rõ rệt: Chi tiêu (Type 0) và Thu nhập (Type 1), kèm mã màu và icon đại diện, để phân loại các dòng tiền vào/ra một cách khoa học.
* **Đặc tả kỹ thuật**:
  * Entity: `Category` (`Id`, `Name`, `Type`, `Color`, `Icon`). Bảng CSDL: `"Categories"`.
  * Controller: `CategoriesController` (`Index`, `Create`, `Edit`, `Delete`).
  * Giao diện: `Views/Categories/Index.cshtml`, `Create.cshtml`, `Edit.cshtml`, `Delete.cshtml`.
  * Tích hợp Input Color (`<input type="color">`) và hiển thị số lượng giao dịch thuộc danh mục.
* **Tiêu chí nghiệm thu (Acceptance Criteria)**:
  * **AC-04.1 (Tạo danh mục Chi tiêu & Thu nhập)**:
    * *Given*: Người dùng ở trang `/Categories/Create`.
    * *When*: Nhập tên `"Cà phê & Gặp gỡ"`, chọn Loại `"Chi tiêu"`, chọn màu `#ff6b81` và icon `fa-mug-hot` rồi bấm Lưu.
    * *Then*: Danh mục được tạo thành công với `Type = 0`; hiển thị đúng nhóm Chi tiêu trong danh sách.
  * **AC-04.2 (Chặn xóa danh mục đang có giao dịch)**:
    * *Given*: Danh mục `"Ăn uống"` đã được gán cho các giao dịch trong hệ thống.
    * *When*: Người dùng cố gắng thực hiện thao tác xóa danh mục.
    * *Then*: Hệ thống kiểm tra ràng buộc khóa ngoại, ngăn chặn xóa và thông báo: *"Không thể xóa danh mục đang được sử dụng trong các giao dịch"*.

---

### PHÂN HỆ 3: SỔ GHI CHÉP GIAO DỊCH (TRANSACTIONS MANAGEMENT)

#### PBI-05: Ghi chép Giao dịch Thu - Chi và Tự động tính toán số dư Ví
* **Phân hệ**: Sổ Giao Dịch (Transactions)
* **GitHub Issue**: Resolves #5, #6
* **Độ ưu tiên**: Must Have | **Điểm ước lượng**: 8 SP | **Sprint**: Sprint 2 | **Trạng thái**: Done
* **User Story**:
  > Là một người dùng, tôi muốn ghi lại các khoản thu hoặc chi phát sinh (chọn ví, danh mục, số tiền, ngày giao dịch và ghi chú); để hệ thống tự động cộng hoặc trừ số dư ví tương ứng theo thời gian thực và cảnh báo nếu số dư bị âm.
* **Đặc tả kỹ thuật**:
  * Entity: `Transaction` (`Id`, `WalletId`, `CategoryId`, `Amount`, `TransactionDate`, `Note`, `UserId`). Bảng CSDL: `"Transactions"`.
  * Controller: `TransactionsController` (`Create [HttpGet]`, `Create [HttpPost]`).
  * Logic số dư: 
    * Nếu `Category.Type == 1` (Thu): `wallet.InitialBalance += transaction.Amount`.
    * Nếu `Category.Type == 0` (Chi): `wallet.InitialBalance -= transaction.Amount`.
  * Kích hoạt cờ cảnh báo nếu `transaction.Amount > wallet.InitialBalance` (thấu chi).
* **Tiêu chí nghiệm thu (Acceptance Criteria)**:
  * **AC-05.1 (Ghi nhận khoản Thu hợp lệ)**:
    * *Given*: Ví `"Tiền mặt"` đang có số dư `500.000 đ`.
    * *When*: Tạo giao dịch Thu `"Lương part-time"` số tiền `3.000.000 đ` vào ví `"Tiền mặt"`.
    * *Then*: Giao dịch được lưu vào bảng `Transactions`; số dư ví `"Tiền mặt"` tự động tăng lên thành `3.500.000 đ`.
  * **AC-05.2 (Ghi nhận khoản Chi và Cảnh báo số dư âm)**:
    * *Given*: Ví `"MoMo"` có số dư `200.000 đ`.
    * *When*: Tạo giao dịch Chi `"Mua sách"` số tiền `350.000 đ`.
    * *Then*: Giao dịch được ghi nhận; số dư ví `"MoMo"` giảm còn `-150.000 đ`; hệ thống hiển thị cảnh báo: *"Ví 'MoMo' hiện đang bị âm số dư (-150.000 đ)"*.
  * **AC-05.3 (Validate số tiền giao dịch)**:
    * *Given*: Người dùng nhập số tiền giao dịch `<= 0` hoặc chuỗi ký tự không hợp lệ.
    * *When*: Nhấn nút Lưu giao dịch.
    * *Then*: Form báo lỗi yêu cầu số tiền phải lớn hơn 0 đ.

#### PBI-06: Lọc, Tìm kiếm & Điều chỉnh / Xóa Giao dịch chuẩn Kế toán
* **Phân hệ**: Sổ Giao Dịch (Transactions)
* **GitHub Issue**: Resolves #5, #6
* **Độ ưu tiên**: Must Have | **Điểm ước lượng**: 5 SP | **Sprint**: Sprint 2 | **Trạng thái**: Done
* **User Story**:
  > Là một người dùng, tôi muốn tra cứu giao dịch theo bộ lọc đa chiều (theo ví, danh mục, loại thu/chi, khoảng thời gian); cũng như có thể sửa hoặc xóa giao dịch với cơ chế hoàn trả số dư ví chuẩn xác.
* **Đặc tả kỹ thuật**:
  * Controller: `TransactionsController` (`Index` có parameters query, `Edit`, `DeleteConfirmed`).
  * Logic hoàn trả số dư khi xóa:
    * Xóa giao dịch Chi $\rightarrow$ Hoàn tiền: `wallet.InitialBalance += transaction.Amount`.
    * Xóa giao dịch Thu $\rightarrow$ Thu hồi: `wallet.InitialBalance -= transaction.Amount`.
* **Tiêu chí nghiệm thu (Acceptance Criteria)**:
  * **AC-06.1 (Lọc giao dịch chính xác)**:
    * *Given*: Người dùng có 20 giao dịch trong tháng 11/2026.
    * *When*: Chọn lọc theo Ví `"Tiền mặt"`, Danh mục `"Ăn uống"` và khoảng ngày `01/11/2026 - 10/11/2026`.
    * *Then*: Danh sách chỉ hiển thị các giao dịch thỏa mãn đồng thời tất cả các tiêu chí trên.
  * **AC-06.2 (Xóa khoản chi hoàn trả số dư chuẩn mực)**:
    * *Given*: Ví `"Tiền mặt"` có số dư `400.000 đ` và 1 giao dịch Chi `"Xem phim 100.000 đ"`.
    * *When*: Người dùng thực hiện xóa giao dịch xem phim đó.
    * *Then*: Giao dịch bị xóa khỏi CSDL; số dư ví `"Tiền mặt"` tự động được hoàn lại thành `500.000 đ`.

---

### PHÂN HỆ 4: QUẢN LÝ NGÂN SÁCH CHI TIÊU (BUDGETS MANAGEMENT)

#### PBI-07: Thiết lập Ngân sách Hàng tháng và Cảnh báo vượt ngưỡng 80% & Bội chi 100%
* **Phân hệ**: Quản lý Ngân sách (Budgets)
* **GitHub Issue**: Resolves #8
* **Độ ưu tiên**: High (Must Have) | **Điểm ước lượng**: 5 SP | **Sprint**: Sprint 3 | **Trạng thái**: Done
* **User Story**:
  > Là một người dùng, tôi muốn đặt hạn mức chi tiêu tối đa cho từng danh mục theo tháng và năm; để hệ thống theo dõi mức chi thực tế và tự động đổi màu cảnh báo khi chi tiêu vượt quá 80% hoặc vượt 100% hạn mức.
* **Đặc tả kỹ thuật**:
  * Entity: `Budget` (`Id`, `CategoryId`, `Amount`, `Month`, `Year`, `UserId`). Bảng CSDL: `"Budgets"`.
  * Controller: `BudgetsController` (`Index`, `Create`, `Edit`, `Delete`).
  * Giao diện: `Views/Budgets/Index.cshtml`, `Create.cshtml`, `Edit.cshtml`.
  * Logic tính toán: Truy vấn tổng chi thực tế `ActualSpent = SUM(Transactions.Amount)` trong tháng của danh mục tương ứng.
    * `Percent = (ActualSpent / Amount) * 100`.
    * Nếu `Percent >= 100%`: Cảnh báo Đỏ nguy hiểm (*"Bội chi ngân sách"*).
    * Nếu `80% <= Percent < 100%`: Cảnh báo Vàng cam (*"Cảnh báo: Đã tiêu vượt 80% hạn mức"*).
    * Nếu `Percent < 80%`: Thanh tiến độ Xanh an toàn.
* **Tiêu chí nghiệm thu (Acceptance Criteria)**:
  * **AC-07.1 (Tạo ngân sách không trùng lặp)**:
    * *Given*: Người dùng chưa đặt ngân sách cho danh mục `"Ăn uống"` trong tháng 11/2026.
    * *When*: Đặt hạn mức `2.000.000 đ` cho tháng 11/2026.
    * *Then*: Ngân sách được lưu thành công; hiển thị thanh tiến độ theo dõi.
    * *When*: Người dùng tạo tiếp ngân sách thứ 2 cho cùng danh mục `"Ăn uống"` trong cùng tháng 11/2026.
    * *Then*: Hệ thống bắt trùng và báo lỗi: *"Danh mục này đã được thiết lập ngân sách trong tháng được chọn."*.
  * **AC-07.2 (Kích hoạt cảnh báo 80%)**:
    * *Given*: Ngân sách `"Mua sắm"` tháng 11 là `1.000.000 đ`. Tổng chi hiện tại là `850.000 đ` (85%).
    * *When*: Người dùng truy cập trang Ngân sách.
    * *Then*: Thanh tiến độ chuyển sang màu vàng-cam kèm huy hiệu cảnh báo: *"Đã tiêu 85% - Cảnh báo sắp chạm trần ngân sách!"*.
  * **AC-07.3 (Kích hoạt cảnh báo bội chi >100%)**:
    * *Given*: Tổng chi tiêu thực tế đạt `1.200.000 đ` trên hạn mức `1.000.000 đ` (120%).
    * *Then*: Thanh tiến độ chuyển sang màu đỏ rực kèm thông báo: *"Đã vượt hạn mức 200.000 đ (120%)"*.

---

### PHÂN HỆ 5: MỤC TIÊU TIẾT KIỆM (SAVINGS GOALS)

#### PBI-08: Quản lý Mục tiêu Tiết kiệm và Liên kết Trích nạp / Rút tiền từ Ví
* **Phân hệ**: Mục tiêu Tiết kiệm (Savings Goals)
* **GitHub Issue**: Resolves #11, Update Feature (`ce417f8`)
* **Độ ưu tiên**: High (Must Have) | **Điểm ước lượng**: 5 SP | **Sprint**: Sprint 3 | **Trạng thái**: Done
* **User Story**:
  > Là một người dùng, tôi muốn tạo mục tiêu tiết kiệm tài chính (tên mục tiêu, số tiền cần đạt, ngày hết hạn); đồng thời có thể trích tiền từ ví cụ thể để nạp vào mục tiêu (tự trừ ví và tạo giao dịch chi), hoặc rút tiền tích lũy về ví khi cần (tự cộng ví và tạo giao dịch thu).
* **Đặc tả kỹ thuật**:
  * Entity: `SavingsGoal` (`Id`, `Name`, `TargetAmount`, `CurrentAmount`, `TargetDate`, `UserId`). Bảng CSDL: `"Goals"`.
  * Controller: `SavingsGoalsController` (`Index`, `Create`, `Details`, `Edit`, `AddFunds`, `WithdrawFunds`, `Delete`).
  * Giao diện: `Views/SavingsGoals/Index.cshtml`, `Details.cshtml`.
  * Tự động đảm bảo danh mục hệ thống: *"Tiết kiệm & Tích lũy"* (Type 0) và *"Thu hồi quỹ tiết kiệm"* (Type 1).
* **Tiêu chí nghiệm thu (Acceptance Criteria)**:
  * **AC-08.1 (Nạp tiền từ ví vào mục tiêu)**:
    * *Given*: Mục tiêu `"Mua Laptop Asus"` đang có `2.000.000 đ` / `15.000.000 đ`. Ví `"Ngân hàng"` có số dư `5.000.000 đ`.
    * *When*: Chọn nạp `3.000.000 đ` từ ví `"Ngân hàng"` vào mục tiêu.
    * *Then*: Số dư ví `"Ngân hàng"` giảm còn `2.000.000 đ`; xuất hiện 1 giao dịch chi tiêu mang danh mục *"Tiết kiệm & Tích lũy"*; số tiền tích lũy của mục tiêu tăng lên `5.000.000 đ` (đạt 33.3%).
  * **AC-08.2 (Chặn nạp tiền khi ví không đủ số dư)**:
    * *Given*: Ví chỉ còn `500.000 đ`.
    * *When*: Người dùng cố nạp `1.000.000 đ` vào mục tiêu.
    * *Then*: Hệ thống chặn giao dịch và báo lỗi: *"Ví không đủ số dư để nạp (Số dư khả dụng: 500.000 đ)"*.
  * **AC-08.3 (Rút tiền từ mục tiêu hoàn trả về ví)**:
    * *Given*: Mục tiêu đang có `5.000.000 đ`.
    * *When*: Rút `2.000.000 đ` từ mục tiêu chuyển về ví `"Tiền mặt"`.
    * *Then*: Mục tiêu còn lại `3.000.000 đ`; ví `"Tiền mặt"` được cộng thêm `2.000.000 đ`; tự động tạo giao dịch thu mang danh mục *"Thu hồi quỹ tiết kiệm"*.

---

### PHÂN HỆ 6: BÁO CÁO & PHÂN TÍCH TÀI CHÍNH (REPORTS & DASHBOARD)

#### PBI-09: Bảng điều khiển Tài chính Tổng quan (Dashboard)
* **Phân hệ**: Báo cáo & Dashboard
* **GitHub Issue**: Resolves #7
* **Độ ưu tiên**: Must Have | **Điểm ước lượng**: 3 SP | **Sprint**: Sprint 3 | **Trạng thái**: Done
* **User Story**:
  > Là một người dùng, tôi muốn một trang chủ Dashboard tổng hợp 3 chỉ số tài chính quan trọng trong tháng (Tổng tài sản, Tổng thu, Tổng chi), biểu đồ tròn phân tích chi tiêu và danh sách các ví kèm giao dịch gần nhất ngay khi đăng nhập.
* **Đặc tả kỹ thuật**:
  * Controller: `HomeController` (`Index`).
  * Giao diện: `Views/Home/Index.cshtml` kết hợp thư viện `Chart.js` vẽ trên thẻ `<canvas>`.
  * Truy vấn dữ liệu tổng hợp theo `UserId` trong tháng hiện tại.
* **Tiêu chí nghiệm thu (Acceptance Criteria)**:
  * **AC-09.1 (Hiển thị 3 chỉ số tài chính chuẩn)**:
    * *Given*: Người dùng có các ví với tổng số dư `10.000.000 đ`, trong tháng 11 tổng thu là `8.000.000 đ`, tổng chi là `3.000.000 đ`.
    * *When*: Vào trang chủ `/Home/Index`.
    * *Then*: 3 thẻ hiển thị chính xác các giá trị định dạng tiền VNĐ (`10.000.000 đ`, `8.000.000 đ`, `3.000.000 đ`).
  * **AC-09.2 (Vẽ biểu đồ tròn cơ cấu chi tiêu)**:
    * *Given*: Có dữ liệu các khoản chi thuộc danh mục Ăn uống, Mua sắm, Đi lại.
    * *Then*: Biểu đồ tròn hiển thị đầy đủ các lát cắt màu tương ứng; khi rê chuột (hover) hiển thị tooltip tên danh mục, số tiền và tỷ trọng phần trăm.

#### PBI-10: Báo cáo Thống kê Chuyên sâu & Xu hướng Thu/Chi 6 tháng
* **Phân hệ**: Báo cáo & Dashboard
* **GitHub Issue**: Resolves #7
* **Độ ưu tiên**: Should Have | **Điểm ước lượng**: 3 SP | **Sprint**: Sprint 3 | **Trạng thái**: Done
* **User Story**:
  > Là một người dùng, tôi muốn xem trang Báo cáo chuyên sâu với biểu đồ cột kép so sánh Thu nhập vs Chi tiêu qua 6 tháng liên tiếp, để tôi nhận diện xu hướng tài chính và kiểm soát dòng tiền dài hạn.
* **Đặc tả kỹ thuật**:
  * Controller: `ReportsController` (`Index`).
  * Giao diện: `Views/Reports/Index.cshtml`.
  * Tích hợp Chart.js Bar Chart dạng đối chiếu 2 cột (Cột xanh: Thu, Cột đỏ: Chi) theo từng tháng (`Tháng M/YYYY`).
* **Tiêu chí nghiệm thu (Acceptance Criteria)**:
  * **AC-10.1 (Hiển thị biểu đồ 6 tháng liên tiếp)**:
    * *Given*: Hệ thống có dữ liệu thu chi từ tháng 06/2026 đến tháng 11/2026.
    * *When*: Người dùng vào mục Báo cáo (`/Reports`).
    * *Then*: Trục hoành hiển thị đúng nhãn 6 tháng; mỗi tháng có 2 cột Thu/Chi với độ cao tương ứng số tiền thực tế.

---

### PHÂN HỆ 7: XÁC THỰC, BẢO MẬT & CÔ LẬP DỮ LIỆU (SECURITY & MULTI-TENANCY)

#### PBI-11: Xác thực ASP.NET Core Identity & Cơ chế Lockout chống Brute-force
* **Phân hệ**: Xác thực & Bảo mật (Identity)
* **GitHub Issue**: Resolves #10, #13, #14
* **Độ ưu tiên**: Must Have | **Điểm ước lượng**: 5 SP | **Sprint**: Sprint 3 | **Trạng thái**: Done
* **User Story**:
  > Là một người dùng, tôi muốn đăng ký và đăng nhập tài khoản an toàn với chuẩn ASP.NET Core Identity, được mã hóa mật khẩu và tự động khóa tài khoản tạm thời nếu nhập sai mật khẩu quá 5 lần liên tiếp.
* **Đặc tả kỹ thuật**:
  * Entity: `ApplicationUser : IdentityUser` (`FullName`, `CreatedAt`). Bảng CSDL: `"AspNetUsers"`.
  * Controller: `AccountController` (`Register`, `Login`, `Logout`, `AccessDenied`).
  * Cấu hình Cookie Authentication bảo mật, `LockoutMaxFailedAccessAttempts = 5`, `DefaultLockoutTimeSpan = 5 phút`.
  * Tắt `autocomplete="off"` trên các trường biểu mẫu xác thực (`0c3032d`).
* **Tiêu chí nghiệm thu (Acceptance Criteria)**:
  * **AC-11.1 (Đăng ký tài khoản hợp lệ)**:
    * *Given*: Người dùng nhập đầy đủ Họ tên, Email hợp lệ, Mật khẩu có độ dài tối thiểu 6 ký tự.
    * *When*: Bấm Đăng ký.
    * *Then*: Tài khoản được tạo thành công trong `"AspNetUsers"`, mật khẩu được băm (hash PBKDF2), tự động đăng nhập và gán Role `"User"`.
  * **AC-11.2 (Khóa tài khoản khi nhập sai quá 5 lần)**:
    * *Given*: Người dùng nhập sai mật khẩu 5 lần liên tiếp.
    * *When*: Thực hiện lần đăng nhập thứ 5 không chính xác.
    * *Then*: Hệ thống khóa tài khoản 5 phút, hiển thị cảnh báo đỏ chi tiết: *"Tài khoản đã bị tạm khóa do nhập sai mật khẩu nhiều lần. Vui lòng thử lại sau ít phút hoặc liên hệ Quản trị viên."*.

#### PBI-12: Cô lập Dữ liệu Người dùng Tuyệt đối (Multi-tenant Data Isolation)
* **Phân hệ**: Xác thực & Bảo mật (Identity)
* **GitHub Issue**: Resolves #15
* **Độ ưu tiên**: Must Have | **Điểm ước lượng**: 5 SP | **Sprint**: Sprint 3 | **Trạng thái**: Done
* **User Story**:
  > Là một người dùng, tôi muốn dữ liệu tài chính của mình (Ví, Giao dịch, Ngân sách, Mục tiêu) hoàn toàn độc lập và bảo mật, người dùng khác không thể xem hoặc chỉnh sửa dữ liệu của tôi qua việc thay đổi URL/ID (Chống lỗ hổng IDOR).
* **Đặc tả kỹ thuật**:
  * Mọi truy vấn `SELECT`, `UPDATE`, `DELETE` trong `WalletsController`, `TransactionsController`, `BudgetsController`, `SavingsGoalsController` đều gắn điều kiện bắt buộc: `.Where(x => x.UserId == CurrentUserId)`.
  * Trả về HTTP 404 NotFound nếu người dùng cố tình truy cập vào ID tài nguyên thuộc sở hữu của tài khoản khác.
* **Tiêu chí nghiệm thu (Acceptance Criteria)**:
  * **AC-12.1 (Cô lập dữ liệu truy vấn)**:
    * *Given*: Người dùng A đăng nhập và tạo 2 ví tiền. Người dùng B đăng nhập tài khoản khác.
    * *When*: Người dùng B vào trang `/Wallets`.
    * *Then*: Người dùng B chỉ nhìn thấy ví của mình, tuyệt đối không nhìn thấy 2 ví của người dùng A.
  * **AC-12.2 (Chống tấn công IDOR)**:
    * *Given*: Ví có `Id = 5` thuộc quyền sở hữu của Người dùng A.
    * *When*: Người dùng B gửi yêu cầu `POST /Wallets/Edit/5` hoặc `GET /Wallets/Delete/5`.
    * *Then*: Hệ thống phản hồi mã lỗi `404 Not Found` và từ chối xử lý.

---

### PHÂN HỆ 8: PHÂN HỆ QUẢN TRỊ VIÊN (ADMINISTRATION PANEL)

#### PBI-13: Trang Quản trị Hệ thống, Quản lý Tài khoản & Phân quyền
* **Phân hệ**: Quản trị Hệ thống (Admin Panel)
* **GitHub Issue**: Resolves #14, Update Security Fix (`a6d4fab`)
* **Độ ưu tiên**: High (Must Have) | **Điểm ước lượng**: 5 SP | **Sprint**: Sprint 3 | **Trạng thái**: Done
* **User Story**:
  > Là một Quản trị viên (Admin), tôi muốn truy cập bảng điều khiển Admin để xem thống kê toàn hệ thống, tìm kiếm/lọc danh sách người dùng, khóa/mở khóa tài khoản, cấp lại mật khẩu và xóa tài khoản vi phạm một cách an toàn.
* **Đặc tả kỹ thuật**:
  * Controller: `AdminController` có phân quyền chặt chẽ `[Authorize(Roles = "Admin")]`.
  * Giao diện: `Views/Admin/Index.cshtml`.
  * Các Action nghiệp vụ:
    * `Index(string search, string status)`: Tìm kiếm theo Tên/Email, lọc theo trạng thái Hoạt động / Bị khóa.
    * `ToggleLockout(string id)`: Khóa hoặc mở khóa tài khoản 1-click.
    * `ResetPassword(string id, string newPassword)`: Đổi mật khẩu mới an toàn qua `UserManager.RemovePasswordAsync` + `AddPasswordAsync`.
    * `DeleteUser(string id)`: Xóa tài khoản người dùng và dọn sạch dữ liệu liên quan.
    * **Cơ chế chống tự khóa (Self-Lockout Protection)**: Ngăn chặn tuyệt đối việc Admin tự khóa hoặc tự xóa tài khoản của chính mình.
* **Tiêu chí nghiệm thu (Acceptance Criteria)**:
  * **AC-13.1 (Phân quyền truy cập Admin)**:
    * *Given*: Người dùng thông thường (Role `"User"`) cố tình gõ URL `/Admin`.
    * *When*: Trình duyệt gửi yêu cầu.
    * *Then*: Hệ thống chặn và chuyển hướng sang trang `/Account/AccessDenied`.
  * **AC-13.2 (Khóa và mở khóa người dùng)**:
    * *Given*: Tài khoản người dùng đang ở trạng thái Hoạt động.
    * *When*: Admin nhấn nút *"Khóa tài khoản"*.
    * *Then*: Cột `LockoutEnd` trong CSDL được cập nhật về năm 2999; người dùng đó lập tức không thể đăng nhập vào hệ thống; Admin bấm lại nút đó thì tài khoản được mở khóa ngay lập tức.
  * **AC-13.3 (Bảo vệ tài khoản Admin đang đăng nhập)**:
    * *Given*: Admin đang đăng nhập bằng tài khoản `admin@gmail.com`.
    * *When*: Trên danh sách người dùng tại dòng tài khoản của chính mình.
    * *Then*: Các nút Khóa và Xóa bị vô hiệu hóa hoặc ẩn đi kèm huy hiệu *"Đang đăng nhập - Được bảo vệ"*; nếu cố tình gọi Action thì Backend chặn và trả về cảnh báo.

---

### PHÂN HỆ 9: TRẢI NGHIỆM GIAO DIỆN & KHỞI TẠO HỆ THỐNG (UI/UX & DEVOPS)

#### PBI-14: Tối ưu Hóa Giao diện Responsive Mobile & Bảng màu Pastel
* **Phân hệ**: Giao diện & Trải nghiệm người dùng (UI/UX)
* **GitHub Issue**: Resolves #9, #13
* **Độ ưu tiên**: Should Have | **Điểm ước lượng**: 3 SP | **Sprint**: Sprint 3 | **Trạng thái**: Done
* **User Story**:
  > Là một người dùng truy cập bằng điện thoại di động, tôi muốn giao diện tự động co giãn, các nút bấm, thanh điều hướng Navbar và bảng biểu hiển thị gọn gàng, không bị vỡ giao diện trên màn hình nhỏ.
* **Đặc tả kỹ thuật**:
  * Sử dụng hệ thống lưới Bootstrap 5 Grid System (`col-12 col-md-6 col-lg-4`), lớp tiện ích linh hoạt `d-flex flex-wrap`.
  * Bảng màu Pastel hiện đại (tone màu chủ đạo dịu mắt, độ tương phản văn bản đạt chuẩn WCAG AA).
* **Tiêu chí nghiệm thu (Acceptance Criteria)**:
  * **AC-14.1 (Responsive trên Mobile Viewport < 576px)**:
    * *Given*: Người dùng mở ứng dụng trên màn hình điện thoại (độ rộng 375px - 414px).
    * *Then*: Thanh Navbar thu gọn vào nút Hamburger Menu; các thẻ ví và biểu đồ tự động xếp chồng theo chiều dọc; không có thanh cuộn ngang vỡ khung hình.

#### PBI-15: Tự động Seeding Dữ liệu Mẫu & Tích hợp Cloud CI/CD
* **Phân hệ**: Triển khai & Khởi tạo (DevOps)
* **GitHub Issue**: Resolves #10
* **Độ ưu tiên**: Must Have | **Điểm ước lượng**: 3 SP | **Sprint**: Sprint 3 | **Trạng thái**: Done
* **User Story**:
  > Là một người chấm đồ án hoặc người dùng mới, tôi muốn hệ thống khi triển khai lần đầu trên Cloud tự động nạp sẵn dữ liệu danh mục chuẩn, tạo tài khoản Admin mặc định và đảm bảo các bảng CSDL tự sinh, để có thể sử dụng và trình diễn ngay lập tức.
* **Đặc tả kỹ thuật**:
  * File cấu hình: `Models/DbInitializer.cs`, `Program.cs`.
  * Tự động khởi tạo Role `"Admin"`, Role `"User"`.
  * Tự động tạo 9 danh mục thu/chi mặc định kèm mã màu và icon chuẩn.
  * Tự động tạo tài khoản quản trị mặc định: `admin@gmail.com` / `Admin@2026`.
  * Tự động tạo cấu trúc bảng `"Budgets"` và `"Goals"` nếu CSDL Supabase chưa có.
  * Cấu hình Docker multi-stage build tối ưu kích thước image và triển khai 24/7 trên Render Cloud.
* **Tiêu chí nghiệm thu (Acceptance Criteria)**:
  * **AC-15.1 (Khởi động hệ thống rỗng)**:
    * *Given*: Triển khai ứng dụng vào một cơ sở dữ liệu PostgreSQL Supabase hoàn toàn mới.
    * *When*: Ứng dụng khởi động lần đầu trên Render Cloud.
    * *Then*: `DbInitializer.SeedAsync()` chạy thành công; các bảng được tạo đầy đủ; tài khoản Admin và 9 danh mục được nạp tự động; hệ thống sẵn sàng hoạt động mà không cần can thiệp thủ công.

---


### PHÂN HỆ 9: MỞ RỘNG FINTECH & OPEN BANKING INTEGRATION (SPRINT 4)

#### PBI-16: Phân loại nguồn ví: Ví Tiền Mặt & Ví Ngân Hàng / Điện Tử liên kết
* **Phân hệ**: Quản lý Ví tiền & Ngân hàng số
* **GitHub Issue**: Resolves #19
* **Độ ưu tiên**: Must Have | **Điểm ước lượng**: 5 SP | **Sprint**: Sprint 4 | **Trạng thái**: Done
* **User Story**:
  > Là một người dùng quản lý tài chính, tôi muốn ứng dụng phân biệt rõ ràng giữa Ví Tiền Mặt ngoài đời (để ghi chép chi tiêu tiền giấy thủ công) và Ví Ngân Hàng / Ví Điện Tử (để theo dõi số dư tài khoản ngân hàng số và liên kết Open Banking), giúp việc quản lý dòng tiền không bị lẫn lộn giữa thế giới thực và số.
* **Đặc tả kỹ thuật**:
  * Mở rộng Entity `Wallet`: thêm trường `WalletType` (0 = Tiền mặt, 1 = Ngân hàng/Điện tử), `BankName`, `AccountNumber`.
  * Cập nhật Controller `WalletsController` (`Create`, `Edit`).
  * Giao diện: `Views/Wallets/Create.cshtml`, `Views/Wallets/Edit.cshtml`, `Views/Wallets/Index.cshtml` hiển thị Badge phân loại trực quan.

#### PBI-17: Hệ thống Mã PIN Bảo Mật (Security PIN) & Phân quyền công tắc ví điện tử
* **Phân hệ**: An toàn thông tin & Bảo mật FinTech
* **GitHub Issue**: Resolves #20
* **Độ ưu tiên**: Must Have | **Điểm ước lượng**: 5 SP | **Sprint**: Sprint 4 | **Trạng thái**: Done
* **User Story**:
  > Là một chủ tài khoản, tôi muốn bảo vệ các quyền nhạy cảm và thao tác trích tiền ngân hàng bằng Mã PIN Bảo Mật 6 số, đồng thời có thể chủ động cấp/hủy quyền trích tiết kiệm và theo dõi biến động số dư thông qua 2 công tắc gạt (Consent Toggles).
* **Đặc tả kỹ thuật**:
  * Mở rộng `ApplicationUser`: thêm trường `SecurityPinHash` băm bảo mật PBKDF2/Identity PasswordHasher.
  * Tích hợp 2 công tắc phân quyền trên dòng ví ngân hàng: `IsSavingsAllowed` (Cho phép trích tiết kiệm) và `IsSyncEnabled` (Tự động theo dõi biến động số dư).
  * Hỗ trợ quy trình Đổi Mã PIN 3 lớp an toàn: `[Mã PIN hiện tại]` + `[Mã PIN mới]` + `[Xác nhận PIN mới]`.

#### PBI-18: Tích hợp Open Banking Webhook (SePAY) và Bộ Giả Lập Giao Dịch Sandbox Simulator
* **Phân hệ**: Tích hợp Cổng Ngân Hàng Số
* **GitHub Issue**: Resolves #21
* **Độ ưu tiên**: Must Have | **Điểm ước lượng**: 5 SP | **Sprint**: Sprint 4 | **Trạng thái**: Done
* **User Story**:
  > Là một người dùng hiện đại và nhóm phát triển đồ án, chúng tôi muốn hệ thống có khả năng nhận biến động số dư ngân hàng tự động qua cổng Webhook Open Banking chuẩn RESTful, đồng thời có sẵn Bộ Giả Lập Sandbox Simulator trên giao diện để phục vụ việc kiểm thử và demo 2 chiều (Thu nhập & Chi tiêu) trước hội đồng đánh giá.
* **Đặc tả kỹ thuật**:
  * Endpoint API: `POST /api/banking/webhook` (và `/api/sepay/webhook`) nhận JSON chuẩn SePAY/Casso.
  * Tự động khớp số tài khoản, kiểm tra quyền `IsSyncEnabled`, cập nhật số dư ví và tạo giao dịch "Thu nhập từ ngân hàng" hoặc "Chi tiêu qua ngân hàng".
  * Giao diện: Modal "Giả lập giao dịch Ngân hàng (Sandbox Simulator)" trên `Views/Wallets/Index.cshtml`.
  * Endpoint kiểm tra trạng thái cổng: `GET /api/banking/test`.

#### PBI-19: Khóa phân luồng nguồn ví cho Mục Tiêu Tiết Kiệm (Strict Wallet Type Lock & PIN Authentication)
* **Phân hệ**: Quản lý Mục Tiêu Tiết Kiệm
* **GitHub Issue**: Resolves #22
* **Độ ưu tiên**: Should Have | **Điểm ước lượng**: 3 SP | **Sprint**: Sprint 4 | **Trạng thái**: Done
* **User Story**:
  > Là một người dùng tiết kiệm, tôi muốn khi đặt mục tiêu tiết kiệm, dòng tiền được phân luồng nhất quán: Mục tiêu tiền mặt chỉ trích từ tiền mặt và có nhắc nhở cất tiền riêng ngoài đời; Mục tiêu ngân hàng chỉ trích từ ví ngân hàng và yêu cầu xác thực Mã PIN 6 số.
* **Đặc tả kỹ thuật**:
  * Mở rộng `SavingsGoal`: thêm trường `GoalType` (0 = Tiền mặt, 1 = Ngân hàng).
  * Filter dropdown nguồn ví trong `Views/SavingsGoals/Details.cshtml` theo đúng loại mục tiêu.
  * Trích ví tiền mặt: hiển thị alert nhắc nhở bỏ heo đất/phong bì riêng.
  * Trích ví ngân hàng: bắt buộc nhập mã PIN 6 số xác thực trước khi trừ tiền.

#### PBI-20: Real-time Live Sync & Web Audio Chime (Zero-Reload Banking Notification)
* **Phân hệ**: Tích hợp Ngân hàng & Trải nghiệm Người dùng (UX)
* **GitHub Issue**: Resolves #23
* **Độ ưu tiên**: Should Have | **Điểm ước lượng**: 3 SP | **Sprint**: Sprint 4 | **Trạng thái**: Done
* **User Story**:
  > Là một người dùng hiện đại, tôi muốn khi có biến động số dư ngân hàng nổ từ SePAY hoặc Sandbox Simulator, màn hình web tự động nhảy số dư ví và chèn dòng giao dịch mới ngay lập tức mà không cần bấm F5, đồng thời hiển thị thông báo Toast và chuông báo nhẹ nhàng.
* **Đặc tả kỹ thuật**:
  * Endpoint API: `GET /api/banking/live-sync?lastTxId={id}` truy vấn biến động theo thời gian thực.
  * Tự động cập nhật số dư ví trên `Views/Wallets/Index.cshtml` kèm hiệu ứng `live-pulse-green` / `live-pulse-red`.
  * Tự động trượt dòng giao dịch mới vào đầu bảng `Views/Transactions/Index.cshtml` kèm cập nhật 3 thẻ tổng thu, chi, ròng.
  * Âm thanh chuông báo hai cung (D5 - A5) phát bằng Web Audio API không phụ thuộc file âm thanh ngoài.

---
## 4. MA TRẬN TRUY XUẤT YÊU CẦU (TRACEABILITY MATRIX)

Ma trận đối chiếu giữa các User Story trong Product Backlog và 15 Issues đã đóng trên GitHub Repository:

| Mã PBI | Tên Hạng mục Nghiệp vụ | GitHub Issue | Nhánh Git / Pull Request | Trạng thái Nghiệm thu |
| :---: | :--- | :---: | :--- | :---: |
| **PBI-01** | Tạo & Quản lý Đa Ví tiền | Issue #1, #2 | `sprint-1-wallets` (PR #12) | **Đã nghiệm thu (Done)** |
| **PBI-02** | Sửa ví & Khóa sửa số dư trực tiếp | Issue #3 | Commit `5ff112b` (main) | **Đã nghiệm thu (Done)** |
| **PBI-03** | Xóa ví an toàn với ràng buộc 2 lớp | Issue #3 | Commit `eedf1d8` (main) | **Đã nghiệm thu (Done)** |
| **PBI-04** | Phân loại Danh mục Thu / Chi | Issue #4 | `feature/issue-4-categories` (PR #12) | **Đã nghiệm thu (Done)** |
| **PBI-05** | Ghi chép Thu/Chi & Tự tính số dư ví | Issue #5, #6 | `sprint-2-core-transactions` (PR #16) | **Đã nghiệm thu (Done)** |
| **PBI-06** | Lọc giao dịch & Hoàn tiền chuẩn kế toán | Issue #5, #6 | `sprint-2-core-transactions` (PR #16) | **Đã nghiệm thu (Done)** |
| **PBI-07** | Ngân sách hàng tháng & Cảnh báo 80% | Issue #8 | `feature/issue-8-budgets` (Merged) | **Đã nghiệm thu (Done)** |
| **PBI-08** | Mục tiêu tiết kiệm & Liên kết nạp/rút ví | Issue #11 | Commit `d61a6fd`, `ce417f8` | **Đã nghiệm thu (Done)** |
| **PBI-09** | Dashboard 3 thẻ tài chính & Chart.js | Issue #7 | `feature/frontend-revamp` (PR #17) | **Đã nghiệm thu (Done)** |
| **PBI-10** | Báo cáo chuyên sâu 6 tháng Thu vs Chi | Issue #7 | `feature/frontend-revamp` (PR #17) | **Đã nghiệm thu (Done)** |
| **PBI-11** | ASP.NET Core Identity & Lockout | Issue #14, #13 | Commit `2972c0b` (main) | **Đã nghiệm thu (Done)** |
| **PBI-12** | Cô lập dữ liệu người dùng (Multi-tenancy) | Issue #15 | Commit `2972c0b` (main) | **Đã nghiệm thu (Done)** |
| **PBI-13** | Phân hệ Admin Panel & Chống tự khóa | Issue #14 | Commit `a6d4fab` (main) | **Đã nghiệm thu (Done)** |
| **PBI-14** | Tối ưu Responsive Mobile & UI Pastel | Issue #9 | `feature/frontend-revamp` (PR #17) | **Đã nghiệm thu (Done)** |
| **PBI-15** | Seeding Demo Data & Render Docker Cloud | Issue #10 | Commit `ea85168`, `360e666` | **Đã nghiệm thu (Done)** |
| **PBI-16** | Phân loại Ví kép (Tiền mặt vs Ngân hàng) | Issue #19 | Commit `883066b` (main) | **Đã nghiệm thu (Done)** |
| **PBI-17** | Hệ thống Mã PIN 6 số & Công tắc phân quyền | Issue #20 | Commit `883066b`, `bdf1099` | **Đã nghiệm thu (Done)** |
| **PBI-18** | Open Banking Webhook & Sandbox Simulator | Issue #21 | Commit `883066b`, `f25a621` | **Đã nghiệm thu (Done)** |
| **PBI-19** | Phân luồng Mục tiêu tiết kiệm & Xác thực PIN | Issue #22 | Commit `883066b` (main) | **Đã nghiệm thu (Done)** |
| **PBI-20** | Real-time Live Sync & Web Audio Chime | Issue #23 | Commit `main` | **Đã nghiệm thu (Done)** |

---
*Tài liệu Product Backlog này được lập theo chuẩn kỹ thuật phần mềm Scrum/Agile, phản ánh 100% hiện trạng kiến trúc và mã nguồn thực tế của đồ án CashFlow, không chứa các thành phần giả lập.*
