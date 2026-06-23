using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ClothingShop.Application.Services;
using ClothingShop.Domain.Entities;
using ClothingShop.Domain.Interfaces;

namespace ClothingShop.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CheckoutApiController : ControllerBase
    {
        private readonly OrderService _orderService;
        private readonly CartService _cartService;
        private readonly VoucherService _voucherService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;

        public CheckoutApiController(
            OrderService orderService,
            CartService cartService,
            VoucherService voucherService,
            IUnitOfWork unitOfWork,
            IConfiguration configuration)
        {
            _orderService = orderService;
            _cartService = cartService;
            _voucherService = voucherService;
            _unitOfWork = unitOfWork;
            _configuration = configuration;
        }

        [HttpPost("place-order")]
        public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderDto model)
        {
            if (string.IsNullOrEmpty(model.Address))
            {
                return BadRequest(new { message = "Vui lòng nhập địa chỉ giao hàng." });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            try
            {
                var cart = await _cartService.GetCartByUserIdAsync(userId);
                if (cart == null || !cart.CartItems.Any())
                {
                    return BadRequest(new { message = "Giỏ hàng của bạn đang trống." });
                }

                // If VNPay, we redirect to real VNPay sandbox portal
                if (model.PaymentMethod == "VNPay")
                {
                    decimal subtotal = cart.CartItems.Sum(item => (item.ProductVariant.Product.DiscountPrice ?? item.ProductVariant.Product.Price) * item.Quantity);
                    decimal discount = 0;

                    if (!string.IsNullOrEmpty(model.VoucherCode))
                    {
                        var voucher = await _voucherService.GetVoucherByCodeAsync(model.VoucherCode);
                        if (voucher != null && await _voucherService.ValidateVoucherAsync(model.VoucherCode))
                        {
                            discount = voucher.DiscountPercent.HasValue 
                                ? subtotal * voucher.DiscountPercent.Value / 100m 
                                : (voucher.DiscountAmount ?? 0);
                            discount = Math.Min(discount, subtotal);
                        }
                    }

                    decimal totalAmount = subtotal - discount;

                    // Read VNPay Sandbox configs
                    string vnp_TmnCode = _configuration["VnPay:TmnCode"] ?? "UR58PZ7C";
                    string vnp_HashSecret = _configuration["VnPay:HashSecret"] ?? "PXGZJZWNDZHNSXPLXVOFWPHJXVJZQXJZ";
                    string vnp_BaseUrl = _configuration["VnPay:BaseUrl"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
                    string vnp_ReturnUrl = _configuration["VnPay:ReturnUrl"] ?? "http://localhost:5173/vnpay-return.html";

                    var vnpay = new VnPayLibrary();
                    long amountInt = (long)(totalAmount * 100); // VNPay expects amount multiplied by 100

                    vnpay.AddRequestData("vnp_Version", "2.1.0");
                    vnpay.AddRequestData("vnp_Command", "pay");
                    vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
                    vnpay.AddRequestData("vnp_Amount", amountInt.ToString());
                    vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
                    vnpay.AddRequestData("vnp_CurrCode", "VND");
                    vnpay.AddRequestData("vnp_IpAddr", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1");
                    vnpay.AddRequestData("vnp_Locale", "vn");
                    vnpay.AddRequestData("vnp_OrderInfo", "Thanh toan don hang DMTShop");
                    vnpay.AddRequestData("vnp_OrderType", "other");
                    vnpay.AddRequestData("vnp_ReturnUrl", vnp_ReturnUrl);
                    vnpay.AddRequestData("vnp_TxnRef", DateTime.Now.Ticks.ToString()); // Generate a unique transaction ref

                    string paymentUrl = vnpay.CreateRequestUrl(vnp_BaseUrl, vnp_HashSecret);

                    return Ok(new
                    {
                        requiresRedirect = true,
                        redirectUrl = paymentUrl
                    });
                }

                // COD Payment: Directly create order
                string fullAddress = !string.IsNullOrEmpty(model.ReceiverName) && !string.IsNullOrEmpty(model.PhoneNumber)
                    ? $"{model.ReceiverName} ({model.PhoneNumber}) - {model.Address}"
                    : model.Address;

                var order = await _orderService.CreateOrderAsync(userId, fullAddress, model.PaymentMethod, model.VoucherCode);

                return Ok(new
                {
                    requiresRedirect = false,
                    orderId = order.Id,
                    message = "Đặt hàng thành công!"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("vnpay-callback")]
        public async Task<IActionResult> VNPayCallback([FromBody] VNPayCallbackDto model)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            if (model.ResponseCode == "00") // Successful transaction
            {
                try
                {
                    string fullAddress = !string.IsNullOrEmpty(model.ReceiverName) && !string.IsNullOrEmpty(model.PhoneNumber)
                        ? $"{model.ReceiverName} ({model.PhoneNumber}) - {model.Address}"
                        : model.Address;

                    var order = await _orderService.CreateOrderAsync(userId, fullAddress, "VNPay (Đã thanh toán Online)", model.VoucherCode);

                    return Ok(new
                    {
                        success = true,
                        orderId = order.Id,
                        message = "Thanh toán qua cổng VNPay thành công!"
                    });
                }
                catch (Exception ex)
                {
                    return BadRequest(new { message = "Có lỗi xảy ra khi tạo đơn hàng sau khi thanh toán: " + ex.Message });
                }
            }

            return BadRequest(new { message = "Giao dịch thanh toán qua cổng VNPay đã bị hủy hoặc không thành công." });
        }

        [HttpPost("vnpay-return-callback")]
        public async Task<IActionResult> VnPayReturnCallback([FromBody] VnPayReturnDto model)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            string vnp_HashSecret = _configuration["VnPay:HashSecret"] ?? "PXGZJZWNDZHNSXPLXVOFWPHJXVJZQXJZ";

            var vnpay = new VnPayLibrary();
            string incomingSecureHash = "";
            foreach (var kv in model.VnPayParams)
            {
                if (kv.Key == "vnp_SecureHash")
                {
                    incomingSecureHash = kv.Value;
                }
                else
                {
                    vnpay.AddResponseData(kv.Key, kv.Value);
                }
            }

            bool isValidSignature = vnpay.ValidateSignature(incomingSecureHash, vnp_HashSecret);
            if (!isValidSignature)
            {
                return BadRequest(new { message = "Xác thực chữ ký VNPay thất bại. Giao dịch không hợp lệ." });
            }

            string responseCode = vnpay.GetResponseData("vnp_ResponseCode");
            if (responseCode != "00")
            {
                return BadRequest(new { message = $"Giao dịch VNPay thất bại hoặc bị hủy. Mã phản hồi: {responseCode}" });
            }

            // Check Amount
            string amountStr = vnpay.GetResponseData("vnp_Amount");
            if (long.TryParse(amountStr, out long vnpAmount))
            {
                decimal amountPaid = vnpAmount / 100m;
                var cart = await _cartService.GetCartByUserIdAsync(userId);
                if (cart == null || !cart.CartItems.Any())
                {
                    return BadRequest(new { message = "Giỏ hàng của bạn đang trống." });
                }

                decimal subtotal = cart.CartItems.Sum(item => (item.ProductVariant.Product.DiscountPrice ?? item.ProductVariant.Product.Price) * item.Quantity);
                decimal discount = 0;

                if (!string.IsNullOrEmpty(model.VoucherCode))
                {
                    var voucher = await _voucherService.GetVoucherByCodeAsync(model.VoucherCode);
                    if (voucher != null && await _voucherService.ValidateVoucherAsync(model.VoucherCode))
                    {
                        discount = voucher.DiscountPercent.HasValue 
                            ? subtotal * voucher.DiscountPercent.Value / 100m 
                            : (voucher.DiscountAmount ?? 0);
                        discount = Math.Min(discount, subtotal);
                    }
                }

                decimal expectedAmount = subtotal - discount;

                if (Math.Abs(expectedAmount - amountPaid) > 10.0m) // max 10 VND diff for rounding
                {
                    return BadRequest(new { message = $"Số tiền thanh toán ({amountPaid:N0}đ) không khớp với số tiền đơn hàng ({expectedAmount:N0}đ)." });
                }
            }
            else
            {
                return BadRequest(new { message = "Không đọc được số tiền thanh toán từ VNPay." });
            }

            try
            {
                string fullAddress = !string.IsNullOrEmpty(model.ReceiverName) && !string.IsNullOrEmpty(model.PhoneNumber)
                    ? $"{model.ReceiverName} ({model.PhoneNumber}) - {model.Address}"
                    : model.Address;

                var order = await _orderService.CreateOrderAsync(userId, fullAddress, "VNPay (Thanh toán Online)", model.VoucherCode);

                return Ok(new
                {
                    success = true,
                    orderId = order.Id,
                    message = "Thanh toán thành công qua VNPay và đơn hàng đã được tạo!"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Có lỗi xảy ra khi tạo đơn hàng sau khi thanh toán: " + ex.Message });
            }
        }

        [HttpPost("apply-voucher")]
        public async Task<IActionResult> ApplyVoucher([FromBody] ApplyVoucherDto model)
        {
            if (string.IsNullOrEmpty(model.Code))
            {
                return BadRequest(new { message = "Vui lòng nhập mã giảm giá." });
            }

            var isValid = await _voucherService.ValidateVoucherAsync(model.Code);
            if (!isValid)
            {
                return BadRequest(new { message = "Mã giảm giá không hợp lệ hoặc đã hết hạn." });
            }

            var voucher = await _voucherService.GetVoucherByCodeAsync(model.Code);
            return Ok(new 
            {
                success = true, 
                code = voucher.Code,
                percent = voucher.DiscountPercent,
                amount = voucher.DiscountAmount
            });
        }
    }

    public class PlaceOrderDto
    {
        public string Address { get; set; } = null!;
        public string PaymentMethod { get; set; } = null!;
        public string? VoucherCode { get; set; }
        public string? ReceiverName { get; set; }
        public string? PhoneNumber { get; set; }
    }

    public class VNPayCallbackDto
    {
        public string Address { get; set; } = null!;
        public string? VoucherCode { get; set; }
        public string ResponseCode { get; set; } = null!;
        public string? ReceiverName { get; set; }
        public string? PhoneNumber { get; set; }
    }

    public class VnPayReturnDto
    {
        public Dictionary<string, string> VnPayParams { get; set; } = new Dictionary<string, string>();
        public string Address { get; set; } = null!;
        public string? ReceiverName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? VoucherCode { get; set; }
    }

    public class ApplyVoucherDto
    {
        public string Code { get; set; } = null!;
    }
}
