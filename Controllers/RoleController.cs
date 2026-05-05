using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using VendingIot.Data;

namespace VendingIot.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoleController : ControllerBase
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _context;
    private readonly IValidator<RoleCreateDto> _validator;

    public RoleController(
        RoleManager<IdentityRole> roleManager, 
        ApplicationDbContext context, 
        IValidator<RoleCreateDto> validator)
    {
        _roleManager = roleManager;
        _context = context;
        _validator = validator;
    }

    [HttpGet]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _roleManager.Roles
            .Select(r => new { r.Id, r.Name })
            .ToListAsync();

        return Ok(new { data = roles });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetRole(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role == null) return NotFound(new { message = "Role tidak ditemukan" });

        var claims = await _roleManager.GetClaimsAsync(role);
        var permissions = claims.Where(c => c.Type == "Permission").Select(c => c.Value).ToList();

        return Ok(new { data = new { role.Id, role.Name, Permissions = permissions } });
    }

    [HttpPost]
    public async Task<IActionResult> CreateRole([FromBody] RoleCreateDto dto)
    {
        var validationResult = await _validator.ValidateAsync(dto);
        if (!validationResult.IsValid)
            return BadRequest(new { errors = validationResult.ToDictionary() });

        var newRole = new IdentityRole(dto.Name);
        var result = await _roleManager.CreateAsync(newRole);

        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        // Simpan Permission berdasarkan Name (karena tidak ada Slug)
        if (dto.PermissionIds != null && dto.PermissionIds.Any())
        {
            var permissionNames = await _context.Permissions
                .Where(p => dto.PermissionIds.Contains(p.Id))
                .Select(p => p.Name)
                .ToListAsync();

            foreach (var pName in permissionNames)
            {
                await _roleManager.AddClaimAsync(newRole, new Claim("Permission", pName));
            }
        }

        return CreatedAtAction(nameof(GetRole), new { id = newRole.Id }, new
        {
            message = "Role berhasil dibuat",
            data = new { newRole.Id, newRole.Name }
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateRole(string id, [FromBody] RoleCreateDto dto)
    {
        dto.Id = id; // Pastikan ID DTO sinkron dengan URL
        var validationResult = await _validator.ValidateAsync(dto);
        if (!validationResult.IsValid)
            return BadRequest(new { errors = validationResult.ToDictionary() });

        var role = await _roleManager.FindByIdAsync(id);
        if (role == null) return NotFound(new { message = "Role tidak ditemukan" });

        role.Name = dto.Name;
        await _roleManager.UpdateAsync(role);

        var existingClaims = await _roleManager.GetClaimsAsync(role);
        foreach (var claim in existingClaims.Where(c => c.Type == "Permission"))
        {
            await _roleManager.RemoveClaimAsync(role, claim);
        }

        if (dto.PermissionIds != null && dto.PermissionIds.Any())
        {
            var permissionNames = await _context.Permissions
                .Where(p => dto.PermissionIds.Contains(p.Id))
                .Select(p => p.Name)
                .ToListAsync();

            foreach (var pName in permissionNames)
            {
                await _roleManager.AddClaimAsync(role, new Claim("Permission", pName));
            }
        }

        return Ok(new { message = "Role berhasil diperbarui" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRole(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role == null) return NotFound();

        var result = await _roleManager.DeleteAsync(role);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        return Ok(new { message = "Role berhasil dihapus" });
    }
}