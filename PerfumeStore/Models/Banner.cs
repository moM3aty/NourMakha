using System.ComponentModel.DataAnnotations;

namespace PerfumeStore.Models
{
    public class Banner
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;
        public string? TitleAr { get; set; }

        public string? Subtitle { get; set; }
        public string? SubtitleAr { get; set; }

        public string? Description { get; set; }
        public string? DescriptionAr { get; set; }

        [Required]
        public string ImageUrl { get; set; } = string.Empty;

        public string? LinkUrl { get; set; } // الرابط الذي يوجه إليه البنر

        public string? ButtonText { get; set; }
        public string? ButtonTextAr { get; set; }

        public int DisplayOrder { get; set; } // ترتيب العرض
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Helper لترجمة النصوص
        public string GetLocalizedTitle(bool isAr) => isAr && !string.IsNullOrEmpty(TitleAr) ? TitleAr : Title;
        public string GetLocalizedSubtitle(bool isAr) => isAr && !string.IsNullOrEmpty(SubtitleAr) ? SubtitleAr : Subtitle ?? "";
        public string GetLocalizedDescription(bool isAr) => isAr && !string.IsNullOrEmpty(DescriptionAr) ? DescriptionAr : Description ?? "";
        public string GetLocalizedButton(bool isAr) => isAr && !string.IsNullOrEmpty(ButtonTextAr) ? ButtonTextAr : ButtonText ?? (isAr ? "تسوق الآن" : "Shop Now");
    }
}