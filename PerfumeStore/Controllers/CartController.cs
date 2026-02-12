using Microsoft.AspNetCore.Mvc;
using PerfumeStore.Services;
using PerfumeStore.ViewModels;
using PerfumeStore.Data;
using PerfumeStore.Models;
using System.Globalization;
using Microsoft.EntityFrameworkCore;

namespace PerfumeStore.Controllers
{
    public class CartController : Controller
    {
        private readonly ICartService _cartService;
        private readonly IOrderService _orderService;
        private readonly ApplicationDbContext _context;

        public CartController(ICartService cartService, IOrderService orderService, ApplicationDbContext context)
        {
            _cartService = cartService;
            _orderService = orderService;
            _context = context;
        }

        private bool IsArabic => CultureInfo.CurrentUICulture.Name.StartsWith("ar");

        private string? GetUserId()
        {
            return User.Identity?.IsAuthenticated ?? false 
                ? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                : null;
        }

        public async Task<IActionResult> Index()
        {
            var userId = GetUserId();
            var sessionId = HttpContext.Session.Id;

            var cart = await _cartService.GetOrCreateCartAsync(userId, sessionId);
            var total = await _cartService.GetCartTotalAsync(userId, sessionId);

            return View(new CartIndexViewModel { Cart = cart, Subtotal = cart.Subtotal, Total = total });
        }

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] CartAddRequest request)
        {
            try
            {
                if (request == null || request.ProductId <= 0)
                    return Json(new { success = false, message = IsArabic ? "بيانات غير صالحة" : "Invalid data" });

                var userId = GetUserId();
                var sessionId = HttpContext.Session.Id;

                await _cartService.AddToCartAsync(userId, sessionId, request.ProductId, request.Quantity <= 0 ? 1 : request.Quantity);
                var count = await _cartService.GetCartItemCountAsync(userId, sessionId);

                return Json(new { success = true, count = count });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Update(int cartItemId, int quantity)
        {
            var userId = GetUserId();
            var sessionId = HttpContext.Session.Id;
            await _cartService.UpdateCartItemAsync(userId, sessionId, cartItemId, quantity);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Remove(int cartItemId)
        {
            var userId = GetUserId();
            var sessionId = HttpContext.Session.Id;
            await _cartService.RemoveFromCartAsync(userId, sessionId, cartItemId);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> GetCartCount()
        {
            var userId = GetUserId();
            var sessionId = HttpContext.Session.Id;
            var count = await _cartService.GetCartItemCountAsync(userId, sessionId);
            return Json(new { count });
        }

        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var userId = GetUserId();
            var sessionId = HttpContext.Session.Id;

            var cart = await _cartService.GetOrCreateCartAsync(userId, sessionId);
            if (!cart.Items.Any())
            {
                TempData["Error"] = IsArabic ? "السلة فارغة" : "Cart is empty";
                return RedirectToAction(nameof(Index));
            }

            var model = new CheckoutViewModel();
            if (!string.IsNullOrEmpty(userId))
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    model.UserId = userId;
                    model.FirstName = user.FirstName;
                    model.LastName = user.LastName;
                    model.Email = user.Email ?? "";
                    model.Phone = user.PhoneNumber ?? "";
                    model.Address = user.Address ?? "";
                    model.City = user.City ?? "";
                    model.PostalCode = user.PostalCode;
                    model.Country = user.Country ?? "Oman";
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userId = GetUserId();
            var sessionId = HttpContext.Session.Id;

            model.UserId = userId;

            try
            {
                var order = await _orderService.CreateOrderAsync(userId, sessionId, model);
                return RedirectToAction("OrderConfirmation", new { id = order.Id });
            }
            catch (Exception)
            {
                ModelState.AddModelError("", IsArabic ? "حدث خطأ أثناء معالجة الطلب" : "An error occurred while processing your order");
                return View(model);
            }
        }

        public async Task<IActionResult> OrderConfirmation(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound();

            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> ValidateCoupon(string code)
        {
            if (string.IsNullOrEmpty(code))
                return Json(new { valid = false, message = IsArabic ? "يرجى إدخال كود الخصم" : "Please enter coupon code" });

            var coupon = await _context.Coupons
                .FirstOrDefaultAsync(c => c.Code == code.ToUpper() && c.IsActive);

            if (coupon == null)
            {
                return Json(new { valid = false, message = IsArabic ? "كود الخصم غير صالح" : "Invalid coupon code" });
            }

            if (!coupon.IsValid)
            {
                return Json(new { valid = false, message = IsArabic ? "كود الخصم منتهي الصلاحية" : "Coupon expired" });
            }

            var message = coupon.DiscountType == "Percentage"
                ? (IsArabic ? $"خصم {coupon.DiscountValue}%" : $"{coupon.DiscountValue}% discount")
                : (IsArabic ? $"خصم {coupon.DiscountValue} ريال" : $"{coupon.DiscountValue} OMR discount");

            return Json(new { valid = true, message = message });
        }
    }

    public class CartAddRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
