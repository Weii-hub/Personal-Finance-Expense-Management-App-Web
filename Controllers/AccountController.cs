using Microsoft.AspNetCore.Mvc;
using PersonalFinanceApp.Models.ViewModels;

namespace PersonalFinanceApp.Controllers
{
    public class AccountController : Controller
    {
        // GET: /Account/Login
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        // POST: /Account/Login (Frontend stub - sẽ tích hợp trọn vẹn Identity ở Issue #14)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Giả lập đăng nhập thành công cho frontend
            TempData["SuccessMessage"] = $"Chào mừng bạn ({model.Email}) đã đăng nhập thành công!";
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/Register
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        // POST: /Account/Register (Frontend stub - sẽ tích hợp trọn vẹn Identity ở Issue #14)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            TempData["SuccessMessage"] = $"Đăng ký tài khoản {model.Email} thành công! Hãy đăng nhập nhé.";
            return RedirectToAction(nameof(Login));
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            TempData["SuccessMessage"] = "Đã đăng xuất an toàn.";
            return RedirectToAction("Index", "Home");
        }
    }
}
