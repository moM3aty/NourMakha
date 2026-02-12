using System.ComponentModel.DataAnnotations.Schema;

namespace PerfumeStore.Models
{
    public class Cart
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public string? SessionId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }
        
        public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
        // Helper properties (NotMapped)
        [NotMapped]
        public int ItemCount => Items?.Sum(i => i.Quantity) ?? 0;
        
        [NotMapped]
        public decimal Subtotal => Items?.Sum(i => i.Total) ?? 0;
    }
}
