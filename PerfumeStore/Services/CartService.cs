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

        // التعديل: إرجاع العدد الجديد مباشرة
        Task<int> AddToCartAsync(string? userId, string sessionId, int productId, int quantity);

        Task UpdateCartItemAsync(string? userId, string sessionId, int cartItemId, int quantity);
        Task RemoveFromCartAsync(string? userId, string sessionId, int cartItemId);
        Task ClearCartAsync(string? userId, string sessionId);
        Task<decimal> GetCartTotalAsync(string? userId, string sessionId);
        Task<int> GetCartItemCountAsync(string? userId, string sessionId);
        Task<CartTotalsResult> CalculateCartTotalsAsync(string? userId, string sessionId, string? couponCode, int? shippingZoneId);
    }

    public class CartTotalsResult
    {
        public decimal Subtotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal GrandTotal { get; set; }
        public string? AppliedCouponCode { get; set; }
        public string? Message { get; set; }
        public bool IsCouponValid { get; set; }
    }

    public class CartService : ICartService
    {
        private readonly ApplicationDbContext _context;

        public CartService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Cart?> GetCartAsync(string? userId, string sessionId)
        {
            IQueryable<Cart> query = _context.Carts
                .Include(c => c.Items)
                .ThenInclude(ci => ci.Product);

            if (!string.IsNullOrEmpty(userId))
            {
                return await query.FirstOrDefaultAsync(c => c.UserId == userId);
            }
            return await query.FirstOrDefaultAsync(c => c.SessionId == sessionId);
        }

        public async Task<Cart> GetOrCreateCartAsync(string? userId, string sessionId)
        {
            var cart = await GetCartAsync(userId, sessionId);
            if (cart == null)
            {
                // إنشاء سلة جديدة
                cart = new Cart
                {
                    UserId = userId,
                    SessionId = sessionId,
                    CreatedAt = DateTime.Now
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }
            // إذا سجل المستخدم دخوله ولديه سلة زائر، نربطها به
            else if (!string.IsNullOrEmpty(userId) && string.IsNullOrEmpty(cart.UserId))
            {
                cart.UserId = userId;
                _context.Carts.Update(cart);
                await _context.SaveChangesAsync();
            }
            return cart;
        }

        public async Task<int> AddToCartAsync(string? userId, string sessionId, int productId, int quantity)
        {
            var cart = await GetOrCreateCartAsync(userId, sessionId);

            var product = await _context.Products.FindAsync(productId);
            if (product == null || !product.IsActive)
                throw new Exception("المنتج غير متوفر");

            // التحقق من المخزون
            if (product.StockQuantity < quantity)
                throw new Exception("الكمية المطلوبة غير متوفرة");

            var cartItem = cart.Items.FirstOrDefault(i => i.ProductId == productId);

            if (cartItem != null)
            {
                cartItem.Quantity += quantity;
                // تحديث السعر في حال تغير
                cartItem.UnitPrice = product.Price;
                _context.CartItems.Update(cartItem);
            }
            else
            {
                cartItem = new CartItem
                {
                    CartId = cart.Id,
                    ProductId = productId,
                    Quantity = quantity,
                    UnitPrice = product.Price,
                    AddedAt = DateTime.Now
                };
                _context.CartItems.Add(cartItem);
            }

            cart.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            // إرجاع العدد الجديد ليتم تحديث الواجهة فوراً
            // ملاحظة: نعيد جلب العدد من الداتا بيز لضمان الدقة
            return await _context.CartItems.Where(c => c.CartId == cart.Id).SumAsync(i => i.Quantity);
        }

        public async Task UpdateCartItemAsync(string? userId, string sessionId, int cartItemId, int quantity)
        {
            var cart = await GetCartAsync(userId, sessionId);
            if (cart == null) return;

            var item = cart.Items.FirstOrDefault(i => i.Id == cartItemId);
            if (item != null)
            {
                if (quantity > 0)
                {
                    // التحقق من المخزون
                    if (item.Product != null && item.Product.StockQuantity < quantity)
                        quantity = item.Product.StockQuantity;

                    item.Quantity = quantity;
                    _context.CartItems.Update(item);
                }
                else
                {
                    _context.CartItems.Remove(item);
                }
                cart.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }

        public async Task RemoveFromCartAsync(string? userId, string sessionId, int cartItemId)
        {
            var cart = await GetCartAsync(userId, sessionId);
            if (cart == null) return;

            var item = cart.Items.FirstOrDefault(i => i.Id == cartItemId);
            if (item != null)
            {
                _context.CartItems.Remove(item);
                cart.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }

        public async Task ClearCartAsync(string? userId, string sessionId)
        {
            var cart = await GetCartAsync(userId, sessionId);
            if (cart != null && cart.Items.Any())
            {
                _context.CartItems.RemoveRange(cart.Items);
                cart.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<decimal> GetCartTotalAsync(string? userId, string sessionId)
        {
            var cart = await GetCartAsync(userId, sessionId);
            return cart?.Items.Sum(i => i.Quantity * i.UnitPrice) ?? 0;
        }

        public async Task<int> GetCartItemCountAsync(string? userId, string sessionId)
        {
            var cart = await GetCartAsync(userId, sessionId);
            return cart?.Items.Sum(i => i.Quantity) ?? 0;
        }

        public async Task<CartTotalsResult> CalculateCartTotalsAsync(string? userId, string sessionId, string? couponCode, int? shippingZoneId)
        {
            var result = new CartTotalsResult();
            var cart = await GetCartAsync(userId, sessionId);

            if (cart == null || !cart.Items.Any()) return result;

            result.Subtotal = cart.Items.Sum(i => i.Quantity * i.UnitPrice);

            if (shippingZoneId.HasValue && shippingZoneId.Value > 0)
            {
                var zone = await _context.ShippingZones.FindAsync(shippingZoneId.Value);
                if (zone != null) result.ShippingCost = zone.Cost;
            }

            if (!string.IsNullOrWhiteSpace(couponCode))
            {
                var cleanCode = couponCode.Trim().ToUpper();
                var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code == cleanCode && c.IsActive);

                if (coupon != null)
                {
                    var now = DateTime.Now;
                    // تمديد الصلاحية لنهاية اليوم
                    var expiry = coupon.EndDate?.Date.AddDays(1).AddSeconds(-1) ?? DateTime.MaxValue;

                    bool validDate = now >= coupon.StartDate && now <= expiry;
                    bool validUsage = !coupon.UsageLimit.HasValue || coupon.UsedCount < coupon.UsageLimit.Value;
                    decimal minOrder = coupon.MinimumOrderAmount ?? coupon.MinOrderAmount ?? 0;
                    bool validMinOrder = result.Subtotal >= minOrder;

                    if (validDate && validUsage && validMinOrder)
                    {
                        result.IsCouponValid = true;
                        result.AppliedCouponCode = cleanCode;

                        if (coupon.DiscountType == "Percentage")
                        {
                            result.DiscountAmount = (result.Subtotal * coupon.DiscountValue) / 100;
                            decimal maxDisc = coupon.MaximumDiscountAmount ?? coupon.MaxDiscount ?? decimal.MaxValue;
                            if (result.DiscountAmount > maxDisc) result.DiscountAmount = maxDisc;
                        }
                        else
                        {
                            result.DiscountAmount = coupon.DiscountValue;
                        }

                        if (result.DiscountAmount > result.Subtotal) result.DiscountAmount = result.Subtotal;
                        result.Message = "تم تطبيق الكوبون";
                    }
                    else
                    {
                        result.Message = !validMinOrder ? $"الحد الأدنى {minOrder} ريال" : "الكوبون غير صالح";
                    }
                }
                else
                {
                    result.Message = "الكوبون غير صحيح";
                }
            }

            result.GrandTotal = (result.Subtotal - result.DiscountAmount) + result.ShippingCost;
            if (result.GrandTotal < 0) result.GrandTotal = 0;

            return result;
        }
    }
}