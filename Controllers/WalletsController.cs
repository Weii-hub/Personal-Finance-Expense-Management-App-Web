using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalFinanceApp.Models;

namespace PersonalFinanceApp.Controllers
{
    [Authorize]
    public class WalletsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public WalletsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private string GetCurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // GET: Wallets (Chỉ lấy ví của chính người đang đăng nhập - Issue #15)
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            var myWallets = await _context.Wallets
                .Where(w => w.UserId == userId)
                .AsNoTracking()
                .ToListAsync();

            return View(myWallets);
        }

        // GET: Wallets/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Wallets/Create (Gắn UserId của người tạo vào ví mới - Issue #15)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,InitialBalance,Icon")] Wallet wallet)
        {
            wallet.UserId = GetCurrentUserId();

            if (ModelState.IsValid)
            {
                _context.Add(wallet);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã tạo ví {wallet.Name} thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(wallet);
        }
    }
}
