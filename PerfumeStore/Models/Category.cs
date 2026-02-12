using System.ComponentModel.DataAnnotations;

namespace PerfumeStore.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string NameAr { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(500)]
        public string? DescriptionAr { get; set; }

        [StringLength(500)]
        public string? ImageUrl { get; set; }

        public int? ParentCategoryId { get; set; }

        public bool IsActive { get; set; } = true;

        public int DisplayOrder { get; set; }

        // Navigation Properties
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
