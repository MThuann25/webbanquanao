-- CREATE DATABASE ClothingShopDB
-- GO
-- USE ClothingShopDB
-- GO

-- --------------------------------------------------
-- 1. DROP TABLES IF THEY EXIST (ORDER MATTERS)
-- --------------------------------------------------
IF OBJECT_ID('dbo.ChatLogs', 'U') IS NOT NULL DROP TABLE dbo.ChatLogs;
IF OBJECT_ID('dbo.Reviews', 'U') IS NOT NULL DROP TABLE dbo.Reviews;
IF OBJECT_ID('dbo.OrderDetails', 'U') IS NOT NULL DROP TABLE dbo.OrderDetails;
IF OBJECT_ID('dbo.Orders', 'U') IS NOT NULL DROP TABLE dbo.Orders;
IF OBJECT_ID('dbo.CartItems', 'U') IS NOT NULL DROP TABLE dbo.CartItems;
IF OBJECT_ID('dbo.Carts', 'U') IS NOT NULL DROP TABLE dbo.Carts;
IF OBJECT_ID('dbo.ProductVariants', 'U') IS NOT NULL DROP TABLE dbo.ProductVariants;
IF OBJECT_ID('dbo.ProductImages', 'U') IS NOT NULL DROP TABLE dbo.ProductImages;
IF OBJECT_ID('dbo.Products', 'U') IS NOT NULL DROP TABLE dbo.Products;
IF OBJECT_ID('dbo.Brands', 'U') IS NOT NULL DROP TABLE dbo.Brands;
IF OBJECT_ID('dbo.Categories', 'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID('dbo.Vouchers', 'U') IS NOT NULL DROP TABLE dbo.Vouchers;

-- AspNet Identity Tables (in case they need to be dropped)
IF OBJECT_ID('dbo.AspNetUserTokens', 'U') IS NOT NULL DROP TABLE dbo.AspNetUserTokens;
IF OBJECT_ID('dbo.AspNetUserRoles', 'U') IS NOT NULL DROP TABLE dbo.AspNetUserRoles;
IF OBJECT_ID('dbo.AspNetUserLogins', 'U') IS NOT NULL DROP TABLE dbo.AspNetUserLogins;
IF OBJECT_ID('dbo.AspNetUserClaims', 'U') IS NOT NULL DROP TABLE dbo.AspNetUserClaims;
IF OBJECT_ID('dbo.AspNetRoleClaims', 'U') IS NOT NULL DROP TABLE dbo.AspNetRoleClaims;
IF OBJECT_ID('dbo.AspNetUsers', 'U') IS NOT NULL DROP TABLE dbo.AspNetUsers;
IF OBJECT_ID('dbo.AspNetRoles', 'U') IS NOT NULL DROP TABLE dbo.AspNetRoles;

-- --------------------------------------------------
-- 2. CREATE ASP.NET IDENTITY TABLES (SQL 2014 COMPATIBLE)
-- --------------------------------------------------
CREATE TABLE dbo.AspNetRoles (
    Id NVARCHAR(450) NOT NULL PRIMARY KEY,
    Name NVARCHAR(256) NULL,
    NormalizedName NVARCHAR(256) NULL,
    ConcurrencyStamp NVARCHAR(MAX) NULL
);

CREATE TABLE dbo.AspNetUsers (
    Id NVARCHAR(450) NOT NULL PRIMARY KEY,
    UserName NVARCHAR(256) NULL,
    NormalizedUserName NVARCHAR(256) NULL,
    Email NVARCHAR(256) NULL,
    NormalizedEmail NVARCHAR(256) NULL,
    EmailConfirmed BIT NOT NULL,
    PasswordHash NVARCHAR(MAX) NULL,
    SecurityStamp NVARCHAR(MAX) NULL,
    ConcurrencyStamp NVARCHAR(MAX) NULL,
    PhoneNumber NVARCHAR(MAX) NULL,
    PhoneNumberConfirmed BIT NOT NULL,
    TwoFactorEnabled BIT NOT NULL,
    LockoutEnd DATETIMEOFFSET NULL,
    LockoutEnabled BIT NOT NULL,
    AccessFailedCount INT NOT NULL,
    -- Custom fields
    FullName NVARCHAR(256) NULL,
    Address NVARCHAR(500) NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE()
);

CREATE TABLE dbo.AspNetRoleClaims (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    RoleId NVARCHAR(450) NOT NULL FOREIGN KEY REFERENCES dbo.AspNetRoles(Id) ON DELETE CASCADE,
    ClaimType NVARCHAR(MAX) NULL,
    ClaimValue NVARCHAR(MAX) NULL
);

CREATE TABLE dbo.AspNetUserClaims (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    UserId NVARCHAR(450) NOT NULL FOREIGN KEY REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE,
    ClaimType NVARCHAR(MAX) NULL,
    ClaimValue NVARCHAR(MAX) NULL
);

CREATE TABLE dbo.AspNetUserLogins (
    LoginProvider NVARCHAR(450) NOT NULL,
    ProviderKey NVARCHAR(450) NOT NULL,
    ProviderDisplayName NVARCHAR(MAX) NULL,
    UserId NVARCHAR(450) NOT NULL FOREIGN KEY REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE,
    PRIMARY KEY (LoginProvider, ProviderKey)
);

CREATE TABLE dbo.AspNetUserRoles (
    UserId NVARCHAR(450) NOT NULL FOREIGN KEY REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE,
    RoleId NVARCHAR(450) NOT NULL FOREIGN KEY REFERENCES dbo.AspNetRoles(Id) ON DELETE CASCADE,
    PRIMARY KEY (UserId, RoleId)
);

CREATE TABLE dbo.AspNetUserTokens (
    UserId NVARCHAR(450) NOT NULL FOREIGN KEY REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE,
    LoginProvider NVARCHAR(450) NOT NULL,
    Name NVARCHAR(450) NOT NULL,
    Value NVARCHAR(MAX) NULL,
    PRIMARY KEY (UserId, LoginProvider, Name)
);

-- --------------------------------------------------
-- 3. CREATE BUSINESS LOGIC TABLES
-- --------------------------------------------------
CREATE TABLE dbo.Categories (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    ParentId INT NULL FOREIGN KEY REFERENCES dbo.Categories(Id)
);

CREATE TABLE dbo.Brands (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL
);

CREATE TABLE dbo.Products (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Name NVARCHAR(250) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    Price DECIMAL(18,2) NOT NULL,
    DiscountPrice DECIMAL(18,2) NULL,
    BrandId INT NULL FOREIGN KEY REFERENCES dbo.Brands(Id) ON DELETE SET NULL,
    CategoryId INT NULL FOREIGN KEY REFERENCES dbo.Categories(Id) ON DELETE SET NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    IsActive BIT NOT NULL DEFAULT 1
);

CREATE TABLE dbo.ProductImages (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ProductId INT NOT NULL FOREIGN KEY REFERENCES dbo.Products(Id) ON DELETE CASCADE,
    ImageUrl NVARCHAR(500) NOT NULL,
    IsMain BIT NOT NULL DEFAULT 0
);

CREATE TABLE dbo.ProductVariants (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ProductId INT NOT NULL FOREIGN KEY REFERENCES dbo.Products(Id) ON DELETE CASCADE,
    Size NVARCHAR(50) NOT NULL,
    Color NVARCHAR(50) NOT NULL,
    Quantity INT NOT NULL DEFAULT 0,
    SKU NVARCHAR(100) NULL
);

CREATE TABLE dbo.Vouchers (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Code NVARCHAR(50) NOT NULL UNIQUE,
    DiscountPercent INT NULL,
    DiscountAmount DECIMAL(18,2) NULL,
    ExpiryDate DATETIME2 NOT NULL,
    UsageLimit INT NOT NULL DEFAULT 0,
    UsedCount INT NOT NULL DEFAULT 0
);

CREATE TABLE dbo.Carts (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    UserId NVARCHAR(450) NULL FOREIGN KEY REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE()
);

CREATE TABLE dbo.CartItems (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    CartId INT NOT NULL FOREIGN KEY REFERENCES dbo.Carts(Id) ON DELETE CASCADE,
    ProductVariantId INT NOT NULL FOREIGN KEY REFERENCES dbo.ProductVariants(Id) ON DELETE CASCADE,
    Quantity INT NOT NULL DEFAULT 1
);

CREATE TABLE dbo.Orders (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    UserId NVARCHAR(450) NOT NULL FOREIGN KEY REFERENCES dbo.AspNetUsers(Id),
    OrderDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    Status NVARCHAR(50) NOT NULL, -- Chờ xác nhận, Đang giao, Hoàn thành, Đã hủy
    TotalAmount DECIMAL(18,2) NOT NULL,
    DiscountAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    ShippingAddress NVARCHAR(500) NOT NULL,
    PaymentMethod NVARCHAR(100) NOT NULL, -- COD, VNPay
    VoucherCode NVARCHAR(50) NULL
);

CREATE TABLE dbo.OrderDetails (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    OrderId INT NOT NULL FOREIGN KEY REFERENCES dbo.Orders(Id) ON DELETE CASCADE,
    ProductVariantId INT NOT NULL FOREIGN KEY REFERENCES dbo.ProductVariants(Id),
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL
);

CREATE TABLE dbo.Reviews (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ProductId INT NOT NULL FOREIGN KEY REFERENCES dbo.Products(Id) ON DELETE CASCADE,
    UserId NVARCHAR(450) NOT NULL FOREIGN KEY REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE,
    Rating INT NOT NULL CHECK (Rating >= 1 AND Rating <= 5),
    Comment NVARCHAR(MAX) NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    IsApproved BIT NOT NULL DEFAULT 1
);

CREATE TABLE dbo.ChatLogs (
    Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    UserId NVARCHAR(450) NULL FOREIGN KEY REFERENCES dbo.AspNetUsers(Id) ON DELETE SET NULL,
    Message NVARCHAR(MAX) NOT NULL,
    Response NVARCHAR(MAX) NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE()
);

-- --------------------------------------------------
-- 4. SEED SECTIONS
-- --------------------------------------------------

-- 4.1. Roles & Users (Password is 'Admin@123', standard PBKDF2 hash)
INSERT INTO dbo.AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp) VALUES
('r1', 'Admin', 'ADMIN', 'stamp-admin'),
('r2', 'User', 'USER', 'stamp-user');

