using System;
using System.Linq;
using System.Security.Claims;
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
    public class ProductApiController : ControllerBase
    {
        private readonly ProductService _productService;
        private readonly CategoryService _categoryService;
        private readonly BrandService _brandService;
        private readonly IUnitOfWork _unitOfWork;

        public ProductApiController(
            ProductService productService,
            CategoryService categoryService,
            BrandService brandService,
            IUnitOfWork unitOfWork)
        {
            _productService = productService;
            _categoryService = categoryService;
            _brandService = brandService;
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public async Task<IActionResult> GetProducts(
            [FromQuery] string? search, 
            [FromQuery] int? categoryId, 
            [FromQuery] int? brandId, 
            [FromQuery] int? productTypeId,
            [FromQuery] string? size, 
            [FromQuery] string? color,
            [FromQuery] decimal? minPrice, 
            [FromQuery] decimal? maxPrice, 
            [FromQuery] bool? isDiscount,
            [FromQuery] string sortBy = "newest", 
            [FromQuery] int page = 1)
        {
            int pageSize = 9;
            var result = await _productService.GetFilteredProductsAsync(
                search, categoryId, brandId, productTypeId, size, color, minPrice, maxPrice, sortBy, page, pageSize, isDiscount);

            var categories = await _categoryService.GetAllCategoriesAsync();
            var brands = await _brandService.GetAllBrandsAsync();
            var productTypes = await _unitOfWork.Repository<ProductType>().GetQueryable().ToListAsync();

            var allVariants = _unitOfWork.Repository<ProductVariant>().GetQueryable().ToList();
            var allSizes = allVariants.Select(v => v.Size).Distinct().OrderBy(s => s).ToList();
            var allColors = allVariants.Select(v => v.Color).Distinct().OrderBy(c => c).ToList();

            var productsList = result.Products.Select(p => new
            {
                id = p.Id,
                name = p.Name,
                price = p.Price,
                discountPrice = p.DiscountPrice,
                description = p.Description,
                isActive = p.IsActive,
                categoryId = p.CategoryId,
                brandId = p.BrandId,
                productTypeId = p.ProductTypeId,
                imageUrl = p.ProductImages.FirstOrDefault(pi => pi.IsMain)?.ImageUrl ?? "https://images.unsplash.com/photo-1523381210434-271e8be1f52b?w=400",
                images = p.ProductImages.Select(pi => pi.ImageUrl).ToList(),
                categoryName = p.Category?.Name,
                brandName = p.Brand?.Name,
                productTypeName = p.ProductType?.Name,
                rating = p.Reviews.Any(r => r.IsApproved) ? Math.Round(p.Reviews.Where(r => r.IsApproved).Average(r => r.Rating), 1) : (double?)null,
                ratingCount = p.Reviews.Count(r => r.IsApproved),
                defaultVariantId = p.ProductVariants.FirstOrDefault()?.Id
            });

            return Ok(new
            {
                products = productsList,
                totalCount = result.TotalCount,
                currentPage = page,
                totalPages = (int)Math.Ceiling((double)result.TotalCount / pageSize),
                categories = categories.Select(c => new { id = c.Id, name = c.Name, parentId = c.ParentId }),
                brands = brands.Select(b => new { id = b.Id, name = b.Name }),
                productTypes = productTypes.Select(pt => new { id = pt.Id, name = pt.Name }),
                allSizes,
                allColors
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDetails(int id)
        {
            var product = await _productService.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound(new { message = "Không tìm thấy sản phẩm." });
            }

            var relatedProducts = _unitOfWork.Repository<Product>().GetQueryable()
                .Where(p => p.CategoryId == product.CategoryId && p.Id != product.Id && p.IsActive)
                .Include(p => p.ProductImages)
                .Take(4)
                .Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    price = p.Price,
                    discountPrice = p.DiscountPrice,
                    imageUrl = p.ProductImages.Where(pi => pi.IsMain).Select(pi => pi.ImageUrl).FirstOrDefault() ?? "https://images.unsplash.com/photo-1523381210434-271e8be1f52b?w=400"
                })
                .ToList();

            var reviews = _unitOfWork.Repository<Review>().GetQueryable()
                .Where(r => r.ProductId == id && r.IsApproved)
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedDate)
                .Select(r => new
                {
                    id = r.Id,
                    userName = r.User.FullName != null ? r.User.FullName : r.User.UserName,
                    rating = r.Rating,
                    comment = r.Comment,
                    createdDate = r.CreatedDate.ToString("dd/MM/yyyy HH:mm")
                })
                .ToList();

            var approvedReviews = reviews; // đã là IsApproved=true từ query
            double? avgRating = approvedReviews.Any() ? Math.Round(approvedReviews.Average(r => r.rating), 1) : (double?)null;
            int ratingCount = approvedReviews.Count();

            var result = new
            {
                id = product.Id,
                name = product.Name,
                price = product.Price,
                discountPrice = product.DiscountPrice,
                description = product.Description,
                isActive = product.IsActive,
                categoryName = product.Category?.Name,
                brandName = product.Brand?.Name,
                productTypeName = product.ProductType?.Name,
                images = product.ProductImages.Select(pi => new { id = pi.Id, imageUrl = pi.ImageUrl, isMain = pi.IsMain }).ToList(),
                variants = product.ProductVariants.Select(pv => new { id = pv.Id, size = pv.Size, color = pv.Color, sku = pv.SKU, stock = pv.Quantity }).ToList(),
                reviews,
                rating = avgRating,
                ratingCount,
                relatedProducts
            };

            return Ok(result);
        }

        // API kiểm tra người dùng đã mua sản phẩm chưa
        [HttpGet("{id}/check-purchase")]
        [Authorize]
        public IActionResult CheckPurchase(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { hasPurchased = false });

            // Kiểm tra có đơn hàng Hoàn thành chứa sản phẩm này không
            var hasPurchased = _unitOfWork.Repository<Order>().GetQueryable()
                .Where(o => o.UserId == userId && o.Status == "Hoàn thành")
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.ProductVariant)
                .Any(o => o.OrderDetails.Any(od => od.ProductVariant.ProductId == id));

            // Kiểm tra đã đánh giá chưa
            var hasReviewed = _unitOfWork.Repository<Review>().GetQueryable()
                .Any(r => r.ProductId == id && r.UserId == userId);

            return Ok(new { hasPurchased, hasReviewed });
        }

        [HttpPost("{id}/reviews")]
        [Authorize]
        public async Task<IActionResult> AddReview(int id, [FromBody] ReviewDto model)
        {
            if (model.Rating < 1 || model.Rating > 5)
            {
                return BadRequest(new { message = "Đánh giá sao phải từ 1 đến 5." });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "Bạn cần đăng nhập để gửi đánh giá." });
            }

            // Kiểm tra đã mua hàng chưa (phải có đơn Hoàn thành)
            var hasPurchased = _unitOfWork.Repository<Order>().GetQueryable()
                .Where(o => o.UserId == userId && o.Status == "Hoàn thành")
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.ProductVariant)
                .Any(o => o.OrderDetails.Any(od => od.ProductVariant.ProductId == id));

            if (!hasPurchased)
            {
                return BadRequest(new { message = "Bạn chỉ có thể đánh giá sản phẩm sau khi đã mua và nhận hàng thành công." });
            }

            // Kiểm tra đã đánh giá chưa
            var existingReview = _unitOfWork.Repository<Review>().GetQueryable()
                .FirstOrDefault(r => r.ProductId == id && r.UserId == userId);

            if (existingReview != null)
            {
                return BadRequest(new { message = "Bạn đã đánh giá sản phẩm này rồi." });
            }

            var review = new Review
            {
                ProductId = id,
                UserId = userId,
                Rating = model.Rating,
                Comment = model.Comment ?? "",
                CreatedDate = DateTime.UtcNow,
                IsApproved = true
            };

            await _unitOfWork.Repository<Review>().AddAsync(review);
            await _unitOfWork.SaveChangesAsync();

            return Ok(new { message = "Cảm ơn bạn đã gửi đánh giá sản phẩm!" });
        }
    }

    public class ReviewDto
    {
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }
}
