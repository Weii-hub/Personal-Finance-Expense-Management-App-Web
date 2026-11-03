SAVINGS GOALS - ISSUE #11
=========================

Gói này chỉ chứa các file cần thêm và file _Layout.cshtml đã thêm liên kết Savings Goals.
Không thay thế toàn bộ project.

CÁCH CÀI ĐẶT
------------
1. Sao lưu project hiện tại.
2. Giải nén ZIP này vào thư mục gốc của project (thư mục chứa PersonalFinanceApp.csproj).
3. Khi Windows hỏi có ghi đè Views/Shared/_Layout.cshtml không, chọn Replace.
4. Mở Supabase SQL Editor.
5. Mở file Database/Issue11_CreateGoalsTable.sql, copy toàn bộ nội dung và chạy một lần.
6. Mở solution bằng Visual Studio và chạy project.

CÁC FILE ĐƯỢC THÊM
------------------
- Models/SavingsGoal.cs
- Controllers/SavingsGoalsController.cs
- Views/SavingsGoals/Index.cshtml
- Views/SavingsGoals/Create.cshtml
- Views/SavingsGoals/Edit.cshtml
- Views/SavingsGoals/Details.cshtml
- Database/Issue11_CreateGoalsTable.sql

FILE ĐƯỢC CẬP NHẬT
------------------
- Views/Shared/_Layout.cshtml: chỉ thêm menu Savings Goals trong nhóm menu của người dùng đã đăng nhập.

CHỨC NĂNG
---------
- Tạo mục tiêu tiết kiệm.
- Xem danh sách và chi tiết mục tiêu.
- Chỉnh sửa và xóa mục tiêu.
- Thêm tiền vào quỹ.
- Tự tính phần trăm và cập nhật progress bar.
- Đánh dấu hoàn thành khi số tiền đã tiết kiệm đạt/vượt mục tiêu.
- Mỗi tài khoản chỉ xem và thao tác trên mục tiêu của chính mình.

LƯU Ý
-----
- Controller sử dụng ConnectionStrings:DefaultConnection và Npgsql đã có trong project.
- Không cần sửa Program.cs hoặc ApplicationDbContext.cs.
- Menu chỉ hiển thị khi người dùng đăng nhập.
- Đây là gói file bổ sung, không chứa toàn bộ project.
