using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VendingIot.Data;
using VendingIot.Extensions;
using VendingIot.Models;

namespace VendingIot.Controllers;

[Authorize]
public class PermissionCategoryController : BaseApiController
{
    private readonly ApplicationDbContext _context;
    private readonly IValidator<PermissionCategory> _validator;

    public PermissionCategoryController(ApplicationDbContext context, IValidator<PermissionCategory> validator)
    {
        _context = context;
        _validator = validator;
    }

    [HttpGet]
    public async Task<IActionResult> GetPermissionCategories([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var query = _context.PermissionCategories.AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(x => x.Name.Contains(search));
        }

        var result = await query
            .OrderByDescending(x => x.Id)
            .ToPagedResponseAsync(page, pageSize);

        return Ok(result);
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
            return ValidationBadRequest(validationResult);
        }

        _context.PermissionCategories.Add(permissionCategory);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPermissionCategory), new { id = permissionCategory.Id }, new
        {
            message = "Berhasil Membuat permission category baru",
            data = permissionCategory
        });
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
            return ValidationBadRequest(validationResult);
        }

        if (!await _context.PermissionCategories.AnyAsync(e => e.Id == id))
        {
            return NotFound(new { message = "Data sudah tidak ada di database." });
        }

        _context.Entry(permissionCategory).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Data Berhasil Diperbarui.", data = permissionCategory });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePermissionCategory(int id)
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