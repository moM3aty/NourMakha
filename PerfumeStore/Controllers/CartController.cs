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
        private string? GetUserId() => User.Identity?.IsAuthenticated ?? false ? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value : null;

        public async Task<IActionResult> Index()
        {
            var u = GetUserId();
            var s = HttpContext.Session.Id;
            var c = await _cartService.GetOrCreateCartAsync(u, s);
            var t = await _cartService.GetCartTotalAsync(u, s);
            return View(new CartIndexViewModel { Cart = c, Subtotal = c.Subtotal, Total = t });
        }

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] CartAddRequest request)
        {
            try
            {
                if (request == null || request.ProductId <= 0)
                    return Json(new { success = false, message = IsArabic ? "بيانات المنتج غير صحيحة" : "Invalid product data" });

                var userId = GetUserId();

                // تثبيت الجلسة (مهم جداً لعدم تصفير السلة)
                HttpContext.Session.SetString("SessionInitialized", "true");

                var sessionId = HttpContext.Session.Id;

                // الإضافة للسلة والحصول على العدد المحدث
                var newCount = await _cartService.AddToCartAsync(userId, sessionId, request.ProductId, request.Quantity <= 0 ? 1 : request.Quantity);

                return Json(new { success = true, count = newCount });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost] public async Task<IActionResult> Update(int cartItemId, int quantity) { await _cartService.UpdateCartItemAsync(GetUserId(), HttpContext.Session.Id, cartItemId, quantity); return RedirectToAction(nameof(Index)); }
        [HttpPost] public async Task<IActionResult> Remove(int cartItemId) { await _cartService.RemoveFromCartAsync(GetUserId(), HttpContext.Session.Id, cartItemId); return RedirectToAction(nameof(Index)); }

        public async Task<IActionResult> GetCartCount()
        {
            return Json(new { count = await _cartService.GetCartItemCountAsync(GetUserId(), HttpContext.Session.Id) });
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
            var sessionCoupon = HttpContext.Session.GetString("AppliedCoupon");
            var totals = await _cartService.CalculateCartTotalsAsync(userId, sessionId, sessionCoupon, null);
            ViewBag.CartSubtotal = totals.Subtotal;
            ViewBag.SavedCoupon = sessionCoupon;
            var model = new CheckoutViewModel();
            if (!string.IsNullOrEmpty(sessionCoupon)) model.CouponCode = sessionCoupon;
            if (!string.IsNullOrEmpty(userId))
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    model.UserId = userId; model.FirstName = user.FirstName; model.LastName = user.LastName; model.Email = user.Email ?? ""; model.Phone = user.PhoneNumber ?? ""; model.Address = user.Address ?? ""; model.City = user.City ?? ""; model.PostalCode = user.PostalCode; model.Country = user.Country ?? "Oman";
                }
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var userId = GetUserId();
            var sessionId = HttpContext.Session.Id;

            // تحديد الكوبون النهائي
            var finalCoupon = !string.IsNullOrWhiteSpace(model.CouponCode) ? model.CouponCode : HttpContext.Session.GetString("AppliedCoupon");

            try
            {
                // إنشاء الطلب والحصول على كائن النتيجة الذي يدعم التوجيه
                var result = await _orderService.CreateOrderAsync(userId, sessionId, model, finalCoupon);
                HttpContext.Session.Remove("AppliedCoupon");

                // إذا كان الدفع بالفيزا، نتوجه لصفحة الـ Gateway
                if (result.IsPaymentRequired && !string.IsNullOrEmpty(result.RedirectUrl))
                {
                    return Redirect(result.RedirectUrl);
                }

                // إذا كان الدفع عند الاستلام، نتوجه لصفحة التأكيد مباشرة
                return RedirectToAction("OrderConfirmation", new { id = result.Order.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", IsArabic ? "حدث خطأ: " + ex.Message : "Error: " + ex.Message);
                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> ValidateCoupon(string code)
        {
            var result = await _cartService.CalculateCartTotalsAsync(GetUserId(), HttpContext.Session.Id, code, null);
            if (result.IsCouponValid) { HttpContext.Session.SetString("AppliedCoupon", code.ToUpper()); return Json(new { valid = true, message = IsArabic ? "تم تطبيق الخصم" : "Coupon Applied", discountAmount = result.DiscountAmount }); }
            else { HttpContext.Session.Remove("AppliedCoupon"); return Json(new { valid = false, message = result.Message ?? "Invalid" }); }
        }

        public async Task<IActionResult> OrderConfirmation(int id) { var o = await _context.Orders.Include(x => x.OrderItems).Include(x => x.User).FirstOrDefaultAsync(x => x.Id == id); return o == null ? NotFound() : View(o); }
    }

    public class CartAddRequest { public int ProductId { get; set; } public int Quantity { get; set; } }
}