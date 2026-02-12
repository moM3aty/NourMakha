using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PerfumeStore.Models
{
    public class Coupon
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Description { get; set; }

        [StringLength(200)]
        public string? DescriptionAr { get; set; }

        [Required]
        [StringLength(20)]
        public string DiscountType { get; set; } = "Percentage"; // Percentage, Fixed

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountValue { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MaxDiscount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MinOrderAmount { get; set; }

        public int? UsageLimit { get; set; }
        public int UsageCount { get; set; } = 0;
        public int UsedCount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        [Column(TypeName = "decimal(18,2)")]
        public decimal? MinimumOrderAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MaximumDiscountAmount { get; set; }


        public bool IsActive { get; set; } = true;

        [NotMapped]
        public bool IsValid => IsActive && DateTime.Now >= StartDate && DateTime.Now <= EndDate 
            && (!UsageLimit.HasValue || UsageCount < UsageLimit.Value);

        public string GetLocalizedDescription(bool isArabic) => 
            isArabic && !string.IsNullOrEmpty(DescriptionAr) ? DescriptionAr : Description ?? "";
    }
}