INSERT INTO dbo.AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount, FullName, Address, CreatedDate) VALUES
('u1', 'admin@clothingshop.com', 'ADMIN@CLOTHINGSHOP.COM', 'admin@clothingshop.com', 'ADMIN@CLOTHINGSHOP.COM', 1, 'AQAAAAIAAYagAAAAEO9v969+4kFh8Y3r2XW8d5O3aD1h6mD2q3w5e6r7t8y9==', 'sec-stamp-admin', 'con-stamp-admin', 0, 0, 1, 0, 'System Administrator', '123 Main St, Hanoi', GETDATE()),
('u2', 'user@clothingshop.com', 'USER@CLOTHINGSHOP.COM', 'user@clothingshop.com', 'USER@CLOTHINGSHOP.COM', 1, 'AQAAAAIAAYagAAAAEO9v969+4kFh8Y3r2XW8d5O3aD1h6mD2q3w5e6r7t8y9==', 'sec-stamp-user', 'con-stamp-user', 0, 0, 1, 0, 'Nguyễn Văn Khách', '456 Le Loi, Ho Chi Minh City', GETDATE());

INSERT INTO dbo.AspNetUserRoles (UserId, RoleId) VALUES
('u1', 'r1'), -- Admin has Admin role
('u1', 'r2'), -- Admin has User role too
('u2', 'r2'); -- User has User role

