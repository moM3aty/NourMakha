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

        // DbSets
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<ProductImage> ProductImages { get; set; } = null!;
        public DbSet<Review> Reviews { get; set; } = null!;
        public DbSet<Cart> Carts { get; set; } = null!;
        public DbSet<CartItem> CartItems { get; set; } = null!;
        public DbSet<WishlistItem> WishlistItems { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderItem> OrderItems { get; set; } = null!;
        public DbSet<Coupon> Coupons { get; set; } = null!;
        public DbSet<ContactMessage> ContactMessages { get; set; } = null!;
        public DbSet<OTPCode> OTPCodes { get; set; } = null!;
        public DbSet<SiteSetting> SiteSettings { get; set; } = null!;
        public DbSet<ShippingZone> ShippingZones { get; set; } = null!;
        public DbSet<Banner> Banners { get; set; } = null!;
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Category Configuration
            modelBuilder.Entity<Category>()
                .HasMany(c => c.Products)
                .WithOne(p => p.Category)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Product Configuration
            modelBuilder.Entity<Product>()
                .HasMany(p => p.Images)
                .WithOne(i => i.Product)
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Product>()
                .HasMany(p => p.Reviews)
                .WithOne(r => r.Product)
                .HasForeignKey(r => r.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Product>()
                .HasMany(p => p.CartItems)
                .WithOne(ci => ci.Product)
                .HasForeignKey(ci => ci.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Product>()
                .HasMany(p => p.OrderItems)
                .WithOne(oi => oi.Product)
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Product>()
                .HasMany(p => p.WishlistItems)
                .WithOne(w => w.Product)
                .HasForeignKey(w => w.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Cart Configuration
            modelBuilder.Entity<Cart>()
                .HasMany(c => c.Items)
                .WithOne(ci => ci.Cart)
                .HasForeignKey(ci => ci.CartId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Cart>()
                .HasOne(c => c.User)
                .WithOne(u => u.Cart)
                .HasForeignKey<Cart>(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Order Configuration
            modelBuilder.Entity<Order>()
                .HasMany(o => o.OrderItems)
                .WithOne(oi => oi.Order)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Review Configuration
            modelBuilder.Entity<Review>()
                .HasOne(r => r.User)
                .WithMany(u => u.Reviews)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Wishlist Configuration
            modelBuilder.Entity<WishlistItem>()
                .HasOne(w => w.User)
                .WithMany(u => u.WishlistItems)
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Name);

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Brand);

            modelBuilder.Entity<Order>()
                .HasIndex(o => o.OrderNumber);

            modelBuilder.Entity<Order>()
                .HasIndex(o => o.Status);

            // Seed Categories
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Men's Perfumes", NameAr = "عطور رجالية", Description = "Premium men's fragrances", DescriptionAr = "عطور رجالية فاخرة", DisplayOrder = 1 },
                new Category { Id = 2, Name = "Women's Perfumes", NameAr = "عطور نسائية", Description = "Elegant women's fragrances", DescriptionAr = "عطور نسائية راقية", DisplayOrder = 2 },
                new Category { Id = 3, Name = "Unisex Perfumes", NameAr = "عطور للجنسين", Description = "Versatile unisex fragrances", DescriptionAr = "عطور متنوعة للجنسين", DisplayOrder = 3 },
                new Category { Id = 4, Name = "Luxury Collections", NameAr = "مجموعات فاخرة", Description = "Exclusive luxury perfume collections", DescriptionAr = "مجموعات عطور فاخرة حصرية", DisplayOrder = 4 },
                new Category { Id = 5, Name = "Gift Sets", NameAr = "هدايا", Description = "Perfect gift sets for loved ones", DescriptionAr = "مجموعات هدايا مثالية لأحبائك", DisplayOrder = 5 }
            );
            modelBuilder.Entity<ShippingZone>().HasData(
                new ShippingZone { Id = 1, NameAr = "مسقط", NameEn = "Muscat", Cost = 2.00m, EstimatedDays = 2, IsActive = true },
                new ShippingZone { Id = 2, NameAr = "الداخلية", NameEn = "Ad Dakhiliyah", Cost = 3.00m, EstimatedDays = 3, IsActive = true },
                new ShippingZone { Id = 3, NameAr = "ظفار", NameEn = "Dhofar", Cost = 4.00m, EstimatedDays = 4, IsActive = true }
            );

            // تهيئة بيانات أولية للإعدادات
            modelBuilder.Entity<SiteSetting>().HasData(
                new SiteSetting { Id = 1, Key = "AnnouncementBar", Value = "خصم 20% لفترة محدودة على جميع العطور!", IsEnabled = true }
            );
        }
    }
}