using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ClothingShop.Domain.Entities;
using ClothingShop.Domain.Interfaces;

namespace ClothingShop.Application.Services
{
    public class CartService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CartService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Cart> GetCartByUserIdAsync(string userId)
        {
            var cart = await _unitOfWork.Repository<Cart>().GetQueryable()
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                            .ThenInclude(p => p.ProductImages)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                cart = new Cart { UserId = userId, CreatedDate = DateTime.UtcNow };
                await _unitOfWork.Repository<Cart>().AddAsync(cart);
                await _unitOfWork.SaveChangesAsync();
            }

            return cart;
        }

        public async Task<bool> AddToCartAsync(string userId, int variantId, int quantity)
        {
            var variant = await _unitOfWork.Repository<ProductVariant>().GetByIdAsync(variantId);
            if (variant == null || variant.Quantity < quantity)
            {
                return false; // Out of stock or invalid
            }

            var cart = await GetCartByUserIdAsync(userId);
            var cartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductVariantId == variantId);

            if (cartItem == null)
            {
                cartItem = new CartItem
                {
                    CartId = cart.Id,
                    ProductVariantId = variantId,
                    Quantity = quantity
                };
                await _unitOfWork.Repository<CartItem>().AddAsync(cartItem);
            }
            else
            {
                if (variant.Quantity < cartItem.Quantity + quantity)
                {
                    return false; // Total request exceeds stock
                }
                cartItem.Quantity += quantity;
                _unitOfWork.Repository<CartItem>().Update(cartItem);
            }

            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateCartItemQuantityAsync(string userId, int cartItemId, int quantity)
        {
            var cartItem = await _unitOfWork.Repository<CartItem>().GetQueryable()
                .Include(ci => ci.Cart)
                .Include(ci => ci.ProductVariant)
                .FirstOrDefaultAsync(ci => ci.Id == cartItemId && ci.Cart.UserId == userId);

            if (cartItem == null || quantity <= 0)
            {
                return false;
            }

            if (cartItem.ProductVariant.Quantity < quantity)
            {
                return false; // Insufficient stock
            }

            cartItem.Quantity = quantity;
            _unitOfWork.Repository<CartItem>().Update(cartItem);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveFromCartAsync(string userId, int cartItemId)
        {
            var cartItem = await _unitOfWork.Repository<CartItem>().GetQueryable()
                .Include(ci => ci.Cart)
                .FirstOrDefaultAsync(ci => ci.Id == cartItemId && ci.Cart.UserId == userId);

            if (cartItem == null)
            {
                return false;
            }

            _unitOfWork.Repository<CartItem>().Delete(cartItem);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task ClearCartAsync(string userId)
        {
            var cart = await _unitOfWork.Repository<Cart>().GetQueryable()
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart != null && cart.CartItems.Any())
            {
                foreach (var item in cart.CartItems.ToList())
                {
                    _unitOfWork.Repository<CartItem>().Delete(item);
                }
                await _unitOfWork.SaveChangesAsync();
            }
        }

        public async Task MigrateSessionCartToDbAsync(string userId, List<(int variantId, int quantity)> sessionCartItems)
        {
            if (sessionCartItems == null || !sessionCartItems.Any()) return;

            var cart = await GetCartByUserIdAsync(userId);
            foreach (var item in sessionCartItems)
            {
                var variant = await _unitOfWork.Repository<ProductVariant>().GetByIdAsync(item.variantId);
                if (variant == null) continue;

                var dbItem = cart.CartItems.FirstOrDefault(ci => ci.ProductVariantId == item.variantId);
                var neededQty = item.quantity;

                if (dbItem == null)
                {
                    var addedQty = Math.Min(neededQty, variant.Quantity);
                    if (addedQty > 0)
                    {
                        await _unitOfWork.Repository<CartItem>().AddAsync(new CartItem
                        {
                            CartId = cart.Id,
                            ProductVariantId = item.variantId,
                            Quantity = addedQty
                        });
                    }
                }
                else
                {
                    var newQty = Math.Min(dbItem.Quantity + neededQty, variant.Quantity);
                    if (newQty > dbItem.Quantity)
                    {
                        dbItem.Quantity = newQty;
                        _unitOfWork.Repository<CartItem>().Update(dbItem);
                    }
                }
            }
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