-- 4.2. Categories
INSERT INTO dbo.Categories (Name, ParentId) VALUES
(N'Áo Nam', NULL),       -- Id 1
(N'Quần Nam', NULL),     -- Id 2
(N'Áo Nữ', NULL),        -- Id 3
(N'Váy Nữ', NULL),       -- Id 4
(N'Phụ Kiện', NULL);     -- Id 5

-- 4.3. Brands
INSERT INTO dbo.Brands (Name) VALUES
('Nike'),    -- Id 1
('Adidas'),  -- Id 2
('Uniqlo'),  -- Id 3
('Zara'),    -- Id 4
('H&M');     -- Id 5

-- 4.4. Products (20 products)
-- We use placeholder image urls like /images/products/productX.jpg which we can generate or build
INSERT INTO dbo.Products (Name, Description, Price, DiscountPrice, BrandId, CategoryId, CreatedDate, IsActive) VALUES
-- Áo Nam (Cat 1)
(N'Áo Sơ Mi Nam Công Sở Zara', N'Áo sơ mi nam Zara chất liệu cotton cao cấp, thoáng mát, form dáng ôm vừa vặn, thích hợp đi làm và hội họp.', 450000.00, 390000.00, 4, 1, GETDATE(), 1), -- Id 1
(N'Áo Thun Nam Trơn Basic Uniqlo', N'Áo thun nam Uniqlo 100% cotton mềm mại, co giãn tốt, công nghệ Dry-Ex thấm hút mồ hôi hiệu quả.', 250000.00, NULL, 3, 1, GETDATE(), 1), -- Id 2
(N'Áo Hoodie Nam Thể Thao H&M', N'Áo hoodie phong cách thể thao, năng động từ H&M, chất nỉ ấm áp cho những ngày lạnh.', 550000.00, 490000.00, 5, 1, GETDATE(), 1), -- Id 3
(N'Áo Khoác Denim Nam Zara', N'Áo khoác denim chất lừ từ Zara, thiết kế cổ điển pha nét hiện đại, vải jean dày dặn.', 850000.00, 790000.00, 4, 1, GETDATE(), 1), -- Id 4

