using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ClothingShop.Domain.Entities;
using ClothingShop.Domain.Interfaces;

namespace ClothingShop.Application.Services
{
    public class VoucherService
    {
        private readonly IUnitOfWork _unitOfWork;

        public VoucherService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Voucher> GetVoucherByCodeAsync(string code)
        {
            if (string.IsNullOrEmpty(code)) return null;
            return await _unitOfWork.Repository<Voucher>().GetQueryable()
                .FirstOrDefaultAsync(v => v.Code.ToUpper() == code.ToUpper());
        }

        public async Task<bool> ValidateVoucherAsync(string code)
        {
            var voucher = await GetVoucherByCodeAsync(code);
            if (voucher == null)
            {
                return false;
            }

            if (voucher.ExpiryDate < DateTime.UtcNow)
            {
                return false; // Expired
            }

            if (voucher.UsageLimit > 0 && voucher.UsedCount >= voucher.UsageLimit)
            {
                return false; // Usage limit reached
            }

            return true;
        }

        public async Task UseVoucherAsync(string code)
        {
            var voucher = await GetVoucherByCodeAsync(code);
            if (voucher != null)
            {
                voucher.UsedCount++;
                _unitOfWork.Repository<Voucher>().Update(voucher);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<Voucher>> GetAllVouchersAsync()
        {
            return await _unitOfWork.Repository<Voucher>().GetAllAsync();
        }

        public async Task<Voucher> GetByIdAsync(int id)
        {
            return await _unitOfWork.Repository<Voucher>().GetByIdAsync(id);
        }

        public async Task AddVoucherAsync(Voucher voucher)
        {
            await _unitOfWork.Repository<Voucher>().AddAsync(voucher);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task UpdateVoucherAsync(Voucher voucher)
        {
            _unitOfWork.Repository<Voucher>().Update(voucher);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteVoucherAsync(int id)
        {
            var voucher = await _unitOfWork.Repository<Voucher>().GetByIdAsync(id);
            if (voucher != null)
            {
                _unitOfWork.Repository<Voucher>().Delete(voucher);
                await _unitOfWork.SaveChangesAsync();
            }
        }
    }
}
