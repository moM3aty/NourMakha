using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PerfumeStore.Data;
using PerfumeStore.Models;
using PerfumeStore.ViewModels;
using System.Globalization;

namespace PerfumeStore.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsArabic => CultureInfo.CurrentUICulture.Name.StartsWith("ar");

        public async Task<IActionResult> Index(ProductFilterViewModel filter)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive);

            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                query = query.Where(p =>
                    p.Name.Contains(filter.SearchTerm) ||
                    p.NameAr!.Contains(filter.SearchTerm) ||
                    p.Brand.Contains(filter.SearchTerm) ||
                    p.BrandAr!.Contains(filter.SearchTerm));
            }

            if (filter.CategoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == filter.CategoryId);
            }

            if (!string.IsNullOrEmpty(filter.Gender))
            {
                query = query.Where(p => p.Gender == filter.Gender);
            }

            if (filter.MinPrice.HasValue)
            {
                query = query.Where(p => p.Price >= filter.MinPrice);
            }

            if (filter.MaxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= filter.MaxPrice);
            }

            if (!string.IsNullOrEmpty(filter.ScentFamily))
            {
                query = query.Where(p => p.ScentFamily == filter.ScentFamily || p.ScentFamilyAr == filter.ScentFamily);
            }

            query = filter.SortBy switch
            {
                "price-low" => query.OrderBy(p => p.Price),
                "price-high" => query.OrderByDescending(p => p.Price),
                "name" => query.OrderBy(p => p.Name),
                "newest" => query.OrderByDescending(p => p.CreatedAt),
                _ => query.OrderByDescending(p => p.IsFeatured).ThenByDescending(p => p.CreatedAt)
            };

            var totalItems = await query.CountAsync();
            var products = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var viewModel = new ProductListViewModel
            {
                Products = products,
                Filter = filter,
                Categories = await _context.Categories.Where(c => c.IsActive).ToListAsync(),
                TotalCount = totalItems,
                CurrentPage = filter.Page,
                PageSize = filter.PageSize
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Reviews)
                .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound();

            var relatedProducts = await _context.Products
                .Where(p => p.CategoryId == product.CategoryId && p.Id != id && p.IsActive)
                .Take(4)
                .ToListAsync();

            var viewModel = new ProductDetailsViewModel
            {
                Product = product,
                Reviews = product.Reviews.ToList(),
                RelatedProducts = relatedProducts,
                AverageRating = product.Reviews.Any() ? (int)Math.Round(product.Reviews.Average(r => r.Rating)) : 0,
                ReviewCount = product.Reviews.Count
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(int productId, int rating, string comment)
        {
            if (!User.Identity?.IsAuthenticated ?? true)
                return RedirectToAction("Login", "Account");

            var userId = _context.Users
                .FirstOrDefault(u => u.UserName == User.Identity.Name)?.Id;

            if (userId == null)
                return Unauthorized();

            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.ProductId == productId && r.UserId == userId);

            if (existingReview != null)
            {
                TempData["Error"] = IsArabic ? "لقد قمت بتقييم هذا المنتج من قبل" : "You have already reviewed this product";
                return RedirectToAction(nameof(Details), new { id = productId });
            }

            var review = new Review
            {
                ProductId = productId,
                UserId = userId,
                Rating = rating,
                Comment = comment,
                CreatedAt = DateTime.Now
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            TempData["Success"] = IsArabic ? "شكراً لتقييمك!" : "Thank you for your review!";
            return RedirectToAction(nameof(Details), new { id = productId });
        }

        public async Task<IActionResult> Category(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return NotFound();

            var products = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.CategoryId == id && p.IsActive)
                .ToListAsync();

            ViewBag.Category = category;
            return View(products);
        }
    }
}