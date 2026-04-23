using FluentValidation;
using Microsoft.EntityFrameworkCore;
using VendingIot.Data;
using VendingIot.Models;

namespace VendingIot.Validators;

public class PermissionCategoryValidator : AbstractValidator<PermissionCategory>
{
    private readonly ApplicationDbContext _context;

    public PermissionCategoryValidator(ApplicationDbContext context)
    {
        _context = context;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Please fill permission category name")
            .MustAsync(BeUniqueName).WithMessage("This permission category name already exist");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Please fill permission category description");
    }

    private async Task<bool> BeUniqueName(PermissionCategory model, string name, CancellationToken token)
    {
        if (string.IsNullOrEmpty(name)) return true;

        var exists = await _context.PermissionCategories
            .AnyAsync(c => c.Name == name && c.Id != model.Id, token);

        return !exists;
    }
}