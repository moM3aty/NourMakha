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
                // جلب الدومين الخاص بالموقع
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

        // هذا هو الرابط الذي تعود إليه ثواني بعد نجاح الدفع
        [Route("Payment/Success")]
        public async Task<IActionResult> Success(string session_id)
        {
            if (string.IsNullOrEmpty(session_id)) return RedirectToAction("Index", "Home");

            try
            {
                // التحقق من حالة الدفع من خوادم ثواني
                var verification = await _paymentService.VerifyPaymentAsync(session_id);
                var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderNumber == verification.OrderNumber);

                if (order == null) return NotFound();

                if (verification.IsPaid)
                {
                    // تحديث حالة الطلب إلى ناجح
                    order.Status = "Confirmed";
                    order.PaymentMethod = "Credit Card (Paid)";
                    order.UpdatedAt = DateTime.Now;

                    await _context.SaveChangesAsync();

                    // إرسال إيميل التأكيد
                    await _emailService.SendOrderConfirmationAsync(order.ShippingEmail, order.Id, order.OrderNumber);

                    // توجيه العميل لصفحة نجاح الطلب
                    return RedirectToAction("OrderConfirmation", "Cart", new { id = order.Id });
                }
                else
                {
                    TempData["Error"] = "عذراً، عملية الدفع لم تكتمل بنجاح.";
                    return RedirectToAction("Checkout", "Cart");
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "حدث خطأ أثناء التحقق من الدفع. يرجى التواصل مع الدعم الفني.";
                // في حالة الخطأ يمكن توجيهه لصفحة الفاتورة ليرى أنها ما زالت معلقة
                return RedirectToAction("Index", "Home");
            }
        }

        // هذا الرابط إذا ألغى العميل الدفع من صفحة ثواني
        [Route("Payment/Cancel")]
        public IActionResult Cancel()
        {
            TempData["Error"] = "تم إلغاء عملية الدفع.";
            return RedirectToAction("Checkout", "Cart");
        }
    }
}