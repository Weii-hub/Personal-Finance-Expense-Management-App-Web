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
                wallet.Name = (wallet.Name ?? string.Empty).Trim();
                wallet.Icon = string.IsNullOrWhiteSpace(wallet.Icon) ? "fa-wallet" : wallet.Icon.Trim();

                _context.Add(wallet);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã tạo ví '{wallet.Name}' thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(wallet);
        }

        // GET: Wallets/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var userId = GetCurrentUserId();
            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);
            if (wallet == null) return NotFound();

            return View(wallet);
        }

        // POST: Wallets/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,InitialBalance,Icon")] Wallet wallet)
        {
            if (id != wallet.Id) return NotFound();

            var userId = GetCurrentUserId();
            var existingWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);
            if (existingWallet == null) return NotFound();

            if (ModelState.IsValid)
            {
                existingWallet.Name = (wallet.Name ?? string.Empty).Trim();
                existingWallet.Icon = string.IsNullOrWhiteSpace(wallet.Icon) ? "fa-wallet" : wallet.Icon.Trim();
                existingWallet.InitialBalance = wallet.InitialBalance;

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã cập nhật thông tin ví '{existingWallet.Name}' thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(wallet);
        }

        // GET: Wallets/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var userId = GetCurrentUserId();
            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);
            if (wallet == null) return NotFound();

            var totalWallets = await _context.Wallets.CountAsync(w => w.UserId == userId);
            var transactionCount = await _context.Transactions.CountAsync(t => t.WalletId == id && t.UserId == userId);

            ViewBag.TotalWallets = totalWallets;
            ViewBag.TransactionCount = transactionCount;
            ViewBag.CanDelete = totalWallets > 1 && transactionCount == 0;

            if (totalWallets <= 1)
            {
                ViewBag.BlockReason = "Đây là chiếc ví duy nhất của bạn. Hệ thống yêu cầu phải duy trì tối thiểu 1 ví để quản lý dòng tiền.";
            }
            else if (transactionCount > 0)
            {
                ViewBag.BlockReason = $"Ví này hiện đang liên kết với {transactionCount} giao dịch thu/chi. Để bảo toàn lịch sử sổ sách tài chính, hệ thống không cho phép xóa ví này.";
            }

            return View(wallet);
        }

        // POST: Wallets/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = GetCurrentUserId();
            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);
            if (wallet == null) return NotFound();

            var totalWallets = await _context.Wallets.CountAsync(w => w.UserId == userId);
            var transactionCount = await _context.Transactions.CountAsync(t => t.WalletId == id && t.UserId == userId);

            // Kiểm tra 2 điều kiện bảo vệ dữ liệu
            if (totalWallets <= 1)
            {
                TempData["ErrorMessage"] = "Không thể xóa chiếc ví duy nhất của bạn! Hệ thống bắt buộc phải có ít nhất 1 ví.";
                return RedirectToAction(nameof(Index));
            }

            if (transactionCount > 0)
            {
                TempData["ErrorMessage"] = $"Không thể xóa ví '{wallet.Name}' vì đang có {transactionCount} giao dịch liên quan. Hãy xóa hoặc chuyển các giao dịch trước!";
                return RedirectToAction(nameof(Index));
            }

            _context.Wallets.Remove(wallet);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã xóa ví '{wallet.Name}' thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}
