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
            [FromQuery] string? size, 
            [FromQuery] string? color,
            [FromQuery] decimal? minPrice, 
            [FromQuery] decimal? maxPrice, 
            [FromQuery] string sortBy = "newest", 
            [FromQuery] int page = 1)
        {
            int pageSize = 9;
            var result = await _productService.GetFilteredProductsAsync(
                search, categoryId, brandId, size, color, minPrice, maxPrice, sortBy, page, pageSize);

            var categories = await _categoryService.GetAllCategoriesAsync();
            var brands = await _brandService.GetAllBrandsAsync();

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
                imageUrl = p.ProductImages.FirstOrDefault(pi => pi.IsMain)?.ImageUrl ?? "https://images.unsplash.com/photo-1523381210434-271e8be1f52b?w=400",
                images = p.ProductImages.Select(pi => pi.ImageUrl).ToList(),
                categoryName = p.Category?.Name,
                brandName = p.Brand?.Name,
                rating = p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 5.0
            });

            return Ok(new
            {
                products = productsList,
                totalCount = result.TotalCount,
                currentPage = page,
                totalPages = (int)Math.Ceiling((double)result.TotalCount / pageSize),
                categories = categories.Select(c => new { id = c.Id, name = c.Name, parentId = c.ParentId }),
                brands = brands.Select(b => new { id = b.Id, name = b.Name }),
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
                images = product.ProductImages.Select(pi => new { id = pi.Id, imageUrl = pi.ImageUrl, isMain = pi.IsMain }).ToList(),
                variants = product.ProductVariants.Select(pv => new { id = pv.Id, size = pv.Size, color = pv.Color, sku = pv.SKU, stock = pv.Quantity }).ToList(),
                reviews,
                relatedProducts
            };

            return Ok(result);
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

            var review = new Review
            {
                ProductId = id,
                UserId = userId,
                Rating = model.Rating,
                Comment = model.Comment ?? "",
                CreatedDate = DateTime.UtcNow,
                IsApproved = true // Auto-approved for demo
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
