using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ClothingShop.Application.Services;

namespace ClothingShop.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatApiController : ControllerBase
    {
        private readonly ChatService _chatService;

        public ChatApiController(ChatService chatService)
        {
            _chatService = chatService;
        }

        public class ChatRequest
        {
            public string Message { get; set; } = null!;
        }

        [HttpPost]
        public async Task<IActionResult> GetSuggestions([FromBody] ChatRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Message))
            {
                return BadRequest(new { message = "Tin nhắn không được để trống." });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            try
            {
                var result = await _chatService.GetSuggestionsAsync(userId, request.Message);

                var productsPayload = result.SuggestedProducts.Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    price = p.Price,
                    discountPrice = p.DiscountPrice,
                    imageUrl = p.ProductImages.Where(pi => pi.IsMain).Select(pi => pi.ImageUrl).FirstOrDefault() ?? "https://images.unsplash.com/photo-1523381210434-271e8be1f52b?w=400",
                }).ToList();

                return Ok(new
                {
                    responseText = result.ResponseText,
                    suggestedProducts = productsPayload
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống: " + ex.Message });
            }
        }
    }
}
