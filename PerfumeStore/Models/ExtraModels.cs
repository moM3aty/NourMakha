using System.ComponentModel.DataAnnotations;

namespace PerfumeStore.Models
{
    // إعدادات الموقع (للشريط الإعلاني وغيره)
    public class SiteSetting
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty; // e.g., "AnnouncementBar"
        public string Value { get; set; } = string.Empty; // e.g., "خصم 20% لفترة محدودة"
        public bool IsEnabled { get; set; } = true;
    }

    // مناطق الشحن
    public class ShippingZone
    {
        public int Id { get; set; }

        [Required]
        public string NameAr { get; set; } = string.Empty; // الموالح، الداخلية، إلخ

        [Required]
        public string NameEn { get; set; } = string.Empty;

        public decimal Cost { get; set; } // تكلفة الشحن
        public int EstimatedDays { get; set; } // عدد الأيام المتوقع
        public bool IsActive { get; set; } = true;
    }
}