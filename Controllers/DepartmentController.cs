using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VendingIot.Data;
using VendingIot.Extensions;
using VendingIot.Models;

namespace VendingIot.Controllers;

[Authorize]
public class DepartmentController : BaseApiController
{
    private readonly ApplicationDbContext _context;
    private readonly IValidator<Department> _validator;

    public DepartmentController(ApplicationDbContext context, IValidator<Department> validator)
    {
        _context = context;
        _validator = validator;
    }

    [HttpGet]
    public async Task<IActionResult> GetDepartments([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var query = _context.Departments.AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(d =>
                d.Name.Contains(search) ||
                (d.Description != null && d.Description.Contains(search)));
        }

        var result = await query
            .OrderByDescending(d => d.Id)
            .ToPagedResponseAsync(page, pageSize);

        return Ok(result);
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
            return ValidationBadRequest(validationResult);
        }

        _context.Departments.Add(department);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetDepartment), new { id = department.Id }, new
        {
            message = "Berhasil Membuat department baru",
            data = department
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateDepartment(int id, Department department)
    {
        if (id != department.Id)
        {
            return BadRequest(new { message = $"ID di URL dan data tidak cocok. id:{department.Id}" });
        }

        var validationResult = await _validator.ValidateAsync(department);
        if (!validationResult.IsValid)
        {
            return ValidationBadRequest(validationResult);
        }

        if (!await _context.Departments.AnyAsync(e => e.Id == id))
        {
            return NotFound(new { message = "Data sudah tidak ada di database." });
        }

        _context.Entry(department).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Data Berhasil Diperbarui.", data = department });
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