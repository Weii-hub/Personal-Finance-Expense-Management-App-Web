using System.Diagnostics;
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
            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var wallets = await _context.Wallets.AsNoTracking().ToListAsync();
            var totalBalance = wallets.Sum(w => w.InitialBalance);

            var monthlyTransactions = await _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.TransactionDate >= startOfMonth)
                .AsNoTracking()
                .ToListAsync();

            var monthlyIncome = monthlyTransactions
                .Where(t => t.Category?.Type == 1)
                .Sum(t => t.Amount);

            var monthlyExpense = monthlyTransactions
                .Where(t => t.Category?.Type == 0)
                .Sum(t => t.Amount);

            var recentTransactions = await _context.Transactions
                .Include(t => t.Category)
                .Include(t => t.Wallet)
                .OrderByDescending(t => t.TransactionDate)
                .ThenByDescending(t => t.Id)
                .Take(5)
                .AsNoTracking()
                .ToListAsync();

            var totalTransactionsCount = await _context.Transactions.CountAsync();

            var viewModel = new HomeDashboardViewModel
            {
                TotalBalance = totalBalance,
                TotalWallets = wallets.Count,
                TotalTransactions = totalTransactionsCount,
                MonthlyIncome = monthlyIncome,
                MonthlyExpense = monthlyExpense,
                RecentTransactions = recentTransactions
            };

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
