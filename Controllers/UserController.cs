using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VendingIot.Authorization;
using VendingIot.Data;
using VendingIot.Extensions;
using VendingIot.Helpers;
using VendingIot.Models;
using VendingIot.Models.DTO;

namespace VendingIot.Controllers;

[Authorize]
public class UserController : BaseApiController
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IValidator<UserCreateDto> _createValidator;
    private readonly IValidator<UserUpdateDTO> _updateValidator;
    private readonly ApplicationDbContext _context;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IFileService _fileService;

    public UserController(
        UserManager<ApplicationUser> userManager,
        IValidator<UserCreateDto> createValidator,
        IValidator<UserUpdateDTO> updateValidator,
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext context,
        IFileService fileService)
    {
        _userManager = userManager;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _roleManager = roleManager;
        _context = context;
        _fileService = fileService;
    }

    [HttpGet]
    [HasPermission("view-employee")]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 10 : pageSize;

        var query = _userManager.Users.AsNoTracking();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(u => (u.FullName != null && u.FullName.Contains(search)) ||
                                     (u.Email != null && u.Email.Contains(search)));
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

        return Ok(users.ToPagedResponse(totalCount, page, pageSize));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { message = "User tidak ditemukan" });
        }

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
    [HasPermission("create-employee")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create([FromForm] UserCreateDto dto)
    {
        var validationResult = await _createValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            return ValidationBadRequest(validationResult);
        }

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FullName = dto.FullName
        };

        if (dto.Photo != null && dto.Photo.Length > 0)
        {
            user.Photo = await _fileService.SaveFileAsync(dto.Photo, "users");
        }

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            _fileService.DeleteFile(user.Photo, "users");
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        var role = await _roleManager.FindByIdAsync(dto.RoleId);
        if (role == null)
        {
            await _userManager.DeleteAsync(user);
            _fileService.DeleteFile(user.Photo, "users");
            return BadRequest(new { message = "Role tidak ditemukan" });
        }

        var roleResult = await _userManager.AddToRoleAsync(user, role.Name!);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            _fileService.DeleteFile(user.Photo, "users");
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
                Role = role.Name,
                user.Photo,
                PhotoUrl = string.IsNullOrEmpty(user.Photo) ? null : $"/uploads/users/{user.Photo}"
            }
        });
    }

    [HttpPut("{id}")]
    [HasPermission("edit-employee")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Update(string id, [FromForm] UserUpdateDTO dto)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { message = "User tidak ditemukan" });
        }

        dto.Id = id;
        var validationResult = await _updateValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            return ValidationBadRequest(validationResult);
        }

        user.FullName = dto.FullName;
        user.Email = dto.Email;
        user.UserName = dto.Email;

        if (!string.IsNullOrEmpty(dto.Password))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            await _userManager.ResetPasswordAsync(user, token, dto.Password);
        }

        if (dto.Photo != null)
        {
            _fileService.DeleteFile(user.Photo, "users");
            user.Photo = await _fileService.SaveFileAsync(dto.Photo, "users");
        }

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        var targetRole = !string.IsNullOrEmpty(dto.RoleId)
            ? await _roleManager.FindByIdAsync(dto.RoleId)
            : null;

        if (targetRole != null && !currentRoles.Contains(targetRole.Name!))
        {
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
    [HasPermission("delete-employee")]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { message = "User tidak ditemukan" });
        }

        _fileService.DeleteFile(user.Photo, "users");

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(new { message = "Gagal menghapus user" });
        }

        return Ok(new { message = $"User {user.FullName} berhasil dihapus." });
    }
}