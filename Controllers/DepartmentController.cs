using VendingIot.Models;
using Microsoft.AspNetCore.Identity;
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
    public class DepartmentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IValidator<Department> _validator;

        public DepartmentController(ApplicationDbContext context, IValidator<Department> validator)
        {
            _context = context;
            _validator = validator;
        }

        [HttpGet]
        public async Task<IActionResult> GetDepartments([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                page = page < 1 ? 1 : page;
                pageSize = pageSize < 1 ? 10 : pageSize;

                var totalCount = await _context.Departments.CountAsync();
                var departments = await _context.Departments
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return Ok(new
                {
                    data = departments,
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
        public async Task<ActionResult<Department>> GetDepartment(int id)
        {
            var department = await _context.Departments.FindAsync(id);

            if (department == null)
            {
                return NotFound(new { message = $"Departemen with ID {id} not found." });
            }

            return Ok(department);
        }

        [HttpPost]
        public async Task<IActionResult> CreateDepartment(Department department)
        {
            var validationResult = await _validator.ValidateAsync(department);

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
                _context.Departments.Add(department);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetDepartment), new { id = department.Id }, new
                {
                    message = "Berhasil Membuat department baru",
                    data = department
                });
            }
            catch (Exception e)
            {
                return StatusCode(500, new { message = "Gagal menyimpan ke database.", error = e.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDepartment(int id, Department department)
        {
            if (id != department.Id)
            {
                return BadRequest(new { message = "ID di URL dan data tidak cocok." });
            }

            var validationResult = await _validator.ValidateAsync(department);

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

            _context.Entry(department).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Data Berhasil Diperbarui.", data = department });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Departments.Any(e => e.Id == id))
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
        public async Task<IActionResult> DeleteDepartment(int id)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department == null)
            {
                return NotFound(new { message = "Data tidak ditemukan." });
            }

            _context.Departments.Remove(department);
            await _context.SaveChangesAsync();
            return Ok(new { message = $"Departemen {department.Name} berhasil dihapus." });
        }
    }
}