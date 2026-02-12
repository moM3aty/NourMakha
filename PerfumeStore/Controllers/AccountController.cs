using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PerfumeStore.Data;
using PerfumeStore.Models;
using PerfumeStore.Services;
using PerfumeStore.ViewModels;

namespace PerfumeStore.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IOTPService _otpService;
        private readonly ApplicationDbContext _context;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IOTPService otpService,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _otpService = otpService;
            _context = context;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid email or password");
                return View(model);
            }

            // Generate and send OTP
            await _otpService.GenerateOTPAsync(model.Email, "Login");

            return RedirectToAction(nameof(VerifyOTP), new { email = model.Email, purpose = "Login", returnUrl = returnUrl ?? model.ReturnUrl });
        }

        [HttpGet]
        public IActionResult VerifyOTP(string email, string purpose, string? returnUrl = null)
        {
            return View(new OTPVerificationViewModel
            {
                Email = email,
                Purpose = purpose,
                ReturnUrl = returnUrl
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOTP(OTPVerificationViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var isValid = await _otpService.VerifyOTPAsync(model.Email, model.OTPCode, model.Purpose);
            if (!isValid)
            {
                ModelState.AddModelError("OTPCode", "Invalid or expired OTP code");
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "User not found");
                return View(model);
            }

            if (model.Purpose == "Login")
            {
                await _signInManager.SignInAsync(user, false);
                user.IsEmailVerified = true;
                await _userManager.UpdateAsync(user);

                if (!string.IsNullOrEmpty(model.ReturnUrl))
                    return Redirect(model.ReturnUrl);

                return RedirectToAction("Index", "Home");
            }

            if (model.Purpose == "Register")
            {
                user.EmailConfirmed = true;
                user.IsEmailVerified = true;
                await _userManager.UpdateAsync(user);
                await _signInManager.SignInAsync(user, false);
                return RedirectToAction("Index", "Home");
            }

            if (model.Purpose == "ResetPassword")
            {
                return RedirectToAction(nameof(ResetPassword), new { email = model.Email, code = model.OTPCode });
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> ResendOTP(string email, string purpose)
        {
            await _otpService.GenerateOTPAsync(email, purpose);
            return Json(new { success = true, message = "OTP sent successfully" });
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
                ModelState.AddModelError("Email", "Email is already registered");
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
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return View(model);
            }

            await _userManager.AddToRoleAsync(user, "Customer");

            // Generate and send OTP
            await _otpService.GenerateOTPAsync(model.Email, "Register");

            return RedirectToAction(nameof(VerifyOTP), new { email = model.Email, purpose = "Register" });
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }

            await _otpService.GenerateOTPAsync(model.Email, "ResetPassword");
            return RedirectToAction(nameof(VerifyOTP), new { email = model.Email, purpose = "ResetPassword" });
        }

        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword(string email, string code)
        {
            return View(new ResetPasswordViewModel { Email = email, OTPCode = code });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }

            var isValid = await _otpService.VerifyOTPAsync(model.Email, model.OTPCode, "ResetPassword");
            if (!isValid)
            {
                ModelState.AddModelError("OTPCode", "Invalid or expired OTP code");
                return View(model);
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

            if (result.Succeeded)
            {
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            return View(model);
        }

        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
                return RedirectToAction(nameof(Login));

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound();

            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.UserId == user.Id)
                .OrderByDescending(o => o.CreatedAt)
                .Take(10)
                .ToListAsync();

            ViewBag.Orders = orders;
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(ApplicationUser model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound();

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Address = model.Address;
            user.City = model.City;
            user.PostalCode = model.PostalCode;
            user.Country = model.Country;

            await _userManager.UpdateAsync(user);
            TempData["Success"] = "Profile updated successfully";
            return RedirectToAction(nameof(Profile));
        }
    }
}
