using System.ComponentModel.DataAnnotations;

namespace PerfumeStore.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(100)]
        public string? NameAr { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(500)]
        public string? DescriptionAr { get; set; }

        [StringLength(500)]
        public string? ImageUrl { get; set; }

        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation Properties
        public ICollection<Product> Products { get; set; } = new List<Product>();

        // Helper property for localized name
        public string GetLocalizedName(bool isArabic) => isArabic && !string.IsNullOrEmpty(NameAr) ? NameAr : Name;
    }
}
