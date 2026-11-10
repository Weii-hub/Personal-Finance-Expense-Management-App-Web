# Issue #8 - Budgets & cảnh báo chi tiêu

## Phạm vi

Overlay này bổ sung chức năng ngân sách theo danh mục chi tiêu, tháng và năm. Mỗi tài khoản chỉ xem/sửa/xóa ngân sách của chính mình. Tổng chi được tính từ giao dịch của chính người dùng, đúng danh mục và trong tháng đang chọn; chỉ lấy danh mục có `Category.Type = 0` (chi tiêu).

Cảnh báo màu đỏ xuất hiện khi `SpentAmount > Amount * 0.8`. Đúng 80% chưa bị cảnh báo; vượt quá 80% mới cảnh báo theo yêu cầu Issue #8.

## Các file mới

- `Models/Budget.cs`
- `Controllers/BudgetsController.cs`
- `Views/Budgets/Index.cshtml`
- `Views/Budgets/Create.cshtml`
- `Views/Budgets/Edit.cshtml`
- `Database/Issue8_CreateBudgetsTable.sql`

Không sửa `Program.cs`, `ApplicationDbContext.cs`, `Transaction.cs`, `CategoriesController.cs` hoặc `_Layout.cshtml`. Controller dùng Npgsql trực tiếp theo cách triển khai hiện có của `SavingsGoalsController`, do đó không cần đăng ký DI hay tạo migration EF Core.

## Cách tích hợp

1. Giải nén overlay vào thư mục gốc repo, giữ nguyên cấu trúc thư mục. Chỉ chép các file mới; không ghi đè file hiện có.
2. Mở PostgreSQL database mà ứng dụng đang dùng.
3. Chạy toàn bộ script `Database/Issue8_CreateBudgetsTable.sql`.
4. Chạy ứng dụng và đăng nhập bằng tài khoản người dùng.
5. Truy cập trực tiếp `/Budgets` để quản lý ngân sách. Navbar chưa được chỉnh sửa để tránh thay đổi file ngoài phạm vi tối thiểu.

## Quy tắc dữ liệu

- Một ngân sách cho mỗi người dùng + danh mục + tháng + năm.
- Nếu tạo lại cùng tổ hợp, ngân sách được cập nhật số tiền nhờ `ON CONFLICT`.
- Giao dịch được lọc theo `UserId`, `CategoryId` và khoảng thời gian tháng `[đầu tháng, đầu tháng kế tiếp)`.
- Các truy vấn ghi/xóa đều giới hạn theo `UserId` lấy từ claim đăng nhập; không nhận UserId từ form.

## Kiểm thử đề xuất

1. Tạo ngân sách cho một danh mục chi tiêu; kiểm tra dữ liệu được thêm vào `Budgets`.
2. Tạo giao dịch chi tiêu cùng danh mục trong tháng: tổng chi nhỏ hơn 80% thì không có cảnh báo.
3. Kiểm tra đúng 80%: không có cảnh báo.
4. Tăng tổng chi vượt 80%: thẻ ngân sách hiển thị cảnh báo màu đỏ.
5. Giao dịch thuộc danh mục thu nhập hoặc tháng khác không được cộng vào tổng chi.
6. Đăng nhập tài khoản khác: không nhìn thấy/sửa/xóa được ngân sách của tài khoản trước.
7. Thử tạo hai ngân sách cùng danh mục/tháng/năm: bản ghi được cập nhật thay vì tạo trùng.

## Lưu ý trước khi chạy

Code được viết dựa trên cấu trúc `main` đã kiểm tra: các bảng PostgreSQL có tên `Transactions`, `Categories`, `AspNetUsers`; cột giao dịch gồm `TransactionDate`, `CategoryId`, `UserId`, `Amount`; cột danh mục gồm `Type`, `Name`. `Category.Type = 0` là chi tiêu. `TransactionDate` được truy vấn bằng mốc UTC để phù hợp kiểu timestamp with time zone mặc định của Npgsql.

Gói này là overlay source, chưa được build hoặc chạy integration test trên database của bạn. Không có file hiện hữu nào bị thay thế.
