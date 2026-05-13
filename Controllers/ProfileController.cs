using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VendingIot.Models;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProfileController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    private readonly RoleManager<IdentityRole> _roleManager;

    private readonly IValidator<ChangePasswordDTO> _changePasswordValidator;
    private readonly IValidator<UpdateProfileDTO> _updateProfileValidator;

    public ProfileController(
        UserManager<ApplicationUser> userManager,
        IValidator<ChangePasswordDTO> changePasswordValidator,
        IValidator<UpdateProfileDTO> updateProfileValidator,
        RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _changePasswordValidator = changePasswordValidator;
        _updateProfileValidator = updateProfileValidator;
        _roleManager = roleManager;
    }

    [HttpPut("updateprofile")]
    public async Task<IActionResult> UpdateProfile([FromForm] UpdateProfileDTO model)
    {
        var validationResult = await _updateProfileValidator.ValidateAsync(model);
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

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _userManager.FindByIdAsync(userId!);
        if (user == null) return Unauthorized(new { message = "User tidak ditemukan" });

        user.FullName = model.FullName;
        user.Email = model.Email;
        user.UserName = model.Email; 

        if (model.Photo != null && model.Photo.Length > 0)
        {
            try
            {
                if (!string.IsNullOrEmpty(user.Photo))
                {
                    DeleteOldFile(user.Photo);
                }

                user.Photo = await SaveFile(model.Photo);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Gagal menyimpan foto: " + ex.Message });
            }
        }

        var result = await _userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var permissions = new List<string>();

            foreach (var roleName in roles)
            {
                var role = await _roleManager.FindByNameAsync(roleName);
                if (role != null)
                {
                    var roleClaims = await _roleManager.GetClaimsAsync(role);
                    permissions.AddRange(roleClaims.Select(c => c.Value));
                }
            }

            return Ok(new
            {
                message = "Profile Berhasil Diperbarui",
                email = user.Email,
                fullName = user.FullName,
                photoUrl = user.Photo,
                roles = roles,
                permissions = permissions.Distinct().ToList()
            });
        }

        return BadRequest(new
        {
            message = "Gagal memperbarui profil",
            errors = result.Errors.Select(e => e.Description)
        });
    }

    [HttpPut("changepassword")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO model)
    {
        var validationResult = await _changePasswordValidator.ValidateAsync(model);

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
        var userid = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _userManager.FindByIdAsync(userid!);

        if (user == null) return Unauthorized();

        var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);

        if (result.Succeeded)
        {
            return Ok(new { message = "Password Berhasil Diubah" });
        }

        return BadRequest(new { message = "Gagal Mengubah Password", errors = result.Errors });
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