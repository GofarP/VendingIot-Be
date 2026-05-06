using System.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using VendingIot.Data;
using VendingIot.Models;

namespace VendingIot.Validators;

public class ItemValidator : AbstractValidator<Item>
{
    private readonly ApplicationDbContext _context;

    public ItemValidator(ApplicationDbContext context)
    {
        _context = context;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Please fill item name")
            .MustAsync(BeUniqueName).WithMessage("This item name '{PropertyValue}' already exists");

        RuleFor(x => x.Price)
        .NotEmpty().WithMessage("Please fill item price");

        RuleFor(x=>x.Quantity)
        .NotEmpty().WithMessage("Please fill quantity");

        RuleFor(x => x.ItemCategoryId)
            .GreaterThan(0).WithMessage("Please select a valid item category")
            .MustAsync(ItemExists).WithMessage("The selected category ID '{PropertyValue}' does not exist in the database");

    }

    private async Task<bool> BeUniqueName(Item model, string name, CancellationToken token)
    {
        if (string.IsNullOrEmpty(name)) return true;

        var exists = await _context.Items
            .AnyAsync(p => p.Name == name && p.Id != model.Id, token);

        return !exists;
    }


    private async Task<bool> ItemExists(int itemId, CancellationToken token)
    {
        if (itemId <= 0) return true;

        var exists = await _context.ItemCategories
        .AnyAsync(c => c.Id == itemId, token);

        return exists;

    }




}