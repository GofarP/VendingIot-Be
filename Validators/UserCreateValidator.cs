using FluentValidation;
using Microsoft.AspNetCore.Identity;
using VendingIot.Models;
using VendingIot.Models.DTO;

namespace VendingIot.Validators;

public class UserCreateValidator : AbstractValidator<UserCreateDto>
{
    private readonly RoleManager<IdentityRole> _roleManager;
    public UserCreateValidator(UserManager<ApplicationUser> userManager)
    {
        RuleFor(x => x.FullName).NotEmpty().WithMessage("Nama lengkap wajib diisi.");
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email wajib diisi.")
            .EmailAddress().WithMessage("Format email tidak valid.")
            .MustAsync(async (email, token) => await userManager.FindByEmailAsync(email) == null)
            .WithMessage("Email sudah terdaftar.");
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6).WithMessage("Password minimal 6 karakter.");

        RuleFor(x => x.PhotoFile).Must(ValidatePhoto).WithMessage("Format foto harus JPG/PNG dan maksimal 2MB.");

        RuleFor(x => x.RoleName)
            .NotEmpty().WithMessage("Role wajib dipilih.")
            .MustAsync(RoleExists).WithMessage("Role '{PropertyValue}' tidak ditemukan di sistem.");
    }

    private bool ValidatePhoto(IFormFile? file)
    {
        if (file == null) return true;
        var extension = Path.GetExtension(file.FileName).ToLower();
        return (file.Length <= 2 * 1024 * 1024) && (new[] { ".jpg", ".jpeg", ".png" }.Contains(extension));
    }
    private async Task<bool> RoleExists(string roleName, CancellationToken token)
    {
        return await _roleManager.RoleExistsAsync(roleName);
    }
}

