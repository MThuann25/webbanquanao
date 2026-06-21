using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClothingShop.Application.Services;
using ClothingShop.Domain.Entities;
using ClothingShop.Domain.Interfaces;

namespace ClothingShop.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminApiController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ProductService _productService;
        private readonly CategoryService _categoryService;
        private readonly BrandService _brandService;
        private readonly VoucherService _voucherService;

        public AdminApiController(
            IUnitOfWork unitOfWork,
            ProductService productService,
            CategoryService categoryService,
            BrandService brandService,
            VoucherService voucherService)
        {
            _unitOfWork = unitOfWork;
            _productService = productService;
            _categoryService = categoryService;
            _brandService = brandService;
            _voucherService = voucherService;
        }

        // ================= DASHBOARD =================
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var orders = _unitOfWork.Repository<Order>().GetQueryable();
            var completedOrders = await orders.Where(o => o.Status == "Hoàn thành").ToListAsync();
            
            var totalRevenue = completedOrders.Sum(o => o.TotalAmount);
            var totalOrdersCount = await orders.CountAsync();
            var activeProductsCount = await _unitOfWork.Repository<Product>().GetQueryable().Where(p => p.IsActive).CountAsync();
            var totalUsersCount = await _unitOfWork.Repository<ApplicationUser>().GetQueryable().CountAsync();

            var orderDetails = _unitOfWork.Repository<OrderDetail>().GetQueryable();
            var topProducts = await orderDetails
                .Include(od => od.ProductVariant)
                    .ThenInclude(pv => pv.Product)
                .GroupBy(od => od.ProductVariant.Product)
                .Select(g => new
                {
                    productId = g.Key.Id,
                    name = g.Key.Name,
                    quantitySold = g.Sum(od => od.Quantity),
                    totalRevenue = g.Sum(od => od.Quantity * od.UnitPrice)
                })
                .OrderByDescending(x => x.quantitySold)
                .Take(5)
                .ToListAsync();

            var currentYear = DateTime.UtcNow.Year;
            var monthlyData = await orders
                .Where(o => o.Status == "Hoàn thành" && o.OrderDate.Year == currentYear)
                .GroupBy(o => o.OrderDate.Month)
                .Select(g => new
                {
                    monthNum = g.Key,
                    revenue = g.Sum(o => o.TotalAmount)
                })
                .ToListAsync();

            var chartData = new decimal[12];
            foreach (var item in monthlyData)
            {
                if (item.monthNum >= 1 && item.monthNum <= 12)
                {
                    chartData[item.monthNum - 1] = item.revenue;
                }
            }

            return Ok(new
            {
                totalRevenue,
                totalOrders = totalOrdersCount,
                activeProducts = activeProductsCount,
                totalUsers = totalUsersCount,
                topProducts,
                chartLabels = new[] { "T1", "T2", "T3", "T4", "T5", "T6", "T7", "T8", "T9", "T10", "T11", "T12" },
                chartData
            });
        }

        // ================= PRODUCT CRUD =================
        [HttpGet("products")]
        public async Task<IActionResult> GetProducts([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _productService.GetFilteredProductsAsync(null, null, null, null, null, null, null, "newest", page, pageSize);
            
            var productsList = result.Products.Select(p => new
            {
                id = p.Id,
                name = p.Name,
                price = p.Price,
                discountPrice = p.DiscountPrice,
                isActive = p.IsActive,
                categoryName = p.Category?.Name,
                brandName = p.Brand?.Name,
                stock = p.ProductVariants.Sum(v => v.Quantity)
            }).ToList();

            return Ok(new
            {
                products = productsList,
                currentPage = page,
                totalPages = (int)Math.Ceiling((double)result.TotalCount / pageSize),
                totalCount = result.TotalCount
            });
        }

        [HttpPost("products")]
        public async Task<IActionResult> CreateProduct([FromBody] AdminProductDto model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var product = new Product
            {
                Name = model.Name,
                Description = model.Description ?? "",
                Price = model.Price,
                DiscountPrice = model.DiscountPrice,
                CategoryId = model.CategoryId,
                BrandId = model.BrandId,
                IsActive = model.IsActive,
                CreatedDate = DateTime.UtcNow
            };

            await _unitOfWork.Repository<Product>().AddAsync(product);
            await _unitOfWork.SaveChangesAsync(); // Generate Product ID

            // Add Main Image
            var mainImg = new ProductImage
            {
                ProductId = product.Id,
                ImageUrl = string.IsNullOrEmpty(model.MainImageUrl) ? "https://images.unsplash.com/photo-1523381210434-271e8be1f52b?w=400" : model.MainImageUrl,
                IsMain = true
            };
            await _unitOfWork.Repository<ProductImage>().AddAsync(mainImg);

            // Add Sub Images
            if (model.SubImageUrls != null && model.SubImageUrls.Any())
            {
                foreach (var url in model.SubImageUrls)
                {
                    if (!string.IsNullOrEmpty(url))
                    {
                        var img = new ProductImage { ProductId = product.Id, ImageUrl = url, IsMain = false };
                        await _unitOfWork.Repository<ProductImage>().AddAsync(img);
                    }
                }
            }

            // Create Variants
            var sizes = string.IsNullOrEmpty(model.Sizes) ? new[] { "M" } : model.Sizes.Split(',').Select(s => s.Trim()).ToArray();
            var colors = string.IsNullOrEmpty(model.Colors) ? new[] { "Basic" } : model.Colors.Split(',').Select(c => c.Trim()).ToArray();

            foreach (var size in sizes)
            {
                foreach (var color in colors)
                {
                    var variant = new ProductVariant
                    {
                        ProductId = product.Id,
                        Size = size,
                        Color = color,
                        Quantity = model.InitialStock,
                        SKU = $"{product.Name.Substring(0, Math.Min(5, product.Name.Length)).ToUpper().Replace(" ", "")}-{size.ToUpper()}-{color.ToUpper()}"
                    };
                    await _unitOfWork.Repository<ProductVariant>().AddAsync(variant);
                }
            }

            await _unitOfWork.SaveChangesAsync();
            return Ok(new { message = "Thêm sản phẩm mới thành công!", productId = product.Id });
        }

        [HttpPut("products/{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] AdminProductUpdateDto model)
        {
            var product = await _productService.GetByIdAsync(id);
            if (product == null) return NotFound(new { message = "Không tìm thấy sản phẩm." });

            product.Name = model.Name;
            product.Description = model.Description ?? "";
            product.Price = model.Price;
            product.DiscountPrice = model.DiscountPrice;
            product.CategoryId = model.CategoryId;
            product.BrandId = model.BrandId;
            product.IsActive = model.IsActive;

            // Handle Main Image Update
            if (!string.IsNullOrEmpty(model.MainImageUrl))
            {
                var oldMain = product.ProductImages.FirstOrDefault(pi => pi.IsMain);
                if (oldMain != null)
                {
                    _unitOfWork.Repository<ProductImage>().Delete(oldMain);
                }
                var newMain = new ProductImage { ProductId = product.Id, ImageUrl = model.MainImageUrl, IsMain = true };
                await _unitOfWork.Repository<ProductImage>().AddAsync(newMain);
            }

            _unitOfWork.Repository<Product>().Update(product);
            await _unitOfWork.SaveChangesAsync();

            return Ok(new { message = "Cập nhật sản phẩm thành công!" });
        }

        [HttpDelete("products/{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            await _productService.DeleteProductAsync(id);
            return Ok(new { message = "Đã xóa sản phẩm thành công!" });
        }

        // ================= CATEGORIES CRUD =================
        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _categoryService.GetAllCategoriesAsync();
            return Ok(categories.Select(c => new { id = c.Id, name = c.Name, parentId = c.ParentId }));
        }

        [HttpPost("categories")]
        public async Task<IActionResult> CreateCategory([FromBody] CategoryDto model)
        {
            var cat = new Category { Name = model.Name, ParentId = model.ParentId };
            await _unitOfWork.Repository<Category>().AddAsync(cat);
            await _unitOfWork.SaveChangesAsync();
            return Ok(new { message = "Thêm danh mục thành công!", id = cat.Id });
        }

        [HttpPut("categories/{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryDto model)
        {
            var cat = await _unitOfWork.Repository<Category>().GetByIdAsync(id);
            if (cat == null) return NotFound();

            cat.Name = model.Name;
            cat.ParentId = model.ParentId;
            _unitOfWork.Repository<Category>().Update(cat);
            await _unitOfWork.SaveChangesAsync();
            return Ok(new { message = "Cập nhật danh mục thành công!" });
        }

        [HttpDelete("categories/{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var cat = await _unitOfWork.Repository<Category>().GetByIdAsync(id);
            if (cat == null) return NotFound();

            _unitOfWork.Repository<Category>().Delete(cat);
            await _unitOfWork.SaveChangesAsync();
            return Ok(new { message = "Đã xóa danh mục thành công!" });
        }

        // ================= BRANDS CRUD =================
        [HttpGet("brands")]
        public async Task<IActionResult> GetBrands()
        {
            var brands = await _brandService.GetAllBrandsAsync();
            return Ok(brands.Select(b => new { id = b.Id, name = b.Name }));
        }

        [HttpPost("brands")]
        public async Task<IActionResult> CreateBrand([FromBody] BrandDto model)
        {
            var brand = new Brand { Name = model.Name };
            await _unitOfWork.Repository<Brand>().AddAsync(brand);
            await _unitOfWork.SaveChangesAsync();
            return Ok(new { message = "Thêm thương hiệu thành công!", id = brand.Id });
        }

        [HttpPut("brands/{id}")]
        public async Task<IActionResult> UpdateBrand(int id, [FromBody] BrandDto model)
        {
            var brand = await _unitOfWork.Repository<Brand>().GetByIdAsync(id);
            if (brand == null) return NotFound();

            brand.Name = model.Name;
            _unitOfWork.Repository<Brand>().Update(brand);
            await _unitOfWork.SaveChangesAsync();
            return Ok(new { message = "Cập nhật thương hiệu thành công!" });
        }

        [HttpDelete("brands/{id}")]
        public async Task<IActionResult> DeleteBrand(int id)
        {
            var brand = await _unitOfWork.Repository<Brand>().GetByIdAsync(id);
            if (brand == null) return NotFound();

            _unitOfWork.Repository<Brand>().Delete(brand);
            await _unitOfWork.SaveChangesAsync();
            return Ok(new { message = "Đã xóa thương hiệu thành công!" });
        }

        // ================= VOUCHERS CRUD =================
        [HttpGet("vouchers")]
        public async Task<IActionResult> GetVouchers()
        {
            var vouchers = await _unitOfWork.Repository<Voucher>().GetQueryable().OrderByDescending(v => v.ExpiryDate).ToListAsync();
            return Ok(vouchers.Select(v => new
            {
                id = v.Id,
                code = v.Code,
                discountPercent = v.DiscountPercent,
                discountAmount = v.DiscountAmount,
                expiryDate = v.ExpiryDate.ToString("yyyy-MM-dd"),
                isActive = v.ExpiryDate >= DateTime.UtcNow && (v.UsageLimit == 0 || v.UsedCount < v.UsageLimit)
            }));
        }

        [HttpPost("vouchers")]
        public async Task<IActionResult> CreateVoucher([FromBody] VoucherDto model)
        {
            var voucher = new Voucher
            {
                Code = model.Code.ToUpper(),
                DiscountPercent = model.DiscountPercent,
                DiscountAmount = model.DiscountAmount,
                ExpiryDate = model.ExpiryDate,
                UsageLimit = 100, // Default usage limit
                UsedCount = 0
            };
            await _unitOfWork.Repository<Voucher>().AddAsync(voucher);
            await _unitOfWork.SaveChangesAsync();
            return Ok(new { message = "Thêm mã giảm giá thành công!", id = voucher.Id });
        }

        [HttpPut("vouchers/{id}")]
        public async Task<IActionResult> UpdateVoucher(int id, [FromBody] VoucherDto model)
        {
            var voucher = await _unitOfWork.Repository<Voucher>().GetByIdAsync(id);
            if (voucher == null) return NotFound();

            voucher.Code = model.Code.ToUpper();
            voucher.DiscountPercent = model.DiscountPercent;
            voucher.DiscountAmount = model.DiscountAmount;
            voucher.ExpiryDate = model.ExpiryDate;
            _unitOfWork.Repository<Voucher>().Update(voucher);
            await _unitOfWork.SaveChangesAsync();
            return Ok(new { message = "Cập nhật mã giảm giá thành công!" });
        }

        [HttpDelete("vouchers/{id}")]
        public async Task<IActionResult> DeleteVoucher(int id)
        {
            var voucher = await _unitOfWork.Repository<Voucher>().GetByIdAsync(id);
            if (voucher == null) return NotFound();

            _unitOfWork.Repository<Voucher>().Delete(voucher);
            await _unitOfWork.SaveChangesAsync();
            return Ok(new { message = "Đã xóa mã giảm giá thành công!" });
        }

        // ================= REVIEWS / COMMENTS =================
        [HttpGet("reviews")]
        public async Task<IActionResult> GetReviews()
        {
            var reviews = await _unitOfWork.Repository<Review>().GetQueryable()
                .Include(r => r.Product)
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedDate)
                .Select(r => new
                {
                    id = r.Id,
                    productName = r.Product.Name,
                    userName = r.User.FullName != null ? r.User.FullName : r.User.UserName,
                    rating = r.Rating,
                    comment = r.Comment,
                    createdDate = r.CreatedDate.ToString("dd/MM/yyyy HH:mm")
                })
                .ToListAsync();

            return Ok(reviews);
        }

        [HttpDelete("reviews/{id}")]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var review = await _unitOfWork.Repository<Review>().GetByIdAsync(id);
            if (review == null) return NotFound();

            _unitOfWork.Repository<Review>().Delete(review);
            await _unitOfWork.SaveChangesAsync();
            return Ok(new { message = "Đã xóa bình luận thành công!" });
        }

        // ================= ORDERS MANAGEMENT =================
        [HttpGet("orders")]
        public async Task<IActionResult> GetOrders()
        {
            var ordersList = await _unitOfWork.Repository<Order>().GetQueryable()
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new
                {
                    id = o.Id,
                    userName = o.User.FullName != null ? o.User.FullName : o.User.UserName,
                    orderDate = o.OrderDate.ToString("dd/MM/yyyy HH:mm"),
                    totalAmount = o.TotalAmount,
                    status = o.Status,
                    paymentMethod = o.PaymentMethod,
                    paymentStatus = o.PaymentMethod.Contains("Đã thanh toán") ? "Paid" : "Pending"
                })
                .ToListAsync();

            return Ok(ordersList);
        }

        [HttpPut("orders/{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] OrderStatusUpdateDto model)
        {
            var order = await _unitOfWork.Repository<Order>().GetByIdAsync(id);
            if (order == null) return NotFound(new { message = "Không tìm thấy đơn hàng." });

            if (!string.IsNullOrEmpty(model.Status))
            {
                order.Status = model.Status;
            }

            if (!string.IsNullOrEmpty(model.PaymentStatus))
            {
                if (model.PaymentStatus == "Paid" && !order.PaymentMethod.Contains("Đã thanh toán"))
                {
                    order.PaymentMethod += " (Đã thanh toán Online)";
                }
            }

            _unitOfWork.Repository<Order>().Update(order);
            await _unitOfWork.SaveChangesAsync();

            return Ok(new { message = "Cập nhật trạng thái đơn hàng thành công!" });
        }
    }

    // DTOs
    public class AdminProductDto
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public int? CategoryId { get; set; }
        public int? BrandId { get; set; }
        public bool IsActive { get; set; } = true;
        public string? Sizes { get; set; }
        public string? Colors { get; set; }
        public int InitialStock { get; set; } = 10;
        public string? MainImageUrl { get; set; }
        public List<string>? SubImageUrls { get; set; }
    }

    public class AdminProductUpdateDto
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public int? CategoryId { get; set; }
        public int? BrandId { get; set; }
        public bool IsActive { get; set; }
        public string? MainImageUrl { get; set; }
    }

    public class CategoryDto
    {
        public string Name { get; set; } = null!;
        public int? ParentId { get; set; }
    }

    public class BrandDto
    {
        public string Name { get; set; } = null!;
    }

    public class VoucherDto
    {
        public string Code { get; set; } = null!;
        public int? DiscountPercent { get; set; }
        public decimal? DiscountAmount { get; set; }
        public DateTime ExpiryDate { get; set; }
    }

    public class OrderStatusUpdateDto
    {
        public string? Status { get; set; }
        public string? PaymentStatus { get; set; }
    }
}
