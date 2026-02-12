using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PerfumeStore.Models;

namespace PerfumeStore.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<OTPCode> OTPCodes { get; set; }
        public DbSet<Coupon> Coupons { get; set; }
        public DbSet<ContactMessage> ContactMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Product Configuration
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Order Configuration
            modelBuilder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // OrderItem Configuration
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Review Configuration
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Product)
                .WithMany(p => p.Reviews)
                .HasForeignKey(r => r.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Seed Categories
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Men's Perfumes", NameAr = "عطور رجالية", Description = "Premium men's fragrances", DescriptionAr = "عطور رجالية فاخرة", DisplayOrder = 1 },
                new Category { Id = 2, Name = "Women's Perfumes", NameAr = "عطور نسائية", Description = "Elegant women's fragrances", DescriptionAr = "عطور نسائية راقية", DisplayOrder = 2 },
                new Category { Id = 3, Name = "Unisex Perfumes", NameAr = "عطور للجنسين", Description = "Versatile unisex fragrances", DescriptionAr = "عطور متنوعة للجنسين", DisplayOrder = 3 },
                new Category { Id = 4, Name = "Luxury Collections", NameAr = "مجموعات فاخرة", Description = "Exclusive luxury perfume collections", DescriptionAr = "مجموعات عطور فاخرة حصرية", DisplayOrder = 4 },
                new Category { Id = 5, Name = "Gift Sets", NameAr = "هدايا", Description = "Perfect gift sets for loved ones", DescriptionAr = "مجموعات هدايا مثالية لأحبائك", DisplayOrder = 5 }
            );
        }
    }
}
