using Microsoft.AspNetCore.Identity;

namespace PerfumeStore.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        
        // Address Information
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? PostalCode { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? LastLoginAt { get; set; }
        public bool IsEmailVerified { get; set; } = false;

        // Navigation Properties
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();
        public Cart? Cart { get; set; }
    }
}
