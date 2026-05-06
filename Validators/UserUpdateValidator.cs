using FluentValidation;
using Microsoft.AspNetCore.Identity;

namespace VendingIot.Models.DTO;

public class UserUpdateValidator : AbstractValidator<UserUpdateDTO>
{
    private readonly RoleManager<IdentityRole> _roleManager;

    public UserUpdateValidator(
        UserManager<ApplicationUser> userManager, 
        RoleManager<IdentityRole> roleManager) // Inject RoleManager di sini
    {
        _roleManager = roleManager;

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Nama wajib diisi.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email wajib diisi.")
            .EmailAddress().WithMessage("Format email salah.")
            .MustAsync(async (model, email, token) =>
            {
                var user = await userManager.FindByEmailAsync(email);
                // Validasi agar email unik, kecuali untuk user itu sendiri
                return user == null || user.Id == model.Id;
            })
            .WithMessage("Email sudah digunakan oleh user lain.");

        // --- TAMBAHKAN VALIDASI ROLE DI SINI ---
        RuleFor(x => x.RoleName)
            .NotEmpty().WithMessage("Role wajib dipilih.")
            .MustAsync(async (roleName, token) => 
            {
                return await _roleManager.RoleExistsAsync(roleName);
            })
            .WithMessage("Role '{PropertyValue}' tidak ditemukan di sistem.");

        RuleFor(x => x.Password)
            .MinimumLength(6)
            .When(x => !string.IsNullOrEmpty(x.Password))
            .WithMessage("Password minimal harus 6 karakter.");

        RuleFor(x => x.PhotoFile)
            .Must(ValidatePhoto)
            .WithMessage("Format foto harus JPG/PNG dan maksimal 2MB.");
    }

    private bool ValidatePhoto(IFormFile? file) => 
        file == null || (file.Length <= 2 * 1024 * 1024 && 
        new[] { ".jpg", ".png", ".jpeg" }.Contains(Path.GetExtension(file.FileName).ToLower()));
}