-- ==================================================
-- DATABASE SCRIPT: ClothingShopDB
-- Generated: 2026-06-23 22:01:30
-- ==================================================

-- CREATE DATABASE ClothingShopDB
-- GO
-- USE ClothingShopDB
-- GO

-- --------------------------------------------------
-- 1. DROP TABLES (ORDER MATTERS)
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
IF OBJECT_ID('dbo.AspNetUserTokens', 'U') IS NOT NULL DROP TABLE dbo.AspNetUserTokens;
IF OBJECT_ID('dbo.AspNetUserRoles', 'U') IS NOT NULL DROP TABLE dbo.AspNetUserRoles;
IF OBJECT_ID('dbo.AspNetUserLogins', 'U') IS NOT NULL DROP TABLE dbo.AspNetUserLogins;
IF OBJECT_ID('dbo.AspNetUserClaims', 'U') IS NOT NULL DROP TABLE dbo.AspNetUserClaims;
IF OBJECT_ID('dbo.AspNetRoleClaims', 'U') IS NOT NULL DROP TABLE dbo.AspNetRoleClaims;
IF OBJECT_ID('dbo.AspNetUsers', 'U') IS NOT NULL DROP TABLE dbo.AspNetUsers;
IF OBJECT_ID('dbo.AspNetRoles', 'U') IS NOT NULL DROP TABLE dbo.AspNetRoles;

-- --------------------------------------------------
-- 2. CREATE ASP.NET IDENTITY TABLES
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
-- 3. CREATE BUSINESS TABLES
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
    Status NVARCHAR(50) NOT NULL,
    TotalAmount DECIMAL(18,2) NOT NULL,
    DiscountAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    ShippingAddress NVARCHAR(500) NOT NULL,
    PaymentMethod NVARCHAR(100) NOT NULL,
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
-- 4. SEED DATA
-- --------------------------------------------------

-- 4.1 Roles
INSERT INTO dbo.AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp) VALUES ('r1', N'Admin', N'ADMIN', 'stamp-admin');
INSERT INTO dbo.AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp) VALUES ('r2', N'User', N'USER', 'stamp-user');

-- 4.2 Users
INSERT INTO dbo.AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount, FullName, Address, CreatedDate) VALUES (
  'u1', 'admin@clothingshop.com', 'ADMIN@CLOTHINGSHOP.COM', 'admin@clothingshop.com', 'ADMIN@CLOTHINGSHOP.COM',
  1, 'AQAAAAIAAYagAAAAENCi+SU2qqv8rMq5S1oiTA/nJXFm0BAimE9j//ImkoKqvoVT9xSbkgApE5s1p+SBww==', 'H2QCLXITQL7XVADGBCEYO5CJIF5OHK3V', '63167bc6-5640-4e40-9720-c0103f5bf188',
  NULL, 0, 0, 1, 0,
  N'System Administrator', N'123 Main St, Hanoi', '2026-06-23 21:09:54');
INSERT INTO dbo.AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount, FullName, Address, CreatedDate) VALUES (
  'u2', 'user@clothingshop.com', 'USER@CLOTHINGSHOP.COM', 'user@clothingshop.com', 'USER@CLOTHINGSHOP.COM',
  1, 'AQAAAAIAAYagAAAAEP8x8F/OzTS6UmRIuriXy5R8k1F3WyYcHarNHKCzdWkHmwgdFsA3x6Tl2el79IplRQ==', '3Q6TMI7NMYHEVTRN2BVTEXQE7UVCTOZ7', '58fa39dc-8b1d-4366-b4ed-7356eb267e57',
  NULL, 0, 0, 1, 0,
  N'Nguy?n Van Khách', N'456 Le Loi, Ho Chi Minh City', '2026-06-23 21:09:54');

-- 4.3 UserRoles
INSERT INTO dbo.AspNetUserRoles (UserId, RoleId) VALUES ('u1', 'r1');
INSERT INTO dbo.AspNetUserRoles (UserId, RoleId) VALUES ('u1', 'r2');
INSERT INTO dbo.AspNetUserRoles (UserId, RoleId) VALUES ('u2', 'r2');

