using FluentValidation;
using Microsoft.AspNetCore.Identity;

namespace VendingIot.Models.DTO;

public class UserUpdateValidator : AbstractValidator<UserUpdateDTO> // Pastikan nama DTO konsisten
{
    public UserUpdateValidator(UserManager<ApplicationUser> userManager) // HAPUS string userId di sini!
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Nama wajib diisi.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email wajib diisi.")
            .EmailAddress().WithMessage("Format email salah.")
            .MustAsync(async (model, email, token) =>
            {
                var user = await userManager.FindByEmailAsync(email);
                
                // Di sini kita pakai model.Id (ID yang ada di DTO), bukan dari constructor
                return user == null || user.Id == model.Id;
            })
            .WithMessage("Email sudah digunakan oleh user lain.");

        RuleFor(x => x.Password)
            .MinimumLength(6)
            .When(x => !string.IsNullOrEmpty(x.Password));

        RuleFor(x => x.PhotoFile)
            .Must(ValidatePhoto)
            .WithMessage("Format foto harus JPG/PNG dan maksimal 2MB.");
    }

    private bool ValidatePhoto(IFormFile? file) => 
        file == null || (file.Length <= 2 * 1024 * 1024 && new[] { ".jpg", ".png", ".jpeg" }.Contains(Path.GetExtension(file.FileName).ToLower()));
}