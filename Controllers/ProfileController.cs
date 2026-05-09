using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VendingIot.Models;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    private readonly RoleManager<IdentityRole> _roleManager;

    private readonly IValidator<ChangePasswordDTO> _changePasswordValidator;
    private readonly IValidator<UpdateProfileDTO> _updateProfileValidator;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        IValidator<ChangePasswordDTO> changePasswordValidator,
        IValidator<UpdateProfileDTO> updateProfileValidator,
        RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _changePasswordValidator = changePasswordValidator;
        _updateProfileValidator = updateProfileValidator;
        _roleManager=roleManager;
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDTO model)
    {
        var validationResult = await _updateProfileValidator.ValidateAsync(model);
        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                message = "Validasi data gagal",
                errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList()
            });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _userManager.FindByIdAsync(userId!);
        if (user == null) return Unauthorized();

        user.FullName = model.FullName;
        user.Email = model.Email;
        user.UserName = model.Email;

        var result = await _userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            // 4. Ambil Roles dan Permissions Terbaru
            // Ini sangat penting agar Sidebar & Header tidak rusak datanya
            var roles = await _userManager.GetRolesAsync(user);
            var permissions = new List<string>();

            foreach (var roleName in roles)
            {
                var role = await _roleManager.FindByNameAsync(roleName);
                if (role != null)
                {
                    var roleClaims = await _roleManager.GetClaimsAsync(role);
                    // Ambil nilai claim untuk daftar permission
                    foreach (var claim in roleClaims)
                    {
                        permissions.Add(claim.Value);
                    }
                }
            }

            return Ok(new
            {
                message = "Profile Berhasil Diperbarui",
                email = user.Email,
                fullName = user.FullName,
                roles = roles,
                permissions = permissions.Distinct().ToList() // Hapus duplikat permission
            });
        }

        return BadRequest(new { message = "Gagal memperbarui profil", errors = result.Errors.Select(e => e.Description) });
    }

    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO model)
    {
        var validationResult = await _changePasswordValidator.ValidateAsync(model);

        if (!validationResult.IsValid)
        {
            var errorMessages = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(new
            {
                message = "Validasi data gagal",
                errors = errorMessages
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
}