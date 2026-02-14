using Microsoft.EntityFrameworkCore;
using PerfumeStore.Data;
using PerfumeStore.Models;
using PerfumeStore.ViewModels;

namespace PerfumeStore.Services
{
    public interface ICartService
    {
        Task<Cart?> GetCartAsync(string? userId, string sessionId);
        Task<Cart> GetOrCreateCartAsync(string? userId, string sessionId);
        Task AddToCartAsync(string? userId, string sessionId, int productId, int quantity);
        Task UpdateCartItemAsync(string? userId, string sessionId, int cartItemId, int quantity);
        Task RemoveFromCartAsync(string? userId, string sessionId, int cartItemId);
        Task ClearCartAsync(string? userId, string sessionId);
        Task<decimal> GetCartTotalAsync(string? userId, string sessionId);
        Task<int> GetCartItemCountAsync(string? userId, string sessionId);
        Task<(decimal discount, string? error)> ValidateCouponAsync(string code, decimal subtotal);
    }

    public class CartService : ICartService
    {
        private readonly ApplicationDbContext _context;

        public CartService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Cart> GetOrCreateCartAsync(string? userId, string sessionId)
        {
            Cart? cart = null;

            if (!string.IsNullOrEmpty(userId))
            {
                cart = await _context.Carts
                    .Include(c => c.Items)
                    .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(c => c.UserId == userId);
            }

            if (cart == null)
            {
                cart = await _context.Carts
                    .Include(c => c.Items)
                    .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(c => c.SessionId == sessionId);
            }

            if (cart == null)
            {
                cart = new Cart
                {
                    UserId = userId,
                    SessionId = sessionId,
                    CreatedAt = DateTime.Now
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }
            else if (!string.IsNullOrEmpty(userId) && string.IsNullOrEmpty(cart.UserId))
            {
                cart.UserId = userId;
                await _context.SaveChangesAsync();
            }

            return cart;
        }

        public async Task AddToCartAsync(string? userId, string sessionId, int productId, int quantity)
        {
            var cart = await GetOrCreateCartAsync(userId, sessionId);
            var product = await _context.Products.FindAsync(productId);

            if (product == null || !product.IsActive)
                throw new Exception("Product not available");

            if (product.StockQuantity < quantity)
                throw new Exception("Insufficient stock");

            var existingItem = cart.Items.FirstOrDefault(ci => ci.ProductId == productId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
                existingItem.UnitPrice = product.Price;
            }
            else
            {
                var cartItem = new CartItem
                {
                    CartId = cart.Id,
                    ProductId = productId,
                    Quantity = quantity,
                    UnitPrice = product.Price
                };
                _context.CartItems.Add(cartItem);
            }

            cart.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
        }

        public async Task UpdateCartItemAsync(string? userId, string sessionId, int cartItemId, int quantity)
        {
            var cart = await GetOrCreateCartAsync(userId, sessionId);
            var cartItem = cart.Items.FirstOrDefault(ci => ci.Id == cartItemId);

            if (cartItem != null)
            {
                if (quantity <= 0)
                {
                    _context.CartItems.Remove(cartItem);
                }
                else
                {
                    if (cartItem.Product != null && cartItem.Product.StockQuantity < quantity)
                    {
                        quantity = cartItem.Product.StockQuantity;
                    }
                    cartItem.Quantity = quantity;
                }

                cart.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }

        public async Task RemoveFromCartAsync(string? userId, string sessionId, int cartItemId)
        {
            var cart = await GetOrCreateCartAsync(userId, sessionId);
            var cartItem = cart.Items.FirstOrDefault(ci => ci.Id == cartItemId);

            if (cartItem != null)
            {
                _context.CartItems.Remove(cartItem);
                cart.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }

        public async Task ClearCartAsync(string? userId, string sessionId)
        {
            var cart = await GetOrCreateCartAsync(userId, sessionId);
            if (cart.Items.Any())
            {
                _context.CartItems.RemoveRange(cart.Items);
                cart.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<decimal> GetCartTotalAsync(string? userId, string sessionId)
        {
            var cart = await GetOrCreateCartAsync(userId, sessionId);
            return cart.Items.Sum(ci => ci.Quantity * ci.UnitPrice);
        }

        public async Task<int> GetCartItemCountAsync(string? userId, string sessionId)
        {
            var cart = await GetOrCreateCartAsync(userId, sessionId);
            return cart.Items.Sum(ci => ci.Quantity);
        }

        public async Task<Cart?> GetCartAsync(string? userId, string sessionId)
        {
            Cart? cart = null;

            if (!string.IsNullOrEmpty(userId))
            {
                cart = await _context.Carts
                    .Include(c => c.Items)
                    .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(c => c.UserId == userId);
            }

            if (cart == null)
            {
                cart = await _context.Carts
                    .Include(c => c.Items)
                    .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(c => c.SessionId == sessionId);
            }

            return cart;
        }

        public async Task<(decimal discount, string? error)> ValidateCouponAsync(string code, decimal subtotal)
        {
            var coupon = await _context.Coupons
                .FirstOrDefaultAsync(c => c.Code == code.ToUpper());

            if (coupon == null)
                return (0, "Invalid coupon code");

            if (!coupon.IsValid)
                return (0, "Coupon has expired");

            if (coupon.MinOrderAmount.HasValue && subtotal < coupon.MinOrderAmount.Value)
                return (0, $"Minimum order amount is {coupon.MinOrderAmount.Value} OMR");

            decimal discount = coupon.DiscountType == "Percentage"
                ? subtotal * coupon.DiscountValue / 100
                : coupon.DiscountValue;

            if (coupon.MaxDiscount.HasValue && discount > coupon.MaxDiscount.Value)
                discount = coupon.MaxDiscount.Value;

            return (discount, null);
        }
    }
}