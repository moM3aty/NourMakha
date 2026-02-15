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
        private readonly IConfiguration _configuration;

        public PaymentController(ApplicationDbContext c, IPaymentService p, IEmailService e, IConfiguration conf) { _context = c; _paymentService = p; _emailService = e; _configuration = conf; }

        [HttpGet]
        public async Task<IActionResult> Gateway(int orderId)
        {
            var order = await _context.Orders.Include(o => o.User).FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null || order.Status != "Awaiting Payment") return RedirectToAction("Index", "Home");

            var lang = System.Globalization.CultureInfo.CurrentCulture.Name.StartsWith("ar") ? "AR" : "EN";
            var paymentParams = _paymentService.PreparePaymentRequest(order.OrderNumber, order.GrandTotal, lang);
            ViewBag.PaymentUrl = _configuration["AmwalPaySettings:BaseUrl"];
            return View(paymentParams);
        }

        [Route("Payment/Response")]
        public async Task<IActionResult> Response(string response_code, string response_message, string trackid)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderNumber == trackid);
            if (order == null) return NotFound();

            if (response_code == "000" || response_code == "00")
            {
                order.Status = "Confirmed";
                order.PaymentMethod = "CreditCard (Paid)";
                order.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                await _emailService.SendOrderConfirmationAsync(order.ShippingEmail, order.Id, order.OrderNumber);
                return RedirectToAction("OrderConfirmation", "Cart", new { id = order.Id });
            }
            else
            {
                TempData["Error"] = "Payment Failed: " + response_message;
                return RedirectToAction("Checkout", "Cart");
            }
        }
    }
}