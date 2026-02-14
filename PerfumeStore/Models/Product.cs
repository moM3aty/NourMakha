using System.ComponentModel.DataAnnotations;

namespace PerfumeStore.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string? NameAr { get; set; }

        [Required]
        [StringLength(100)]
        public string Brand { get; set; } = string.Empty;

        [StringLength(100)]
        public string? BrandAr { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Price { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? OldPrice { get; set; }

        [StringLength(2000)]
        public string? Description { get; set; }

        [StringLength(2000)]
        public string? DescriptionAr { get; set; }

        [StringLength(500)]
        public string? ImageUrl { get; set; }

        [StringLength(100)]
        public string? ScentFamily { get; set; }

        [StringLength(100)]
        public string? ScentFamilyAr { get; set; }

        [StringLength(50)]
        public string? Size { get; set; }

        [StringLength(30)]
        public string? Gender { get; set; } // Men, Women, Unisex

        [StringLength(50)]
        public string? Concentration { get; set; } // EDP, EDT, Parfum, etc.

        [Required]
        [Range(0, int.MaxValue)]
        public int StockQuantity { get; set; }

        public int CategoryId { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsFeatured { get; set; }
        public bool IsNewArrival { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        public Category? Category { get; set; }
        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();

        // Helper properties
        public bool IsInStock => StockQuantity > 0;
        public int? DiscountPercentage => OldPrice.HasValue && OldPrice > Price
            ? (int)Math.Round((1 - Price / OldPrice.Value) * 100)
            : null;

        public string GetLocalizedName(bool isArabic) => isArabic && !string.IsNullOrEmpty(NameAr) ? NameAr : Name;
        public string GetLocalizedBrand(bool isArabic) => isArabic && !string.IsNullOrEmpty(BrandAr) ? BrandAr : Brand;
        public string GetLocalizedDescription(bool isArabic) => isArabic && !string.IsNullOrEmpty(DescriptionAr) ? DescriptionAr : Description ?? "";
        public string GetLocalizedScentFamily(bool isArabic) => isArabic && !string.IsNullOrEmpty(ScentFamilyAr) ? ScentFamilyAr : ScentFamily ?? "";
    }
}