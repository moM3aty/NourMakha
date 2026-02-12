using System.Net;
using System.Net.Mail;

namespace PerfumeStore.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
        Task SendOrderConfirmationAsync(string toEmail, int orderId, decimal total);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var smtpServer = _configuration["EmailSettings:SmtpServer"];
            var smtpPort = _configuration.GetValue<int>("EmailSettings:SmtpPort");
            var senderEmail = _configuration["EmailSettings:SenderEmail"];
            var senderPassword = _configuration["EmailSettings:SenderPassword"];
            var enableSsl = _configuration.GetValue<bool>("EmailSettings:EnableSsl");

            using var client = new SmtpClient(smtpServer, smtpPort);
            client.EnableSsl = enableSsl;
            client.Credentials = new NetworkCredential(senderEmail, senderPassword);

            var mailMessage = new MailMessage
            {
                From = new MailAddress(senderEmail!, "Perfume Store"),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);

            await client.SendMailAsync(mailMessage);
        }

        public async Task SendOrderConfirmationAsync(string toEmail, int orderId, decimal total)
        {
            var subject = $"Order Confirmation - #{orderId}";
            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <div style='background: linear-gradient(135deg, #1a1a2e 0%, #16213e 100%); padding: 30px; border-radius: 15px;'>
                        <h1 style='color: #d4af37; text-align: center; margin-bottom: 30px;'>Order Confirmed!</h1>
                        <div style='background: rgba(255,255,255,0.1); padding: 25px; border-radius: 10px;'>
                            <p style='color: #fff; font-size: 18px;'>Thank you for your order!</p>
                            <p style='color: #ccc;'>Order Number: <strong style='color: #d4af37;'>#{orderId}</strong></p>
                            <p style='color: #ccc;'>Total Amount: <strong style='color: #d4af37;'>${total:F2}</strong></p>
                            <p style='color: #888; font-size: 14px; margin-top: 20px;'>We'll send you a tracking number once your order ships.</p>
                        </div>
                    </div>
                </div>";

            await SendEmailAsync(toEmail, subject, body);
        }
    }
}
