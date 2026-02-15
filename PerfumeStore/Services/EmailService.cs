using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace PerfumeStore.Services
{
    public interface IEmailService
    {
        Task SendOtpEmailAsync(string email, string otpCode, string purpose);
        Task SendOrderConfirmationAsync(string email, int orderId, string orderNumber);
        Task SendOrderStatusUpdateAsync(string email, int orderId, string orderNumber, string status);
        Task SendEmailAsync(string to, string subject, string body);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendOtpEmailAsync(string email, string otpCode, string purpose)
        {
            var subject = purpose switch
            {
                "Register" => "تفعيل حسابك - NourMakha",
                "ResetPassword" => "استعادة كلمة المرور - NourMakha",
                _ => "رمز التحقق الخاص بك - NourMakha"
            };

            var body = $@"
            <div style='font-family: Arial, sans-serif; text-align: center; padding: 20px; border: 1px solid #eee; border-radius: 10px; max-width: 500px; margin: auto;'>
                <h2 style='color: #002855;'>NourMakha Perfumes</h2>
                <p style='font-size: 1.1rem;'>رمز التحقق الخاص بك هو:</p>
                <div style='background: #f8f9fa; padding: 15px; font-size: 2rem; font-weight: bold; letter-spacing: 10px; color: #002855; border: 1px dashed #002855; margin: 20px 0;'>
                    {otpCode}
                </div>
                <p style='color: #666;'>هذا الرمز صالح لمدة 10 دقائق فقط. يرجى عدم مشاركته مع أي شخص.</p>
            </div>";

            await SendEmailAsync(email, subject, body);
        }

        public async Task SendOrderConfirmationAsync(string email, int orderId, string orderNumber)
        {
            var subject = $"تأكيد الطلب رقم {orderNumber} - NourMakha";
            var body = $"<h1>شكراً لتسوقك معنا!</h1><p>تم استلام طلبك رقم {orderNumber} وهو قيد المعالجة الآن.</p>";
            await SendEmailAsync(email, subject, body);
        }

        public async Task SendOrderStatusUpdateAsync(string email, int orderId, string orderNumber, string status)
        {
            var subject = $"تحديث حالة الطلب {orderNumber}";
            var body = $"<h2>تم تحديث حالة طلبك إلى: {status}</h2>";
            await SendEmailAsync(email, subject, body);
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            // تصحيح: جلب الإعدادات بناءً على هيكلة appsettings.json
            var smtpServer = _configuration["EmailSettings:SmtpServer"];
            var smtpPortStr = _configuration["EmailSettings:SmtpPort"];
            var smtpUsername = _configuration["EmailSettings:SenderEmail"];
            var smtpPassword = _configuration["EmailSettings:SenderPassword"];
            var enableSsl = bool.Parse(_configuration["EmailSettings:EnableSsl"] ?? "true");

            if (string.IsNullOrEmpty(smtpServer) || string.IsNullOrEmpty(smtpUsername))
                return;

            using var client = new SmtpClient(smtpServer, int.Parse(smtpPortStr ?? "587"))
            {
                EnableSsl = enableSsl,
                Credentials = new NetworkCredential(smtpUsername, smtpPassword),
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(smtpUsername, "NourMakha Perfumes"),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            mailMessage.To.Add(to);

            await client.SendMailAsync(mailMessage);
        }
    }
}