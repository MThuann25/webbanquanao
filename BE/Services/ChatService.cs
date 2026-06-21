using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ClothingShop.Domain.Entities;
using ClothingShop.Domain.Interfaces;

namespace ClothingShop.Application.Services
{
    public class ChatService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ChatService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<(string ResponseText, List<Product> SuggestedProducts)> GetSuggestionsAsync(string userId, string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return ("Chào bạn, tôi có thể giúp gì cho bạn hôm nay?", new List<Product>());
            }

            var queryText = message.ToLower();
            
            // Filters to be extracted
            string categoryName = null;
            int? brandId = null;
            decimal? maxPrice = null;
            decimal? minPrice = null;
            string gender = null;
            bool sportsQuery = false;
            bool officeQuery = false;
            bool beachQuery = false;

            // 1. Detect Category keywords
            if (queryText.Contains("áo sơ mi") || queryText.Contains("sơ mi")) categoryName = "Áo Nam"; // or Áo Nữ, handled below
            else if (queryText.Contains("áo thun") || queryText.Contains("phông")) categoryName = "Áo Nam";
            else if (queryText.Contains("hoodie") || queryText.Contains("áo nỉ")) categoryName = "Áo Nam";
            else if (queryText.Contains("áo khoác") || queryText.Contains("denim") || queryText.Contains("jean")) categoryName = "Áo Nam";
            else if (queryText.Contains("quần tây") || queryText.Contains("quần jean") || queryText.Contains("quần short") || queryText.Contains("jogger")) categoryName = "Quần Nam";
            else if (queryText.Contains("áo") && !queryText.Contains("quần") && !queryText.Contains("váy")) categoryName = "Áo Nam";
            else if (queryText.Contains("quần")) categoryName = "Quần Nam";
            else if (queryText.Contains("váy") || queryText.Contains("đầm") || queryText.Contains("chân váy")) categoryName = "Váy Nữ";
            else if (queryText.Contains("nón") || queryText.Contains("mũ") || queryText.Contains("tất") || queryText.Contains("vớ") || queryText.Contains("kính") || queryText.Contains("thắt lưng") || queryText.Contains("phụ kiện")) categoryName = "Phụ Kiện";

            // 2. Detect Gender keywords
            if (queryText.Contains("nam"))
            {
                gender = "nam";
                if (categoryName == "Áo Nam") categoryName = "Áo Nam";
                else if (categoryName == "Quần Nam") categoryName = "Quần Nam";
            }
            else if (queryText.Contains("nữ") || queryText.Contains("gái"))
            {
                gender = "nữ";
                if (categoryName == "Áo Nam") categoryName = "Áo Nữ";
                else if (categoryName == "Quần Nam") categoryName = "Váy Nữ"; // default female clothing to dress/skirt
            }

            // 3. Detect Brands
            if (queryText.Contains("nike")) brandId = 1;
            else if (queryText.Contains("adidas")) brandId = 2;
            else if (queryText.Contains("uniqlo")) brandId = 3;
            else if (queryText.Contains("zara")) brandId = 4;
            else if (queryText.Contains("h&m") || queryText.Contains("hm")) brandId = 5;

            // 4. Detect Price conditions (e.g. "dưới 500k", "dưới 300.000", "dưới 1 triệu")
            var underMatch = Regex.Match(queryText, @"dưới\s+([0-9\.]+)\s*(k|tr|triệu|trăm|đồng)?");
            if (underMatch.Success)
            {
                var numStr = underMatch.Groups[1].Value.Replace(".", "");
                if (decimal.TryParse(numStr, out decimal val))
                {
                    var unit = underMatch.Groups[2].Value;
                    if (unit == "k") maxPrice = val * 1000;
                    else if (unit == "tr" || unit == "triệu") maxPrice = val * 1000000;
                    else if (val < 1000) maxPrice = val * 1000; // assume 'k' if user wrote "dưới 500"
                    else maxPrice = val;
                }
            }

            var overMatch = Regex.Match(queryText, @"trên\s+([0-9\.]+)\s*(k|tr|triệu|trăm)?");
            if (overMatch.Success)
            {
                var numStr = overMatch.Groups[1].Value.Replace(".", "");
                if (decimal.TryParse(numStr, out decimal val))
                {
                    var unit = overMatch.Groups[2].Value;
                    if (unit == "k") minPrice = val * 1000;
                    else if (unit == "tr" || unit == "triệu") minPrice = val * 1000000;
                    else if (val < 1000) minPrice = val * 1000;
                    else minPrice = val;
                }
            }

            // 5. Detect Context style keywords
            if (queryText.Contains("thể thao") || queryText.Contains("gym") || queryText.Contains("chạy bộ")) sportsQuery = true;
            if (queryText.Contains("công sở") || queryText.Contains("đi làm") || queryText.Contains("lịch lãm")) officeQuery = true;
            if (queryText.Contains("đi biển") || queryText.Contains("du lịch") || queryText.Contains("mùa hè")) beachQuery = true;

