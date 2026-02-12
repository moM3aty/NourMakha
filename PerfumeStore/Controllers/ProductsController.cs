using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PerfumeStore.Data;
using PerfumeStore.Models;
using PerfumeStore.ViewModels;

namespace PerfumeStore.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(ProductFilterViewModel filter)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive);

            // Apply filters
            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                query = query.Where(p => p.Name.Contains(filter.SearchTerm) ||
                                         p.NameAr.Contains(filter.SearchTerm) ||
                                         p.Brand.Contains(filter.SearchTerm) ||
                                         p.BrandAr.Contains(filter.SearchTerm));
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

            // Apply sorting
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
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)filter.PageSize)
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

            // Get related products
            var relatedProducts = await _context.Products
                .Where(p => p.CategoryId == product.CategoryId && p.Id != id && p.IsActive)
                .Take(4)
                .ToListAsync();

            var viewModel = new ProductDetailsViewModel
            {
                Product = product,
                RelatedProducts = relatedProducts,
                AverageRating = product.Reviews.Any() ? product.Reviews.Average(r => r.Rating) : 0,
                TotalReviews = product.Reviews.Count
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
                TempData["Error"] = "You have already reviewed this product";
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

            TempData["Success"] = "Thank you for your review!";
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

    public class ProductListViewModel
    {
        public List<Product> Products { get; set; } = new();
        public ProductFilterViewModel Filter { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
    }

    public class ProductDetailsViewModel
    {
        public Product Product { get; set; } = new();
        public List<Product> RelatedProducts { get; set; } = new();
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
    }
}
