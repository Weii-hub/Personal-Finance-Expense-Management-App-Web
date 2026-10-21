using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PersonalFinanceApp.Models;

namespace PersonalFinanceApp.Controllers
{
    public class TransactionsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TransactionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Transactions
        public async Task<IActionResult> Index(int? walletId, int? categoryId, int? type, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.Transactions
                .Include(t => t.Wallet)
                .Include(t => t.Category)
                .AsQueryable();

            if (walletId.HasValue && walletId.Value > 0)
                query = query.Where(t => t.WalletId == walletId.Value);

            if (categoryId.HasValue && categoryId.Value > 0)
                query = query.Where(t => t.CategoryId == categoryId.Value);

            if (type.HasValue)
                query = query.Where(t => t.Category != null && t.Category.Type == type.Value);

            if (fromDate.HasValue)
            {
                var fromUtc = fromDate.Value.Date;
                query = query.Where(t => t.TransactionDate >= fromUtc);
            }

            if (toDate.HasValue)
            {
                var toUtc = toDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(t => t.TransactionDate <= toUtc);
            }

            var transactions = await query
                .OrderByDescending(t => t.TransactionDate)
                .ThenByDescending(t => t.Id)
                .ToListAsync();

            // Tính toán tổng số liệu thống kê
            decimal totalIncome = transactions
                .Where(t => t.Category?.Type == 1)
                .Sum(t => t.Amount);

            decimal totalExpense = transactions
                .Where(t => t.Category?.Type == 0)
                .Sum(t => t.Amount);

            ViewBag.TotalIncome = totalIncome;
            ViewBag.TotalExpense = totalExpense;
            ViewBag.NetBalance = totalIncome - totalExpense;

            // Dữ liệu cho bộ lọc
            ViewBag.Wallets = await _context.Wallets.OrderBy(w => w.Name).ToListAsync();
            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Type).ThenBy(c => c.Name).ToListAsync();
            ViewBag.SelectedWalletId = walletId;
            ViewBag.SelectedCategoryId = categoryId;
            ViewBag.SelectedType = type;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            return View(transactions);
        }

        // GET: Transactions/Create
        public async Task<IActionResult> Create(int? walletId)
        {
            await PopulateSelectListsAsync(walletId);
            var model = new Transaction
            {
                TransactionDate = DateTime.Now,
                WalletId = walletId ?? 0
            };
            return View(model);
        }

        // POST: Transactions/Create (Xử lý Issue #5 & Issue #6: Tự động cộng/trừ số dư ví)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Transaction transaction)
        {
            var wallet = await _context.Wallets.FindAsync(transaction.WalletId);
            var category = await _context.Categories.FindAsync(transaction.CategoryId);

            if (wallet == null)
                ModelState.AddModelError("WalletId", "Ví tiền không tồn tại.");

            if (category == null)
                ModelState.AddModelError("CategoryId", "Danh mục không tồn tại.");

            if (!ModelState.IsValid)
            {
                await PopulateSelectListsAsync(transaction.WalletId, transaction.CategoryId);
                return View(transaction);
            }

            // Issue #6 Logic: Cập nhật số dư ví theo loại danh mục
            if (category!.Type == 1) // Thu nhập (Income)
            {
                wallet!.InitialBalance += transaction.Amount;
            }
            else // Chi tiêu (Expense)
            {
                wallet!.InitialBalance -= transaction.Amount;
            }

            // Đảm bảo TransactionDate được lưu chuẩn theo UTC/Local
            if (transaction.TransactionDate.Kind == DateTimeKind.Unspecified)
            {
                transaction.TransactionDate = DateTime.SpecifyKind(transaction.TransactionDate, DateTimeKind.Utc);
            }

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã ghi nhận giao dịch {(category.Type == 1 ? "thu" : "chi")} {transaction.Amount:N0} đ thành công!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Transactions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var transaction = await _context.Transactions.FindAsync(id);
            if (transaction == null) return NotFound();

            await PopulateSelectListsAsync(transaction.WalletId, transaction.CategoryId);
            return View(transaction);
        }

        // POST: Transactions/Edit/5 (Xử lý Issue #6: Hoàn số dư ví cũ và cập nhật ví mới)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Transaction transaction)
        {
            if (id != transaction.Id) return NotFound();

            var existingTransaction = await _context.Transactions
                .Include(t => t.Category)
                .Include(t => t.Wallet)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (existingTransaction == null) return NotFound();

            var newWallet = await _context.Wallets.FindAsync(transaction.WalletId);
            var newCategory = await _context.Categories.FindAsync(transaction.CategoryId);

            if (newWallet == null)
                ModelState.AddModelError("WalletId", "Ví tiền không tồn tại.");
            if (newCategory == null)
                ModelState.AddModelError("CategoryId", "Danh mục không tồn tại.");

            if (!ModelState.IsValid)
            {
                await PopulateSelectListsAsync(transaction.WalletId, transaction.CategoryId);
                return View(transaction);
            }

            // 1. Hoàn lại số dư ví cũ
            var oldWallet = existingTransaction.Wallet;
            var oldCategory = existingTransaction.Category;

            if (oldWallet != null && oldCategory != null)
            {
                if (oldCategory.Type == 1) // Hoàn lại khoản thu: trừ lại
                    oldWallet.InitialBalance -= existingTransaction.Amount;
                else // Hoàn lại khoản chi: cộng lại
                    oldWallet.InitialBalance += existingTransaction.Amount;
            }

            // 2. Áp dụng số tiền mới vào ví mới
            if (newCategory!.Type == 1)
                newWallet!.InitialBalance += transaction.Amount;
            else
                newWallet!.InitialBalance -= transaction.Amount;

            // 3. Cập nhật các trường dữ liệu
            existingTransaction.Amount = transaction.Amount;
            existingTransaction.TransactionDate = transaction.TransactionDate.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(transaction.TransactionDate, DateTimeKind.Utc)
                : transaction.TransactionDate;
            existingTransaction.Note = transaction.Note;
            existingTransaction.WalletId = transaction.WalletId;
            existingTransaction.CategoryId = transaction.CategoryId;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cập nhật giao dịch và số dư ví thành công!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Transactions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var transaction = await _context.Transactions
                .Include(t => t.Wallet)
                .Include(t => t.Category)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transaction == null) return NotFound();

            return View(transaction);
        }

        // POST: Transactions/Delete/5 (Xử lý Issue #6: Hoàn trả số dư ví khi xóa)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var transaction = await _context.Transactions
                .Include(t => t.Wallet)
                .Include(t => t.Category)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transaction != null)
            {
                // Hoàn lại số dư ví trước khi xóa giao dịch
                if (transaction.Wallet != null && transaction.Category != null)
                {
                    if (transaction.Category.Type == 1)
                        transaction.Wallet.InitialBalance -= transaction.Amount; // Xóa khoản thu -> trừ bớt
                    else
                        transaction.Wallet.InitialBalance += transaction.Amount; // Xóa khoản chi -> hoàn tiền
                }

                _context.Transactions.Remove(transaction);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa giao dịch và hoàn lại số dư ví!";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateSelectListsAsync(int? selectedWalletId = null, int? selectedCategoryId = null)
        {
            var wallets = await _context.Wallets
                .OrderBy(w => w.Name)
                .Select(w => new { w.Id, Display = $"{w.Name} (Số dư: {w.InitialBalance:N0} đ)" })
                .ToListAsync();

            var categories = await _context.Categories
                .OrderBy(c => c.Type)
                .ThenBy(c => c.Name)
                .Select(c => new { c.Id, Display = $"{(c.Type == 1 ? "➕ [Thu]" : "➖ [Chi]")} {c.Name}" })
                .ToListAsync();

            ViewBag.WalletId = new SelectList(wallets, "Id", "Display", selectedWalletId);
            ViewBag.CategoryId = new SelectList(categories, "Id", "Display", selectedCategoryId);
        }
    }
}
