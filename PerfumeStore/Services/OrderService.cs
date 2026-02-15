using Microsoft.EntityFrameworkCore;
using PerfumeStore.Data;
using PerfumeStore.Models;
using PerfumeStore.ViewModels;
using System.Globalization;

namespace PerfumeStore.Services
{
    public class OrderCreationResult
    {
        public Order Order { get; set; } = null!;
        public string? RedirectUrl { get; set; }
        public bool IsPaymentRequired { get; set; }
    }

    public interface IOrderService
    {
        Task<Order?> GetOrderByIdAsync(int id);
        Task<OrderCreationResult> CreateOrderAsync(string? userId, string? sessionId, CheckoutViewModel model, string? couponCode = null);
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

        public async Task<Order?> GetOrderByIdAsync(int id) =>
            await _context.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.Id == id);

        public async Task<OrderCreationResult> CreateOrderAsync(string? userId, string? sessionId, CheckoutViewModel model, string? couponCode = null)
        {
            var cart = await _cartService.GetCartAsync(userId, sessionId);
            if (cart == null || !cart.Items.Any())
                throw new InvalidOperationException("Cart is empty");

            var totals = await _cartService.CalculateCartTotalsAsync(userId, sessionId, couponCode, model.ShippingZoneId);
            var orderNumber = $"NM-{DateTime.Now:yyMMdd}-{new Random().Next(1000, 9999)}";

            // تحديد الحالة بناءً على نوع الدفع
            string status = model.PaymentMethod == "CreditCard" ? "Awaiting Payment" : "Pending";

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
                PaymentMethod = model.PaymentMethod,
                Status = status,
                CreatedAt = DateTime.Now,
                Subtotal = totals.Subtotal,
                TotalAmount = totals.Subtotal,
                ShippingCost = totals.ShippingCost,
                Discount = totals.DiscountAmount,
                DiscountAmount = totals.DiscountAmount,
                CouponCode = totals.IsCouponValid ? totals.AppliedCouponCode : null,
                GrandTotal = totals.GrandTotal,
                TaxAmount = 0
            };

            // تحديث الكوبون
            if (totals.IsCouponValid && !string.IsNullOrEmpty(totals.AppliedCouponCode))
            {
                var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code == totals.AppliedCouponCode);
                if (coupon != null) { coupon.UsedCount++; _context.Coupons.Update(coupon); }
            }

            // إضافة العناصر
            foreach (var item in cart.Items)
            {
                order.OrderItems.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    ProductName = item.Product?.GetLocalizedName(CultureInfo.CurrentUICulture.Name.StartsWith("ar")) ?? "Product",
                    ProductImage = item.Product?.ImageUrl,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = item.Total
                });

                if (item.Product != null)
                {
                    item.Product.StockQuantity -= item.Quantity;
                    _context.Products.Update(item.Product);
                }
            }

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            await _cartService.ClearCartAsync(userId, sessionId);

            var result = new OrderCreationResult { Order = order };

            // تفعيل رابط الدفع
            if (model.PaymentMethod == "CreditCard")
            {
                result.IsPaymentRequired = true;
                result.RedirectUrl = "/Payment/Gateway?orderId=" + order.Id;
            }

            return result;
        }

        public async Task UpdateOrderStatusAsync(int orderId, string status)
        {
            var o = await _context.Orders.FindAsync(orderId);
            if (o != null) { o.Status = status; o.UpdatedAt = DateTime.Now; await _context.SaveChangesAsync(); }
        }

        public async Task CancelOrderAsync(int orderId)
        {
            var o = await _context.Orders.Include(x => x.OrderItems).FirstOrDefaultAsync(x => x.Id == orderId);
            if (o != null) { o.Status = "Cancelled"; foreach (var i in o.OrderItems) if (i.ProductId.HasValue) { var p = await _context.Products.FindAsync(i.ProductId.Value); if (p != null) p.StockQuantity += i.Quantity; } await _context.SaveChangesAsync(); }
        }
    }
}