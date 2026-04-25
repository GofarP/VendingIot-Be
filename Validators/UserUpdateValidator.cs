using FluentValidation;
using Microsoft.AspNetCore.Identity;

namespace VendingIot.Models.DTO;
public class UserUpdateValidator : AbstractValidator<UserUpdateDTO>
{
    public UserUpdateValidator(UserManager<ApplicationUser> userManager, string userId)
    {
        RuleFor(x => x.FullName).NotEmpty().WithMessage("Nama wajib diisi.");
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email wajib diisi.")
            .MustAsync(async (email, token) => {
                var user = await userManager.FindByEmailAsync(email);
                return user == null || user.Id == userId; // Boleh jika milik sendiri
            }).WithMessage("Email sudah digunakan user lain.");
            
        RuleFor(x => x.Password).MinimumLength(6).When(x => !string.IsNullOrEmpty(x.Password));
        RuleFor(x => x.PhotoFile).Must(ValidatePhoto).WithMessage("Format foto harus JPG/PNG dan maksimal 2MB.");
    }

    private bool ValidatePhoto(IFormFile? file) => file == null || (file.Length <= 2 * 1024 * 1024 && new[] { ".jpg", ".png" }.Contains(Path.GetExtension(file.FileName).ToLower()));
}