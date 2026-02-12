using PerfumeStore.Data;
using PerfumeStore.Models;
using Microsoft.EntityFrameworkCore;

namespace PerfumeStore.Services
{
    public interface IOrderService
    {
        Task<Order> CreateOrderAsync(string userId, string? couponCode);
        Task<Order?> GetOrderByIdAsync(int orderId);
        Task<List<Order>> GetUserOrdersAsync(string userId);
        Task UpdateOrderStatusAsync(int orderId, string status);
        Task<List<Order>> GetAllOrdersAsync();
        Task<Dictionary<string, decimal>> GetSalesReportAsync(DateTime startDate, DateTime endDate);
    }

    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICartService _cartService;
        private readonly IEmailService _emailService;

        public OrderService(ApplicationDbContext context, ICartService cartService, IEmailService emailService)
        {
            _context = context;
            _cartService = cartService;
            _emailService = emailService;
        }

        public async Task<Order> CreateOrderAsync(string userId, string? couponCode)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new Exception("User not found");

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null || !cart.CartItems.Any())
                throw new Exception("Cart is empty");

            decimal subtotal = cart.CartItems.Sum(ci => ci.Quantity * ci.UnitPrice);
            decimal discount = 0;
            decimal shipping = subtotal > 200 ? 0 : 15;
            decimal tax = subtotal * 0.05m; // 5% tax
            decimal total = subtotal + shipping + tax;

            // Apply coupon
            if (!string.IsNullOrEmpty(couponCode))
            {
                var coupon = await _context.Coupons
                    .FirstOrDefaultAsync(c => c.Code == couponCode && c.IsActive);

                if (coupon != null && coupon.StartDate <= DateTime.Now && coupon.EndDate >= DateTime.Now)
                {
                    if (coupon.DiscountType == "Percentage")
                    {
                        discount = subtotal * (coupon.DiscountValue / 100);
                    }
                    else
                    {
                        discount = coupon.DiscountValue;
                    }

                    if (coupon.MaximumDiscountAmount.HasValue)
                    {
                        discount = Math.Min(discount, coupon.MaximumDiscountAmount.Value);
                    }

                    coupon.UsedCount++;
                }
            }

            total = subtotal - discount + shipping + tax;

            var order = new Order
            {
                UserId = userId,
                TotalAmount = subtotal,
                DiscountAmount = discount,
                ShippingCost = shipping,
                TaxAmount = tax,
                GrandTotal = total,
                Status = "Pending",
                PaymentStatus = "Pending",
                ShippingFirstName = user.FirstName,
                ShippingLastName = user.LastName,
                ShippingAddress = user.Address ?? "N/A",
                ShippingCity = user.City ?? "N/A",
                ShippingCountry = user.Country ?? "N/A",
                ShippingPhone = user.PhoneNumber ?? "N/A",
                CouponCode = couponCode,
                CreatedAt = DateTime.Now
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Add order items
            foreach (var cartItem in cart.CartItems)
            {
                var orderItem = new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = cartItem.ProductId,
                    ProductName = cartItem.Product!.Name,
                    UnitPrice = cartItem.UnitPrice,
                    Quantity = cartItem.Quantity,
                    TotalPrice = cartItem.Quantity * cartItem.UnitPrice,
                    ProductImage = cartItem.Product.ImageUrl
                };

                _context.OrderItems.Add(orderItem);

                // Update stock
                cartItem.Product.StockQuantity -= cartItem.Quantity;
            }

            await _context.SaveChangesAsync();

            // Clear cart
            _context.CartItems.RemoveRange(cart.CartItems);
            await _context.SaveChangesAsync();

            // Send confirmation email
            await _emailService.SendOrderConfirmationAsync(user.Email!, order.Id, total);

            return order;
        }

        public async Task<Order?> GetOrderByIdAsync(int orderId)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == orderId);
        }

        public async Task<List<Order>> GetUserOrdersAsync(string userId)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task UpdateOrderStatusAsync(int orderId, string status)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null)
            {
                order.Status = status;

                if (status == "Shipped")
                    order.ShippedAt = DateTime.Now;
                else if (status == "Delivered")
                    order.DeliveredAt = DateTime.Now;

                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<Order>> GetAllOrdersAsync()
        {
            return await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<Dictionary<string, decimal>> GetSalesReportAsync(DateTime startDate, DateTime endDate)
        {
            var orders = await _context.Orders
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate && o.Status != "Cancelled")
                .ToListAsync();

            return new Dictionary<string, decimal>
            {
                { "TotalSales", orders.Sum(o => o.GrandTotal) },
                { "TotalOrders", orders.Count },
                { "AverageOrderValue", orders.Any() ? orders.Average(o => o.GrandTotal) : 0 },
                { "TotalDiscounts", orders.Sum(o => o.DiscountAmount) },
                { "TotalShipping", orders.Sum(o => o.ShippingCost) },
                { "TotalTax", orders.Sum(o => o.TaxAmount) }
            };
        }
    }
}
