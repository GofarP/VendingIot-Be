using VendingIot.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;
using VendingIot.Helpers;
using VendingIot.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Data;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PermissionController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public PermissionController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetPermission([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 10 : pageSize;
            var totalCount = await _context.Permissions.CountAsync();
            var permissions = await _context.Permissions
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
            return NotFound(new { message = $"PermissionCategory with ID {id} not found." });
        }
        return Ok(permission);
    }

    [HttpPost]
    public async Task<IActionResult> CreatePermission(Permission permission)
    {
        Validation.Required(ModelState, "Name", permission.Name, "Please fill permission name");
        Validation.Required(ModelState, "PermissionCategoryId", permission.PermissionCategoryId, "Please Choose Permission Category First");

        if (!string.IsNullOrEmpty(permission.Name))
        {
            await Validation.Unique(ModelState, _context.Permissions, d => d.Name.ToLower() == permission.Name.ToLower(), "Name", "This permission name already exist");
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                message = "Validation failed",
                errors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray()
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

        Validation.Required(ModelState, "Name", permission.Name, "Please fill permission name");

        if (permission.PermissionCategoryId <= 0)
        {
            ModelState.AddModelError("PermissionCategoryId", "Please select a valid permission category");
        }

        if (!string.IsNullOrEmpty(permission.Name))
        {
            await Validation.Unique(
                ModelState,
                _context.Permissions,
                d => d.Name == permission.Name && d.Id != id,
                "Name",
                "This permission name already exists"
            );
        }

        if (permission.PermissionCategoryId > 0)
        {
            var categoryExists = await _context.PermissionCategories.AnyAsync(c => c.Id == permission.PermissionCategoryId);
            if (!categoryExists)
            {
                ModelState.AddModelError("PermissionCategoryId", "The selected category does not exist");
            }
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                message = "Validation Failed",
                errors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray()
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
