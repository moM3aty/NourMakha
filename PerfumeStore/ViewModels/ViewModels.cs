using System.ComponentModel.DataAnnotations;
using PerfumeStore.Models;

namespace PerfumeStore.ViewModels
{
    // ===================================
    // Account ViewModels
    // ===================================
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
        public string? ReturnUrl { get; set; }
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "First name is required")]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [Phone]
        public string PhoneNumber { get; set; } = string.Empty;
    }

    public class OTPVerificationViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(6, MinimumLength = 6)]
        public string OTPCode { get; set; } = string.Empty;

        public string Purpose { get; set; } = string.Empty;
        public string? ReturnUrl { get; set; }
    }

    // ===================================
    // Product ViewModels
    // ===================================
    public class ProductViewModel
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
        public string? Gender { get; set; }

        [StringLength(50)]
        public string? Concentration { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int StockQuantity { get; set; }

        [Required]
        public int CategoryId { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsFeatured { get; set; }
        public bool IsNewArrival { get; set; }
    }

    public class ProductFilterViewModel
    {
        public string? SearchTerm { get; set; }
        public int? CategoryId { get; set; }
        public string? Gender { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? ScentFamily { get; set; }
        public string? SortBy { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
    }
    public class ProductDetailsViewModel
    {
        public Product Product { get; set; } = new();
        public List<Product> RelatedProducts { get; set; } = new();
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public List<Review> Reviews { get; set; } = new();
        public bool IsInWishlist { get; set; }
        public int? UserRating { get; set; }
        public int ReviewCount { get; set; }
    }

    public class ProductListViewModel
    {
        public List<Product> Products { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public ProductFilterViewModel Filter { get; set; } = new();
        public int TotalCount { get; set; }
        public int TotalItems { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 1;
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }

    // ===================================
    // Checkout ViewModels
    // ===================================
    public class CheckoutViewModel
    {
        public string? UserId { get; set; }

        [Required(ErrorMessage = "First name is required")]
        [StringLength(200)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(200)]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required")]
        [StringLength(300)]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "City is required")]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [StringLength(20)]
        public string? PostalCode { get; set; }

        [Required(ErrorMessage = "Country is required")]
        [StringLength(100)]
        public string Country { get; set; } = "Oman";

        [Required(ErrorMessage = "Phone is required")]
        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Notes { get; set; }

        public string? CouponCode { get; set; }

        [Required]
        public string PaymentMethod { get; set; } = "CashOnDelivery";
    }

    // ===================================
    // Cart ViewModels
    // ===================================
    public class CartIndexViewModel
    {
        public Cart Cart { get; set; } = new();
        public decimal Subtotal { get; set; }
        public decimal Discount { get; set; }
        public decimal Total { get; set; }
        public string? AppliedCoupon { get; set; }
        public string? CouponError { get; set; }
    }

    // ===================================
    // Review ViewModel
    // ===================================
    public class ReviewViewModel
    {
        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        [StringLength(1000)]
        public string? Comment { get; set; }

        public int ProductId { get; set; }
    }

    // ===================================
    // Contact ViewModel
    // ===================================
    public class ContactViewModel
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Subject is required")]
        [StringLength(200)]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Message is required")]
        [StringLength(2000)]
        public string Message { get; set; } = string.Empty;
    }
}
