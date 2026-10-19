# Issue #4 — Quản lý danh mục

## Những gì được thêm
- CRUD danh mục: danh sách, thêm, sửa và xóa.
- Phân loại riêng Thu nhập (`Type = 1`, xanh lá) và Chi tiêu (`Type = 0`, đỏ).
- Kiểm tra tên danh mục trùng trong cùng một loại.
- Ngăn xóa danh mục đang được giao dịch sử dụng để tránh làm mất dữ liệu liên quan.
- Thống kê số giao dịch đang tham chiếu từng danh mục.
- Giao diện responsive theo Bootstrap, thông báo thành công/lỗi và xác thực form.

## Cách áp dụng
1. Chép `Controllers/CategoriesController.cs` vào thư mục `Controllers`.
2. Chép thư mục `Views/Categories` vào `Views`.
3. Chép `wwwroot/css/categories.css` vào `wwwroot/css`.
4. Trong `Views/Shared/_Layout.cshtml`, thêm dòng sau sau stylesheet `site.css`:
   `<link rel="stylesheet" href="~/css/categories.css" asp-append-version="true" />`
5. Thêm liên kết điều hướng trong danh sách navbar:
   `<li class="nav-item"><a class="nav-link text-dark" asp-controller="Categories" asp-action="Index">🏷️ Danh mục</a></li>`
6. Build và chạy ứng dụng. Không cần migration mới vì model `Category` và bảng `Categories` đã tồn tại trong migration hiện tại.

## Kiểm thử thủ công
- Thêm danh mục Chi tiêu và Thu nhập.
- Thử thêm hai danh mục cùng tên và cùng loại: phải hiện validation lỗi.
- Sửa tên/loại danh mục và xác nhận danh sách đổi đúng nhóm/màu.
- Xóa danh mục chưa được dùng: thành công.
- Thử xóa danh mục đang có giao dịch: bị chặn và dữ liệu giao dịch được giữ nguyên.
