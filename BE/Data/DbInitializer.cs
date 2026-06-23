using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ClothingShop.Domain.Entities;

namespace ClothingShop.Infrastructure.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
        {
            // Ensure database is created
            await context.Database.EnsureCreatedAsync();

            // Tu dong cap nhat cot Points trong SQL Server neu chua co
            try
            {
                if (context.Database.IsSqlServer())
                {
                    await context.Database.ExecuteSqlRawAsync(@"
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.AspNetUsers') AND name = 'Points')
                        BEGIN
                            ALTER TABLE dbo.AspNetUsers ADD Points INT NOT NULL DEFAULT 0;
                        END
                    ");
                }
            }
            catch (Exception)
            {
                // Bo qua loi
            }

            // Tu dong cap nhat cot AvatarUrl trong SQL Server hoac Postgres neu chua co
            try
            {
                if (context.Database.IsSqlServer())
                {
                    await context.Database.ExecuteSqlRawAsync(@"
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.AspNetUsers') AND name = 'AvatarUrl')
                        BEGIN
                            ALTER TABLE dbo.AspNetUsers ADD AvatarUrl NVARCHAR(500) NULL;
                        END
                    ");
                }
                else
                {
                    // Neon PostgreSQL (Postgres 9.6+ ho tro ADD COLUMN IF NOT EXISTS)
                    await context.Database.ExecuteSqlRawAsync(@"
                        ALTER TABLE ""AspNetUsers"" ADD COLUMN IF NOT EXISTS ""AvatarUrl"" text NULL;
                    ");
                }
            }
            catch (Exception)
            {
                // Bo qua loi
            }

            // 1. Seed Roles
            string[] roleNames = { "Admin", "User" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new ApplicationRole(roleName));
                }
            }

            // 2. Seed Users
            var adminEmail = "admin@clothingshop.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FullName = "System Administrator",
                    Address = "123 Main St, Hanoi",
                    CreatedDate = DateTime.UtcNow
                };
                var result = await userManager.CreateAsync(adminUser, "Admin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                    await userManager.AddToRoleAsync(adminUser, "User");
                }
            }
            else
            {
                var token = await userManager.GeneratePasswordResetTokenAsync(adminUser);
                await userManager.ResetPasswordAsync(adminUser, token, "Admin@123");
            }

            var customerEmail = "user@clothingshop.com";
            var customerUser = await userManager.FindByEmailAsync(customerEmail);
            if (customerUser == null)
            {
                customerUser = new ApplicationUser
                {
                    UserName = customerEmail,
                    Email = customerEmail,
                    EmailConfirmed = true,
                    FullName = "Nguyen Van Khach",
                    Address = "456 Le Loi, Ho Chi Minh City",
                    CreatedDate = DateTime.UtcNow
                };
                var result = await userManager.CreateAsync(customerUser, "Admin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(customerUser, "User");
                }
            }
            else
            {
                var token = await userManager.GeneratePasswordResetTokenAsync(customerUser);
                await userManager.ResetPasswordAsync(customerUser, token, "Admin@123");
            }

            // 3. Seed Brands
            if (!await context.Brands.AnyAsync())
            {
                var brands = new List<Brand>
                {
                    new Brand { Name = "Nike" },
                    new Brand { Name = "Adidas" },
                    new Brand { Name = "Uniqlo" },
                    new Brand { Name = "Zara" },
                    new Brand { Name = "H&M" }
                };
                await context.Brands.AddRangeAsync(brands);
                await context.SaveChangesAsync();
            }

            // 4. Seed Categories
            if (!await context.Categories.AnyAsync())
            {
                var categories = new List<Category>
                {
                    new Category { Name = "Áo Nam" },
                    new Category { Name = "Quần Nam" },
                    new Category { Name = "Áo Nữ" },
                    new Category { Name = "Váy Nữ" },
                    new Category { Name = "Phụ Kiện" }
                };
                await context.Categories.AddRangeAsync(categories);
                await context.SaveChangesAsync();
            }

            // 5. Seed Products, Images, and Variants
            if (!await context.Products.AnyAsync())
            {
                var zara = await context.Brands.FirstAsync(b => b.Name == "Zara");
                var uniqlo = await context.Brands.FirstAsync(b => b.Name == "Uniqlo");
                var hm = await context.Brands.FirstAsync(b => b.Name == "H&M");
                var nike = await context.Brands.FirstAsync(b => b.Name == "Nike");
                var adidas = await context.Brands.FirstAsync(b => b.Name == "Adidas");

                var aoNam = await context.Categories.FirstAsync(c => c.Name == "Áo Nam");
                var quanNam = await context.Categories.FirstAsync(c => c.Name == "Quần Nam");
                var aoNu = await context.Categories.FirstAsync(c => c.Name == "Áo Nữ");
                var vayNu = await context.Categories.FirstAsync(c => c.Name == "Váy Nữ");
                var phuKien = await context.Categories.FirstAsync(c => c.Name == "Phụ Kiện");

                var productsData = new List<(Product Product, List<ProductImage> Images, List<ProductVariant> Variants)>
                {
                    // 1. Áo Sơ Mi Nam Công Sở Zara (Price 450k)
                    (
                        new Product { Name = "Áo Sơ Mi Nam Công Sở Zara", Description = "Áo sơ mi nam Zara chất liệu cotton cao cấp, thoáng mát, form dáng ôm vừa vặn, thích hợp đi làm và hội họp.", Price = 450000.00m, DiscountPrice = 390000.00m, Category = aoNam, Brand = zara },
                        new List<ProductImage> { new ProductImage { ImageUrl = "/images/products/p1-main.jpg", IsMain = true }, new ProductImage { ImageUrl = "/images/products/p1-sub.jpg", IsMain = false } },
                        new List<ProductVariant> { new ProductVariant { Size = "M", Color = "White", Quantity = 50, SKU = "ZARA-SHIRT-M-WHT" }, new ProductVariant { Size = "L", Color = "White", Quantity = 30, SKU = "ZARA-SHIRT-L-WHT" }, new ProductVariant { Size = "M", Color = "Blue", Quantity = 20, SKU = "ZARA-SHIRT-M-BLU" } }
                    ),
                    // 2. Áo Thun Nam Trơn Basic Uniqlo (Price 250k)
                    (
                        new Product { Name = "Áo Thun Nam Trơn Basic Uniqlo", Description = "Áo thun nam Uniqlo 100% cotton mềm mại, co giãn tốt, công nghệ Dry-Ex thấm hút mồ hôi hiệu quả.", Price = 250000.00m, Category = aoNam, Brand = uniqlo },
                        new List<ProductImage> { new ProductImage { ImageUrl = "/images/products/p2-main.jpg", IsMain = true } },
                        new List<ProductVariant> { new ProductVariant { Size = "S", Color = "Black", Quantity = 100, SKU = "UNI-TSHIRT-S-BLK" }, new ProductVariant { Size = "M", Color = "Black", Quantity = 150, SKU = "UNI-TSHIRT-M-BLK" }, new ProductVariant { Size = "L", Color = "White", Quantity = 120, SKU = "UNI-TSHIRT-L-WHT" } }
                    ),
                    // 3. Áo Hoodie Nam Thể Thao H&M (Price 550k)
                    (
                        new Product { Name = "Áo Hoodie Nam Thể Thao H&M", Description = "Áo hoodie phong cách thể thao, năng động từ H&M, chất nỉ ấm áp cho những ngày lạnh.", Price = 550000.00m, DiscountPrice = 490000.00m, Category = aoNam, Brand = hm },
                        new List<ProductImage> { new ProductImage { ImageUrl = "/images/products/p3-main.jpg", IsMain = true } },
                        new List<ProductVariant> { new ProductVariant { Size = "M", Color = "Grey", Quantity = 40, SKU = "HM-HD-M-GRY" }, new ProductVariant { Size = "L", Color = "Grey", Quantity = 35, SKU = "HM-HD-L-GRY" } }
                    ),
                    // 4. Áo Khoác Denim Nam Zara (Price 850k)
                    (
                        new Product { Name = "Áo Khoác Denim Nam Zara", Description = "Áo khoác denim chất lừ từ Zara, thiết kế cổ điển pha nét hiện đại, vải jean dày dặn.", Price = 850000.00m, DiscountPrice = 790000.00m, Category = aoNam, Brand = zara },
                        new List<ProductImage> { new ProductImage { ImageUrl = "/images/products/p4-main.jpg", IsMain = true } },
                        new List<ProductVariant> { new ProductVariant { Size = "M", Color = "Blue", Quantity = 15, SKU = "ZARA-DENIM-M-BLU" }, new ProductVariant { Size = "L", Color = "Blue", Quantity = 10, SKU = "ZARA-DENIM-L-BLU" } }
                    ),
                    // 5. Quần Tây Nam Lịch Lãm Uniqlo
                    (
                        new Product { Name = "Quần Tây Nam Lịch Lãm Uniqlo", Description = "Quần tây nam thiết kế tinh tế, chất vải chống nhăn tuyệt đối, chuẩn form công sở.", Price = 600000.00m, Category = quanNam, Brand = uniqlo },
                        new List<ProductImage> { new ProductImage { ImageUrl = "/images/products/p5-main.jpg", IsMain = true } },
                        new List<ProductVariant> { new ProductVariant { Size = "30", Color = "Black", Quantity = 30, SKU = "UNI-PANTS-30-BLK" }, new ProductVariant { Size = "31", Color = "Black", Quantity = 40, SKU = "UNI-PANTS-31-BLK" } }
                    ),
                    // 6. Quần Jean Nam Skinny Zara
                    (
                        new Product { Name = "Quần Jean Nam Skinny Zara", Description = "Quần jean skinny Zara co giãn nhẹ, ôm dáng trẻ trung năng động, dễ phối đồ.", Price = 750000.00m, DiscountPrice = 690000.00m, Category = quanNam, Brand = zara },
                        new List<ProductImage> { new ProductImage { ImageUrl = "/images/products/p6-main.jpg", IsMain = true } },
                        new List<ProductVariant> { new ProductVariant { Size = "30", Color = "Blue", Quantity = 25, SKU = "ZARA-JEAN-30-BLU" }, new ProductVariant { Size = "32", Color = "Blue", Quantity = 20, SKU = "ZARA-JEAN-32-BLU" } }
                    ),
                    // 7. Quần Short Thể Thao Nike Pro
                    (
                        new Product { Name = "Quần Short Thể Thao Nike Pro", Description = "Quần short thể thao Nike Pro chuyên dụng chạy bộ và tập gym, chất liệu siêu nhẹ thoáng khí.", Price = 400000.00m, Category = quanNam, Brand = nike },
                        new List<ProductImage> { new ProductImage { ImageUrl = "/images/products/p7-main.jpg", IsMain = true } },
                        new List<ProductVariant> { new ProductVariant { Size = "M", Color = "Black", Quantity = 80, SKU = "NIKE-SHORT-M-BLK" }, new ProductVariant { Size = "L", Color = "Black", Quantity = 60, SKU = "NIKE-SHORT-L-BLK" } }
                    ),
                    // 8. Quần Jogger Nỉ Thể Thao Adidas
                    (
                        new Product { Name = "Quần Jogger Nỉ Thể Thao Adidas", Description = "Quần jogger nỉ Adidas 3 sọc huyền thoại, thoải mái vận động cả ngày dài.", Price = 900000.00m, DiscountPrice = 850000.00m, Category = quanNam, Brand = adidas },
                        new List<ProductImage> { new ProductImage { ImageUrl = "/images/products/p8-main.jpg", IsMain = true } },
                        new List<ProductVariant> { new ProductVariant { Size = "M", Color = "Black", Quantity = 50, SKU = "ADI-JOG-M-BLK" }, new ProductVariant { Size = "L", Color = "Grey", Quantity = 40, SKU = "ADI-JOG-L-GRY" } }
                    ),
                    // 9. Áo Thun Nữ Cotton Uniqlo
                    (
                        new Product { Name = "Áo Thun Nữ Cotton Uniqlo", Description = "Áo thun nữ Uniqlo cổ tròn cơ bản, nhiều màu sắc dễ thương, chất liệu cotton mịn màng.", Price = 199000.00m, Category = aoNu, Brand = uniqlo },
                        new List<ProductImage> { new ProductImage { ImageUrl = "/images/products/p9-main.jpg", IsMain = true } },
                        new List<ProductVariant> { new ProductVariant { Size = "S", Color = "White", Quantity = 60, SKU = "UNI-WTSHIRT-S-WHT" }, new ProductVariant { Size = "M", Color = "Red", Quantity = 45, SKU = "UNI-WTSHIRT-M-RED" } }
                    ),
                    // 10. Áo Sơ Mi Nữ Tay Phồng H&M
                    (
                        new Product { Name = "Áo Sơ Mi Nữ Tay Phồng H&M", Description = "Áo sơ mi nữ H&M phong cách tiểu thư cổ điển, tay phồng nhẹ nhàng thanh lịch.", Price = 350000.00m, DiscountPrice = 290000.00m, Category = aoNu, Brand = hm },
                        new List<ProductImage> { new ProductImage { ImageUrl = "/images/products/p10-main.jpg", IsMain = true } },
                        new List<ProductVariant> { new ProductVariant { Size = "M", Color = "White", Quantity = 30, SKU = "HM-WSHIRT-M-WHT" }, new ProductVariant { Size = "S", Color = "Blue", Quantity = 25, SKU = "HM-WSHIRT-S-BLU" } }
                    ),
                    // 11. Áo Croptop Nữ Cá Tính Zara
                    (
                        new Product { Name = "Áo Croptop Nữ Cá Tính Zara", Description = "Áo croptop ôm sát Zara tôn dáng, chất thun gân mát mẻ thích hợp mùa hè.", Price = 220000.00m, Category = aoNu, Brand = zara },
                        new List<ProductImage> { new ProductImage { ImageUrl = "/images/products/p11-main.jpg", IsMain = true } },
                        new List<ProductVariant> { new ProductVariant { Size = "S", Color = "Black", Quantity = 70, SKU = "ZARA-CROP-S-BLK" }, new ProductVariant { Size = "M", Color = "White", Quantity = 80, SKU = "ZARA-CROP-M-WHT" } }
                    ),
                    // 12. Áo Vest Blazer Nữ Thanh Lịch H&M
                    (
                        new Product { Name = "Áo Vest Blazer Nữ Thanh Lịch H&M", Description = "Áo vest blazer dáng rộng thời trang H&M, thích hợp khoác ngoài đi làm hoặc đi chơi.", Price = 750000.00m, DiscountPrice = 650000.00m, Category = aoNu, Brand = hm },
                        new List<ProductImage> { new ProductImage { ImageUrl = "/images/products/p12-main.jpg", IsMain = true } },
                        new List<ProductVariant> { new ProductVariant { Size = "M", Color = "Grey", Quantity = 20, SKU = "HM-BLZ-M-GRY" } }
                    ),
                    // 13. Váy Hoa Nhí Đi Biển Zara
                    (
                        new Product { Name = "Váy Hoa Nhí Đi Biển Zara", Description = "Váy hai dây hoa nhí Zara thướt tha, chất voan mát rượi thích hợp những chuyến du lịch.", Price = 650000.00m, DiscountPrice = 550000.00m, Category = vayNu, Brand = zara },
                        new List<ProductImage> { new ProductImage { ImageUrl = "/images/products/p13-main.jpg", IsMain = true } },
                        new List<ProductVariant> { new ProductVariant { Size = "S", Color = "Red", Quantity = 15, SKU = "ZARA-DRS-S-RED" }, new ProductVariant { Size = "M", Color = "Red", Quantity = 20, SKU = "ZARA-DRS-M-RED" } }
                    ),
                    // 14. Chân Váy Chữ A Công Sở Uniqlo
                    (
                        new Product { Name = "Chân Váy Chữ A Công Sở Uniqlo", Description = "Chân váy chữ A Uniqlo dễ phối đồ, chất vải tuyết mưa dày dặn đứng dáng.", Price = 399000.00m, Category = vayNu, Brand = uniqlo },
                        new List<ProductImage> { new ProductImage { ImageUrl = "/images/products/p14-main.jpg", IsMain = true } },
                        new List<ProductVariant> { new ProductVariant { Size = "M", Color = "Black", Quantity = 35, SKU = "UNI-SKT-M-BLK" } }
                    ),
                    // 15. Đầm Dạ Hội Sang Trọng H&M
                    (
                        new Product { Name = "Đầm Dạ Hội Sang Trọng H&M", Description = "Đầm ôm body trễ vai quý phái thích hợp cho các buổi tiệc tối lung linh từ H&M.", Price = 1200000.00m, DiscountPrice = 990000.00m, Category = vayNu, Brand = hm },
                        new List<ProductImage> { new ProductImage { ImageUrl = "/images/products/p15-main.jpg", IsMain = true } },
                        new List<ProductVariant> { new ProductVariant { Size = "S", Color = "Black", Quantity = 10, SKU = "HM-EVE-S-BLK" }, new ProductVariant { Size = "M", Color = "Black", Quantity = 8, SKU = "HM-EVE-M-BLK" } }
                    ),
                    // 16. Đầm Suông Linen Mùa Hè Uniqlo
                    (
                        new Product { Name = "Đầm Suông Linen Mùa Hè Uniqlo", Description = "Đầm suông chất liệu linen tự nhiên từ Uniqlo, nhẹ nhàng thoáng mát tốt cho da.", Price = 500000.00m, DiscountPrice = 450000.00m, Category = vayNu, Brand = uniqlo },
                        new List<ProductImage> { new ProductImage { ImageUrl = "/images/products/p16-main.jpg", IsMain = true } },
                        new List<ProductVariant> { new ProductVariant { Size = "M", Color = "White", Quantity = 22, SKU = "UNI-LN-M-WHT" } }
                    ),
                    // 17. Nón Kết Thể Thao Nike Classic
                    (
                        new Product { Name = "Nón Kết Thể Thao Nike Classic", Description = "Nón lưỡi trai Nike chất liệu dù chống thấm nước, logo thêu nổi bật phong cách.", Price = 300000.00m, Category = phuKien, Brand = nike },
                        new List<ProductImage> { new ProductImage { ImageUrl = "/images/products/p17-main.jpg", IsMain = true } },
                        new List<ProductVariant> { new ProductVariant { Size = "FreeSize", Color = "Black", Quantity = 150, SKU = "NIKE-CAP-FS-BLK" } }
                    ),
                    // 18. Tất Cổ Cao Thể Thao Adidas Trio
                    (
                        new Product { Name = "Tất Cổ Cao Thể Thao Adidas Trio", Description = "Set 3 đôi tất cổ cao Adidas cotton êm ái, thấm hút mồ hôi và nâng đỡ bàn chân.", Price = 180000.00m, DiscountPrice = 150000.00m, Category = phuKien, Brand = adidas },
                        new List<ProductImage> { new ProductImage { ImageUrl = "/images/products/p18-main.jpg", IsMain = true } },
                        new List<ProductVariant> { new ProductVariant { Size = "FreeSize", Color = "White", Quantity = 300, SKU = "ADI-SOX-FS-WHT" } }
                    ),
                    // 19. Thắt Lưng Da Nam Zara Classic
                    (
                        new Product { Name = "Thắt Lưng Da Nam Zara Classic", Description = "Thắt lưng da bò thật Zara, mặt khóa kim loại chống gỉ sang trọng.", Price = 490000.00m, Category = phuKien, Brand = zara },
                        new List<ProductImage> { new ProductImage { ImageUrl = "/images/products/p19-main.jpg", IsMain = true } },
                        new List<ProductVariant> { new ProductVariant { Size = "95", Color = "Black", Quantity = 40, SKU = "ZARA-BELT-95-BLK" } }
                    ),
                    // 20. Kính Mát Thời Trang H&M Retro
                    (
                        new Product { Name = "Kính Mát Thời Trang H&M Retro", Description = "Kính râm gọng nhựa H&M chống tia UV tuyệt đối, phụ kiện không thể thiếu khi ra đường.", Price = 290000.00m, DiscountPrice = 250000.00m, Category = phuKien, Brand = hm },
                        new List<ProductImage> { new ProductImage { ImageUrl = "/images/products/p20-main.jpg", IsMain = true } },
                        new List<ProductVariant> { new ProductVariant { Size = "FreeSize", Color = "Black", Quantity = 60, SKU = "HM-GLASS-FS-BLK" } }
                    )
                };

                foreach (var item in productsData)
                {
                    await context.Products.AddAsync(item.Product);
                    await context.SaveChangesAsync(); // Generates Product.Id

                    foreach (var img in item.Images)
                    {
                        img.ProductId = item.Product.Id;
                        await context.ProductImages.AddAsync(img);
                    }

                    foreach (var v in item.Variants)
                    {
                        v.ProductId = item.Product.Id;
                        await context.ProductVariants.AddAsync(v);
                    }
                }
                await context.SaveChangesAsync();
            }

            // 6. Seed Vouchers
            if (!await context.Vouchers.AnyAsync())
            {
                var vouchers = new List<Voucher>
                {
                    new Voucher { Code = "HE2026", DiscountPercent = 15, ExpiryDate = DateTime.UtcNow.AddMonths(3), UsageLimit = 100, UsedCount = 0 },
                    new Voucher { Code = "SALE50K", DiscountAmount = 50000.00m, ExpiryDate = DateTime.UtcNow.AddMonths(6), UsageLimit = 500, UsedCount = 0 },
                    new Voucher { Code = "WELCOME", DiscountPercent = 10, ExpiryDate = DateTime.UtcNow.AddMonths(12), UsageLimit = 1000, UsedCount = 0 }
                };
                await context.Vouchers.AddRangeAsync(vouchers);
                await context.SaveChangesAsync();
            }

            // 7. Seed Reviews
            if (!await context.Reviews.AnyAsync())
            {
                var user = await userManager.FindByEmailAsync(customerEmail);
                var product1 = await context.Products.FirstOrDefaultAsync(p => p.Name.Contains("Sơ Mi Nam Công Sở"));
                var product2 = await context.Products.FirstOrDefaultAsync(p => p.Name.Contains("Áo Thun Nam Trơn"));
                
                if (user != null && product1 != null && product2 != null)
                {
                    var reviews = new List<Review>
                    {
                        new Review { ProductId = product1.Id, UserId = user.Id, Rating = 5, Comment = "Áo mặc rất đẹp, đúng form công sở, chất vải mát ít nhăn.", CreatedDate = DateTime.UtcNow, IsApproved = true },
                        new Review { ProductId = product2.Id, UserId = user.Id, Rating = 4, Comment = "Áo thun mặc ở nhà rất thoải mái, co giãn tốt.", CreatedDate = DateTime.UtcNow, IsApproved = true }
                    };
                    await context.Reviews.AddRangeAsync(reviews);
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}
