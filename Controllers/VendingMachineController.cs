using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VendingIot.Data;
using VendingIot.Extensions;
using VendingIot.Models;

namespace VendingIot.Controllers;

[Authorize]
public class VendingMachineController : BaseApiController
{
    private readonly ApplicationDbContext _context;
    private readonly IValidator<VendingMachine> _validator;

    public VendingMachineController(ApplicationDbContext context, IValidator<VendingMachine> validator)
    {
        _context = context;
        _validator = validator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var query = _context.VendingMachines.AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(x => x.Name.Contains(search) ||
                                     x.MachineCode.Contains(search) ||
                                     x.Location.Contains(search));
        }

        var result = await query
            .OrderByDescending(x => x.Id)
            .ToPagedResponseAsync(page, pageSize);

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var machine = await _context.VendingMachines.FindAsync(id);
        if (machine == null)
        {
            return NotFound(new { message = "Mesin tidak ditemukan." });
        }

        return Ok(machine);
    }

    [HttpPost]
    public async Task<IActionResult> Create(VendingMachine machine)
    {
        var validationResult = await _validator.ValidateAsync(machine);
        if (!validationResult.IsValid)
        {
            return ValidationBadRequest(validationResult);
        }

        _context.VendingMachines.Add(machine);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = machine.Id }, new
        {
            message = "Mesin berhasil didaftarkan.",
            data = machine
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, VendingMachine machine)
    {
        if (id != machine.Id)
        {
            return BadRequest(new { message = "ID tidak cocok." });
        }

        var validationResult = await _validator.ValidateAsync(machine);
        if (!validationResult.IsValid)
        {
            return ValidationBadRequest(validationResult);
        }

        if (!await _context.VendingMachines.AnyAsync(e => e.Id == id))
        {
            return NotFound();
        }

        _context.Entry(machine).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Data mesin berhasil diperbarui.", data = machine });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var machine = await _context.VendingMachines.FindAsync(id);
        if (machine == null)
        {
            return NotFound(new { message = "Data tidak ditemukan." });
        }

        _context.VendingMachines.Remove(machine);
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Mesin {machine.MachineCode} berhasil dihapus." });
    }

    [HttpPost("{id}/heartbeat")]
    public async Task<IActionResult> Heartbeat(int id)
    {
        var machine = await _context.VendingMachines.FindAsync(id);
        if (machine == null)
        {
            return NotFound();
        }

        machine.LastRestock = DateTime.Now;
        await _context.SaveChangesAsync();

        return Ok(new { status = "Online", timestamp = machine.LastRestock });
    }
}