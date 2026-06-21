using System;
using System.Collections.Generic;
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
    [Authorize]
    public class CartApiController : ControllerBase
    {
        private readonly CartService _cartService;
        private readonly IUnitOfWork _unitOfWork;

        public CartApiController(CartService cartService, IUnitOfWork unitOfWork)
        {
            _cartService = cartService;
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var dbCart = await _cartService.GetCartByUserIdAsync(userId);
            var cartItems = dbCart.CartItems.Select(ci => new
            {
                id = ci.Id,
                productVariantId = ci.ProductVariantId,
                productName = ci.ProductVariant.Product.Name,
                size = ci.ProductVariant.Size,
                color = ci.ProductVariant.Color,
                price = ci.ProductVariant.Product.DiscountPrice ?? ci.ProductVariant.Product.Price,
                imageUrl = ci.ProductVariant.Product.ProductImages.FirstOrDefault(pi => pi.IsMain)?.ImageUrl ?? "https://images.unsplash.com/photo-1523381210434-271e8be1f52b?w=400",
                quantity = ci.Quantity,
                stockQuantity = ci.ProductVariant.Quantity,
                totalPrice = (ci.ProductVariant.Product.DiscountPrice ?? ci.ProductVariant.Product.Price) * ci.Quantity
            }).ToList();

            return Ok(cartItems);
        }

        [HttpPost("add")]
        public async Task<IActionResult> Add([FromBody] AddCartItemDto model)
        {
            if (model.Quantity <= 0) model.Quantity = 1;

            var variant = await _unitOfWork.Repository<ProductVariant>().GetQueryable()
                .Include(v => v.Product)
                .FirstOrDefaultAsync(v => v.Id == model.VariantId);

            if (variant == null)
            {
                return BadRequest(new { message = "Sản phẩm không tồn tại." });
            }

            if (variant.Quantity < model.Quantity)
            {
                return BadRequest(new { message = "Số lượng trong kho không đủ." });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            bool added = await _cartService.AddToCartAsync(userId, model.VariantId, model.Quantity);
            if (!added)
            {
                return BadRequest(new { message = "Đoạn mã kho không đủ hoặc lỗi không xác định." });
            }

            return Ok(new { message = "Đã thêm sản phẩm vào giỏ hàng thành công!" });
        }

        [HttpPost("update")]
        public async Task<IActionResult> Update([FromBody] UpdateCartItemDto model)
        {
            if (model.Quantity <= 0)
            {
                return BadRequest(new { message = "Số lượng phải lớn hơn 0." });
            }

            var variant = await _unitOfWork.Repository<ProductVariant>().GetByIdAsync(model.VariantId);
            if (variant == null)
            {
                return BadRequest(new { message = "Biến thể không tồn tại." });
            }

            if (variant.Quantity < model.Quantity)
            {
                return BadRequest(new { message = $"Số lượng trong kho không đủ. Chỉ còn {variant.Quantity} sản phẩm." });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            bool updated = await _cartService.UpdateCartItemQuantityAsync(userId, model.CartItemId, model.Quantity);
            if (!updated)
            {
                return BadRequest(new { message = "Không thể cập nhật số lượng." });
            }

            return Ok(new { message = "Cập nhật số lượng thành công!" });
        }

        [HttpPost("remove")]
        public async Task<IActionResult> Remove([FromBody] RemoveCartItemDto model)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            bool removed = await _cartService.RemoveFromCartAsync(userId, model.CartItemId);
            if (!removed)
            {
                return BadRequest(new { message = "Không thể xóa sản phẩm khỏi giỏ hàng." });
            }

            return Ok(new { message = "Đã xóa sản phẩm khỏi giỏ hàng!" });
        }

        [HttpPost("sync")]
        public async Task<IActionResult> Sync([FromBody] List<CartItemInput> items)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (items != null && items.Any())
            {
                var itemsToMigrate = items.Select(i => (i.ProductVariantId, i.Quantity)).ToList();
                await _cartService.MigrateSessionCartToDbAsync(userId, itemsToMigrate);
            }
            return Ok(new { message = "Đồng bộ giỏ hàng thành công!" });
        }
    }

    public class AddCartItemDto
    {
        public int VariantId { get; set; }
        public int Quantity { get; set; }
    }

    public class UpdateCartItemDto
    {
        public int CartItemId { get; set; }
        public int VariantId { get; set; }
        public int Quantity { get; set; }
    }

    public class RemoveCartItemDto
    {
        public int CartItemId { get; set; }
    }
}
