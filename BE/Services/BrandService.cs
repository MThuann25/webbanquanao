using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ClothingShop.Domain.Entities;
using ClothingShop.Domain.Interfaces;

namespace ClothingShop.Application.Services
{
    public class BrandService
    {
        private readonly IUnitOfWork _unitOfWork;

        public BrandService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<Brand>> GetAllBrandsAsync()
        {
            return await _unitOfWork.Repository<Brand>().GetAllAsync();
        }

        public async Task<Brand> GetByIdAsync(int id)
        {
            return await _unitOfWork.Repository<Brand>().GetByIdAsync(id);
        }

        public async Task AddBrandAsync(Brand brand)
        {
            await _unitOfWork.Repository<Brand>().AddAsync(brand);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task UpdateBrandAsync(Brand brand)
        {
            _unitOfWork.Repository<Brand>().Update(brand);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteBrandAsync(int id)
        {
            var brand = await _unitOfWork.Repository<Brand>().GetByIdAsync(id);
            if (brand != null)
            {
                _unitOfWork.Repository<Brand>().Delete(brand);
                await _unitOfWork.SaveChangesAsync();
            }
        }
    }
}
