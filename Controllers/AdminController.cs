using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalFinanceApp.Models;
using PersonalFinanceApp.Models.ViewModels;

namespace PersonalFinanceApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public AdminController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // GET: /Admin (Tìm kiếm, Lọc & Thống kê thông số chi tiết User)
        public async Task<IActionResult> Index(string? search, string? status)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var usersQuery = _userManager.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var kw = search.Trim().ToLower();
                usersQuery = usersQuery.Where(u =>
                    (u.Email != null && u.Email.ToLower().Contains(kw)) ||
                    (u.FullName != null && u.FullName.ToLower().Contains(kw)));
            }

            var users = await usersQuery.OrderByDescending(u => u.CreatedAt).ToListAsync();

            // Lọc theo trạng thái Hoạt động / Đã bị khóa
            if (status == "active")
            {
                users = users.Where(u => !u.LockoutEnd.HasValue || u.LockoutEnd <= DateTimeOffset.UtcNow).ToList();
            }
            else if (status == "locked")
            {
                users = users.Where(u => u.LockoutEnd.HasValue && u.LockoutEnd > DateTimeOffset.UtcNow).ToList();
            }

            var totalWallets = await _context.Wallets.CountAsync();
            var totalTransactions = await _context.Transactions.CountAsync();
            var totalCategories = await _context.Categories.CountAsync();
            var totalUsersCount = await _userManager.Users.CountAsync();

            ViewBag.TotalUsers = totalUsersCount;
            ViewBag.TotalWallets = totalWallets;
            ViewBag.TotalTransactions = totalTransactions;
            ViewBag.TotalCategories = totalCategories;
            ViewBag.Search = search;
            ViewBag.Status = status;

            // Thống kê số lượng ví và giao dịch cho từng User
            var userIds = users.Select(u => u.Id).ToList();

            var walletCounts = await _context.Wallets
                .Where(w => w.UserId != null && userIds.Contains(w.UserId))
                .GroupBy(w => w.UserId!)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.UserId, x => x.Count);

            var transactionCounts = await _context.Transactions
                .Where(t => t.UserId != null && userIds.Contains(t.UserId))
                .GroupBy(t => t.UserId!)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.UserId, x => x.Count);

            var userViewModels = new List<AdminUserViewModel>();
            foreach (var user in users)
            {
                var isLocked = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow;
                var isCurrentAdmin = user.Id == currentUserId;
                var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

                userViewModels.Add(new AdminUserViewModel
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    FullName = user.FullName ?? string.Empty,
                    CreatedAt = user.CreatedAt,
                    IsLocked = isLocked,
                    IsCurrentAdmin = isCurrentAdmin,
                    IsAdmin = isAdmin,
                    WalletCount = walletCounts.GetValueOrDefault(user.Id, 0),
                    TransactionCount = transactionCounts.GetValueOrDefault(user.Id, 0)
                });
            }

            return View(userViewModels);
        }

        // POST: /Admin/ToggleLockout/5 (Khóa / Mở khóa an toàn)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLockout(string id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (id == currentUserId)
            {
                TempData["ErrorMessage"] = "🚫 Bạn không thể tự khóa tài khoản Admin đang đăng nhập!";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (await _userManager.IsLockedOutAsync(user))
            {
                // Mở khóa tài khoản
                await _userManager.SetLockoutEndDateAsync(user, null);
                await _userManager.UpdateSecurityStampAsync(user);
                TempData["SuccessMessage"] = $"Đã mở khóa tài khoản {user.Email} ({user.FullName}) thành công!";
            }
            else
            {
                // Khóa tài khoản
                await _userManager.SetLockoutEnabledAsync(user, true);
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
                // Cập nhật SecurityStamp để hủy session đăng nhập ngay lập tức
                await _userManager.UpdateSecurityStampAsync(user);
                TempData["SuccessMessage"] = $"Đã khóa tài khoản {user.Email} ({user.FullName})!";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/ResetPassword (Đặt lại mật khẩu cho User)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string id, string? newPassword)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var defaultPassword = string.IsNullOrWhiteSpace(newPassword) ? "Reset@2026" : newPassword.Trim();

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, defaultPassword);

            if (result.Succeeded)
            {
                await _userManager.UpdateSecurityStampAsync(user);
                TempData["SuccessMessage"] = $"🔑 Đã đặt lại mật khẩu cho {user.Email} thành: '{defaultPassword}'. Hãy gửi mật khẩu này cho người dùng.";
            }
            else
            {
                var errorMsg = string.Join("; ", result.Errors.Select(e => e.Description));
                TempData["ErrorMessage"] = $"Lỗi khi đặt lại mật khẩu: {errorMsg}";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/DeleteUser (Xóa tài khoản rác / Spam)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (id == currentUserId)
            {
                TempData["ErrorMessage"] = "🚫 Bạn không thể xóa tài khoản Admin đang đăng nhập!";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                TempData["ErrorMessage"] = "🚫 Không thể xóa tài khoản có vai trò Quản trị viên!";
                return RedirectToAction(nameof(Index));
            }

            // Xóa sạch các giao dịch và ví liên quan đến user để không bị lỗi ràng buộc dữ liệu
            var userTransactions = _context.Transactions.Where(t => t.UserId == id);
            _context.Transactions.RemoveRange(userTransactions);

            var userWallets = _context.Wallets.Where(w => w.UserId == id);
            _context.Wallets.RemoveRange(userWallets);

            await _context.SaveChangesAsync();

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"🗑️ Đã xóa vĩnh viễn tài khoản {user.Email} và toàn bộ dữ liệu kèm theo!";
            }
            else
            {
                TempData["ErrorMessage"] = "Lỗi khi xóa tài khoản người dùng.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