-- 4.4 Categories
SET IDENTITY_INSERT dbo.Categories ON;
INSERT INTO dbo.Categories (Id, Name, ParentId) VALUES (1, N'Áo Nam', NULL);
INSERT INTO dbo.Categories (Id, Name, ParentId) VALUES (2, N'Quần Nam', NULL);
INSERT INTO dbo.Categories (Id, Name, ParentId) VALUES (3, N'Áo Nữ', NULL);
INSERT INTO dbo.Categories (Id, Name, ParentId) VALUES (4, N'Váy Nữ', NULL);
INSERT INTO dbo.Categories (Id, Name, ParentId) VALUES (5, N'Phụ Kiện', NULL);
SET IDENTITY_INSERT dbo.Categories OFF;

-- 4.5 Brands
SET IDENTITY_INSERT dbo.Brands ON;
INSERT INTO dbo.Brands (Id, Name) VALUES (1, N'Nike');
INSERT INTO dbo.Brands (Id, Name) VALUES (2, N'Adidas');
INSERT INTO dbo.Brands (Id, Name) VALUES (3, N'Uniqlo');
INSERT INTO dbo.Brands (Id, Name) VALUES (4, N'Zara');
INSERT INTO dbo.Brands (Id, Name) VALUES (5, N'H&M');
SET IDENTITY_INSERT dbo.Brands OFF;

-- 4.6 Products
SET IDENTITY_INSERT dbo.Products ON;
INSERT INTO dbo.Products (Id, Name, Description, Price, DiscountPrice, BrandId, CategoryId, CreatedDate, IsActive) VALUES (
  1, N'Áo Sơ Mi Nam Công Sở Zara', N'Áo sơ mi nam Zara chất liệu cotton cao cấp, thoáng mát, form dáng ôm vừa vặn, thích hợp đi làm và hội họp.', 450000.00, 390000.00,
  1, 1, '2026-06-23 21:09:54', 1);
INSERT INTO dbo.Products (Id, Name, Description, Price, DiscountPrice, BrandId, CategoryId, CreatedDate, IsActive) VALUES (
  2, N'Áo Thun Nam Trơn Basic Uniqlo', N'Áo thun nam Uniqlo 100% cotton mềm mại, co giãn tốt, công nghệ Dry-Ex thấm hút mồ hôi hiệu quả.', 250000.00, NULL,
  1, 1, '2026-06-23 21:09:54', 1);
SET IDENTITY_INSERT dbo.Products OFF;

-- 4.7 ProductImages
SET IDENTITY_INSERT dbo.ProductImages ON;
INSERT INTO dbo.ProductImages (Id, ProductId, ImageUrl, IsMain) VALUES (1, 1, 'https://down-vn.img.susercontent.com/file/sg-11134202-7rd5v-lwkt0i7j3fkc70', 1);
INSERT INTO dbo.ProductImages (Id, ProductId, ImageUrl, IsMain) VALUES (2, 1, '/images/products/p1-sub.jpg', 0);
INSERT INTO dbo.ProductImages (Id, ProductId, ImageUrl, IsMain) VALUES (3, 2, 'https://www.uniqlo.com/vn/vi/news/topics/2024103101/img/62T_241031FvwWcC.png', 1);
SET IDENTITY_INSERT dbo.ProductImages OFF;

-- 4.8 ProductVariants
SET IDENTITY_INSERT dbo.ProductVariants ON;
INSERT INTO dbo.ProductVariants (Id, ProductId, Size, Color, Quantity, SKU) VALUES (1, 1, N'M', N'White', 50, 'ZARA-SHIRT-M-WHT');
INSERT INTO dbo.ProductVariants (Id, ProductId, Size, Color, Quantity, SKU) VALUES (2, 1, N'L', N'White', 30, 'ZARA-SHIRT-L-WHT');
INSERT INTO dbo.ProductVariants (Id, ProductId, Size, Color, Quantity, SKU) VALUES (3, 1, N'M', N'Blue', 20, 'ZARA-SHIRT-M-BLU');
INSERT INTO dbo.ProductVariants (Id, ProductId, Size, Color, Quantity, SKU) VALUES (4, 2, N'S', N'Black', 100, 'UNI-TSHIRT-S-BLK');
INSERT INTO dbo.ProductVariants (Id, ProductId, Size, Color, Quantity, SKU) VALUES (5, 2, N'M', N'Black', 150, 'UNI-TSHIRT-M-BLK');
INSERT INTO dbo.ProductVariants (Id, ProductId, Size, Color, Quantity, SKU) VALUES (6, 2, N'L', N'White', 120, 'UNI-TSHIRT-L-WHT');
SET IDENTITY_INSERT dbo.ProductVariants OFF;

