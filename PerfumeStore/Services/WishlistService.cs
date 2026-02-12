using Microsoft.EntityFrameworkCore;
using PerfumeStore.Data;
using PerfumeStore.Models;

namespace PerfumeStore.Services
{
    public interface IWishlistService
    {
        Task<List<WishlistItem>> GetUserWishlistAsync(string userId);
        Task<bool> IsInWishlistAsync(string userId, int productId);
        Task ToggleWishlistAsync(string userId, int productId);
        Task RemoveFromWishlistAsync(string userId, int productId);
    }

    public class WishlistService : IWishlistService
    {
        private readonly ApplicationDbContext _context;

        public WishlistService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<WishlistItem>> GetUserWishlistAsync(string userId)
        {
            return await _context.WishlistItems
                .Include(w => w.Product)
                .Where(w => w.UserId == userId)
                .OrderByDescending(w => w.AddedAt)
                .ToListAsync();
        }

        public async Task<bool> IsInWishlistAsync(string userId, int productId)
        {
            return await _context.WishlistItems
                .AnyAsync(w => w.UserId == userId && w.ProductId == productId);
        }

        public async Task ToggleWishlistAsync(string userId, int productId)
        {
            var existingItem = await _context.WishlistItems
                .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

            if (existingItem != null)
            {
                _context.WishlistItems.Remove(existingItem);
            }
            else
            {
                var newItem = new WishlistItem
                {
                    UserId = userId,
                    ProductId = productId,
                    AddedAt = DateTime.Now
                };
                _context.WishlistItems.Add(newItem);
            }

            await _context.SaveChangesAsync();
        }

        public async Task RemoveFromWishlistAsync(string userId, int productId)
        {
            var item = await _context.WishlistItems
                .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

            if (item != null)
            {
                _context.WishlistItems.Remove(item);
                await _context.SaveChangesAsync();
            }
        }
    }
}
