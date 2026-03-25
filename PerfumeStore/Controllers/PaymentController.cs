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
                // جلب الدومين الخاص بالموقع لاستخدامه في الرد (Callback)
                string hostUrl = $"{Request.Scheme}://{Request.Host}";

                // إنشاء جلسة الدفع في Paymob وجلب رابط صفحة الدفع (Iframe URL)
                var checkoutUrl = await _paymentService.CreateCheckoutSessionAsync(order, hostUrl);

                // توجيه العميل فوراً إلى شاشة الدفع الخاصة بـ Paymob
                return Redirect(checkoutUrl);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "حدث خطأ أثناء الاتصال ببوابة الدفع (Paymob). " + ex.Message;
                return RedirectToAction("Checkout", "Cart");
            }
        }

        // هذا هو الرابط الذي تعود إليه Paymob بعد انتهاء العميل من الدفع
        [Route("Payment/Success")]
        public async Task<IActionResult> Success([FromQuery] string id, [FromQuery] string success, [FromQuery] string merchant_order_id)
        {
            // id = رقم المعاملة في باي موب
            // merchant_order_id = رقم الطلب في متجرنا (OrderNumber)

            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(merchant_order_id))
                return RedirectToAction("Index", "Home");

            try
            {
                var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderNumber == merchant_order_id);
                if (order == null) return NotFound();

                // التحقق الآمن من حالة الدفع من خوادم باي موب مباشرة (لمنع التلاعب بالرابط)
                var verification = await _paymentService.VerifyPaymentAsync(id);

                // إذا كان الدفع ناجحاً
                if (verification.IsPaid && success == "true")
                {
                    // تحديث حالة الطلب إلى ناجح
                    order.Status = "Confirmed";

                    // تنظيف اسم طريقة الدفع (مثلاً تحويل Paymob_48305 إلى Paymob (Paid))
                    order.PaymentMethod = "Paymob (Paid)";
                    order.UpdatedAt = DateTime.Now;

                    await _context.SaveChangesAsync();

                    // إرسال إيميل التأكيد للعميل
                    await _emailService.SendOrderConfirmationAsync(order.ShippingEmail, order.Id, order.OrderNumber);

                    // توجيه العميل لصفحة نجاح الطلب وإظهار الفاتورة
                    return RedirectToAction("OrderConfirmation", "Cart", new { id = order.Id });
                }
                else
                {
                    // الدفع فشل أو تم رفض البطاقة
                    TempData["Error"] = "عذراً، عملية الدفع لم تكتمل بنجاح أو تم رفض البطاقة من قبل البنك.";
                    return RedirectToAction("Checkout", "Cart");
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "حدث خطأ أثناء التحقق من الدفع. يرجى التواصل مع الدعم الفني.";
                return RedirectToAction("Index", "Home");
            }
        }

        [Route("Payment/Cancel")]
        public IActionResult Cancel()
        {
            TempData["Error"] = "تم إلغاء عملية الدفع.";
            return RedirectToAction("Checkout", "Cart");
        }
    }
}