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
            // توليد رمز مكون من 6 أرقام
            var random = new Random();
            var otp = random.Next(100000, 999999).ToString();

            // إلغاء أي أكواد سابقة غير مستخدمة لهذا الإيميل لنفس الغرض
            var oldCodes = await _context.OTPCodes
                .Where(o => o.Email == email && o.Purpose == purpose && !o.IsUsed)
                .ToListAsync();

            foreach (var old in oldCodes) old.IsUsed = true;

            var otpCode = new OTPCode
            {
                Email = email,
                Code = otp,
                Purpose = purpose,
                IsUsed = false,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10)
            };

            _context.OTPCodes.Add(otpCode);
            await _context.SaveChangesAsync();

            return otp;
        }

        public async Task<bool> VerifyOTPAsync(string email, string code, string purpose)
        {
            // البحث عن آخر كود صالح
            var otpCode = await _context.OTPCodes
                .Where(o => o.Email == email && o.Purpose == purpose && !o.IsUsed)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (otpCode == null) return false;

            // التحقق من انتهاء الصلاحية بالتوقيت العالمي
            if (otpCode.ExpiresAt < DateTime.UtcNow) return false;

            if (otpCode.Code != code) return false;

            // تم الاستخدام بنجاح
            otpCode.IsUsed = true;
            await _context.SaveChangesAsync();

            return true;
        }
    }
}