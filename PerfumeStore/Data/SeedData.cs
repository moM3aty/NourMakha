using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PerfumeStore.Models;

namespace PerfumeStore.Data
{
    public static class SeedData
    {
        public static async Task Initialize(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            // Ensure database is created
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
                    IsEmailVerified = true,
                    CreatedAt = DateTime.Now
                };
                await userManager.CreateAsync(adminUser, "Admin@123");
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }

            // Seed Sample Products
            if (!await context.Products.AnyAsync())
            {
                var products = new List<Product>
                {
                    new Product
                    {
                        Name = "Oud Royal",
                        NameAr = "عود رويال",
                        Brand = "Arabian Luxury",
                        BrandAr = "لوكس عربي",
                        Price = 299.99m,
                        OldPrice = 399.99m,
                        Description = "A majestic oud fragrance with rich amber and musk notes. Perfect for special occasions.",
                        DescriptionAr = "عطر عود فاخر برائحة العنببر والمسك الغنية. مثالي للمناسبات الخاصة.",
                        ScentFamily = "Oud",
                        ScentFamilyAr = "عود",
                        Size = "100ml",
                        Gender = "Unisex",
                        Concentration = "EDP",
                        StockQuantity = 50,
                        CategoryId = 4,
                        IsFeatured = true,
                        IsNewArrival = true,
                        ImageUrl = "/images/products/oud-royal.jpg"
                    },
                    new Product
                    {
                        Name = "Rose Mystique",
                        NameAr = "روز ميستيك",
                        Brand = "Paris Elegance",
                        BrandAr = "أناقة باريس",
                        Price = 189.99m,
                        OldPrice = 249.99m,
                        Description = "An enchanting rose fragrance with hints of jasmine and peony. Feminine and elegant.",
                        DescriptionAr = "عطر ورد ساحر مع لمسات من الياسمين والفاوانيا. أنثوي وراقي.",
                        ScentFamily = "Floral",
                        ScentFamilyAr = "زهر",
                        Size = "75ml",
                        Gender = "Women",
                        Concentration = "EDP",
                        StockQuantity = 75,
                        CategoryId = 2,
                        IsFeatured = true,
                        ImageUrl = "/images/products/rose-mystique.jpg"
                    },
                    new Product
                    {
                        Name = "Blue Ocean",
                        NameAr = "المحيط الأزرق",
                        Brand = "Aqua Fresh",
                        BrandAr = "أكوا فريش",
                        Price = 129.99m,
                        Description = "Fresh aquatic fragrance with marine notes and citrus. Perfect for daily wear.",
                        DescriptionAr = "عطر مائي منعش برائحة البحر والحمضيات. مثالي للاستخدام اليومي.",
                        ScentFamily = "Aquatic",
                        ScentFamilyAr = "مائي",
                        Size = "100ml",
                        Gender = "Men",
                        Concentration = "EDT",
                        StockQuantity = 100,
                        CategoryId = 1,
                        IsNewArrival = true,
                        ImageUrl = "/images/products/blue-ocean.jpg"
                    },
                    new Product
                    {
                        Name = "Amber Nights",
                        NameAr = "ليالي العنبر",
                        Brand = "Oriental Treasures",
                        BrandAr = "كنوز شرقية",
                        Price = 349.99m,
                        Description = "Warm amber fragrance with vanilla and sandalwood. Intoxicating and sensual.",
                        DescriptionAr = "عطر عنبر دافئ مع الفانييلا وخشب الصندل. مسكر وحسي.",
                        ScentFamily = "Amber",
                        ScentFamilyAr = "عنبر",
                        Size = "100ml",
                        Gender = "Unisex",
                        Concentration = "Parfum",
                        StockQuantity = 30,
                        CategoryId = 4,
                        IsFeatured = true,
                        ImageUrl = "/images/products/amber-nights.jpg"
                    },
                    new Product
                    {
                        Name = "Citrus Breeze",
                        NameAr = "نسيم الحمضيات",
                        Brand = "Mediterranean Scents",
                        BrandAr = "روائح متوسطية",
                        Price = 89.99m,
                        Description = "Energetic citrus blend with lemon, bergamot, and grapefruit. Refreshing and uplifting.",
                        DescriptionAr = "مزيج حمضيات منعش مع الليمون والبرغموت والجريب فروت. منعش ومنشط.",
                        ScentFamily = "Citrus",
                        ScentFamilyAr = "حمضيات",
                        Size = "50ml",
                        Gender = "Unisex",
                        Concentration = "EDT",
                        StockQuantity = 120,
                        CategoryId = 3,
                        ImageUrl = "/images/products/citrus-breeze.jpg"
                    },
                    new Product
                    {
                        Name = "Leather Prestige",
                        NameAr = "برستيج جلد",
                        Brand = "Gentleman's Choice",
                        BrandAr = "اختيار الجنتلمان",
                        Price = 279.99m,
                        Description = "Sophisticated leather fragrance with tobacco and spices. Bold and masculine.",
                        DescriptionAr = "عطر جلد راقي مع التبغ والتوابل. جريء ورجولي.",
                        ScentFamily = "Leather",
                        ScentFamilyAr = "جلد",
                        Size = "100ml",
                        Gender = "Men",
                        Concentration = "EDP",
                        StockQuantity = 45,
                        CategoryId = 1,
                        IsFeatured = true,
                        ImageUrl = "/images/products/leather-prestige.jpg"
                    },
                    new Product
                    {
                        Name = "Jasmine Dreams",
                        NameAr = "أحلام ياسمين",
                        Brand = "Garden Collection",
                        BrandAr = "مجموعة الحديقة",
                        Price = 159.99m,
                        Description = "Romantic jasmine fragrance with white flowers and honey. Dreamy and romantic.",
                        DescriptionAr = "عطر ياسمين رومانسي مع الزهور البيضاء والعسل. حالم ورومانسي.",
                        ScentFamily = "Floral",
                        ScentFamilyAr = "زهر",
                        Size = "75ml",
                        Gender = "Women",
                        Concentration = "EDP",
                        StockQuantity = 60,
                        CategoryId = 2,
                        ImageUrl = "/images/products/jasmine-dreams.jpg"
                    },
                    new Product
                    {
                        Name = "Sandalwood Essence",
                        NameAr = "جوهر خشب الصندل",
                        Brand = "Natural Woods",
                        BrandAr = "أخشاب طبيعية",
                        Price = 199.99m,
                        Description = "Pure sandalwood fragrance with cedar and vetiver. Calming and meditative.",
                        DescriptionAr = "عطر خشب صندل نقي مع الأرز والفيتيفر. مهدئ وروحاني.",
                        ScentFamily = "Woody",
                        ScentFamilyAr = "خشبي",
                        Size = "100ml",
                        Gender = "Unisex",
                        Concentration = "EDP",
                        StockQuantity = 55,
                        CategoryId = 3,
                        ImageUrl = "/images/products/sandalwood-essence.jpg"
                    }
                };

                context.Products.AddRange(products);
                await context.SaveChangesAsync();
            }

            // Seed Coupon
            if (!await context.Coupons.AnyAsync())
            {
                context.Coupons.Add(new Coupon
                {
                    Code = "WELCOME20",
                    DiscountType = "Percentage",
                    DiscountValue = 20,
                    MinimumOrderAmount = 100,
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddMonths(3),
                    IsActive = true
                });
                await context.SaveChangesAsync();
            }
        }
    }
}
