using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ClothingShop.Domain.Entities;
using ClothingShop.Domain.Interfaces;

namespace ClothingShop.Application.Services
{
    public class CategoryService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CategoryService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<Category>> GetAllCategoriesAsync()
        {
            return await _unitOfWork.Repository<Category>().GetQueryable()
                .Include(c => c.SubCategories)
                .Include(c => c.ParentCategory)
                .ToListAsync();
        }

        public async Task<Category> GetByIdAsync(int id)
        {
            return await _unitOfWork.Repository<Category>().GetQueryable()
                .Include(c => c.SubCategories)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task AddCategoryAsync(Category category)
        {
            await _unitOfWork.Repository<Category>().AddAsync(category);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task UpdateCategoryAsync(Category category)
        {
            _unitOfWork.Repository<Category>().Update(category);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteCategoryAsync(int id)
        {
            var category = await _unitOfWork.Repository<Category>().GetByIdAsync(id);
            if (category != null)
            {
                _unitOfWork.Repository<Category>().Delete(category);
                await _unitOfWork.SaveChangesAsync();
            }
        }
    }
}
