using Microsoft.AspNetCore.Mvc;
using PerfumeStore.Services;
using PerfumeStore.ViewModels;

namespace PerfumeStore.Controllers
{
    public class CartController : Controller
    {
        private readonly ICartService _cartService;
        private readonly IOrderService _orderService;

        public CartController(ICartService cartService, IOrderService orderService)
        {
            _cartService = cartService;
            _orderService = orderService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.Identity?.IsAuthenticated ?? false ? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value : null;
            var sessionId = HttpContext.Session.Id;

            var cart = await _cartService.GetOrCreateCartAsync(userId, sessionId);
            var total = await _cartService.GetCartTotalAsync(userId, sessionId);

            var viewModel = new CartIndexViewModel
            {
                Cart = cart,
                Total = total
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Add(int productId, int quantity = 1)
        {
            try
            {
                var userId = User.Identity?.IsAuthenticated ?? false ? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value : null;
                var sessionId = HttpContext.Session.Id;

                await _cartService.AddToCartAsync(userId, sessionId, productId, quantity);

                return Json(new { success = true, count = await _cartService.GetCartItemCountAsync(userId, sessionId) });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Update(int cartItemId, int quantity)
        {
            var userId = User.Identity?.IsAuthenticated ?? false ? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value : null;
            var sessionId = HttpContext.Session.Id;

            await _cartService.UpdateCartItemAsync(userId, sessionId, cartItemId, quantity);

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Remove(int cartItemId)
        {
            var userId = User.Identity?.IsAuthenticated ?? false ? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value : null;
            var sessionId = HttpContext.Session.Id;

            await _cartService.RemoveFromCartAsync(userId, sessionId, cartItemId);

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Checkout()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
                return RedirectToAction("Login", "Account", new { returnUrl = "/Cart/Checkout" });

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var sessionId = HttpContext.Session.Id;

            var cart = await _cartService.GetOrCreateCartAsync(userId, sessionId);

            if (!cart.CartItems.Any())
            {
                TempData["Error"] = "Your cart is empty";
                return RedirectToAction(nameof(Index));
            }

            return View(new CheckoutViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return RedirectToAction("Login", "Account");

            try
            {
                var order = await _orderService.CreateOrderAsync(userId, model.CouponCode);

                // Update shipping info if provided
                order.ShippingFirstName = model.FirstName;
                order.ShippingLastName = model.LastName;
                order.ShippingAddress = model.Address;
                order.ShippingCity = model.City;
                order.ShippingPostalCode = model.PostalCode;
                order.ShippingCountry = model.Country;
                order.ShippingPhone = model.Phone;
                order.Notes = model.Notes;
                order.PaymentMethod = model.PaymentMethod;

                TempData["Success"] = "Order placed successfully!";
                return RedirectToAction("OrderConfirmation", new { orderId = order.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
        }

        public async Task<IActionResult> OrderConfirmation(int orderId)
        {
            var order = await _orderService.GetOrderByIdAsync(orderId);
            if (order == null)
                return NotFound();

            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> ValidateCoupon(string code)
        {
            // Implementation for coupon validation
            return Json(new { valid = true, discount = 20, message = "Coupon applied successfully!" });
        }

        public async Task<IActionResult> GetCartCount()
        {
            var userId = User.Identity?.IsAuthenticated ?? false ? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value : null;
            var sessionId = HttpContext.Session.Id;

            var count = await _cartService.GetCartItemCountAsync(userId, sessionId);
            return Json(new { count });
        }
    }

    public class CartIndexViewModel
    {
        public PerfumeStore.Models.Cart Cart { get; set; } = new();
        public decimal Total { get; set; }
    }
}
