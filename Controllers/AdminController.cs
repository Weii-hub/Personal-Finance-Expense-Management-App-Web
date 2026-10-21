using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalFinanceApp.Models;

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

        // GET: /Admin
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.OrderByDescending(u => u.CreatedAt).ToListAsync();
            var totalWallets = await _context.Wallets.CountAsync();
            var totalTransactions = await _context.Transactions.CountAsync();
            var totalCategories = await _context.Categories.CountAsync();

            ViewBag.TotalUsers = users.Count;
            ViewBag.TotalWallets = totalWallets;
            ViewBag.TotalTransactions = totalTransactions;
            ViewBag.TotalCategories = totalCategories;

            return View(users);
        }

        // POST: /Admin/ToggleLockout/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLockout(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (await _userManager.IsLockedOutAsync(user))
            {
                // Mở khóa
                await _userManager.SetLockoutEndDateAsync(user, null);
                TempData["SuccessMessage"] = $"Đã mở khóa tài khoản {user.Email} thành công!";
            }
            else
            {
                // Khóa tài khoản 100 năm
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
                TempData["SuccessMessage"] = $"Đã khóa tài khoản {user.Email}!";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
