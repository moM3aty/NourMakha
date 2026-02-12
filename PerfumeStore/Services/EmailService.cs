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
                "Register" => "Verify Your Email - Perfume Store",
                "ResetPassword" => "Reset Your Password - Perfume Store",
                "Login" => "Your Login Code - Perfume Store",
                _ => "Your OTP Code - Perfume Store"
            };

            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f5f5f5; margin: 0; padding: 20px; }}
        .container {{ max-width: 600px; margin: 0 auto; background: white; border-radius: 10px; overflow: hidden; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #d4af37, #b8960c); padding: 30px; text-align: center; }}
        .header h1 {{ color: white; margin: 0; }}
        .content {{ padding: 40px 30px; text-align: center; }}
        .otp-code {{ font-size: 32px; font-weight: bold; color: #d4af37; letter-spacing: 8px; margin: 20px 0; }}
        .footer {{ background: #f8f8f8; padding: 20px; text-align: center; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🌟 Perfume Store</h1>
        </div>
        <div class='content'>
            <h2>Your verification code is:</h2>
            <div class='otp-code'>{otpCode}</div>
            <p style='color: #666; font-size: 14px;'>This code will expire in 10 minutes.</p>
        </div>
        <div class='footer'>
            <p>© 2024 Perfume Store. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

            await SendEmailAsync(email, subject, body);
        }

        public async Task SendOrderConfirmationAsync(string email, int orderId, string orderNumber)
        {
            var subject = $"Order Confirmation - {orderNumber} - Perfume Store";
            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f5f5f5; margin: 0; padding: 20px; }}
        .container {{ max-width: 600px; margin: 0 auto; background: white; border-radius: 10px; overflow: hidden; }}
        .header {{ background: linear-gradient(135deg, #d4af37, #b8960c); padding: 30px; text-align: center; }}
        .header h1 {{ color: white; margin: 0; }}
        .content {{ padding: 40px 30px; }}
        .order-number {{ font-size: 24px; color: #d4af37; font-weight: bold; }}
        .footer {{ background: #f8f8f8; padding: 20px; text-align: center; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🌟 Order Confirmed!</h1>
        </div>
        <div class='content'>
            <h2>Thank you for your order!</h2>
            <p>Your order has been confirmed and is being processed.</p>
            <p>Order Number: <span class='order-number'>{orderNumber}</span></p>
        </div>
        <div class='footer'>
            <p>© 2024 Perfume Store. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

            await SendEmailAsync(email, subject, body);
        }

        public async Task SendOrderStatusUpdateAsync(string email, int orderId, string orderNumber, string status)
        {
            var subject = $"Order Update - {orderNumber} - {status} - Perfume Store";
            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f5f5f5; margin: 0; padding: 20px; }}
        .container {{ max-width: 600px; margin: 0 auto; background: white; border-radius: 10px; overflow: hidden; }}
        .header {{ background: linear-gradient(135deg, #d4af37, #b8960c); padding: 30px; text-align: center; }}
        .header h1 {{ color: white; margin: 0; }}
        .content {{ padding: 40px 30px; text-align: center; }}
        .status {{ font-size: 28px; color: #d4af37; font-weight: bold; }}
        .footer {{ background: #f8f8f8; padding: 20px; text-align: center; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🌟 Order Update</h1>
        </div>
        <div class='content'>
            <h2>Your Order Status Has Changed</h2>
            <p>Order Number: {orderNumber}</p>
            <p>New Status: <span class='status'>{status}</span></p>
        </div>
        <div class='footer'>
            <p>© 2024 Perfume Store. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

            await SendEmailAsync(email, subject, body);
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var smtpServer = _configuration["Email:SmtpServer"];
            var smtpPortStr = _configuration["Email:SmtpPort"];
            var smtpUsername = _configuration["Email:Username"];
            var smtpPassword = _configuration["Email:Password"];
            var fromEmail = _configuration["Email:FromEmail"];

            if (string.IsNullOrEmpty(smtpServer) || string.IsNullOrEmpty(smtpUsername))
            {
                // Email not configured - skip sending
                return;
            }

            // Use System.Net.Mail for sending emails
            using var client = new System.Net.Mail.SmtpClient(smtpServer, int.Parse(smtpPortStr ?? "587"));
            client.EnableSsl = true;
            client.Credentials = new System.Net.NetworkCredential(smtpUsername, smtpPassword);

            var mailMessage = new System.Net.Mail.MailMessage
            {
                From = new System.Net.Mail.MailAddress(fromEmail ?? smtpUsername, "Perfume Store"),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            mailMessage.To.Add(to);

            await client.SendMailAsync(mailMessage);
        }
    }
}
