using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PerfumeStore.Data;
using PerfumeStore.Models;
using PerfumeStore.Services;
using PerfumeStore.ViewModels;

namespace PerfumeStore.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    [Area("Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IOrderService _orderService;

        public AdminController(ApplicationDbContext context, IOrderService orderService)
        {
            _context = context;
            _orderService = orderService;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);

            var orders = await _context.Orders.ToListAsync();
            var users = await _context.Users.ToListAsync();
            var products = await _context.Products.Where(p => p.IsActive).ToListAsync();

            var viewModel = new DashboardViewModel
            {
                TotalSales = orders.Where(o => o.Status != "Cancelled").Sum(o => o.GrandTotal),
                TotalOrders = orders.Count,
                TotalProducts = products.Count,
                TotalCustomers = users.Count,
                RecentOrders = orders
                    .OrderByDescending(o => o.CreatedAt)
                    .Take(10)
                    .Select(o => new RecentOrderViewModel
                    {
                        Id = o.Id,
                        CustomerName = o.ShippingFirstName + " " + o.ShippingLastName,
                        Total = o.GrandTotal,
                        Status = o.Status,
                        CreatedAt = o.CreatedAt
                    })
                    .ToList(),
                TopProducts = await GetTopProductsAsync(),
                SalesChart = GetSalesChartData(startOfMonth, today)
            };

            return View(viewModel);
        }

        private async Task<List<TopProductViewModel>> GetTopProductsAsync()
        {
            var orderItems = await _context.OrderItems
                .Include(oi => oi.Product)
                .GroupBy(oi => oi.ProductId)
                .Select(g => new TopProductViewModel
                {
                    Id = g.Key,
                    Name = g.First().ProductName,
                    Image = g.First().ProductImage,
                    SalesCount = g.Sum(oi => oi.Quantity),
                    Revenue = g.Sum(oi => oi.TotalPrice)
                })
                .OrderByDescending(p => p.SalesCount)
                .Take(5)
                .ToListAsync();

            return orderItems;
        }

        private List<SalesChartViewModel> GetSalesChartData(DateTime start, DateTime end)
        {
            var data = new List<SalesChartViewModel>();
            var orders = _context.Orders
                .Where(o => o.CreatedAt >= start && o.CreatedAt <= end && o.Status != "Cancelled")
                .ToList();

            for (var date = start; date <= end; date = date.AddDays(1))
            {
                var dayOrders = orders.Where(o => o.CreatedAt.Date == date.Date);
                data.Add(new SalesChartViewModel
                {
                    Date = date.ToString("MM/dd"),
                    Sales = dayOrders.Sum(o => o.GrandTotal),
                    Orders = dayOrders.Count()
                });
            }

            return data;
        }

        #region Products Management

        public async Task<IActionResult> Products()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(products);
        }

        public IActionResult CreateProduct()
        {
            ViewBag.Categories = _context.Categories.Where(c => c.IsActive).ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(ProductViewModel model, IFormFile? image)
        {
            if (ModelState.IsValid)
            {
                var product = new Product
                {
                    Name = model.Name,
                    NameAr = model.NameAr,
                    Brand = model.Brand,
                    BrandAr = model.BrandAr,
                    Price = model.Price,
                    OldPrice = model.OldPrice,
                    Description = model.Description,
                    DescriptionAr = model.DescriptionAr,
                    ScentFamily = model.ScentFamily,
                    ScentFamilyAr = model.ScentFamilyAr,
                    Size = model.Size,
                    Gender = model.Gender,
                    Concentration = model.Concentration,
                    StockQuantity = model.StockQuantity,
                    CategoryId = model.CategoryId,
                    IsActive = model.IsActive,
                    IsFeatured = model.IsFeatured,
                    IsNewArrival = model.IsNewArrival,
                    CreatedAt = DateTime.Now
                };

                if (image != null)
                {
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/products", fileName);

                    Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await image.CopyToAsync(stream);
                    }

                    product.ImageUrl = $"/images/products/{fileName}";
                }

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Products));
            }

            ViewBag.Categories = _context.Categories.Where(c => c.IsActive).ToList();
            return View(model);
        }

        public async Task<IActionResult> EditProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();

            var model = new ProductViewModel
            {
                Id = product.Id,
                Name = product.Name,
                NameAr = product.NameAr,
                Brand = product.Brand,
                BrandAr = product.BrandAr,
                Price = product.Price,
                OldPrice = product.OldPrice,
                Description = product.Description,
                DescriptionAr = product.DescriptionAr,
                ImageUrl = product.ImageUrl,
                ScentFamily = product.ScentFamily,
                ScentFamilyAr = product.ScentFamilyAr,
                Size = product.Size,
                Gender = product.Gender,
                Concentration = product.Concentration,
                StockQuantity = product.StockQuantity,
                CategoryId = product.CategoryId,
                IsActive = product.IsActive,
                IsFeatured = product.IsFeatured,
                IsNewArrival = product.IsNewArrival
            };

            ViewBag.Categories = _context.Categories.Where(c => c.IsActive).ToList();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(ProductViewModel model, IFormFile? image)
        {
            if (ModelState.IsValid)
            {
                var product = await _context.Products.FindAsync(model.Id);
                if (product == null)
                    return NotFound();

                product.Name = model.Name;
                product.NameAr = model.NameAr;
                product.Brand = model.Brand;
                product.BrandAr = model.BrandAr;
                product.Price = model.Price;
                product.OldPrice = model.OldPrice;
                product.Description = model.Description;
                product.DescriptionAr = model.DescriptionAr;
                product.ScentFamily = model.ScentFamily;
                product.ScentFamilyAr = model.ScentFamilyAr;
                product.Size = model.Size;
                product.Gender = model.Gender;
                product.Concentration = model.Concentration;
                product.StockQuantity = model.StockQuantity;
                product.CategoryId = model.CategoryId;
                product.IsActive = model.IsActive;
                product.IsFeatured = model.IsFeatured;
                product.IsNewArrival = model.IsNewArrival;
                product.UpdatedAt = DateTime.Now;

                if (image != null)
                {
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/products", fileName);

                    Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await image.CopyToAsync(stream);
                    }

                    product.ImageUrl = $"/images/products/{fileName}";
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Products));
            }

            ViewBag.Categories = _context.Categories.Where(c => c.IsActive).ToList();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                product.IsActive = false;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Products));
        }

        #endregion

        #region Orders Management

        public async Task<IActionResult> Orders(string? status)
        {
            var query = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(o => o.Status == status);
            }

            var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
            return View(orders);
        }

        public async Task<IActionResult> OrderDetails(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound();

            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, string status)
        {
            await _orderService.UpdateOrderStatusAsync(orderId, status);
            return RedirectToAction(nameof(OrderDetails), new { id = orderId });
        }

        public async Task<IActionResult> Invoice(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound();

            return View(order);
        }

        #endregion

        #region Customers Management

        public async Task<IActionResult> Customers()
        {
            var customers = await _context.Users
                .Include(u => u.Orders)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            return View(customers);
        }

        public async Task<IActionResult> CustomerDetails(string id)
        {
            var customer = await _context.Users
                .Include(u => u.Orders)
                .ThenInclude(o => o.OrderItems)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (customer == null)
                return NotFound();

            return View(customer);
        }

        #endregion

        #region Reports

        public async Task<IActionResult> Reports(DateTime? startDate, DateTime? endDate)
        {
            var start = startDate ?? DateTime.Today.AddMonths(-1);
            var end = endDate ?? DateTime.Today;

            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.CreatedAt >= start && o.CreatedAt <= end && o.Status != "Cancelled")
                .ToListAsync();

            var viewModel = new ReportsViewModel
            {
                StartDate = start,
                EndDate = end,
                TotalSales = orders.Sum(o => o.GrandTotal),
                TotalOrders = orders.Count,
                TotalProductsSold = orders.SelectMany(o => o.OrderItems).Sum(oi => oi.Quantity),
                AverageOrderValue = orders.Any() ? orders.Average(o => o.GrandTotal) : 0,
                OrdersByStatus = orders.GroupBy(o => o.Status).ToDictionary(g => g.Key, g => g.Count()),
                SalesByCategory = await GetSalesByCategoryAsync(start, end)
            };

            return View(viewModel);
        }

        private async Task<Dictionary<string, decimal>> GetSalesByCategoryAsync(DateTime start, DateTime end)
        {
            var orderItems = await _context.OrderItems
                .Include(oi => oi.Product)
                .ThenInclude(p => p!.Category)
                .Where(oi => oi.Order!.CreatedAt >= start && oi.Order!.CreatedAt <= end && oi.Order.Status != "Cancelled")
                .ToListAsync();

            return orderItems
                .GroupBy(oi => oi.Product?.Category?.Name ?? "Uncategorized")
                .ToDictionary(g => g.Key, g => g.Sum(oi => oi.TotalPrice));
        }

        #endregion

        #region Coupons

        public async Task<IActionResult> Coupons()
        {
            var coupons = await _context.Coupons.OrderByDescending(c => c.CreatedAt).ToListAsync();
            return View(coupons);
        }

        public IActionResult CreateCoupon()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCoupon(Coupon model)
        {
            if (ModelState.IsValid)
            {
                model.CreatedAt = DateTime.Now;
                _context.Coupons.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Coupons));
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCoupon(int id)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon != null)
            {
                _context.Coupons.Remove(coupon);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Coupons));
        }

        #endregion
    }

    public class ReportsViewModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalSales { get; set; }
        public int TotalOrders { get; set; }
        public int TotalProductsSold { get; set; }
        public decimal AverageOrderValue { get; set; }
        public Dictionary<string, int> OrdersByStatus { get; set; } = new();
        public Dictionary<string, decimal> SalesByCategory { get; set; } = new();
    }
}
