using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PersonalFinanceApp.Models;
using PersonalFinanceApp.Models.ViewModels;

namespace PersonalFinanceApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        // GET: /Account/Login
        public IActionResult Login(string? returnUrl = null, string? locked = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (locked == "1")
            {
                ViewBag.IsLockedOut = true;
                ViewBag.LockoutMessage = "🚫 TÀI KHOẢN CỦA BẠN ĐÃ BỊ KHÓA! Quản trị viên đã tạm thời khóa quyền truy cập của tài khoản này. Vui lòng liên hệ Admin để được hỗ trợ mở khóa.";
            }
            return View(new LoginViewModel());
        }

        // POST: /Account/Login (Xử lý Đăng nhập thật bằng Identity - Issue #14)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                // Kiểm tra trực tiếp xem tài khoản có đang bị khóa bởi Admin hay không
                if (await _userManager.IsLockedOutAsync(user))
                {
                    ViewBag.IsLockedOut = true;
                    ViewBag.LockoutMessage = $"🚫 TÀI KHOẢN ({user.Email}) ĐÃ BỊ KHÓA! Quản trị viên đã tạm thời khóa quyền truy cập. Vui lòng liên hệ Admin để được hỗ trợ mở khóa.";
                    ModelState.AddModelError(string.Empty, "🚫 Tài khoản của bạn hiện đang bị Quản trị viên khóa. Không thể đăng nhập.");
                    return View(model);
                }
            }

            var result = await _signInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: false);

            if (result.IsLockedOut)
            {
                ViewBag.IsLockedOut = true;
                ViewBag.LockoutMessage = "🚫 TÀI KHOẢN CỦA BẠN ĐÃ BỊ KHÓA! Quản trị viên đã tạm khóa quyền truy cập. Vui lòng liên hệ Admin để được hỗ trợ mở khóa.";
                ModelState.AddModelError(string.Empty, "🚫 Tài khoản của bạn hiện đang bị Quản trị viên khóa. Không thể đăng nhập.");
                return View(model);
            }

            if (result.Succeeded)
            {
                // Kiểm tra lại sau khi đăng nhập đề phòng trường hợp vừa bị khóa
                if (user != null && await _userManager.IsLockedOutAsync(user))
                {
                    await _signInManager.SignOutAsync();
                    ViewBag.IsLockedOut = true;
                    ViewBag.LockoutMessage = "🚫 TÀI KHOẢN CỦA BẠN ĐÃ BỊ KHÓA! Quản trị viên đã tạm khóa tài khoản này.";
                    ModelState.AddModelError(string.Empty, "🚫 Tài khoản của bạn hiện đang bị Quản trị viên khóa.");
                    return View(model);
                }

                if (user != null && await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    TempData["SuccessMessage"] = $"Xin chào Quản trị viên ({user.FullName})!";
                    return RedirectToAction("Index", "Admin");
                }

                TempData["SuccessMessage"] = $"Chào mừng {(user?.FullName ?? model.Email)} đã đăng nhập!";
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index", "Transactions");
            }

            ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không chính xác.");
            return View(model);
        }

        // GET: /Account/Register
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        // POST: /Account/Register (Xử lý Đăng ký thật & Tặng ví mặc định riêng - Issue #14, #15)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Email này đã được sử dụng. Vui lòng chọn email khác.");
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "User");

                // Issue #15: Khởi tạo ngay 1 Ví Tiền Mặt cá nhân riêng cho tài khoản mới
                var initialWallet = new Wallet
                {
                    Name = "Ví Tiền Mặt",
                    InitialBalance = 0,
                    Icon = "fa-wallet",
                    UserId = user.Id
                };
                _context.Wallets.Add(initialWallet);
                await _context.SaveChangesAsync();

                // Tự động đăng nhập sau khi đăng ký
                await _signInManager.SignInAsync(user, isPersistent: false);
                TempData["SuccessMessage"] = $"Chúc mừng {user.FullName}! Tài khoản và Ví cá nhân của bạn đã sẵn sàng.";
                return RedirectToAction("Index", "Transactions");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            TempData["SuccessMessage"] = "Bạn đã đăng xuất an toàn khỏi hệ thống.";
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/AccessDenied
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
