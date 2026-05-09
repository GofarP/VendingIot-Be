using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using VendingIot.Models;
using VendingIot.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using VendingIot.Data;
using VendingIot.Authorization;

namespace VendingIot.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IValidator<UserCreateDto> _createValidator;
    private readonly IValidator<UserUpdateDTO> _updateValidator;
    private readonly IWebHostEnvironment _environment;

    private readonly ApplicationDbContext _context;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UserController(
        UserManager<ApplicationUser> userManager,
        IValidator<UserCreateDto> createValidator,
        IValidator<UserUpdateDTO> updateValidator,
        RoleManager<IdentityRole> roleManager,
        IWebHostEnvironment environment,
        ApplicationDbContext context)
    {
        _userManager = userManager;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _roleManager = roleManager;
        _environment = environment;
        _context = context;
    }

    [HttpGet]
    [HasPermission("view-employee")]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        try
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 10 : pageSize;

            var query = _userManager.Users.AsNoTracking();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u => u.FullName.Contains(search) || u.Email.Contains(search));
            }

            var totalCount = await query.CountAsync();

            var users = await query
            .OrderByDescending(u => u.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.Email,
                u.Photo,
                PhotoUrl = string.IsNullOrEmpty(u.Photo) ? null : $"/uploads/users/{u.Photo}",
                Role = _context.UserRoles
                    .Where(ur => ur.UserId == u.Id)
                    .Join(_context.Roles,
                        ur => ur.RoleId,
                        r => r.Id,
                        (ur, r) => new { r.Id, r.Name })
                    .FirstOrDefault()
            })
            .ToListAsync();

            return Ok(new
            {
                data = users,
                pagination = new
                {
                    totalCount,
                    pageSize,
                    currentPage = page,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Internal Server Error", error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound(new { message = "User tidak ditemukan" });

        var role = await _context.UserRoles
         .Where(ur => ur.UserId == id)
         .Join(_context.Roles,
             ur => ur.RoleId,
             r => r.Id,
             (ur, r) => new { r.Id, r.Name })
         .FirstOrDefaultAsync();

        return Ok(new
        {
            data = new
            {
                user.Id,
                user.FullName,
                user.Email,
                user.Photo,
                PhotoUrl = string.IsNullOrEmpty(user.Photo) ? null : $"/uploads/users/{user.Photo}",
                Role = role
            }
        });
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create([FromForm] UserCreateDto dto)
    {
        // 1. Validasi Input (Validator harus mengecek RoleId)
        var validationResult = await _createValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
            return BadRequest(new { errors = validationResult.ToDictionary() });

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FullName = dto.FullName
        };

        // 2. Handle Upload Foto
        if (dto.Photo != null && dto.Photo.Length > 0)
        {
            user.Photo = await SaveFile(dto.Photo);
        }

        // 3. Simpan User
        var result = await _userManager.CreateAsync(user, dto.Password);

        if (!result.Succeeded)
        {
            if (!string.IsNullOrEmpty(user.Photo)) DeleteOldFile(user.Photo);
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        // 4. Cari Nama Role berdasarkan RoleId untuk Identity
        var role = await _roleManager.FindByIdAsync(dto.RoleId);
        if (role == null)
        {
            // Rollback jika role tidak ditemukan (seharusnya sudah dicek di validator)
            await _userManager.DeleteAsync(user);
            if (!string.IsNullOrEmpty(user.Photo)) DeleteOldFile(user.Photo);
            return BadRequest(new { message = "Role tidak ditemukan" });
        }

        var roleResult = await _userManager.AddToRoleAsync(user, role.Name!);

        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            if (!string.IsNullOrEmpty(user.Photo)) DeleteOldFile(user.Photo);
            return BadRequest(new { errors = roleResult.Errors.Select(e => e.Description) });
        }

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, new
        {
            message = "User Berhasil Ditambahkan",
            data = new
            {
                user.Id,
                user.FullName,
                user.Email,
                Role = role.Name, // Mengembalikan nama role untuk UI
                user.Photo,
                PhotoUrl = string.IsNullOrEmpty(user.Photo) ? null : $"/uploads/users/{user.Photo}"
            }
        });
    }

    [HttpPut("{id}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Update(string id, [FromForm] UserUpdateDTO dto)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound(new { message = "User tidak ditemukan" });

        dto.Id = id;

        var validationResult = await _updateValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
            return BadRequest(new { errors = validationResult.ToDictionary() });

        user.FullName = dto.FullName;
        user.Email = dto.Email;
        user.UserName = dto.Email;

        // Handle Password
        if (!string.IsNullOrEmpty(dto.Password))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            await _userManager.ResetPasswordAsync(user, token, dto.Password);
        }

        // Handle Photo
        if (dto.Photo != null)
        {
            if (!string.IsNullOrEmpty(user.Photo)) DeleteOldFile(user.Photo);
            user.Photo = await SaveFile(dto.Photo);
        }

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        // --- LOGIKA UPDATE ROLE BERDASARKAN ROLEID ---
        var currentRoles = await _userManager.GetRolesAsync(user);
        var targetRole = await _roleManager.FindByIdAsync(dto.RoleId);

        if (targetRole != null && !currentRoles.Contains(targetRole.Name!))
        {
            // Hapus role lama dan tambah yang baru
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            var roleResult = await _userManager.AddToRoleAsync(user, targetRole.Name!);

            if (!roleResult.Succeeded)
            {
                return BadRequest(new { message = "Gagal memperbarui role user", errors = roleResult.Errors.Select(e => e.Description) });
            }
        }

        return Ok(new
        {
            message = "User diperbarui",
            data = new
            {
                user.Id,
                user.FullName,
                user.Email,
                Role = targetRole?.Name ?? "N/A",
                user.Photo,
                PhotoUrl = string.IsNullOrEmpty(user.Photo) ? null : $"/uploads/users/{user.Photo}"
            }
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound(new { message = "User tidak ditemukan" });

        if (!string.IsNullOrEmpty(user.Photo))
        {
            DeleteOldFile(user.Photo);
        }

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded) return BadRequest(new { message = "Gagal menghapus user" });

        return Ok(new { message = $"User {user.FullName} berhasil dihapus." });
    }

    private async Task<string> SaveFile(IFormFile file)
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/users");
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);

        var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
        var fullPath = Path.Combine(path, fileName);

        using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream);
        return fileName;
    }

    private void DeleteOldFile(string fileName)
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/users", fileName);
        if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
    }
}