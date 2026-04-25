using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using VendingIot.Models;
using VendingIot.Models.DTO;

namespace VendingIot.Controllers;

[ApiController]
[Route("api/[controller]")]

public class UserController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IValidator<UserCreateDto> _createValidator;

    public UserController(UserManager<ApplicationUser> userManager, IValidator<UserCreateDto> createValidator)
    {
        _userManager = userManager;
        _createValidator = createValidator;
    }


    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 10 : pageSize;

        var totalCount = await _userManager.Users.CountAsync();
        var users = await _userManager.Users
        .OrderByDescending(u => u.Id)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(u => new
        {
            u.Id,
            u.FullName,
            u.Email,
            u.Photo,
            PhotoUrl = string.IsNullOrEmpty(u.Photo) ? null : $"/uploads/users/{u.Photo}"
        })
        .ToListAsync();

        return Ok(new
        {
            data = users,
            pagination = new { totalCount, pageSize, currentPage = page }
        });

    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound(new { message = "User tidak ditemukan" });

        return Ok(new
        {
            user.Id,
            user.FullName,
            user.Email,
            user.Photo,
            PhotoUrl = string.IsNullOrEmpty(user.Photo) ? null : $"/uploads/users/{user.Photo}"
        });
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create([FromForm] UserCreateDto dto)
    {
        var validationResult = await _createValidator.ValidateAsync(dto);
        if (!validationResult.IsValid) return BadRequest(new { errors = validationResult.ToDictionary() });

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FullName = dto.FullName
        };

        if (dto.PhotoFile != null) user.Photo = await SaveFile(dto.PhotoFile);

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded) return BadRequest(result.Errors);

        return CreatedAtAction(nameof(GetById), new { id = user.Id }, new { message = "User berhasil dibuat", data = user });

    }

    [HttpPut("{id}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Update(string id, [FromForm] UserUpdateDTO dto)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();
        var validator = new UserUpdateValidator(_userManager, id);

        var validationResult = await validator.ValidateAsync(dto);

        user.FullName = dto.FullName;
        user.Email = dto.Email;
        user.UserName = dto.Email;

        if (!string.IsNullOrEmpty(dto.Password))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            await _userManager.ResetPasswordAsync(user, token, dto.Password);
        }

        if (dto.PhotoFile != null)
        {
            if (!string.IsNullOrEmpty(user.Photo)) DeleteOldFile(user.Photo);
            user.Photo = await SaveFile(dto.PhotoFile);
        }

        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded ? Ok(new { message = "User diperbarui", data = user }) : BadRequest(result.Errors);

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
        using var stream = new FileStream(Path.Combine(path, fileName), FileMode.Create);
        await file.CopyToAsync(stream);
        return fileName;
    }

    private void DeleteOldFile(string fileName)
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/users", fileName);
        if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
    }


}
