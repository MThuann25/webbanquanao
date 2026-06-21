using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ClothingShop.Domain.Entities;
using ClothingShop.Domain.Interfaces;

namespace ClothingShop.Application.Services
{
    public class ProductService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ProductService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Product> GetByIdAsync(int id)
        {
            return await _unitOfWork.Repository<Product>().GetQueryable()
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Include(p => p.ProductVariants)
                .Include(p => p.Reviews)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<IEnumerable<Product>> GetFeaturedProductsAsync(int count)
        {
            return await _unitOfWork.Repository<Product>().GetQueryable()
                .Where(p => p.IsActive)
                .Include(p => p.ProductImages)
                .OrderByDescending(p => p.CreatedDate)
                .Take(count)
                .ToListAsync();
        }

        public async Task<(IEnumerable<Product> Products, int TotalCount)> GetFilteredProductsAsync(
            string search, int? categoryId, int? brandId, string size, string color, 
            decimal? minPrice, decimal? maxPrice, string sortBy, int page, int pageSize)
        {
            var query = _unitOfWork.Repository<Product>().GetQueryable()
                .Where(p => p.IsActive)
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Include(p => p.ProductVariants)
                .AsQueryable();

            // Search by Name or Brand
            if (!string.IsNullOrEmpty(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(lowerSearch) || 
                                         (p.Brand != null && p.Brand.Name.ToLower().Contains(lowerSearch)));
            }

            // Filter by Category
            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value || 
                                         (p.Category != null && p.Category.ParentId == categoryId.Value));
            }

            // Filter by Brand
            if (brandId.HasValue)
            {
                query = query.Where(p => p.BrandId == brandId.Value);
            }

            // Filter by Variant Size
            if (!string.IsNullOrEmpty(size))
            {
                query = query.Where(p => p.ProductVariants.Any(v => v.Size == size && v.Quantity > 0));
            }

            // Filter by Variant Color
            if (!string.IsNullOrEmpty(color))
            {
                query = query.Where(p => p.ProductVariants.Any(v => v.Color == color && v.Quantity > 0));
            }

            // Filter by Price
            if (minPrice.HasValue)
            {
                query = query.Where(p => (p.DiscountPrice ?? p.Price) >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                query = query.Where(p => (p.DiscountPrice ?? p.Price) <= maxPrice.Value);
            }

            // Sort
            query = sortBy switch
            {
                "price_asc" => query.OrderBy(p => p.DiscountPrice ?? p.Price),
                "price_desc" => query.OrderByDescending(p => p.DiscountPrice ?? p.Price),
                "newest" => query.OrderByDescending(p => p.CreatedDate),
                _ => query.OrderByDescending(p => p.Id)
            };

            var totalCount = await query.CountAsync();
            var products = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return (products, totalCount);
        }

        public async Task AddProductAsync(Product product)
        {
            await _unitOfWork.Repository<Product>().AddAsync(product);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task UpdateProductAsync(Product product)
        {
            _unitOfWork.Repository<Product>().Update(product);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteProductAsync(int id)
        {
            var product = await _unitOfWork.Repository<Product>().GetByIdAsync(id);
            if (product != null)
            {
                _unitOfWork.Repository<Product>().Delete(product);
                await _unitOfWork.SaveChangesAsync();
            }
        }
    }
}
