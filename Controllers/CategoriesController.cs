using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalFinanceApp.Models;

namespace PersonalFinanceApp.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Categories
        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .OrderBy(c => c.Type)
                .ThenBy(c => c.Name)
                .Select(c => new CategoryListItem
                {
                    Id = c.Id,
                    Name = c.Name,
                    Type = c.Type,
                    Icon = c.Icon,
                    Color = c.Color,
                    TransactionCount = c.Transactions == null ? 0 : c.Transactions.Count
                })
                .ToListAsync();

            return View(categories);
        }

        // GET: Categories/Create
        public IActionResult Create()
        {
            return View(new Category
            {
                Type = 0,
                Icon = "fa-tag",
                Color = "#dc3545"
            });
        }

        // POST: Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            NormalizeCategory(category);
            await ValidateCategoryAsync(category);

            if (!ModelState.IsValid)
                return View(category);

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã thêm danh mục thành công.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Categories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return NotFound();

            return View(category);
        }

        // POST: Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category category)
        {
            if (id != category.Id)
                return NotFound();

            if (!await _context.Categories.AnyAsync(c => c.Id == id))
                return NotFound();

            NormalizeCategory(category);
            await ValidateCategoryAsync(category, id);

            if (!ModelState.IsValid)
                return View(category);

            try
            {
                _context.Update(category);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã cập nhật danh mục thành công.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Categories.AnyAsync(c => c.Id == id))
                    return NotFound();

                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Categories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var category = await _context.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
                return NotFound();

            ViewBag.HasTransactions = await _context.Transactions.AnyAsync(t => t.CategoryId == id);
            return View(category);
        }

        // POST: Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return NotFound();

            var isInUse = await _context.Transactions.AnyAsync(t => t.CategoryId == id);
            if (isInUse)
            {
                TempData["ErrorMessage"] = "Không thể xóa danh mục đã được sử dụng trong giao dịch. Hãy sửa giao dịch liên quan trước.";
                return RedirectToAction(nameof(Index));
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã xóa danh mục thành công.";
            return RedirectToAction(nameof(Index));
        }

        private static void NormalizeCategory(Category category)
        {
            category.Name = (category.Name ?? string.Empty).Trim();
            category.Icon = string.IsNullOrWhiteSpace(category.Icon) ? "fa-tag" : category.Icon.Trim();
            category.Color = category.Type == 1 ? "#198754" : "#dc3545";
        }

        private async Task ValidateCategoryAsync(Category category, int? excludeId = null)
        {
            if (category.Type != 0 && category.Type != 1)
                ModelState.AddModelError(nameof(category.Type), "Vui lòng chọn loại danh mục hợp lệ.");

            if (!string.IsNullOrWhiteSpace(category.Name))
            {
                var duplicate = await _context.Categories.AnyAsync(c =>
                    c.Id != (excludeId ?? 0) &&
                    c.Type == category.Type &&
                    c.Name.ToLower() == category.Name.ToLower());

                if (duplicate)
                    ModelState.AddModelError(nameof(category.Name), "Danh mục cùng loại với tên này đã tồn tại.");
            }
        }

        public sealed class CategoryListItem
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public int Type { get; set; }
            public string Icon { get; set; } = "fa-tag";
            public string Color { get; set; } = "#dc3545";
            public int TransactionCount { get; set; }
        }
    }
}
