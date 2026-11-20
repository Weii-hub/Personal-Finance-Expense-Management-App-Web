using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalFinanceApp.Models;
using PersonalFinanceApp.Models.ViewModels;
using PersonalFinanceApp.Services;

namespace PersonalFinanceApp.Controllers
{
    [Authorize]
    public class WalletsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly IOtpService _otpService;
        private readonly ILogger<WalletsController> _logger;

        public WalletsController(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager,
            IEmailSender emailSender,
            IOtpService otpService,
            ILogger<WalletsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _emailSender = emailSender;
            _otpService = otpService;
            _logger = logger;
        }

        private string GetCurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // GET: Wallets (Chỉ lấy ví của chính người đang đăng nhập - Issue #15)
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            var myWallets = await _context.Wallets
                .Where(w => w.UserId == userId)
                .OrderBy(w => w.WalletType)
                .ThenBy(w => w.Id)
                .AsNoTracking()
                .ToListAsync();

            var user = await _userManager.FindByIdAsync(userId);
            ViewBag.HasPin = !string.IsNullOrEmpty(user?.SecurityPinHash);

            return View(myWallets);
        }

        // GET: Wallets/Create
        public IActionResult Create()
        {
            return View(new Wallet { WalletType = 0, Icon = "fa-wallet" });
        }

        // POST: Wallets/Create (Gắn UserId của người tạo vào ví mới - Issue #15 & Dual-Type Wallet)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,InitialBalance,Icon,WalletType,BankName,AccountNumber")] Wallet wallet)
        {
            wallet.UserId = GetCurrentUserId();

            if (wallet.WalletType == 0)
            {
                // Ví thủ công (Tiền mặt) không có thông tin ngân hàng
                wallet.BankName = null;
                wallet.AccountNumber = null;
                wallet.IsSavingsAllowed = false;
                wallet.IsSyncEnabled = false;
                if (string.IsNullOrWhiteSpace(wallet.Icon)) wallet.Icon = "fa-wallet";
            }
            else
            {
                // Ví điện tử / Ngân hàng liên kết
                if (string.IsNullOrWhiteSpace(wallet.BankName))
                {
                    ModelState.AddModelError("BankName", "Vui lòng chọn hoặc nhập tên Ngân hàng / Ví điện tử.");
                }
                if (string.IsNullOrWhiteSpace(wallet.Icon) || wallet.Icon == "fa-wallet")
                {
                    wallet.Icon = "fa-building-columns";
                }
            }

            if (ModelState.IsValid)
            {
                wallet.Name = (wallet.Name ?? string.Empty).Trim();
                wallet.Icon = string.IsNullOrWhiteSpace(wallet.Icon) ? "fa-wallet" : wallet.Icon.Trim();
                wallet.BankName = wallet.BankName?.Trim();
                wallet.AccountNumber = wallet.AccountNumber?.Trim();

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
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,InitialBalance,Icon,WalletType,BankName,AccountNumber")] Wallet wallet)
        {
            if (id != wallet.Id) return NotFound();

            var userId = GetCurrentUserId();
            var existingWallet = await _context.Wallets.FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);
            if (existingWallet == null) return NotFound();

            if (wallet.WalletType == 1 && string.IsNullOrWhiteSpace(wallet.BankName))
            {
                ModelState.AddModelError("BankName", "Vui lòng chọn hoặc nhập tên Ngân hàng / Ví điện tử.");
            }

            if (ModelState.IsValid)
            {
                existingWallet.Name = (wallet.Name ?? string.Empty).Trim();
                existingWallet.Icon = string.IsNullOrWhiteSpace(wallet.Icon) ? "fa-wallet" : wallet.Icon.Trim();
                existingWallet.WalletType = wallet.WalletType;

                if (wallet.WalletType == 0)
                {
                    existingWallet.BankName = null;
                    existingWallet.AccountNumber = null;
                    existingWallet.IsSavingsAllowed = false;
                    existingWallet.IsSyncEnabled = false;
                }
                else
                {
                    existingWallet.BankName = wallet.BankName?.Trim();
                    existingWallet.AccountNumber = wallet.AccountNumber?.Trim();
                }

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

        // ==========================================
        // CÁC HÀM XỬ LÝ MÃ PIN BẢO MẬT & PHÂN QUYỀN
        // ==========================================

        // GET: /Wallets/CheckPin (Kiểm tra xem người dùng đã tạo PIN chưa)
        [HttpGet]
        public async Task<IActionResult> CheckPin()
        {
            var userId = GetCurrentUserId();
            var user = await _userManager.FindByIdAsync(userId);
            return Json(new { hasPin = !string.IsNullOrEmpty(user?.SecurityPinHash) });
        }

        // POST: /Wallets/SetPin (Thiết lập hoặc Đổi Mã PIN bảo mật 6 số)
        [HttpPost]
        public async Task<IActionResult> SetPin([FromBody] SetPinRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Pin) || req.Pin.Length != 6 || !req.Pin.All(char.IsDigit))
            {
                return Json(new { success = false, message = "Mã PIN mới phải bao gồm đúng 6 chữ số (0-9)." });
            }

            if (req.Pin != req.ConfirmPin)
            {
                return Json(new { success = false, message = "Mã PIN mới xác nhận không trùng khớp. Vui lòng nhập lại!" });
            }

            var userId = GetCurrentUserId();
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return Json(new { success = false, message = "Không tìm thấy người dùng." });

            // NẾU NGƯỜI DÙNG ĐÃ CÓ PIN -> BẮT BUỘC NHẬP MÃ PIN HIỆN TẠI (ĐỔI MÃ PIN)
            if (!string.IsNullOrEmpty(user.SecurityPinHash))
            {
                if (string.IsNullOrWhiteSpace(req.CurrentPin))
                {
                    return Json(new { success = false, message = "Vui lòng nhập Mã PIN hiện tại để xác thực trước khi đổi mã mới!" });
                }

                var verifyCurrent = _userManager.PasswordHasher.VerifyHashedPassword(user, user.SecurityPinHash, req.CurrentPin);
                if (verifyCurrent == PasswordVerificationResult.Failed)
                {
                    return Json(new { success = false, message = "Mã PIN hiện tại không chính xác! Không thể đổi mã PIN." });
                }

                if (req.CurrentPin == req.Pin)
                {
                    return Json(new { success = false, message = "Mã PIN mới không được trùng với mã PIN hiện tại!" });
                }
            }

            user.SecurityPinHash = _userManager.PasswordHasher.HashPassword(user, req.Pin);
            await _userManager.UpdateAsync(user);

            var isChange = !string.IsNullOrEmpty(req.CurrentPin);
            return Json(new { 
                success = true, 
                message = isChange 
                    ? "Đã đổi Mã PIN bảo mật 6 số thành công!" 
                    : "Đã thiết lập Mã PIN bảo mật 6 số lần đầu thành công!" 
            });
        }

        // ============================================================
        // TÍNH NĂNG: QUÊN MÃ PIN BẢO MẬT (FORGOT PIN VIA EMAIL OTP)
        // ============================================================

        // POST: /Wallets/SendForgotPinOtp (Gửi mã OTP 6 số về email khi người dùng quên mã PIN)
        [HttpPost]
        public async Task<IActionResult> SendForgotPinOtp()
        {
            var userId = GetCurrentUserId();
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy người dùng." });
            }

            if (string.IsNullOrEmpty(user.Email))
            {
                return Json(new { success = false, message = "Tài khoản của bạn chưa có email để nhận mã xác thực OTP." });
            }

            var otp = _otpService.GenerateOtp("FORGOT_PIN_OTP_" + user.Id, TimeSpan.FromMinutes(5));

            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailSender.SendOtpEmailAsync(user.Email, user.FullName, otp, "khôi phục Mã PIN bảo mật ví");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi gửi email OTP khôi phục PIN tới {Email}", user.Email);
                }
            });

            return Json(new
            {
                success = true,
                message = $"Mã OTP 6 chữ số đã được gửi đến email: {user.Email}. Vui lòng kiểm tra hộp thư!",
                debugOtp = otp
            });
        }

        // POST: /Wallets/ResetPinWithOtp (Xác thực OTP và đặt Mã PIN mới qua AJAX)
        [HttpPost]
        public async Task<IActionResult> ResetPinWithOtp([FromBody] ResetPinWithOtpRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.OtpCode) || req.OtpCode.Length != 6)
            {
                return Json(new { success = false, message = "Vui lòng nhập đúng 6 chữ số mã OTP xác thực." });
            }

            if (string.IsNullOrWhiteSpace(req.NewPin) || req.NewPin.Length != 6 || !req.NewPin.All(char.IsDigit))
            {
                return Json(new { success = false, message = "Mã PIN mới phải bao gồm đúng 6 chữ số (0-9)." });
            }

            if (req.NewPin != req.ConfirmNewPin)
            {
                return Json(new { success = false, message = "Mã PIN mới xác nhận không trùng khớp. Vui lòng kiểm tra lại!" });
            }

            var userId = GetCurrentUserId();
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return Json(new { success = false, message = "Không tìm thấy người dùng." });

            var isValid = _otpService.ValidateOtp("FORGOT_PIN_OTP_" + user.Id, req.OtpCode);
            if (!isValid)
            {
                return Json(new { success = false, message = "Mã xác thực OTP không chính xác hoặc đã hết hạn (sau 5 phút). Vui lòng thử lại hoặc bấm gửi lại mã!" });
            }

            user.SecurityPinHash = _userManager.PasswordHasher.HashPassword(user, req.NewPin);
            await _userManager.UpdateAsync(user);

            return Json(new
            {
                success = true,
                message = "🎉 Khôi phục Mã PIN bảo mật thành công! Bạn có thể sử dụng mã PIN mới ngay bây giờ."
            });
        }

        // GET: /Wallets/ForgotPin (Trang độc lập Khôi phục Mã PIN)
        [HttpGet]
        public async Task<IActionResult> ForgotPin(string? returnUrl = null)
        {
            var userId = GetCurrentUserId();
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return RedirectToAction("Login", "Account");

            var otp = _otpService.GenerateOtp("FORGOT_PIN_OTP_" + user.Id, TimeSpan.FromMinutes(5));
            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailSender.SendOtpEmailAsync(user.Email!, user.FullName, otp, "khôi phục Mã PIN bảo mật ví");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi gửi email OTP khôi phục PIN tới {Email}", user.Email);
                }
            });

            var model = new ResetPinWithOtpViewModel
            {
                Email = user.Email,
                ReturnUrl = returnUrl
            };

            ViewBag.DebugOtp = otp;
            return View(model);
        }

        // POST: /Wallets/ForgotPin (Xác nhận trang độc lập)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPin(ResetPinWithOtpViewModel model)
        {
            var userId = GetCurrentUserId();
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return RedirectToAction("Login", "Account");

            model.Email = user.Email;

            if (!ModelState.IsValid)
            {
                ViewBag.DebugOtp = _otpService.GetCachedOtp("FORGOT_PIN_OTP_" + user.Id);
                return View(model);
            }

            var isValid = _otpService.ValidateOtp("FORGOT_PIN_OTP_" + user.Id, model.OtpCode);
            if (!isValid)
            {
                ModelState.AddModelError("OtpCode", "Mã xác thực OTP không chính xác hoặc đã hết hạn (sau 5 phút).");
                ViewBag.DebugOtp = _otpService.GetCachedOtp("FORGOT_PIN_OTP_" + user.Id);
                return View(model);
            }

            user.SecurityPinHash = _userManager.PasswordHasher.HashPassword(user, model.NewPin);
            await _userManager.UpdateAsync(user);

            TempData["SuccessMessage"] = "🎉 Khôi phục Mã PIN bảo mật thành công! Bạn đã có thể sử dụng Mã PIN mới.";
            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: /Wallets/ResendForgotPinOtp
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendForgotPinOtp()
        {
            var userId = GetCurrentUserId();
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return Json(new { success = false, message = "Không tìm thấy người dùng." });

            var newOtp = _otpService.GenerateOtp("FORGOT_PIN_OTP_" + user.Id, TimeSpan.FromMinutes(5));
            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailSender.SendOtpEmailAsync(user.Email!, user.FullName, newOtp, "khôi phục Mã PIN bảo mật ví");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi gửi lại mã OTP khôi phục PIN tới {Email}", user.Email);
                }
            });

            return Json(new
            {
                success = true,
                message = $"Mã OTP mới đã được gửi đến email: {user.Email}",
                debugOtp = newOtp
            });
        }

        // POST: /Wallets/TogglePermission (Bật/Tắt công tắc quyền với xác thực PIN)
        [HttpPost]
        public async Task<IActionResult> TogglePermission([FromBody] TogglePermissionRequest req)
        {
            var userId = GetCurrentUserId();
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return Json(new { success = false, message = "Không tìm thấy người dùng." });

            if (string.IsNullOrEmpty(user.SecurityPinHash))
            {
                return Json(new { success = false, needSetPin = true, message = "Bạn chưa thiết lập Mã PIN bảo mật. Vui lòng tạo mã PIN trước!" });
            }

            if (string.IsNullOrWhiteSpace(req.Pin))
            {
                return Json(new { success = false, message = "Vui lòng nhập Mã PIN 6 số để xác thực thay đổi quyền." });
            }

            var verifyResult = _userManager.PasswordHasher.VerifyHashedPassword(user, user.SecurityPinHash, req.Pin);
            if (verifyResult == PasswordVerificationResult.Failed)
            {
                return Json(new { success = false, message = "Mã PIN bảo mật không chính xác. Thao tác bị từ chối!" });
            }

            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.Id == req.WalletId && w.UserId == userId);
            if (wallet == null)
            {
                return Json(new { success = false, message = "Không tìm thấy ví tương ứng." });
            }

            if (wallet.WalletType != 1)
            {
                return Json(new { success = false, message = "Chỉ ví điện tử / ngân hàng liên kết mới có tính năng phân quyền này." });
            }

            if (req.PermissionType == "savings")
            {
                wallet.IsSavingsAllowed = req.Enabled;
                await _context.SaveChangesAsync();
                return Json(new
                {
                    success = true,
                    message = $"Đã {(req.Enabled ? "BẬT" : "TẮT")} 'Quyền trích cho mục tiêu tiết kiệm' của ví '{wallet.Name}'!"
                });
            }
            else if (req.PermissionType == "sync")
            {
                wallet.IsSyncEnabled = req.Enabled;
                await _context.SaveChangesAsync();
                return Json(new
                {
                    success = true,
                    message = $"Đã {(req.Enabled ? "BẬT" : "TẮT")} 'Quyền tự động theo dõi biến động số dư' của ví '{wallet.Name}'!"
                });
            }

            return Json(new { success = false, message = "Loại quyền không hợp lệ." });
        }

        // POST: /Wallets/SimulateTransaction (Kích hoạt biến động số dư qua Bộ Giả Lập Sandbox)
        [HttpPost]
        public async Task<IActionResult> SimulateTransaction([FromBody] SimulateTransactionRequest req)
        {
            var userId = GetCurrentUserId();
            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.Id == req.WalletId && w.UserId == userId);
            if (wallet == null)
            {
                return Json(new { success = false, message = "Không tìm thấy ví đã chọn." });
            }

            if (wallet.WalletType != 1)
            {
                return Json(new { success = false, message = "Chỉ có ví Ngân hàng / Điện tử mới hỗ trợ biến động số dư tự động." });
            }

            if (!wallet.IsSyncEnabled)
            {
                return Json(new { success = false, message = $"Ví '{wallet.Name}' hiện đang TẮT 'Quyền theo dõi biến động số dư'. Vui lòng gạt bật công tắc này trước khi kích hoạt!" });
            }

            if (req.Amount <= 0)
            {
                return Json(new { success = false, message = "Số tiền biến động phải lớn hơn 0 đ." });
            }

            var inCat = await _context.Categories.FirstOrDefaultAsync(c => c.Name == "Thu nhập từ ngân hàng")
                ?? await _context.Categories.FirstOrDefaultAsync(c => c.Type == 1);
            var outCat = await _context.Categories.FirstOrDefaultAsync(c => c.Name == "Chi tiêu qua ngân hàng")
                ?? await _context.Categories.FirstOrDefaultAsync(c => c.Type == 0);

            var isIncome = req.TransferType == "in";
            var rawNote = string.IsNullOrWhiteSpace(req.Content)
                ? (isIncome ? "Chuyển tiền vào tài khoản" : "Thanh toán quẹt thẻ/chuyển khoản")
                : req.Content.Trim();
            var cleanNote = BankingWebhookController.SanitizeBankingContent(rawNote);

            if (isIncome)
            {
                wallet.InitialBalance += req.Amount;
                _context.Transactions.Add(new Transaction
                {
                    Amount = req.Amount,
                    TransactionDate = DateTime.UtcNow,
                    Note = cleanNote,
                    WalletId = wallet.Id,
                    CategoryId = inCat!.Id,
                    UserId = userId
                });
            }
            else
            {
                wallet.InitialBalance -= req.Amount;
                _context.Transactions.Add(new Transaction
                {
                    Amount = req.Amount,
                    TransactionDate = DateTime.UtcNow,
                    Note = cleanNote,
                    WalletId = wallet.Id,
                    CategoryId = outCat!.Id,
                    UserId = userId
                });
            }

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = $"⚡ Biến động số dư thành công! Đã ghi nhận {(isIncome ? "+" : "-")}{req.Amount:N0} đ vào ví '{wallet.Name}'. Số dư mới: {wallet.InitialBalance:N0} đ.",
                newBalance = wallet.InitialBalance
            });
        }
    }

    public class TogglePermissionRequest
    {
        public int WalletId { get; set; }
        public string PermissionType { get; set; } = string.Empty; // "savings" | "sync"
        public bool Enabled { get; set; }
        public string Pin { get; set; } = string.Empty;
    }

    public class SetPinRequest
    {
        public string? CurrentPin { get; set; }
        public string Pin { get; set; } = string.Empty;
        public string ConfirmPin { get; set; } = string.Empty;
    }

    public class ResetPinWithOtpRequest
    {
        public string OtpCode { get; set; } = string.Empty;
        public string NewPin { get; set; } = string.Empty;
        public string ConfirmNewPin { get; set; } = string.Empty;
    }

    public class SimulateTransactionRequest
    {
        public int WalletId { get; set; }
        public string TransferType { get; set; } = "in"; // "in" | "out"
        public decimal Amount { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}
