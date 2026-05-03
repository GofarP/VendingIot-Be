
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using VendingIot.Data;
using VendingIot.Models;

namespace VendingIot.Validators;

public class VendingMachineValidator : AbstractValidator<VendingMachine>
{
    private readonly ApplicationDbContext _context;

    public VendingMachineValidator(ApplicationDbContext context)
    {
        _context = context;

        RuleFor(x => x.MachineCode)
            .NotEmpty().WithMessage("Kode mesin wajib diisi.")
            .MaximumLength(20).WithMessage("Kode mesin maksimal 20 karakter.")
            .MustAsync(BeUniqueCode).WithMessage("Kode mesin '{PropertyValue}' sudah terdaftar.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nama mesin wajib diisi.")
            .MaximumLength(100).WithMessage("Nama mesin maksimal 100 karakter.");

        RuleFor(x => x.Location)
            .NotEmpty().WithMessage("Lokasi mesin wajib diisi.")
            .MaximumLength(200).WithMessage("Keterangan lokasi maksimal 200 karakter.");
    }

    private async Task<bool> BeUniqueCode(VendingMachine model, string code, CancellationToken token)
    {
        if (string.IsNullOrEmpty(code)) return true;

        var exists = await _context.VendingMachines
            .AnyAsync(vm => vm.MachineCode == code && vm.Id != model.Id, token);

        return !exists;
    }
}