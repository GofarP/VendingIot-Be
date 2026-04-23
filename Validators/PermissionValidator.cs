using FluentValidation;
using Microsoft.EntityFrameworkCore;
using VendingIot.Data;
using VendingIot.Models;

namespace VendingIot.Validators;

public class PermissionValidator : AbstractValidator<Permission>
{
    private readonly ApplicationDbContext _context;

    public PermissionValidator(ApplicationDbContext context)
    {
        _context = context;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Please fill permission name")
            .MustAsync(BeUniqueName).WithMessage("This permission name '{PropertyValue}' already exists");

        RuleFor(x => x.PermissionCategoryId)
            .GreaterThan(0).WithMessage("Please select a valid permission category")
            .MustAsync(CategoryExists).WithMessage("The selected category ID '{PropertyValue}' does not exist in the database");
    }

    private async Task<bool> BeUniqueName(Permission model, string name, CancellationToken token)
    {
        if (string.IsNullOrEmpty(name)) return true;

        var exists = await _context.Permissions
            .AnyAsync(p => p.Name == name && p.Id != model.Id, token);

        return !exists;
    }

    // Fungsi Cek Ketersediaan Kategori (isExist)
    private async Task<bool> CategoryExists(int categoryId, CancellationToken token)
    {
        // Jika ID kurang dari 1, biarkan aturan GreaterThan(0) yang menangani error-nya
        if (categoryId <= 0) return true; 

        // Mengecek ke tabel PermissionCategories, apakah ada ID yang cocok?
        var exists = await _context.PermissionCategories
            .AnyAsync(c => c.Id == categoryId, token);

        // Berbeda dengan BeUnique (yang me-return !exists), 
        // di sini kita me-return exists karena kita MEMANG INGIN data tersebut ada.
        return exists;
    }
}