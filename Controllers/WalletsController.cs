using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalFinanceApp.Models;

namespace PersonalFinanceApp.Controllers
{
    public class WalletsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public WalletsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Wallets
        public async Task<IActionResult> Index()
        {
            return View(await _context.Wallets.ToListAsync());
        }

        // GET: Wallets/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Wallets/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,InitialBalance,Icon")] Wallet wallet)
        {
            if (ModelState.IsValid)
            {
                _context.Add(wallet);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(wallet);
        }
    }
}
