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
    [Authorize]
    public class CheckoutApiController : ControllerBase
    {
        private readonly OrderService _orderService;
        private readonly CartService _cartService;
        private readonly VoucherService _voucherService;
        private readonly IUnitOfWork _unitOfWork;

        public CheckoutApiController(
            OrderService orderService,
            CartService cartService,
            VoucherService voucherService,
            IUnitOfWork unitOfWork)
        {
            _orderService = orderService;
            _cartService = cartService;
            _voucherService = voucherService;
            _unitOfWork = unitOfWork;
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

                // If VNPay, we simulate the redirection to vnpay-gateway.html
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

                    // Return parameters to frontend so it can redirect the user to vnpay-gateway.html
                    return Ok(new
                    {
                        requiresRedirect = true,
                        redirectUrl = $"vnpay-gateway.html?amount={totalAmount}&address={Uri.EscapeDataString(model.Address)}&receiverName={Uri.EscapeDataString(model.ReceiverName ?? "")}&phoneNumber={Uri.EscapeDataString(model.PhoneNumber ?? "")}&voucherCode={Uri.EscapeDataString(model.VoucherCode ?? "")}"
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

    public class ApplyVoucherDto
    {
        public string Code { get; set; } = null!;
    }
}
