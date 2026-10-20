using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VendingIot.Helpers;
using VendingIot.Models;

namespace VendingIot.Controllers;

[Authorize]
public class ProfileController : BaseApiController
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IValidator<ChangePasswordDTO> _changePasswordValidator;
    private readonly IValidator<UpdateProfileDTO> _updateProfileValidator;
    private readonly IFileService _fileService;

    public ProfileController(
        UserManager<ApplicationUser> userManager,
        IValidator<ChangePasswordDTO> changePasswordValidator,
        IValidator<UpdateProfileDTO> updateProfileValidator,
        RoleManager<IdentityRole> roleManager,
        IFileService fileService)
    {
        _userManager = userManager;
        _changePasswordValidator = changePasswordValidator;
        _updateProfileValidator = updateProfileValidator;
        _roleManager = roleManager;
        _fileService = fileService;
    }

    [HttpPut("updateprofile")]
    public async Task<IActionResult> UpdateProfile([FromForm] UpdateProfileDTO model)
    {
        var validationResult = await _updateProfileValidator.ValidateAsync(model);
        if (!validationResult.IsValid)
        {
            return ValidationBadRequest(validationResult);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _userManager.FindByIdAsync(userId!);
        if (user == null)
        {
            return Unauthorized(new { message = "User tidak ditemukan" });
        }

        user.FullName = model.FullName;
        user.Email = model.Email;
        user.UserName = model.Email;

        if (model.Photo != null && model.Photo.Length > 0)
        {
            try
            {
                if (!string.IsNullOrEmpty(user.Photo))
                {
                    _fileService.DeleteFile(user.Photo, "users");
                }

                user.Photo = await _fileService.SaveFileAsync(model.Photo, "users");
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
                roles,
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
            return ValidationBadRequest(validationResult);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _userManager.FindByIdAsync(userId!);
        if (user == null)
        {
            return Unauthorized();
        }

        var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
        if (result.Succeeded)
        {
            return Ok(new { message = "Password Berhasil Diubah" });
        }

        return BadRequest(new { message = "Gagal Mengubah Password", errors = result.Errors });
    }
}