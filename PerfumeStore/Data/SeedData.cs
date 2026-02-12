using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PerfumeStore.Models;

namespace PerfumeStore.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Apply migrations
            await context.Database.MigrateAsync();

            // Seed Roles
            string[] roles = { "Admin", "Manager", "Customer" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Seed Admin User
            var adminEmail = "admin@perfumestore.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "Admin",
                    LastName = "User",
                    EmailConfirmed = true,
                    CreatedAt = DateTime.Now
                };
                await userManager.CreateAsync(adminUser, "Admin123!");
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }

            // Seed Sample Products (only if no products exist)
            if (!await context.Products.AnyAsync())
            {
                var categories = await context.Categories.ToListAsync();
                var products = new List<Product>
                {
                    new Product
                    {
                        Name = "Royal Oud",
                        NameAr = "عود ملكي",
                        Brand = "Al Haramain",
                        BrandAr = "الحرمين",
                        Price = 150.00m,
                        OldPrice = 180.00m,
                        Description = "A majestic blend of rare oud and precious spices",
                        DescriptionAr = "مزيج ملكي من العود النادر والتوابل الثمينة",
                        ScentFamily = "Oriental",
                        ScentFamilyAr = "شرقي",
                        Size = "100ml",
                        Gender = "Men",
                        Concentration = "EDP",
                        StockQuantity = 50,
                        CategoryId = 1,
                        IsFeatured = true,
                        IsActive = true,
                        ImageUrl = "/images/products/placeholder.jpg"
                    },
                    new Product
                    {
                        Name = "Rose Garden",
                        NameAr = "حديقة الورود",
                        Brand = "Rasasi",
                        BrandAr = "الرصاصي",
                        Price = 85.00m,
                        Description = "A beautiful bouquet of Bulgarian roses",
                        DescriptionAr = "باقة جميلة من الورود البلغارية",
                        ScentFamily = "Floral",
                        ScentFamilyAr = "زهر",
                        Size = "75ml",
                        Gender = "Women",
                        Concentration = "EDT",
                        StockQuantity = 30,
                        CategoryId = 2,
                        IsFeatured = true,
                        IsActive = true,
                        ImageUrl = "/images/products/placeholder.jpg"
                    },
                    new Product
                    {
                        Name = "Amber Nights",
                        NameAr = "ليالي العنبر",
                        Brand = "Swiss Arabian",
                        BrandAr = "سويس أرابيان",
                        Price = 120.00m,
                        OldPrice = 145.00m,
                        Description = "Warm amber with hints of vanilla and musk",
                        DescriptionAr = "عنبر دافئ مع لمسات من الفانيلا والمسك",
                        ScentFamily = "Amber",
                        ScentFamilyAr = "عنبر",
                        Size = "100ml",
                        Gender = "Unisex",
                        Concentration = "EDP",
                        StockQuantity = 25,
                        CategoryId = 3,
                        IsNewArrival = true,
                        IsActive = true,
                        ImageUrl = "/images/products/placeholder.jpg"
                    },
                    new Product
                    {
                        Name = "Pure Musk",
                        NameAr = "مسك نقي",
                        Brand = "Abdul Samad Al Qurashi",
                        BrandAr = "عبد الصمد القرشي",
                        Price = 200.00m,
                        Description = "Pure white musk from the finest sources",
                        DescriptionAr = "مسك أبيض نقي من أجمل المصادر",
                        ScentFamily = "Musk",
                        ScentFamilyAr = "مسك",
                        Size = "50ml",
                        Gender = "Unisex",
                        Concentration = "Parfum",
                        StockQuantity = 15,
                        CategoryId = 4,
                        IsFeatured = true,
                        IsActive = true,
                        ImageUrl = "/images/products/placeholder.jpg"
                    },
                    new Product
                    {
                        Name = "Desert Rose",
                        NameAr = "وردة الصحراء",
                        Brand = "Arabian Oud",
                        BrandAr = "العربية للعود",
                        Price = 95.00m,
                        Description = "A unique blend of desert flowers and precious woods",
                        DescriptionAr = "مزيج فريد من أزهار الصحراء والأخشاب الثمينة",
                        ScentFamily = "Woody Floral",
                        ScentFamilyAr = "خشبي زهري",
                        Size = "75ml",
                        Gender = "Women",
                        Concentration = "EDP",
                        StockQuantity = 40,
                        CategoryId = 2,
                        IsNewArrival = true,
                        IsActive = true,
                        ImageUrl = "/images/products/placeholder.jpg"
                    }
                };
                await context.Products.AddRangeAsync(products);
                await context.SaveChangesAsync();
            }

            // Seed Sample Coupon
            if (!await context.Coupons.AnyAsync())
            {
                var coupon = new Coupon
                {
                    Code = "WELCOME10",
                    Description = "10% off on your first order",
                    DescriptionAr = "خصم 10% على طلبك الأول",
                    DiscountType = "Percentage",
                    DiscountValue = 10,
                    MaxDiscount = 50,
                    MinOrderAmount = 100,
                    UsageLimit = 100,
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddYears(1),
                    IsActive = true
                };
                await context.Coupons.AddAsync(coupon);
                await context.SaveChangesAsync();
            }
        }
    }
}