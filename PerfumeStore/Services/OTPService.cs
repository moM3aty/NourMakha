using PerfumeStore.Data;
using PerfumeStore.Models;
using Microsoft.EntityFrameworkCore;

namespace PerfumeStore.Services
{
    public interface IOTPService
    {
        Task<string> GenerateOTPAsync(string email, string purpose);
        Task<bool> VerifyOTPAsync(string email, string code, string purpose);
        Task CleanupExpiredOTPsAsync();
    }

    public class OTPService : IOTPService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;

        public OTPService(ApplicationDbContext context, IConfiguration configuration, IEmailService emailService)
        {
            _context = context;
            _configuration = configuration;
            _emailService = emailService;
        }

        public async Task<string> GenerateOTPAsync(string email, string purpose)
        {
            // Clean up old OTPs for this email and purpose
            var existingOTPs = await _context.OTPCodes
                .Where(o => o.Email == email && o.Purpose == purpose)
                .ToListAsync();
            _context.OTPCodes.RemoveRange(existingOTPs);

            // Generate new OTP
            var codeLength = _configuration.GetValue<int>("OTPSettings:CodeLength", 6);
            var random = new Random();
            var code = random.Next((int)Math.Pow(10, codeLength - 1), (int)Math.Pow(10, codeLength)).ToString();

            var expirationMinutes = _configuration.GetValue<int>("OTPSettings:ExpirationMinutes", 5);

            var otp = new OTPCode
            {
                Email = email,
                Code = code,
                Purpose = purpose,
                ExpiresAt = DateTime.Now.AddMinutes(expirationMinutes),
                IsUsed = false,
                Attempts = 0
            };

            _context.OTPCodes.Add(otp);
            await _context.SaveChangesAsync();

            // Send OTP via email
            var subject = purpose switch
            {
                "Login" => "Your Login OTP Code",
                "Register" => "Your Verification OTP Code",
                "ResetPassword" => "Your Password Reset OTP Code",
                _ => "Your OTP Code"
            };

            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
                    <div style='background: linear-gradient(135deg, #1a1a2e 0%, #16213e 100%); padding: 30px; border-radius: 15px; text-align: center;'>
                        <h1 style='color: #d4af37; margin-bottom: 20px;'>Perfume Store</h1>
                        <div style='background: rgba(255,255,255,0.1); padding: 30px; border-radius: 10px; margin: 20px 0;'>
                            <p style='color: #fff; font-size: 16px; margin-bottom: 15px;'>Your verification code is:</p>
                            <div style='background: #d4af37; color: #1a1a2e; font-size: 32px; font-weight: bold; padding: 15px 30px; border-radius: 8px; display: inline-block; letter-spacing: 8px;'>
                                {code}
                            </div>
                            <p style='color: #ccc; font-size: 14px; margin-top: 20px;'>This code will expire in {expirationMinutes} minutes</p>
                        </div>
                        <p style='color: #888; font-size: 12px;'>If you didn't request this code, please ignore this email.</p>
                    </div>
                </div>";

            await _emailService.SendEmailAsync(email, subject, body);

            return code;
        }

        public async Task<bool> VerifyOTPAsync(string email, string code, string purpose)
        {
            var otp = await _context.OTPCodes
                .Where(o => o.Email == email && o.Purpose == purpose && !o.IsUsed)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (otp == null)
                return false;

            // Check attempts
            if (otp.Attempts >= 3)
                return false;

            // Check expiration
            if (otp.ExpiresAt < DateTime.Now)
                return false;

            // Verify code
            if (otp.Code != code)
            {
                otp.Attempts++;
                await _context.SaveChangesAsync();
                return false;
            }

            // Mark as used
            otp.IsUsed = true;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task CleanupExpiredOTPsAsync()
        {
            var expiredOTPs = await _context.OTPCodes
                .Where(o => o.ExpiresAt < DateTime.Now || o.IsUsed)
                .ToListAsync();

            _context.OTPCodes.RemoveRange(expiredOTPs);
            await _context.SaveChangesAsync();
        }
    }
}
