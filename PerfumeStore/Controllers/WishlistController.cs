using Microsoft.AspNetCore.Mvc;
using PerfumeStore.Services;
using PerfumeStore.Data;
using PerfumeStore.Models;
using System.Globalization;
using Microsoft.EntityFrameworkCore;

namespace PerfumeStore.Controllers
{
    public class WishlistController : Controller
    {
        private readonly IWishlistService _wishlistService;
        private readonly ApplicationDbContext _context;

        public WishlistController(IWishlistService wishlistService, ApplicationDbContext context)
        {
            _wishlistService = wishlistService;
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
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var wishlist = await _wishlistService.GetUserWishlistAsync(userId);
            return View(wishlist);
        }

        [HttpPost]
        public async Task<IActionResult> Toggle(int productId)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = IsArabic ? "يرجى تسجيل الدخول" : "Please login" });
            }

            await _wishlistService.ToggleWishlistAsync(userId, productId);
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> Remove(int productId)
        {
            var userId = GetUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                await _wishlistService.RemoveFromWishlistAsync(userId, productId);
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Check(int productId)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { inWishlist = false });
            }

            var inWishlist = await _wishlistService.IsInWishlistAsync(userId, productId);
            return Json(new { inWishlist });
        }
    }
}