-- Quần Nam (Cat 2)
(N'Quần Tây Nam Lịch Lãm Uniqlo', N'Quần tây nam thiết kế tinh tế, chất vải chống nhăn tuyệt đối, chuẩn form công sở.', 600000.00, NULL, 3, 2, GETDATE(), 1), -- Id 5
(N'Quần Jean Nam Skinny Zara', N'Quần jean skinny Zara co giãn nhẹ, ôm dáng trẻ trung năng động, dễ phối đồ.', 750000.00, 690000.00, 4, 2, GETDATE(), 1), -- Id 6
(N'Quần Short Thể Thao Nike Pro', N'Quần short thể thao Nike Pro chuyên dụng chạy bộ và tập gym, chất liệu siêu nhẹ thoáng khí.', 400000.00, NULL, 1, 2, GETDATE(), 1), -- Id 7
(N'Quần Jogger Nỉ Thể Thao Adidas', N'Quần jogger nỉ Adidas 3 sọc huyền thoại, thoải mái vận động cả ngày dài.', 900000.00, 850000.00, 2, 2, GETDATE(), 1), -- Id 8

-- Áo Nữ (Cat 3)
(N'Áo Thun Nữ Cotton Uniqlo', N'Áo thun nữ Uniqlo cổ tròn cơ bản, nhiều màu sắc dễ thương, chất liệu cotton mịn màng.', 199000.00, NULL, 3, 3, GETDATE(), 1), -- Id 9
(N'Áo Sơ Mi Nữ Tay Phồng H&M', N'Áo sơ mi nữ H&M phong cách tiểu thư cổ điển, tay phồng nhẹ nhàng thanh lịch.', 350000.00, 290000.00, 5, 3, GETDATE(), 1), -- Id 10
(N'Áo Croptop Nữ Cá Tính Zara', N'Áo croptop ôm sát Zara tôn dáng, chất thun gân mát mẻ thích hợp mùa hè.', 220000.00, NULL, 4, 3, GETDATE(), 1), -- Id 11
(N'Áo Vest Blazer Nữ Thanh Lịch H&M', N'Áo vest blazer dáng rộng thời trang H&M, thích hợp khoác ngoài đi làm hoặc đi chơi.', 750000.00, 650000.00, 5, 3, GETDATE(), 1), -- Id 12

