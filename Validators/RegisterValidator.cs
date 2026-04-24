using FluentValidation;
using Microsoft.AspNetCore.Identity;
using VendingIot.Models;
using VendingIot.Models.DTO;

public class RegisterValidator : AbstractValidator<RegisterDTO>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public RegisterValidator(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Nama lengkap wajib diisi.")
            .MaximumLength(100).WithMessage("Nama lengkap maksimal 100 karakter.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email wajib diisi.")
            .EmailAddress().WithMessage("Format email tidak valid.")
            .MustAsync(async (email, token) =>
            {
                var user = await _userManager.FindByEmailAsync(email);
                return user == null;
            }).WithMessage("Email '{PropertyValue}' sudah terdaftar, gunakan email lain.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password wajib diisi.")
            .MinimumLength(6).WithMessage("Password minimal 6 karakter.");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Konfirmasi password wajib diisi.")
            .Equal(x => x.Password).WithMessage("Password dan Konfirmasi Password tidak cocok.");
    }
}