            // 6. Build EF Query
            var pQuery = _unitOfWork.Repository<Product>().GetQueryable()
                .Where(p => p.IsActive)
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .AsQueryable();

            if (brandId.HasValue)
            {
                pQuery = pQuery.Where(p => p.BrandId == brandId.Value);
            }

            if (!string.IsNullOrEmpty(categoryName))
            {
                pQuery = pQuery.Where(p => p.Category != null && (p.Category.Name == categoryName || p.Category.ParentCategory.Name == categoryName));
            }

            if (maxPrice.HasValue)
            {
                pQuery = pQuery.Where(p => (p.DiscountPrice ?? p.Price) <= maxPrice.Value);
            }

            if (minPrice.HasValue)
            {
                pQuery = pQuery.Where(p => (p.DiscountPrice ?? p.Price) >= minPrice.Value);
            }

            // Apply style keyword matches
            if (sportsQuery)
            {
                pQuery = pQuery.Where(p => p.Name.Contains("Thể Thao") || p.Description.Contains("thể thao") || p.Brand.Name == "Nike" || p.Brand.Name == "Adidas");
            }
            if (officeQuery)
            {
                pQuery = pQuery.Where(p => p.Name.Contains("Công Sở") || p.Name.Contains("Tây") || p.Name.Contains("Blazer") || p.Name.Contains("Sơ Mi") || p.Description.Contains("công sở"));
            }
            if (beachQuery)
            {
                pQuery = pQuery.Where(p => p.Name.Contains("Biển") || p.Name.Contains("Short") || p.Name.Contains("Hoa Nhí") || p.Description.Contains("biển") || p.Description.Contains("mùa hè"));
            }

            // Search text matches (fuzzy fallback)
            if (string.IsNullOrEmpty(categoryName) && !brandId.HasValue && !sportsQuery && !officeQuery && !beachQuery)
            {
                var cleanTokens = queryText.Split(' ')
                    .Where(t => t.Length > 2 && t != "tìm" && t != "kiếm" && t != "cho" && t != "mua" && t != "gợi" && t != "ý")
                    .ToList();

                if (cleanTokens.Any())
                {
                    pQuery = pQuery.Where(p => cleanTokens.Any(t => p.Name.ToLower().Contains(t) || p.Description.ToLower().Contains(t)));
                }
            }

            var suggestedProducts = await pQuery.Take(4).ToListAsync();

            // 7. Contextual Personalized Recommendations (If user logged in and suggestions are sparse)
            if (suggestedProducts.Count < 2 && !string.IsNullOrEmpty(userId))
            {
                // Find user's last orders
                var lastOrder = await _unitOfWork.Repository<Order>().GetQueryable()
                    .Where(o => o.UserId == userId)
                    .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.ProductVariant)
                            .ThenInclude(pv => pv.Product)
                                .ThenInclude(p => p.ProductImages)
                    .OrderByDescending(o => o.OrderDate)
                    .FirstOrDefaultAsync();

                if (lastOrder != null && lastOrder.OrderDetails.Any())
                {
                    var purchasedProductIds = lastOrder.OrderDetails.Select(od => od.ProductVariant.ProductId).ToList();
                    var userCategories = lastOrder.OrderDetails.Select(od => od.ProductVariant.Product.CategoryId).Distinct().ToList();

                    // Suggest other products in the same categories
                    var additionalSuggestions = await _unitOfWork.Repository<Product>().GetQueryable()
                        .Where(p => p.IsActive && userCategories.Contains(p.CategoryId) && !purchasedProductIds.Contains(p.Id))
                        .Include(p => p.ProductImages)
                        .Take(4 - suggestedProducts.Count)
                        .ToListAsync();

                    suggestedProducts.AddRange(additionalSuggestions);
                }
            }

            // 8. Final text generation
            string responseText;
            if (suggestedProducts.Any())
            {
                responseText = "Dưới đây là một số sản phẩm phù hợp với yêu cầu của bạn. Hãy click vào để xem chi tiết nhé!";
            }
            else
            {
                // Fallback to top products
                suggestedProducts = await _unitOfWork.Repository<Product>().GetQueryable()
                    .Where(p => p.IsActive)
                    .Include(p => p.ProductImages)
                    .OrderByDescending(p => p.CreatedDate)
                    .Take(3)
                    .ToListAsync();
                responseText = "Tôi không tìm thấy sản phẩm nào chính xác như yêu cầu của bạn. Dưới đây là một số sản phẩm mới nhất tại cửa hàng mà bạn có thể thích:";
            }

            // 9. Save ChatLog
            var chatLog = new ChatLog
            {
                UserId = string.IsNullOrEmpty(userId) ? null : userId,
                Message = message,
                Response = $"{responseText} (Gợi ý {suggestedProducts.Count} sản phẩm)",
                CreatedDate = DateTime.UtcNow
            };
            await _unitOfWork.Repository<ChatLog>().AddAsync(chatLog);
            await _unitOfWork.SaveChangesAsync();

            return (responseText, suggestedProducts);
        }
    }
}
