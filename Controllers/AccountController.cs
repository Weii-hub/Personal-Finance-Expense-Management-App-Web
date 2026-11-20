using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PersonalFinanceApp.Models;
using PersonalFinanceApp.Models.ViewModels;
using PersonalFinanceApp.Services;

namespace PersonalFinanceApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly IOtpService _otpService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            IEmailSender emailSender,
            IOtpService otpService,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _emailSender = emailSender;
            _otpService = otpService;
            _logger = logger;
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

        // POST: /Account/Login (Xử lý Đăng nhập bước 1: Kiểm tra mật khẩu & kích hoạt OTP Xác thực 2 bước)
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
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không chính xác.");
                return View(model);
            }

            // Kiểm tra xem tài khoản có đang bị khóa bởi Admin hay không
            if (await _userManager.IsLockedOutAsync(user))
            {
                ViewBag.IsLockedOut = true;
                ViewBag.LockoutMessage = $"🚫 TÀI KHOẢN ({user.Email}) ĐÃ BỊ KHÓA! Quản trị viên đã tạm thời khóa quyền truy cập. Vui lòng liên hệ Admin để được hỗ trợ mở khóa.";
                ModelState.AddModelError(string.Empty, "🚫 Tài khoản của bạn hiện đang bị Quản trị viên khóa. Không thể đăng nhập.");
                return View(model);
            }

            // Kiểm tra mật khẩu (chưa đăng nhập ngay để tiến hành xác thực 2 bước 2FA)
            var passwordValid = await _userManager.CheckPasswordAsync(user, model.Password);
            if (!passwordValid)
            {
                ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không chính xác.");
                return View(model);
            }

            // Sinh mã OTP 6 chữ số (hiệu lực 5 phút)
            var otp = _otpService.GenerateOtp("LOGIN_OTP_" + user.Id, TimeSpan.FromMinutes(5));

            // Gửi email OTP thực tế qua Google Gmail SMTP
            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailSender.SendOtpEmailAsync(user.Email!, user.FullName, otp, "xác thực đăng nhập 2 bước");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi gửi email xác thực 2FA tới {Email}", user.Email);
                }
            });

            TempData["SuccessMessage"] = $"Mã xác thực OTP 6 chữ số đã được gửi đến email: {user.Email}";
            TempData["DebugOtp"] = otp; // Hỗ trợ demo học tập trong trường hợp mạng lớp học chặn cổng SMTP

            return RedirectToAction(nameof(VerifyLoginOtp), new { userId = user.Id, returnUrl });
        }

        // GET: /Account/VerifyLoginOtp (Màn hình nhập mã xác thực 2 bước)
        [HttpGet]
        public async Task<IActionResult> VerifyLoginOtp(string? userId, string? returnUrl = null)
        {
            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Phiên xác thực đã hết hạn hoặc không hợp lệ. Vui lòng đăng nhập lại.";
                return RedirectToAction(nameof(Login));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin tài khoản người dùng.";
                return RedirectToAction(nameof(Login));
            }

            var cachedOtp = _otpService.GetCachedOtp("LOGIN_OTP_" + user.Id);
            if (cachedOtp == null)
            {
                TempData["ErrorMessage"] = "Mã xác thực OTP đã hết hạn (sau 5 phút). Vui lòng đăng nhập lại để nhận mã mới.";
                return RedirectToAction(nameof(Login));
            }

            var model = new VerifyLoginOtpViewModel
            {
                UserId = user.Id,
                Email = user.Email ?? "",
                FullName = user.FullName,
                RememberMe = false,
                ReturnUrl = returnUrl
            };

            ViewBag.DebugOtp = TempData["DebugOtp"] as string ?? cachedOtp;
            return View(model);
        }

        // POST: /Account/VerifyLoginOtp (Xác thực mã OTP và cấp phiên đăng nhập chính thức)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyLoginOtp(VerifyLoginOtpViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.DebugOtp = _otpService.GetCachedOtp("LOGIN_OTP_" + model.UserId);
                return View(model);
            }

            var isValid = _otpService.ValidateOtp("LOGIN_OTP_" + model.UserId, model.OtpCode);
            if (!isValid)
            {
                ModelState.AddModelError("OtpCode", "Mã xác thực OTP không chính xác hoặc đã hết hạn (sau 5 phút). Vui lòng thử lại hoặc bấm 'Gửi lại mã OTP'.");
                ViewBag.DebugOtp = _otpService.GetCachedOtp("LOGIN_OTP_" + model.UserId);
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin tài khoản người dùng.";
                return RedirectToAction(nameof(Login));
            }

            // Cấp phiên đăng nhập Identity chính thức
            await _signInManager.SignInAsync(user, isPersistent: model.RememberMe);

            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                TempData["SuccessMessage"] = $"Xác thực 2 bước thành công! Xin chào Quản trị viên ({user.FullName}).";
                return RedirectToAction("Index", "Admin");
            }

            TempData["SuccessMessage"] = $"Xác thực 2 bước thành công! Chào mừng {(user.FullName ?? user.Email)} đã đăng nhập.";
            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }
            return RedirectToAction("Index", "Transactions");
        }

        // POST: /Account/ResendLoginOtp (Gửi lại mã OTP đăng nhập mới)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendLoginOtp(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy tài khoản người dùng." });
            }

            var newOtp = _otpService.GenerateOtp("LOGIN_OTP_" + user.Id, TimeSpan.FromMinutes(5));
            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailSender.SendOtpEmailAsync(user.Email!, user.FullName, newOtp, "xác thực đăng nhập 2 bước");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi gửi lại mã OTP đăng nhập tới {Email}", user.Email);
                }
            });

            TempData["DebugOtp"] = newOtp;
            return Json(new
            {
                success = true,
                message = $"Mã OTP mới đã được gửi đến email: {user.Email}",
                debugOtp = newOtp
            });
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

        // GET: /Account/ForgotPassword
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        // POST: /Account/ForgotPassword (Gửi mã OTP 6 chữ số đến email để khôi phục mật khẩu)
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
                ModelState.AddModelError("Email", "Địa chỉ email này chưa được đăng ký trong hệ thống CashFlow.");
                return View(model);
            }

            var otp = _otpService.GenerateOtp("RESET_OTP_" + user.Email!.ToLowerInvariant(), TimeSpan.FromMinutes(5));

            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailSender.SendOtpEmailAsync(user.Email!, user.FullName, otp, "đặt lại mật khẩu");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi gửi email OTP đặt lại mật khẩu tới {Email}", user.Email);
                }
            });

            TempData["SuccessMessage"] = $"Mã OTP 6 chữ số đã được gửi đến email: {user.Email}. Vui lòng kiểm tra hộp thư!";
            TempData["DebugResetOtp"] = otp;
            return RedirectToAction(nameof(ResetPasswordOtp), new { email = user.Email });
        }

        // GET: /Account/ResetPasswordOtp (Màn hình nhập mã OTP và mật khẩu mới)
        [HttpGet]
        public IActionResult ResetPasswordOtp(string? email = null)
        {
            if (string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập địa chỉ email tài khoản cần khôi phục.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            var model = new ResetPasswordOtpViewModel
            {
                Email = email
            };

            ViewBag.DebugOtp = TempData["DebugResetOtp"] as string ?? _otpService.GetCachedOtp("RESET_OTP_" + email.ToLowerInvariant());
            return View(model);
        }

        // POST: /Account/ResetPasswordOtp (Xác thực OTP và đặt lại mật khẩu mới)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPasswordOtp(ResetPasswordOtpViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.DebugOtp = _otpService.GetCachedOtp("RESET_OTP_" + model.Email.ToLowerInvariant());
                return View(model);
            }

            var isValid = _otpService.ValidateOtp("RESET_OTP_" + model.Email.ToLowerInvariant(), model.OtpCode);
            if (!isValid)
            {
                ModelState.AddModelError("OtpCode", "Mã xác thực OTP không chính xác hoặc đã hết hạn (sau 5 phút). Vui lòng thử lại hoặc bấm 'Gửi lại mã OTP'.");
                ViewBag.DebugOtp = _otpService.GetCachedOtp("RESET_OTP_" + model.Email.ToLowerInvariant());
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy tài khoản người dùng tương ứng.";
                return RedirectToAction(nameof(Login));
            }

            // Sinh mã token đặt lại mật khẩu của Identity
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "🎉 Đặt lại mật khẩu thành công! Bạn có thể đăng nhập bằng mật khẩu mới ngay.";
                return RedirectToAction(nameof(Login));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            ViewBag.DebugOtp = _otpService.GetCachedOtp("RESET_OTP_" + model.Email.ToLowerInvariant());
            return View(model);
        }

        // POST: /Account/ResendResetOtp (Gửi lại mã OTP đặt lại mật khẩu)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendResetOtp(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return Json(new { success = false, message = "Email không tồn tại trong hệ thống." });
            }

            var newOtp = _otpService.GenerateOtp("RESET_OTP_" + user.Email!.ToLowerInvariant(), TimeSpan.FromMinutes(5));
            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailSender.SendOtpEmailAsync(user.Email!, user.FullName, newOtp, "đặt lại mật khẩu");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi gửi lại mã OTP đặt lại mật khẩu tới {Email}", user.Email);
                }
            });

            TempData["DebugResetOtp"] = newOtp;
            return Json(new
            {
                success = true,
                message = $"Mã OTP mới đã được gửi đến email: {user.Email}",
                debugOtp = newOtp
            });
        }

        // GET: /Account/ResetPassword (Tương thích link cũ hoặc API)
        public IActionResult ResetPassword(string? token = null, string? email = null)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Yêu cầu đặt lại mật khẩu không hợp lệ hoặc thiếu mã bảo mật.";
                return RedirectToAction(nameof(Login));
            }

            var model = new ResetPasswordViewModel
            {
                Token = token,
                Email = email
            };
            return View(model);
        }

        // POST: /Account/ResetPassword
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
                return RedirectToAction(nameof(Login));
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "🎉 Đặt lại mật khẩu thành công! Bạn có thể đăng nhập bằng mật khẩu mới ngay bây giờ.";
                return RedirectToAction(nameof(Login));
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
