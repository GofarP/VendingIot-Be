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
    public class PermissionController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IValidator<Permission> _validator;

        public PermissionController(ApplicationDbContext context, IValidator<Permission> validator)
        {
            _context = context;
            _validator = validator;
        }

        [HttpGet]
        public async Task<IActionResult> GetPermission([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
        {
            try
            {
                page = page < 1 ? 1 : page;
                pageSize = pageSize < 1 ? 10 : pageSize;

                var query = _context.Permissions.AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(x => x.Name.Contains(search));
                }

                var totalCount = await query.CountAsync();
                var permissions = await query
                    .OrderByDescending(x => x.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return Ok(new
                {
                    data = permissions,
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
        public async Task<ActionResult<Permission>> GetPermission(int id)
        {
            var permission = await _context.Permissions.FindAsync(id);

            if (permission == null)
            {
                return NotFound(new { message = $"Permission with ID {id} not found." });
            }
            return Ok(permission);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePermission(Permission permission)
        {
            var validationResult = await _validator.ValidateAsync(permission);

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
                _context.Permissions.Add(permission);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetPermission), new { id = permission.Id }, new
                {
                    message = "Berhasil Membuat permission baru",
                    data = permission
                });
            }
            catch (Exception e)
            {
                return StatusCode(500, new { message = "Gagal menyimpan ke database.", error = e.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePermission(int id, Permission permission)
        {
            if (id != permission.Id)
            {
                return BadRequest(new { message = "ID di URL dan data tidak cocok." });
            }

            var validationResult = await _validator.ValidateAsync(permission);

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

            _context.Entry(permission).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Data Berhasil Diperbarui.", data = permission });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Permissions.Any(e => e.Id == id))
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
        public async Task<IActionResult> DeletePermission(int id)
        {
            var permission = await _context.Permissions.FindAsync(id);
            if (permission == null)
            {
                return NotFound(new { message = "Data is not found." });
            }

            _context.Permissions.Remove(permission);
            await _context.SaveChangesAsync();
            return Ok(new { message = $"Permission {permission.Name} berhasil dihapus." });
        }
    }
}