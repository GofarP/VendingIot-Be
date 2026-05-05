using FluentValidation;
using Microsoft.AspNetCore.Identity;

namespace VendingIot.Validators;

public class RoleValidator : AbstractValidator<RoleCreateDto>
{
    private readonly RoleManager<IdentityRole> _roleManager;

    public RoleValidator(RoleManager<IdentityRole> roleManager)
    {
        _roleManager = roleManager;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nama role wajib diisi.")
            .MaximumLength(30).WithMessage("Nama role maksimal 30 karakter.")
            .MustAsync(BeUniqueName).WithMessage("Nama role '{PropertyValue}' sudah digunakan.");
    }

    private async Task<bool> BeUniqueName(RoleCreateDto dto, string name, CancellationToken token)
    {
        var role = await _roleManager.FindByNameAsync(name);
        
        // Benar jika nama belum ada, ATAU nama ada tapi milik ID yang sedang diedit
        return role == null || role.Id == dto.Id;
    }
}