-- Váy Nữ (Cat 4)
(N'Váy Hoa Nhí Đi Biển Zara', N'Váy hai dây hoa nhí Zara thướt tha, chất voan mát rượi thích hợp những chuyến du lịch.', 650000.00, 550000.00, 4, 4, GETDATE(), 1), -- Id 13
(N'Chân Váy Chữ A Công Sở Uniqlo', N'Chân váy chữ A Uniqlo dễ phối đồ, chất vải tuyết mưa dày dặn đứng dáng.', 399000.00, NULL, 3, 4, GETDATE(), 1), -- Id 14
(N'Đầm Dạ Hội Sang Trọng H&M', N'Đầm ôm body trễ vai quý phái thích hợp cho các buổi tiệc tối lung linh từ H&M.', 1200000.00, 990000.00, 5, 4, GETDATE(), 1), -- Id 15
(N'Đầm Suông Linen Mùa Hè Uniqlo', N'Đầm suông chất liệu linen tự nhiên từ Uniqlo, nhẹ nhàng thoáng mát tốt cho da.', 500000.00, 450000.00, 3, 4, GETDATE(), 1), -- Id 16

-- Phụ Kiện (Cat 5)
(N'Nón Kết Thể Thao Nike Classic', N'Nón lưỡi trai Nike chất liệu dù chống thấm nước, logo thêu nổi bật phong cách.', 300000.00, NULL, 1, 5, GETDATE(), 1), -- Id 17
(N'Tất Cổ Cao Thể Thao Adidas Trio', N'Set 3 đôi tất cổ cao Adidas cotton êm ái, thấm hút mồ hôi và nâng đỡ bàn chân.', 180000.00, 150000.00, 2, 5, GETDATE(), 1), -- Id 18
(N'Thắt Lưng Da Nam Zara Classic', N'Thắt lưng da bò thật Zara, mặt khóa kim loại chống gỉ sang trọng.', 490000.00, NULL, 4, 5, GETDATE(), 1), -- Id 19
(N'Kính Mát Thời Trang H&M Retro', N'Kính râm gọng nhựa H&M chống tia UV tuyệt đối, phụ kiện không thể thiếu khi ra đường.', 290000.00, 250000.00, 5, 5, GETDATE(), 1); -- Id 20

-- 4.5. Product Images (Main & Sub images for products)
-- We will seed main images (IsMain = 1) for all 20 products
INSERT INTO dbo.ProductImages (ProductId, ImageUrl, IsMain) VALUES
(1, '/images/products/p1-main.jpg', 1), (1, '/images/products/p1-sub.jpg', 0),
(2, '/images/products/p2-main.jpg', 1),
(3, '/images/products/p3-main.jpg', 1),
(4, '/images/products/p4-main.jpg', 1),
(5, '/images/products/p5-main.jpg', 1),
(6, '/images/products/p6-main.jpg', 1),
(7, '/images/products/p7-main.jpg', 1),
(8, '/images/products/p8-main.jpg', 1),
(9, '/images/products/p9-main.jpg', 1),
(10, '/images/products/p10-main.jpg', 1),
(11, '/images/products/p11-main.jpg', 1),
(12, '/images/products/p12-main.jpg', 1),
(13, '/images/products/p13-main.jpg', 1),
(14, '/images/products/p14-main.jpg', 1),
(15, '/images/products/p15-main.jpg', 1),
(16, '/images/products/p16-main.jpg', 1),
(17, '/images/products/p17-main.jpg', 1),
(18, '/images/products/p18-main.jpg', 1),
(19, '/images/products/p19-main.jpg', 1),
(20, '/images/products/p20-main.jpg', 1);

