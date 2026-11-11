using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalFinanceApp.Models;

namespace PersonalFinanceApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new HomeDashboardViewModel();

            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var now = DateTime.UtcNow;
                var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

                var wallets = await _context.Wallets
                    .Where(w => w.UserId == userId)
                    .AsNoTracking()
                    .ToListAsync();

                var monthlyTransactions = await _context.Transactions
                    .Include(t => t.Category)
                    .Where(t => t.UserId == userId && t.TransactionDate >= startOfMonth)
                    .AsNoTracking()
                    .ToListAsync();

                var recentTransactions = await _context.Transactions
                    .Include(t => t.Category)
                    .Include(t => t.Wallet)
                    .Where(t => t.UserId == userId)
                    .OrderByDescending(t => t.TransactionDate)
                    .ThenByDescending(t => t.Id)
                    .Take(5)
                    .AsNoTracking()
                    .ToListAsync();

                viewModel.TotalBalance = wallets.Sum(w => w.InitialBalance);
                viewModel.TotalWallets = wallets.Count;
                viewModel.TotalTransactions = await _context.Transactions.CountAsync(t => t.UserId == userId);
                // Chỉ tính các khoản Thu / Chi thực tế, loại trừ dòng tiền chuyển dịch nội bộ giữa Ví và Mục tiêu tiết kiệm
                viewModel.MonthlyIncome = monthlyTransactions
                    .Where(t => t.Category?.Type == 1 && t.Category?.Name != "Thu hồi quỹ tiết kiệm")
                    .Sum(t => t.Amount);

                viewModel.MonthlyExpense = monthlyTransactions
                    .Where(t => t.Category?.Type == 0 && t.Category?.Name != "Tiết kiệm & Tích lũy")
                    .Sum(t => t.Amount);
                viewModel.RecentTransactions = recentTransactions;
            }

            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
