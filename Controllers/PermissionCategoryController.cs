using VendingIot.Models;
using Microsoft.AspNetCore.Mvc;
using VendingIot.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using FluentValidation;

namespace VendingIot.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PermissionCategoryController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IValidator<PermissionCategory> _validator;

        public PermissionCategoryController(ApplicationDbContext context, IValidator<PermissionCategory> validator)
        {
            _context = context;
            _validator = validator;
        }

        [HttpGet]
        public async Task<IActionResult> GetPermissionCategories([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                page = page < 1 ? 1 : page;
                pageSize = pageSize < 1 ? 10 : pageSize;
                var totalCount = await _context.PermissionCategories.CountAsync();
                var permissionCategories = await _context.PermissionCategories
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return Ok(new
                {
                    data = permissionCategories,
                    pagination = new
                    {
                        totalCount,
                        pageSize,
                        currentPage = page,
                        totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal Server Error", error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PermissionCategory>> GetPermissionCategory(int id)
        {
            var permissionCategory = await _context.PermissionCategories.FindAsync(id);

            if (permissionCategory == null)
            {
                return NotFound(new { message = $"PermissionCategory with ID {id} not found." });
            }
            return Ok(permissionCategory);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePermissionCategory(PermissionCategory permissionCategory)
        {
            var validationResult = await _validator.ValidateAsync(permissionCategory);

            if (!validationResult.IsValid)
            {
                return BadRequest(new
                {
                    message = "Validation failed",
                    errors = validationResult.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(e => e.ErrorMessage).ToArray()
                        )
                });
            }

            try
            {
                _context.PermissionCategories.Add(permissionCategory);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetPermissionCategory), new { id = permissionCategory.Id }, new
                {
                    message = "Berhasil Membuat permission category baru",
                    data = permissionCategory
                });
            }
            catch (Exception e)
            {
                return StatusCode(500, new { message = "Gagal menyimpan ke database.", error = e.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePermissionCategory(int id, PermissionCategory permissionCategory)
        {
            if (id != permissionCategory.Id)
            {
                return BadRequest(new { message = "ID di URL dan data tidak cocok." });
            }

            var validationResult = await _validator.ValidateAsync(permissionCategory);

            if (!validationResult.IsValid)
            {
                return BadRequest(new
                {
                    message = "Validation Failed",
                    errors = validationResult.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(e => e.ErrorMessage).ToArray()
                        )
                });
            }

            _context.Entry(permissionCategory).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Data Berhasil Diperbarui.", data = permissionCategory });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.PermissionCategories.Any(e => e.Id == id))
                {
                    return NotFound(new { message = "Data sudah tidak ada di database." });
                }
                throw;
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Gagal memperbarui data.", error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePermissionCategory(int id) // Typo nama fungsi diperbaiki di sini
        {
            var permissionCategory = await _context.PermissionCategories.FindAsync(id);
            if (permissionCategory == null)
            {
                return NotFound(new { message = "Data is not found." });
            }

            _context.PermissionCategories.Remove(permissionCategory);
            await _context.SaveChangesAsync();
            return Ok(new { message = $"Permission Category {permissionCategory.Name} berhasil dihapus." });
        }
    }
}