-- 4.9 Vouchers
SET IDENTITY_INSERT dbo.Vouchers ON;
INSERT INTO dbo.Vouchers (Id, Code, DiscountPercent, DiscountAmount, ExpiryDate, UsageLimit, UsedCount) VALUES (1, 'HE2026', 15, NULL, '2026-08-31 23:59:59', 100, 0);
INSERT INTO dbo.Vouchers (Id, Code, DiscountPercent, DiscountAmount, ExpiryDate, UsageLimit, UsedCount) VALUES (2, 'SALE50K', NULL, 50000.00, '2026-12-31 23:59:59', 500, 0);
INSERT INTO dbo.Vouchers (Id, Code, DiscountPercent, DiscountAmount, ExpiryDate, UsageLimit, UsedCount) VALUES (3, 'WELCOME', 10, NULL, '2026-12-31 23:59:59', 1000, 0);
SET IDENTITY_INSERT dbo.Vouchers OFF;

-- 4.10 Carts
SET IDENTITY_INSERT dbo.Carts ON;
INSERT INTO dbo.Carts (Id, UserId, CreatedDate) VALUES (1, 'u2', '2026-06-23 21:09:55');
INSERT INTO dbo.Carts (Id, UserId, CreatedDate) VALUES (2, 'u1', '2026-06-23 14:11:39');
SET IDENTITY_INSERT dbo.Carts OFF;

-- 4.11 CartItems
SET IDENTITY_INSERT dbo.CartItems ON;
INSERT INTO dbo.CartItems (Id, CartId, ProductVariantId, Quantity) VALUES (1, 1, 1, 2);
SET IDENTITY_INSERT dbo.CartItems OFF;

-- 4.12 Orders
SET IDENTITY_INSERT dbo.Orders ON;
INSERT INTO dbo.Orders (Id, UserId, OrderDate, Status, TotalAmount, DiscountAmount, ShippingAddress, PaymentMethod, VoucherCode) VALUES (
  1, 'u2', '2026-06-23 21:09:55', N'Chờ xác nhận',
  1130000.00, 50000.00, N'456 Le Loi, Quận 1, TP. HCM', 'COD', 'SALE50K');
SET IDENTITY_INSERT dbo.Orders OFF;

-- 4.13 OrderDetails
SET IDENTITY_INSERT dbo.OrderDetails ON;
INSERT INTO dbo.OrderDetails (Id, OrderId, ProductVariantId, Quantity, UnitPrice) VALUES (1, 1, 1, 2, 390000.00);
INSERT INTO dbo.OrderDetails (Id, OrderId, ProductVariantId, Quantity, UnitPrice) VALUES (2, 1, 4, 1, 250000.00);
SET IDENTITY_INSERT dbo.OrderDetails OFF;

-- 4.14 Reviews (0 ban ghi)
-- Khong co du lieu Reviews.

-- 4.15 ChatLogs
SET IDENTITY_INSERT dbo.ChatLogs ON;
INSERT INTO dbo.ChatLogs (Id, UserId, Message, Response, CreatedDate) VALUES (1, 'u2', N'tìm áo sơ mi nam', N'Chào bạn! Dưới đây là gợi ý áo sơ mi nam phù hợp: Áo Sơ Mi Nam Công Sở Zara (390.000đ). Hãy click vào link sau để xem chi tiết.', '2026-06-23 21:09:55');
INSERT INTO dbo.ChatLogs (Id, UserId, Message, Response, CreatedDate) VALUES (2, 'u1', N'a', N'Dưới đây là một số sản phẩm phù hợp với yêu cầu của bạn. Hãy click vào để xem chi tiết nhé! (Gợi ý 2 sản phẩm)', '2026-06-23 14:31:32');
SET IDENTITY_INSERT dbo.ChatLogs OFF;