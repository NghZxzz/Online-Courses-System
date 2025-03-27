using DoAnChuyennNganh.Data;
using DoAnChuyennNganh.Models;
using DoAnChuyennNganh.Services.Email;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace DoAnChuyennNganh.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IEmailService _emailService;

        public AccountController(ApplicationDbContext context, UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager, RoleManager<IdentityRole> roleManager, IEmailService emailService)
        {
            _emailService = emailService;
            _roleManager = roleManager;
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError("", "Email không được để trống");
                return View();
            }
            ViewBag.Email = email;
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                ModelState.AddModelError("", "Email không tồn tại hoặc chưa được đăng ký.");
                return View();
            }

            // Tạo mã token khôi phục mật khẩu
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var callbackUrl = Url.Action(
                "ResetPassword",
                "Account",
                new { token, email = user.Email },
                protocol: HttpContext.Request.Scheme);

            // Gửi email
            var emailService = new EmailService();
            await emailService.SendEmailAsync(user.Email, "Khôi phục mật khẩu",
                $"Nhấn vào đây để khôi phục mật khẩu của bạn: <a href='{callbackUrl}'>Khôi phục mật khẩu</a>");

            return View("ForgotPasswordConfirmation");
        }

        [HttpGet]
        public IActionResult ResetPassword(string token, string email)
        {
            if (token == null || email == null)
            {
                return BadRequest("Token hoặc email không hợp lệ.");
            }

            var model = new ResetPasswordViewModel { Token = token, Email = email };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return RedirectToAction("ResetPasswordConfirmation");
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction("ResetPasswordConfirmation");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View("ResetPasswordConfirmation");
        }
        public async Task LoginByGoogle()
        {
            await HttpContext.ChallengeAsync(GoogleDefaults.AuthenticationScheme,
                new AuthenticationProperties
                {
                    RedirectUri = Url.Action("GoogleResponse")
                });
        }
        public async Task<IActionResult> GoogleResponse()
        {
            var authenticateResult = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

            if (!authenticateResult.Succeeded || authenticateResult.Principal == null)
            {
                TempData["error"] = "Không thể đăng nhập. Vui lòng thử lại.";
                return RedirectToAction("Login");
            }

            // Lấy email và các thông tin khác từ Google
            var email = authenticateResult.Principal.FindFirstValue(ClaimTypes.Email);
            var firstName = authenticateResult.Principal.FindFirstValue(ClaimTypes.GivenName);
            var lastName = authenticateResult.Principal.FindFirstValue(ClaimTypes.Surname);

            // Kiểm tra nếu email không tồn tại
            if (string.IsNullOrEmpty(email))
            {
                TempData["error"] = "Không thể lấy thông tin email từ tài khoản Google.";
                return RedirectToAction("Login");
            }


            // Xử lý người dùng (tương tự như trước)
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    Email = email,
                    UserName = email,
                    FirstName = firstName,
                    LastName = lastName,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user);
                if (!result.Succeeded)
                {
                    TempData["error"] = "Không thể tạo tài khoản mới.";
                    return RedirectToAction("Login");
                }
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            TempData["success"] = "Đăng nhập thành công!";
            return RedirectToAction("Index", "Home");
        }



    }
}
