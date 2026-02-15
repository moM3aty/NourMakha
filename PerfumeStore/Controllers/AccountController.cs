using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PerfumeStore.Data;
using PerfumeStore.Models;
using PerfumeStore.Services;
using PerfumeStore.ViewModels;
using System.Globalization;

namespace PerfumeStore.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailService _emailService;
        private readonly IOTPService _otpService;
        private readonly ApplicationDbContext _context;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailService emailService,
            IOTPService otpService,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
            _otpService = otpService;
            _context = context;
        }

        private bool IsArabic => CultureInfo.CurrentUICulture.Name.StartsWith("ar");

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError("", IsArabic ? "البريد الإلكتروني أو كلمة المرور غير صحيحة" : "Invalid email or password");
                return View(model);
            }

            // محاولة تسجيل الدخول
            var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                user.LastLoginAt = DateTime.Now;
                await _userManager.UpdateAsync(user);

                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                    return LocalRedirect(model.ReturnUrl);
                return RedirectToAction("Index", "Home");
            }

            // === التحقق إذا كان سبب الفشل هو عدم تفعيل الإيميل ===
            if (result.IsNotAllowed && !user.EmailConfirmed)
            {
                // تحسين: التحقق من وجود كود حديث قبل إرسال كود جديد لتجنب إبطال الكود السابق إذا ضغط المستخدم مرتين
                // نفحص إذا كان هناك كود غير مستخدم تم إنشاؤه في آخر دقيقتين
                var recentOtp = await _context.OTPCodes
                    .Where(o => o.Email == model.Email && o.Purpose == "Register" && !o.IsUsed)
                    .OrderByDescending(o => o.CreatedAt)
                    .FirstOrDefaultAsync();

                bool shouldGenerateNew = true;

                if (recentOtp != null && recentOtp.CreatedAt > DateTime.UtcNow.AddMinutes(-2))
                {
                    // يوجد كود حديث، لا داعي لإرسال جديد، فقط وجهه للتحقق
                    shouldGenerateNew = false;
                }

                if (shouldGenerateNew)
                {
                    try
                    {
                        var otp = await _otpService.GenerateOTPAsync(model.Email, "Register");
                        await _emailService.SendOtpEmailAsync(model.Email, otp, "Register");
                    }
                    catch (Exception ex)
                    {
                        // تسجيل الخطأ أو عرضه للمستخدم
                        ModelState.AddModelError("", "حدث خطأ أثناء إرسال رمز التحقق. يرجى المحاولة لاحقاً.");
                    }
                }

                return RedirectToAction("VerifyOTP", new { email = model.Email, purpose = "Register" });
            }

            ModelState.AddModelError("", IsArabic ? "البريد الإلكتروني أو كلمة المرور غير صحيحة" : "Invalid email or password");
            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", IsArabic ? "البريد الإلكتروني مستخدم بالفعل" : "Email is already in use");
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                PhoneNumber = model.PhoneNumber,
                CreatedAt = DateTime.Now
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Customer");

                try
                {
                    // توليد وإرسال الـ OTP
                    var otp = await _otpService.GenerateOTPAsync(model.Email, "Register");
                    await _emailService.SendOtpEmailAsync(model.Email, otp, "Register");
                }
                catch (Exception)
                {
                    // في حال فشل الإرسال، لا نوقف عملية التسجيل ولكن المستخدم سيحتاج لطلب إعادة الإرسال
                }

                return RedirectToAction("VerifyOTP", new { email = model.Email, purpose = "Register" });
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
                return RedirectToAction(nameof(Login));

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.UserId == user.Id)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            ViewBag.Orders = orders;
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(ApplicationUser model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.PhoneNumber = model.PhoneNumber;

            if (!string.IsNullOrEmpty(model.Address)) user.Address = model.Address;
            if (!string.IsNullOrEmpty(model.City)) user.City = model.City;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["Success"] = IsArabic ? "تم تحديث البيانات بنجاح" : "Profile updated successfully";
            }
            else
            {
                TempData["Error"] = IsArabic ? "حدث خطأ أثناء التحديث" : "Error updating profile";
            }

            return RedirectToAction(nameof(Profile));
        }

        [HttpGet]
        public async Task<IActionResult> OrderDetails(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == user.Id);

            if (order == null) return NotFound();

            return View(order);
        }

        [HttpGet]
        public IActionResult VerifyOTP(string email, string purpose)
        {
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Login");
            return View(new OTPVerificationViewModel { Email = email, Purpose = purpose });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOTP(OTPVerificationViewModel model)
        {
            if (string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.OTPCode))
            {
                ModelState.AddModelError("", IsArabic ? "البيانات غير مكتملة" : "Missing data");
                return View(model);
            }

            var isValid = await _otpService.VerifyOTPAsync(model.Email, model.OTPCode, model.Purpose);
            if (!isValid)
            {
                ModelState.AddModelError("OTPCode", IsArabic ? "الكود غير صحيح أو منتهي الصلاحية" : "Invalid or expired code");
                return View(model);
            }

            if (model.Purpose == "Register")
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    user.EmailConfirmed = true;
                    user.IsEmailVerified = true;
                    await _userManager.UpdateAsync(user);
                    await _signInManager.SignInAsync(user, isPersistent: false);
                }
                return RedirectToAction("Index", "Home");
            }
            else if (model.Purpose == "ResetPassword")
            {
                return RedirectToAction("ResetPassword", new { email = model.Email, token = model.OTPCode });
            }

            return RedirectToAction("Login");
        }

        [HttpPost]
        public async Task<IActionResult> ResendOTP([FromBody] OTPVerificationViewModel model)
        {
            if (string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Purpose))
            {
                return Json(new { success = false, message = "Invalid data" });
            }

            try
            {
                // التحقق من وجود كود حديث قبل إعادة الإرسال لتجنب السبام
                var recentOtp = await _context.OTPCodes
                    .Where(o => o.Email == model.Email && o.Purpose == model.Purpose && !o.IsUsed)
                    .OrderByDescending(o => o.CreatedAt)
                    .FirstOrDefaultAsync();

                if (recentOtp != null && recentOtp.CreatedAt > DateTime.UtcNow.AddMinutes(-1))
                {
                    return Json(new { success = false, message = IsArabic ? "يرجى الانتظار قليلاً قبل طلب كود جديد" : "Please wait before requesting a new code" });
                }

                var otp = await _otpService.GenerateOTPAsync(model.Email, model.Purpose);
                await _emailService.SendOtpEmailAsync(model.Email, otp, model.Purpose);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user != null)
            {
                try
                {
                    var otp = await _otpService.GenerateOTPAsync(email, "ResetPassword");
                    await _emailService.SendOtpEmailAsync(email, otp, "ResetPassword");
                }
                catch { /* Log error */ }
            }
            return RedirectToAction("VerifyOTP", new { email = email, purpose = "ResetPassword" });
        }

        [HttpGet]
        public IActionResult ResetPassword(string email, string? token)
        {
            return View(new ResetPasswordViewModel { Email = email, Token = token });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return RedirectToAction("Login");


            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

            if (result.Succeeded)
            {
                TempData["Success"] = IsArabic ? "تم تغيير كلمة المرور بنجاح" : "Password reset successfully";
                return RedirectToAction("Login");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            return View(model);
        }
    }
}