using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClothingShop.Infrastructure.Data;
using ClothingShop.Domain.Entities;
using Microsoft.AspNetCore.Authorization;

namespace ClothingShop.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductTypeApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProductTypeApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/ProductTypeApi
        [HttpGet]
        public async Task<IActionResult> GetProductTypes()
        {
            try
            {
                var productTypes = await _context.ProductTypes
                    .OrderBy(pt => pt.Name)
                    .Select(pt => new { pt.Id, pt.Name })
                    .ToListAsync();
                
                return Ok(productTypes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách loại sản phẩm", error = ex.Message });
            }
        }

        // POST: api/ProductTypeApi
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateProductType([FromBody] ProductType dto)
        {
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(dto.Name))
            {
                return BadRequest(new { message = "Tên loại sản phẩm không hợp lệ" });
            }

            try
            {
                var pt = new ProductType { Name = dto.Name };
                _context.ProductTypes.Add(pt);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Thêm loại sản phẩm thành công", id = pt.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi thêm loại sản phẩm", error = ex.Message });
            }
        }

        // PUT: api/ProductTypeApi/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateProductType(int id, [FromBody] ProductType dto)
        {
            if (id != dto.Id || string.IsNullOrWhiteSpace(dto.Name))
            {
                return BadRequest(new { message = "Dữ liệu không hợp lệ" });
            }

            try
            {
                var pt = await _context.ProductTypes.FindAsync(id);
                if (pt == null)
                {
                    return NotFound(new { message = "Không tìm thấy loại sản phẩm" });
                }

                pt.Name = dto.Name;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Cập nhật loại sản phẩm thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi cập nhật loại sản phẩm", error = ex.Message });
            }
        }

        // DELETE: api/ProductTypeApi/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProductType(int id)
        {
            try
            {
                var pt = await _context.ProductTypes.FindAsync(id);
                if (pt == null)
                {
                    return NotFound(new { message = "Không tìm thấy loại sản phẩm" });
                }

                _context.ProductTypes.Remove(pt);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Xóa loại sản phẩm thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi xóa loại sản phẩm. Vui lòng kiểm tra xem loại sản phẩm này có đang được sử dụng bởi sản phẩm nào không.", error = ex.Message });
            }
        }
    }
}