-- 4.6. Product Variants (Sizes and Colors with quantities)
-- Standard sizes: S, M, L, XL
-- Standard colors: Black, White, Red, Blue, Grey
INSERT INTO dbo.ProductVariants (ProductId, Size, Color, Quantity, SKU) VALUES
-- Product 1: Áo Sơ Mi Zara (Price 450k)
(1, 'M', 'White', 50, 'ZARA-SHIRT-M-WHT'),
(1, 'L', 'White', 30, 'ZARA-SHIRT-L-WHT'),
(1, 'M', 'Blue', 20, 'ZARA-SHIRT-M-BLU'),
-- Product 2: Áo Thun Uniqlo (Price 250k)
(2, 'S', 'Black', 100, 'UNI-TSHIRT-S-BLK'),
(2, 'M', 'Black', 150, 'UNI-TSHIRT-M-BLK'),
(2, 'L', 'White', 120, 'UNI-TSHIRT-L-WHT'),
-- Product 3: Áo Hoodie H&M
(3, 'M', 'Grey', 40, 'HM-HD-M-GRY'),
(3, 'L', 'Grey', 35, 'HM-HD-L-GRY'),
-- Product 4: Áo Khoác Denim Zara
(4, 'M', 'Blue', 15, 'ZARA-DENIM-M-BLU'),
(4, 'L', 'Blue', 10, 'ZARA-DENIM-L-BLU'),
-- Product 5: Quần Tây Uniqlo
(5, '30', 'Black', 30, 'UNI-PANTS-30-BLK'),
(5, '31', 'Black', 40, 'UNI-PANTS-31-BLK'),
-- Product 6: Quần Jean Zara
(6, '30', 'Blue', 25, 'ZARA-JEAN-30-BLU'),
(6, '32', 'Blue', 20, 'ZARA-JEAN-32-BLU'),
-- Product 7: Quần Short Nike
(7, 'M', 'Black', 80, 'NIKE-SHORT-M-BLK'),
(7, 'L', 'Black', 60, 'NIKE-SHORT-L-BLK'),
-- Product 8: Quần Jogger Adidas
(8, 'M', 'Black', 50, 'ADI-JOG-M-BLK'),
(8, 'L', 'Grey', 40, 'ADI-JOG-L-GRY'),
-- Product 9: Áo Thun Nữ Uniqlo
(9, 'S', 'White', 60, 'UNI-WTSHIRT-S-WHT'),
(9, 'M', 'Red', 45, 'UNI-WTSHIRT-M-RED'),
-- Product 10: Áo Sơ Mi Nữ H&M
(10, 'M', 'White', 30, 'HM-WSHIRT-M-WHT'),
(10, 'S', 'Blue', 25, 'HM-WSHIRT-S-BLU'),
-- Product 11: Áo Croptop Zara
(11, 'S', 'Black', 70, 'ZARA-CROP-S-BLK'),
(11, 'M', 'White', 80, 'ZARA-CROP-M-WHT'),
-- Product 12: Blazer H&M
(12, 'M', 'Grey', 20, 'HM-BLZ-M-GRY'),
-- Product 13: Váy Hoa Nhí Zara
(13, 'S', 'Red', 15, 'ZARA-DRS-S-RED'),
(13, 'M', 'Red', 20, 'ZARA-DRS-M-RED'),
-- Product 14: Chân Váy Uniqlo
(14, 'M', 'Black', 35, 'UNI-SKT-M-BLK'),
-- Product 15: Đầm Dạ Hội H&M
(15, 'S', 'Black', 10, 'HM-EVE-S-BLK'),
(15, 'M', 'Black', 8, 'HM-EVE-M-BLK'),
-- Product 16: Đầm Suông Linen Uniqlo
(16, 'M', 'White', 22, 'UNI-LN-M-WHT'),
-- Product 17: Nón Nike
(17, 'FreeSize', 'Black', 150, 'NIKE-CAP-FS-BLK'),
-- Product 18: Tất Adidas
(18, 'FreeSize', 'White', 300, 'ADI-SOX-FS-WHT'),
-- Product 19: Thắt Lưng Zara
(19, '95', 'Black', 40, 'ZARA-BELT-95-BLK'),
-- Product 20: Kính Mát H&M
(20, 'FreeSize', 'Black', 60, 'HM-GLASS-FS-BLK');

