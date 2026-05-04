using FluentValidation;
using Microsoft.EntityFrameworkCore;
using VendingIot.Data;
using VendingIot.Models;

namespace VendingIot.Validators;

public class RoleValidator : AbstractValidator<Role>
{
    private readonly ApplicationDbContext _context;

    public RoleValidator(ApplicationDbContext context)
    {
        _context = context;

        RuleFor(x => x.Name)
                    .NotEmpty().WithMessage("Nama role wajib diisi.")
            .MaximumLength(30).WithMessage("Nama role maksimal 30 karakter.")
            .MustAsync(BeUniqueName).WithMessage("Nama role '{PropertyValue}' sudah ada.");

    }

    private async Task<bool> BeUniqueName(Role model, string name, CancellationToken token)
    {
        var exists = await _context.Roles
           .AnyAsync(r => r.Name == name && r.Id != model.Id, token);

        return !exists;
    }
}