using FluentValidation;
using Microsoft.EntityFrameworkCore;
using VendingIot.Data;
using VendingIot.Models;
namespace VendingIot.Validators;

public class ItemCategoryValidator : AbstractValidator<Department>
{
    private readonly ApplicationDbContext _context;

    public ItemCategoryValidator(ApplicationDbContext context)
    {
        _context = context;
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nama Item Category wajib diisi.")
        .MaximumLength(30).WithMessage("Nama Item Category maksimal 30 karakter.")
        .MustAsync(BeUniqueName).WithMessage("Nama Item Category '{PropertyValue}' sudah ada.");


        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Deskripsi wajib diisi.")
        .MaximumLength(100).WithMessage("Deskripsi maksimal 100 karakter.");

    }


    private async Task<bool> BeUniqueName(Department model, string name, CancellationToken token)
    {
        var exists = await _context.Departments
        .AnyAsync(d => d.Name == name && d.Id != model.Id, token);

        return !exists;
    }

    


}