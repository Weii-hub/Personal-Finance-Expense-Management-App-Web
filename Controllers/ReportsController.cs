using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalFinanceApp.Models;

namespace PersonalFinanceApp.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private string GetCurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // GET: /Reports (Phân quyền: Biểu đồ báo cáo riêng của từng User - Issue #15)
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();

            var transactions = await _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.UserId == userId)
                .AsNoTracking()
                .ToListAsync();

            // 1. Dữ liệu Biểu đồ Tròn: Tỷ lệ chi tiêu theo danh mục
            var expenseByCategory = transactions
                .Where(t => t.Category != null && t.Category.Type == 0) // Chỉ tính Chi tiêu
                .GroupBy(t => new { t.Category!.Name, t.Category.Color })
                .Select(g => new
                {
                    CategoryName = g.Key.Name,
                    Color = string.IsNullOrEmpty(g.Key.Color) ? "#ff6b81" : g.Key.Color,
                    TotalAmount = g.Sum(t => t.Amount)
                })
                .OrderByDescending(x => x.TotalAmount)
                .ToList();

            ViewBag.ExpenseCategories = expenseByCategory.Select(x => x.CategoryName).ToArray();
            ViewBag.ExpenseAmounts = expenseByCategory.Select(x => x.TotalAmount).ToArray();
            ViewBag.ExpenseColors = expenseByCategory.Select(x => x.Color).ToArray();

            // 2. Dữ liệu Biểu đồ Cột: So sánh Thu vs Chi theo 6 tháng gần nhất
            var now = DateTime.UtcNow;
            var sixMonthsAgo = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-5);

            var monthlyLabels = new List<string>();
            var monthlyIncomes = new List<decimal>();
            var monthlyExpenses = new List<decimal>();

            for (int i = 0; i < 6; i++)
            {
                var targetMonth = sixMonthsAgo.AddMonths(i);
                var label = $"Tháng {targetMonth.Month}/{targetMonth.Year}";
                monthlyLabels.Add(label);

                var inc = transactions
                    .Where(t => t.Category?.Type == 1 &&
                                t.TransactionDate.Year == targetMonth.Year &&
                                t.TransactionDate.Month == targetMonth.Month)
                    .Sum(t => t.Amount);

                var exp = transactions
                    .Where(t => t.Category?.Type == 0 &&
                                t.TransactionDate.Year == targetMonth.Year &&
                                t.TransactionDate.Month == targetMonth.Month)
                    .Sum(t => t.Amount);

                monthlyIncomes.Add(inc);
                monthlyExpenses.Add(exp);
            }

            ViewBag.MonthlyLabels = monthlyLabels.ToArray();
            ViewBag.MonthlyIncomes = monthlyIncomes.ToArray();
            ViewBag.MonthlyExpenses = monthlyExpenses.ToArray();

            // Thống kê chung
            ViewBag.TotalAllTimeIncome = transactions.Where(t => t.Category?.Type == 1).Sum(t => t.Amount);
            ViewBag.TotalAllTimeExpense = transactions.Where(t => t.Category?.Type == 0).Sum(t => t.Amount);
            ViewBag.TopExpenseCategory = expenseByCategory.FirstOrDefault()?.CategoryName ?? "Chưa có";

            return View();
        }
    }
}
