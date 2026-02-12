using PerfumeStore.Data;
using PerfumeStore.Models;
using Microsoft.EntityFrameworkCore;

namespace PerfumeStore.Services
{
    public interface ICartService
    {
        Task<Cart> GetOrCreateCartAsync(string? userId, string sessionId);
        Task AddToCartAsync(string? userId, string sessionId, int productId, int quantity);
        Task UpdateCartItemAsync(string? userId, string sessionId, int cartItemId, int quantity);
        Task RemoveFromCartAsync(string? userId, string sessionId, int cartItemId);
        Task ClearCartAsync(string? userId, string sessionId);
        Task<decimal> GetCartTotalAsync(string? userId, string sessionId);
        Task<int> GetCartItemCountAsync(string? userId, string sessionId);
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
                    .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(c => c.UserId == userId);
            }

            if (cart == null)
            {
                cart = await _context.Carts
                    .Include(c => c.CartItems)
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

            return cart;
        }

        public async Task AddToCartAsync(string? userId, string sessionId, int productId, int quantity)
        {
            var cart = await GetOrCreateCartAsync(userId, sessionId);
            var product = await _context.Products.FindAsync(productId);

            if (product == null || !product.IsActive || product.StockQuantity < quantity)
                throw new Exception("Product not available");

            var existingItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);

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
            var cartItem = cart.CartItems.FirstOrDefault(ci => ci.Id == cartItemId);

            if (cartItem != null)
            {
                if (quantity <= 0)
                {
                    _context.CartItems.Remove(cartItem);
                }
                else
                {
                    cartItem.Quantity = quantity;
                }

                cart.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }

        public async Task RemoveFromCartAsync(string? userId, string sessionId, int cartItemId)
        {
            var cart = await GetOrCreateCartAsync(userId, sessionId);
            var cartItem = cart.CartItems.FirstOrDefault(ci => ci.Id == cartItemId);

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
            _context.CartItems.RemoveRange(cart.CartItems);
            cart.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
        }

        public async Task<decimal> GetCartTotalAsync(string? userId, string sessionId)
        {
            var cart = await GetOrCreateCartAsync(userId, sessionId);
            return cart.CartItems.Sum(ci => ci.Quantity * ci.UnitPrice);
        }

        public async Task<int> GetCartItemCountAsync(string? userId, string sessionId)
        {
            var cart = await GetOrCreateCartAsync(userId, sessionId);
            return cart.CartItems.Sum(ci => ci.Quantity);
        }
    }
}
