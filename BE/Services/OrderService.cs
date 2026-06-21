using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ClothingShop.Domain.Entities;
using ClothingShop.Domain.Interfaces;

namespace ClothingShop.Application.Services
{
    public class OrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly CartService _cartService;
        private readonly VoucherService _voucherService;

        public OrderService(IUnitOfWork unitOfWork, CartService cartService, VoucherService voucherService)
        {
            _unitOfWork = unitOfWork;
            _cartService = cartService;
            _voucherService = voucherService;
        }

        public async Task<Order> CreateOrderAsync(string userId, string address, string paymentMethod, string voucherCode)
        {
            var cart = await _cartService.GetCartByUserIdAsync(userId);
            if (cart == null || !cart.CartItems.Any())
            {
                throw new InvalidOperationException("Giỏ hàng của bạn đang trống.");
            }

            // 1. Validate Stock
            foreach (var item in cart.CartItems)
            {
                if (item.ProductVariant.Quantity < item.Quantity)
                {
                    throw new InvalidOperationException($"Sản phẩm '{item.ProductVariant.Product.Name}' (Size: {item.ProductVariant.Size}, Color: {item.ProductVariant.Color}) không đủ số lượng trong kho. Hiện còn {item.ProductVariant.Quantity} sản phẩm.");
                }
            }

            // 2. Calculate totals
            decimal subtotal = cart.CartItems.Sum(item => (item.ProductVariant.Product.DiscountPrice ?? item.ProductVariant.Product.Price) * item.Quantity);
            decimal discount = 0;

            if (!string.IsNullOrEmpty(voucherCode))
            {
                var voucher = await _voucherService.GetVoucherByCodeAsync(voucherCode);
                if (voucher != null && await _voucherService.ValidateVoucherAsync(voucherCode))
                {
                    if (voucher.DiscountPercent.HasValue)
                    {
                        discount = subtotal * voucher.DiscountPercent.Value / 100m;
                    }
                    else if (voucher.DiscountAmount.HasValue)
                    {
                        discount = voucher.DiscountAmount.Value;
                    }
                    discount = Math.Min(discount, subtotal); // Discount cannot exceed subtotal
                }
            }

            decimal totalAmount = subtotal - discount;

            // 3. Create Order
            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.UtcNow,
                Status = "Chờ xác nhận",
                TotalAmount = totalAmount,
                DiscountAmount = discount,
                ShippingAddress = address,
                PaymentMethod = paymentMethod,
                VoucherCode = voucherCode
            };

            await _unitOfWork.Repository<Order>().AddAsync(order);
            await _unitOfWork.SaveChangesAsync(); // Generates order.Id

            // 4. Create OrderDetails & Decrement Stock
            foreach (var item in cart.CartItems)
            {
                var orderDetail = new OrderDetail
                {
                    OrderId = order.Id,
                    ProductVariantId = item.ProductVariantId,
                    Quantity = item.Quantity,
                    UnitPrice = item.ProductVariant.Product.DiscountPrice ?? item.ProductVariant.Product.Price
                };
                await _unitOfWork.Repository<OrderDetail>().AddAsync(orderDetail);

                // Decrement stock
                item.ProductVariant.Quantity -= item.Quantity;
                _unitOfWork.Repository<ProductVariant>().Update(item.ProductVariant);
            }

            // 5. Update Voucher
            if (!string.IsNullOrEmpty(voucherCode) && discount > 0)
            {
                await _voucherService.UseVoucherAsync(voucherCode);
            }

            // 6. Clear Cart
            await _cartService.ClearCartAsync(userId);

            await _unitOfWork.SaveChangesAsync();

            return await GetOrderByIdAsync(order.Id);
        }

        public async Task<Order> GetOrderByIdAsync(int orderId, string userId = null)
        {
            var query = _unitOfWork.Repository<Order>().GetQueryable()
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                            .ThenInclude(p => p.ProductImages);

            if (userId != null)
            {
                return await query.FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);
            }

            return await query.FirstOrDefaultAsync(o => o.Id == orderId);
        }

        public async Task<IEnumerable<Order>> GetOrdersByUserIdAsync(string userId)
        {
            return await _unitOfWork.Repository<Order>().GetQueryable()
                .Where(o => o.UserId == userId)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Order>> GetAllOrdersAsync()
        {
            return await _unitOfWork.Repository<Order>().GetQueryable()
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, string status)
        {
            var order = await _unitOfWork.Repository<Order>().GetByIdAsync(orderId);
            if (order == null)
            {
                return false;
            }

            // Validate status transition if needed, or update directly
            order.Status = status;
            _unitOfWork.Repository<Order>().Update(order);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
    }
}
