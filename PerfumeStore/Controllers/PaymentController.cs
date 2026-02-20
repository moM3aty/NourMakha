using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PerfumeStore.Data;
using PerfumeStore.Services;

namespace PerfumeStore.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPaymentService _paymentService;
        private readonly IEmailService _emailService;

        public PaymentController(ApplicationDbContext context, IPaymentService paymentService, IEmailService emailService)
        {
            _context = context;
            _paymentService = paymentService;
            _emailService = emailService;
        }

        [HttpGet]
        public async Task<IActionResult> Gateway(int orderId)
        {
            var order = await _context.Orders.Include(o => o.User).FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null || order.Status != "Awaiting Payment") return RedirectToAction("Index", "Home");

            try
            {
                // جلب الدومين الخاص بالموقع (مثال: https://www.nourmakha.com)
                string hostUrl = $"{Request.Scheme}://{Request.Host}";

                // إنشاء جلسة الدفع في ثواني وجلب رابط صفحة الدفع
                var checkoutUrl = await _paymentService.CreateCheckoutSessionAsync(order, hostUrl);

                // توجيه العميل فوراً إلى صفحة الدفع الخاصة بثواني
                return Redirect(checkoutUrl);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "حدث خطأ أثناء الاتصال ببوابة الدفع (Thawani). " + ex.Message;
                return RedirectToAction("Checkout", "Cart");
            }
        }

        [Route("Payment/Success")]
        public async Task<IActionResult> Success(string session_id)
        {
            if (string.IsNullOrEmpty(session_id)) return RedirectToAction("Index", "Home");

            // التحقق من حالة الدفع من خوادم ثواني
            var verification = await _paymentService.VerifyPaymentAsync(session_id);
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderNumber == verification.OrderNumber);

            if (order == null) return NotFound();

            if (verification.IsPaid)
            {
                order.Status = "Confirmed";
                order.PaymentMethod = "Thawani Pay (Paid)";
                order.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                await _emailService.SendOrderConfirmationAsync(order.ShippingEmail, order.Id, order.OrderNumber);

                return RedirectToAction("OrderConfirmation", "Cart", new { id = order.Id });
            }
            else
            {
                TempData["Error"] = "عذراً، عملية الدفع لم تكتمل بنجاح.";
                return RedirectToAction("Checkout", "Cart");
            }
        }

        [Route("Payment/Cancel")]
        public IActionResult Cancel()
        {
            TempData["Error"] = "تم إلغاء عملية الدفع من قبل العميل.";
            return RedirectToAction("Checkout", "Cart");
        }
    }
}