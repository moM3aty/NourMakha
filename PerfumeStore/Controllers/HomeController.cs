using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PerfumeStore.Data;
using PerfumeStore.Models;
using PerfumeStore.Services;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using System.Diagnostics;

namespace PerfumeStore.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public HomeController(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        private bool IsArabic => CultureInfo.CurrentUICulture.Name.StartsWith("ar");

        public async Task<IActionResult> Index()
        {
            var viewModel = new HomeViewModel
            {
                FeaturedProducts = await _context.Products
                    .Where(p => p.IsFeatured && p.IsActive)
                    .Include(p => p.Category)
                    .Take(8)
                    .ToListAsync(),

                NewArrivals = await _context.Products
                    .Where(p => p.IsNewArrival && p.IsActive)
                    .Include(p => p.Category)
                    .Take(8)
                    .ToListAsync(),

                Categories = await _context.Categories
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.DisplayOrder)
                    .ToListAsync()
            };

            return View(viewModel);
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }
        public IActionResult Terms()
        {
            return View();
        }
        public IActionResult Privacy()
        {
            return View();
        }
        public IActionResult Returns()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(ContactMessage model)
        {
            if (ModelState.IsValid)
            {
                model.CreatedAt = DateTime.Now;
                _context.ContactMessages.Add(model);
                await _context.SaveChangesAsync();

                TempData["Success"] = IsArabic 
                    ? "تم إرسال رسالتك بنجاح! سنقوم بالرد عليك قريباً." 
                    : "Thank you for your message! We will get back to you soon.";

                return RedirectToAction(nameof(Contact));
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult SetLanguage(string culture, string? returnUrl)
        {
            if (string.IsNullOrEmpty(culture) || (culture != "ar" && culture != "en"))
            {
                culture = "ar";
            }

            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    HttpOnly = true,
                    IsEssential = true,
                    Path = "/",
                    SameSite = SameSiteMode.Lax
                }
            );

            if (string.IsNullOrEmpty(returnUrl) || !Url.IsLocalUrl(returnUrl))
            {
                returnUrl = "/";
            }

            return LocalRedirect(returnUrl);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    public class HomeViewModel
    {
        public List<Product> FeaturedProducts { get; set; } = new();
        public List<Product> NewArrivals { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
    }
}
