using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using FluentValidation;
using VendingIot.Data;
using VendingIot.Models;

namespace VendingIot.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VendingMachineController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IValidator<VendingMachine> _validator;

    public VendingMachineController(ApplicationDbContext context, IValidator<VendingMachine> validator)
    {
        _context = context;
        _validator = validator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 10 : pageSize;

            var totalCount = await _context.VendingMachines.CountAsync();
            var data = await _context.VendingMachines
                .OrderByDescending(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                data,
                pagination = new
                {
                    totalCount,
                    pageSize,
                    currentPage = page,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Internal Server Error", error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var machine = await _context.VendingMachines.FindAsync(id);
        if (machine == null) return NotFound(new { message = "Mesin tidak ditemukan." });
        return Ok(machine);
    }

    [HttpPost]
    public async Task<IActionResult> Create(VendingMachine machine)
    {
        var validationResult = await _validator.ValidateAsync(machine);
        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                message = "Validation Failed",
                errors = validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            });
        }

        try
        {
            _context.VendingMachines.Add(machine);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = machine.Id }, new { message = "Mesin berhasil didaftarkan.", data = machine });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Gagal menyimpan data.", error = ex.Message });
        }
    }

    // 4. PUT (Update)
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, VendingMachine machine)
    {
        if (id != machine.Id) return BadRequest(new { message = "ID tidak cocok." });

        var validationResult = await _validator.ValidateAsync(machine);
        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                message = "Validation Failed",
                errors = validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
            });
        }

        _context.Entry(machine).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
            return Ok(new { message = "Data mesin berhasil diperbarui.", data = machine });
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.VendingMachines.Any(e => e.Id == id)) return NotFound();
            throw;
        }
    }

    // 5. DELETE
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var machine = await _context.VendingMachines.FindAsync(id);
        if (machine == null) return NotFound(new { message = "Data tidak ditemukan." });

        _context.VendingMachines.Remove(machine);
        await _context.SaveChangesAsync();
        return Ok(new { message = $"Mesin {machine.MachineCode} berhasil dihapus." });
    }


    [HttpPost("{id}/heartbeat")]
    public async Task<IActionResult> Heartbeat(int id)
    {
        var machine = await _context.VendingMachines.FindAsync(id);
        if (machine == null) return NotFound();

        machine.LastRestock = DateTime.Now;
        await _context.SaveChangesAsync();

        return Ok(new { status = "Online", timestamp = machine.LastRestock });
    }
}