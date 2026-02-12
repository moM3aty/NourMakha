using Microsoft.EntityFrameworkCore;
using PerfumeStore.Data;
using PerfumeStore.Models;
using PerfumeStore.ViewModels;

namespace PerfumeStore.Services
{
    public interface IOrderService
    {
        Task<Order?> GetOrderByIdAsync(int id);
        Task<Order?> GetOrderByNumberAsync(string orderNumber);
        Task<List<Order>> GetUserOrdersAsync(string userId);
        Task<List<Order>> GetAllOrdersAsync();
        Task<Order> CreateOrderAsync(string? userId, string? sessionId, CheckoutViewModel model, string? couponCode = null);
        Task UpdateOrderStatusAsync(int orderId, string status);
        Task CancelOrderAsync(int orderId);
    }

    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICartService _cartService;

        public OrderService(ApplicationDbContext context, ICartService cartService)
        {
            _context = context;
            _cartService = cartService;
        }

        public async Task<Order?> GetOrderByIdAsync(int id)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<Order?> GetOrderByNumberAsync(string orderNumber)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
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

        public async Task<List<Order>> GetAllOrdersAsync()
        {
            return await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<Order> CreateOrderAsync(string? userId, string? sessionId, CheckoutViewModel model, string? couponCode = null)
        {
            var cart = await _cartService.GetCartAsync(userId, sessionId);
            if (cart == null || !cart.Items.Any())
                throw new InvalidOperationException("Cart is empty");

            var orderNumber = GenerateOrderNumber();
            var subtotal = cart.Items.Sum(i => i.Total);

            var order = new Order
            {
                OrderNumber = orderNumber,
                UserId = userId,
                ShippingFirstName = model.FirstName,
                ShippingLastName = model.LastName,
                ShippingEmail = model.Email,
                ShippingAddress = model.Address,
                ShippingCity = model.City,
                ShippingPostalCode = model.PostalCode,
                ShippingCountry = model.Country,
                ShippingPhone = model.Phone,
                Notes = model.Notes,
                Subtotal = subtotal,
                ShippingCost = subtotal >= 100 ? 0 : 10,
                PaymentMethod = model.PaymentMethod,
                Status = "Pending",
                CreatedAt = DateTime.Now
            };

            if (!string.IsNullOrEmpty(couponCode))
            {
                var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code == couponCode && c.IsValid);
                if (coupon != null)
                {
                    order.CouponCode = couponCode;
                    decimal discount = coupon.DiscountType == "Percentage"
                        ? subtotal * coupon.DiscountValue / 100
                        : coupon.DiscountValue;
                    
                    if (coupon.MaxDiscount.HasValue && discount > coupon.MaxDiscount.Value)
                        discount = coupon.MaxDiscount.Value;
                    
                    order.Discount = discount;
                    coupon.UsageCount++;
                }
            }

            order.GrandTotal = order.Subtotal - order.Discount + order.ShippingCost;

            foreach (var cartItem in cart.Items)
            {
                var orderItem = new OrderItem
                {
                    ProductId = cartItem.ProductId,
                    ProductName = cartItem.Product?.GetLocalizedName(false) ?? "",
                    ProductImage = cartItem.Product?.ImageUrl,
                    Quantity = cartItem.Quantity,
                    UnitPrice = cartItem.UnitPrice,
                    TotalPrice = cartItem.Total
                };
                order.OrderItems.Add(orderItem);

                if (cartItem.Product != null)
                {
                    cartItem.Product.StockQuantity -= cartItem.Quantity;
                }
            }

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            await _cartService.ClearCartAsync(userId, sessionId);

            return order;
        }

        public async Task UpdateOrderStatusAsync(int orderId, string status)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null)
            {
                order.Status = status;
                order.UpdatedAt = DateTime.Now;

                if (status == "Shipped")
                    order.ShippedAt = DateTime.Now;
                else if (status == "Delivered")
                    order.DeliveredAt = DateTime.Now;

                await _context.SaveChangesAsync();
            }
        }

        public async Task CancelOrderAsync(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order != null && order.Status == "Pending")
            {
                order.Status = "Cancelled";
                order.UpdatedAt = DateTime.Now;

                foreach (var item in order.OrderItems)
                {
                    if (item.ProductId.HasValue)
                    {
                        var product = await _context.Products.FindAsync(item.ProductId.Value);
                        if (product != null)
                        {
                            product.StockQuantity += item.Quantity;
                        }
                    }
                }

                await _context.SaveChangesAsync();
            }
        }

        private string GenerateOrderNumber()
        {
            return $"PS-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
        }
    }
}
