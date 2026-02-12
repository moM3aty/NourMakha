namespace PerfumeStore.Models
{
    public class ProductImage
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsMain { get; set; }
        public int DisplayOrder { get; set; }

        // Navigation Property
        public Product? Product { get; set; }
    }
}
