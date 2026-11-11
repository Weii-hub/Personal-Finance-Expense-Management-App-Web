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

        // GET: /Account/ForgotPassword (Issue #24)
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        // POST: /Account/ForgotPassword (Tạo mã Token đặt lại mật khẩu chuẩn Identity Cryptography)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Theo chuẩn bảo mật OWASP: Không tiết lộ email có tồn tại hay không, nhưng hiển thị xác nhận đã xử lý
                ViewBag.UserFound = false;
                ViewBag.TargetEmail = model.Email;
                return View("ForgotPasswordConfirmation");
            }

            // Sinh mã Token đặt lại mật khẩu mã hóa mật mã học (Identity Token Provider)
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            // Tạo link đặt lại mật khẩu hoàn chỉnh
            var resetLink = Url.Action("ResetPassword", "Account", new { token, email = user.Email }, Request.Scheme);

            ViewBag.UserFound = true;
            ViewBag.TargetEmail = user.Email;
            ViewBag.FullName = user.FullName;
            ViewBag.ResetLink = resetLink;
            ViewBag.Token = token;

            return View("ForgotPasswordConfirmation");
        }

        // GET: /Account/ResetPassword
        public IActionResult ResetPassword(string? token = null, string? email = null)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Yêu cầu đặt lại mật khẩu không hợp lệ hoặc thiếu mã bảo mật.";
                return RedirectToAction("Login");
            }

            var model = new ResetPasswordViewModel
            {
                Token = token,
                Email = email
            };
            return View(model);
        }

        // POST: /Account/ResetPassword (Xác nhận đổi mật khẩu mới)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy tài khoản người dùng tương ứng.";
                return RedirectToAction("Login");
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "🎉 Đặt lại mật khẩu thành công! Bạn có thể đăng nhập bằng mật khẩu mới ngay bây giờ.";
                return RedirectToAction("Login");
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

        // =========================================================================
        // RESTful API Endpoints cho Quên Mật Khẩu & Đặt Lại Mật Khẩu (Dành cho Mobile / Postman)
        // =========================================================================

        // POST: /api/account/forgot-password
        [HttpPost("/api/account/forgot-password")]
        public async Task<IActionResult> ApiForgotPassword([FromBody] ForgotPasswordApiRequest? request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new { success = false, message = "Vui lòng cung cấp địa chỉ email hợp lệ." });
            }

            var user = await _userManager.FindByEmailAsync(request.Email.Trim());
            if (user == null)
            {
                return Ok(new
                {
                    success = true,
                    message = "Nếu email tồn tại trong hệ thống, hướng dẫn đặt lại mật khẩu đã được xử lý (Bảo mật OWASP)."
                });
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetUrl = Url.Action("ResetPassword", "Account", new { token, email = user.Email }, Request.Scheme);

            return Ok(new
            {
                success = true,
                message = "Đã tạo mã xác thực đặt lại mật khẩu thành công.",
                email = user.Email,
                resetToken = token,
                resetUrl
            });
        }

        // POST: /api/account/reset-password
        [HttpPost("/api/account/reset-password")]
        public async Task<IActionResult> ApiResetPassword([FromBody] ResetPasswordApiRequest? request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return BadRequest(new { success = false, message = "Thiếu thông tin bắt buộc: email, token hoặc newPassword." });
            }

            if (request.NewPassword.Length < 6)
            {
                return BadRequest(new { success = false, message = "Mật khẩu mới phải có ít nhất 6 ký tự." });
            }

            var user = await _userManager.FindByEmailAsync(request.Email.Trim());
            if (user == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy người dùng." });
            }

            var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
            if (result.Succeeded)
            {
                return Ok(new
                {
                    success = true,
                    message = "Đặt lại mật khẩu thành công! Tài khoản đã được cập nhật mật khẩu mới."
                });
            }

            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return BadRequest(new { success = false, message = "Không thể đặt lại mật khẩu: " + errors });
        }
    }
}
