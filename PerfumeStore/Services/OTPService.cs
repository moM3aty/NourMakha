using Microsoft.EntityFrameworkCore;
using PerfumeStore.Data;
using PerfumeStore.Models;

namespace PerfumeStore.Services
{
    public interface IOTPService
    {
        Task<string> GenerateOTPAsync(string email, string purpose);
        Task<bool> VerifyOTPAsync(string email, string code, string purpose);
    }

    public class OTPService : IOTPService
    {
        private readonly ApplicationDbContext _context;

        public OTPService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<string> GenerateOTPAsync(string email, string purpose)
        {
            // Generate 6-digit OTP
            var random = new Random();
            var otp = random.Next(100000, 999999).ToString();

            var otpCode = new OTPCode
            {
                Email = email,
                Code = otp,
                Purpose = purpose,
                IsUsed = false,
                CreatedAt = DateTime.Now,
                ExpiresAt = DateTime.Now.AddMinutes(10)
            };

            _context.OTPCodes.Add(otpCode);
            await _context.SaveChangesAsync();

            return otp;
        }

        public async Task<bool> VerifyOTPAsync(string email, string code, string purpose)
        {
            var otpCode = await _context.OTPCodes
                .Where(o => o.Email == email && o.Purpose == purpose && !o.IsUsed)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (otpCode == null)
                return false;

            if (otpCode.ExpiresAt < DateTime.Now)
                return false;

            if (otpCode.Code != code)
                return false;

            otpCode.IsUsed = true;
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
