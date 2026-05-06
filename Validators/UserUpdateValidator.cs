using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace VendingIot.Models.DTO;

public class UserUpdateValidator : AbstractValidator<UserUpdateDTO>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UserUpdateValidator(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager) // Inject RoleManager di sini
    {
        _userManager=userManager;
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

        RuleFor(x => x.RoleId)
        .NotEmpty().WithMessage("Role wajib dipilih.")
        .MustAsync(async (roleId, token) =>
        {
            // Cek apakah Role dengan ID tersebut ada
            return await roleManager.Roles.AnyAsync(r => r.Id == roleId);
        })
        .WithMessage("Role yang dipilih tidak valid.");

        RuleFor(x => x.Password)
            .MinimumLength(6)
            .When(x => !string.IsNullOrEmpty(x.Password))
            .WithMessage("Password minimal harus 6 karakter.");

        RuleFor(x => x.Photo)
            .Must(ValidatePhoto)
            .WithMessage("Format foto harus JPG/PNG dan maksimal 2MB.");
    }

    private bool ValidatePhoto(IFormFile? file) =>
        file == null || (file.Length <= 2 * 1024 * 1024 &&
        new[] { ".jpg", ".png", ".jpeg" }.Contains(Path.GetExtension(file.FileName).ToLower()));
}