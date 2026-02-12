using System.ComponentModel.DataAnnotations.Schema;

namespace PerfumeStore.Models
{
    public class CartItem
    {
        public int Id { get; set; }
        
        public int CartId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public DateTime AddedAt { get; set; } = DateTime.Now;

        // Navigation Properties
        [ForeignKey("CartId")]
        public Cart? Cart { get; set; }
        
        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        // Helper property
        [NotMapped]
        public decimal Total => UnitPrice * Quantity;
    }
}
