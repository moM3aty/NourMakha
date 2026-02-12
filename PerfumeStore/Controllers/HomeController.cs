using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PerfumeStore.Data;
using PerfumeStore.Models;

namespace PerfumeStore.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(ContactMessage model)
        {
            if (ModelState.IsValid)
            {
                model.CreatedAt = DateTime.Now;
                _context.ContactMessages.Add(model);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Thank you for your message! We will get back to you soon.";
                return RedirectToAction(nameof(Contact));
            }
            return View(model);
        }

        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            Response.Cookies.Append("Culture", culture, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1)
            });
            return LocalRedirect(returnUrl);
        }
    }

    public class HomeViewModel
    {
        public List<Product> FeaturedProducts { get; set; } = new();
        public List<Product> NewArrivals { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
    }
}
