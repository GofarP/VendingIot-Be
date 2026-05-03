using FluentValidation;
using Microsoft.EntityFrameworkCore;
using VendingIot.Data;
using VendingIot.Models;

namespace VendingIot.Validators;

public class VendingItemValidator : AbstractValidator<VendingItem>
{
    private readonly ApplicationDbContext _context;

    public VendingItemValidator(ApplicationDbContext context)
    {
        _context = context;

        RuleFor(x => x.VendingMachineId)
            .GreaterThan(0).WithMessage("Pilih mesin vending yang valid.")
            .MustAsync(async (id, token) => await _context.VendingMachines.AnyAsync(vm => vm.Id == id, token))
            .WithMessage("Mesin vending tidak ditemukan di database.");

        RuleFor(x => x.ItemId)
            .GreaterThan(0).WithMessage("Pilih item yang valid.")
            .MustAsync(async (id, token) => await _context.Items.AnyAsync(i => i.Id == id, token))
            .WithMessage("Item tidak ditemukan di database.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Kapasitas minimal adalah 1.");

        RuleFor(x => x.Capacity)
            .GreaterThan(0).WithMessage("Kapasitas minimal adalah 1.");

        RuleFor(x => x.Quantity)
            .InclusiveBetween(0, 1000).WithMessage("Jumlah stok harus di antara 0 dan 1000.")
            .Must((model, qty) => qty <= model.Capacity)
            .WithMessage("Jumlah stok tidak boleh melebihi kapasitas mesin.");
    }
}