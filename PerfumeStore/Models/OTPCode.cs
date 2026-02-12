using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PerfumeStore.Models
{
    public class OTPCode
    {
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(200)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(6)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Purpose { get; set; } = string.Empty; // Register, ResetPassword, Login

        public bool IsUsed { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime ExpiresAt { get; set; }

        [NotMapped]
        public bool IsValid => !IsUsed && DateTime.Now <= ExpiresAt;
    }
}
