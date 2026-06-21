using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using ClothingShop.Application.Services;

namespace ClothingShop.Web.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ChatService _chatService;

        public ChatHub(ChatService chatService)
        {
            _chatService = chatService;
        }

        public async Task SendMessage(string message)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            try
            {
                // Call ChatService to analyze message and get matching products
                var result = await _chatService.GetSuggestionsAsync(userId, message);

                // Prepare suggested products payload
                var productsPayload = result.SuggestedProducts.Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    price = p.Price,
                    discountPrice = p.DiscountPrice,
                    imageUrl = p.ProductImages.FirstOrDefault(pi => pi.IsMain)?.ImageUrl ?? "https://images.unsplash.com/photo-1523381210434-271e8be1f52b?w=400",
                }).ToList();

                // Send response text and product cards back to the client that sent the message
                await Clients.Caller.SendAsync("ReceiveMessage", result.ResponseText, productsPayload);
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("ReceiveMessage", "Rất tiếc, đã xảy ra lỗi khi xử lý tin nhắn của bạn. Xin vui lòng thử lại sau.", new object[] { });
            }
        }
    }
}