-- 4.7. Vouchers
INSERT INTO dbo.Vouchers (Code, DiscountPercent, DiscountAmount, ExpiryDate, UsageLimit, UsedCount) VALUES
('HE2026', 15, NULL, '2026-08-31 23:59:59', 100, 0),
('SALE50K', NULL, 50000.00, '2026-12-31 23:59:59', 500, 0),
('WELCOME', 10, NULL, '2026-12-31 23:59:59', 1000, 0);

-- 4.8. Reviews (Some initial reviews for products)
INSERT INTO dbo.Reviews (ProductId, UserId, Rating, Comment, CreatedDate, IsApproved) VALUES
(1, 'u2', 5, N'Áo mặc rất đẹp, đúng form công sở, chất vải mát ít nhăn.', GETDATE(), 1),
(2, 'u2', 4, N'Áo thun mặc ở nhà rất thoải mái, co giãn tốt.', GETDATE(), 1),
(5, 'u2', 5, N'Quần tây vừa vặn, đứng dáng, rất hài lòng.', GETDATE(), 1);

-- 4.9. Carts & Cart Items for seed User (u2)
-- Create a cart for Nguyen Van Khach (u2)
INSERT INTO dbo.Carts (UserId, CreatedDate) VALUES ('u2', GETDATE());
-- Let's put a variant inside the cart (ZARA-SHIRT-M-WHT variant id = 1)
INSERT INTO dbo.CartItems (CartId, ProductVariantId, Quantity) VALUES (1, 1, 2);

-- 4.10. Orders & Order Details for seed User (u2)
INSERT INTO dbo.Orders (UserId, OrderDate, Status, TotalAmount, DiscountAmount, ShippingAddress, PaymentMethod, VoucherCode) VALUES
('u2', GETDATE(), N'Chờ xác nhận', 1130000.00, 50000.00, N'456 Le Loi, Quận 1, TP. HCM', 'COD', 'SALE50K');

-- Detail:
-- ProductVariant 1 (Zara Shirt M White - UnitPrice 390k) - quantity 2 -> 780k
-- ProductVariant 4 (Uniqlo T-shirt S Black - UnitPrice 250k) - quantity 1 -> 250k
-- Subtotal: 1030k + Shipping (e.g. 150k) = 1180k - 50k discount = 1130k
INSERT INTO dbo.OrderDetails (OrderId, ProductVariantId, Quantity, UnitPrice) VALUES
(1, 1, 2, 390000.00),
(1, 4, 1, 250000.00);

-- 4.11. ChatLogs
INSERT INTO dbo.ChatLogs (UserId, Message, Response, CreatedDate) VALUES
('u2', N'tìm áo sơ mi nam', N'Chào bạn! Dưới đây là gợi ý áo sơ mi nam phù hợp: Áo Sơ Mi Nam Công Sở Zara (390.000đ). Hãy click vào link sau để xem chi tiết.', GETDATE());
