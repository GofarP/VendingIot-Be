using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VendingIot.Data;
using VendingIot.Extensions;
using VendingIot.Models;

namespace VendingIot.Controllers;

[Authorize]
public class PermissionController : BaseApiController
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
        var query = _context.Permissions.AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(x => x.Name.Contains(search));
        }

        var result = await query
            .OrderByDescending(x => x.Id)
            .Select(p => new
            {
                p.Id,
                p.Name,
                PermissionCategory = p.PermissionCategory != null ? new
                {
                    p.PermissionCategory.Id,
                    p.PermissionCategory.Name
                } : null
            })
            .ToPagedResponseAsync(page, pageSize, message: "Success retrieve permissions");

        return Ok(result);
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
            return ValidationBadRequest(validationResult);
        }

        _context.Permissions.Add(permission);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPermission), new { id = permission.Id }, new
        {
            message = "Berhasil Membuat permission baru",
            data = permission
        });
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
            return ValidationBadRequest(validationResult);
        }

        if (!await _context.Permissions.AnyAsync(e => e.Id == id))
        {
            return NotFound(new { message = "Data sudah tidak ada di database." });
        }

        _context.Entry(permission).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Data Berhasil Diperbarui.", data = permission });
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