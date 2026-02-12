using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PerfumeStore.Models
{
    public class Order
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string OrderNumber { get; set; } = string.Empty;
        
        public string? UserId { get; set; }

        // Shipping Information
        [Required]
        [StringLength(200)]
        public string ShippingFirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string ShippingLastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(200)]
        public string ShippingEmail { get; set; } = string.Empty;

        [Required]
        [StringLength(300)]
        public string ShippingAddress { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string ShippingCity { get; set; } = string.Empty;

        [StringLength(20)]
        public string? ShippingPostalCode { get; set; }

        [Required]
        [StringLength(100)]
        public string ShippingCountry { get; set; } = "Oman";

        [Required]
        [StringLength(20)]
        public string ShippingPhone { get; set; } = string.Empty;

        // Order Details
        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal Discount { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal ShippingCost { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal GrandTotal { get; set; }

        [StringLength(50)]
        public string? CouponCode { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        [Required]
        [StringLength(50)]
        public string PaymentMethod { get; set; } = "CashOnDelivery";

        [StringLength(50)]
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ShippedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; }
        // Navigation Properties
        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }
        
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        // Helper properties
        [NotMapped]
        public string CustomerName => $"{ShippingFirstName} {ShippingLastName}";
        
        [NotMapped]
        public string StatusColor => Status switch
        {
            "Pending" => "warning",
            "Confirmed" => "info",
            "Processing" => "primary",
            "Shipped" => "secondary",
            "Delivered" => "success",
            "Cancelled" => "danger",
            _ => "secondary"
        };
    }
}
