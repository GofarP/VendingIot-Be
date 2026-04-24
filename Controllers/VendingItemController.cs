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

public class VendingItemController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IValidator<VendingItem> _validator;


    public VendingItemController(ApplicationDbContext context, IValidator<VendingItem> validator)
    {
        _context = context;
        _validator = validator;
    }


    [HttpGet("machine/{machinId}")]
    public async Task<IActionResult> GetItemsByMachine(int machineId)
    {
        var stocks = await _context.VendingItems
        .Include(v => v.Item)
            .ThenInclude(i => i.ItemCategory) // Kita ikutkan kategori barangnya
        .Where(v => v.VendingMachineId == machineId)
        .ToListAsync();

        return Ok(new { data = stocks });
    }

    [HttpPost]
    public async Task<IActionResult> AssignItemToMachine(VendingItem vendingItem)
    {
        var validationResult = await _validator.ValidateAsync(vendingItem);

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

        var exists = await _context.VendingItems
        .AnyAsync(v => v.VendingMachineId == vendingItem.VendingMachineId && v.ItemId == vendingItem.ItemId);

        if (exists) return BadRequest(new { message = "This item is registered" });

        _context.VendingItems.Add(vendingItem);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Item successfully added to machine", data = vendingItem });

    }

    [HttpPut("{id}/restock")]
    public async Task<IActionResult> Restock(int id, [FromBody] int newQuantity)
    {
        var stock = await _context.VendingItems
            .Include(v => v.VendingMachine)
            .FirstOrDefaultAsync(v => v.Id == id);

        if (stock == null) return NotFound(new { message = "Stock is not found." });

        stock.Quantity = newQuantity;
        stock.LastUpdated = DateTime.Now;

        if (stock.VendingMachine != null)
        {
            stock.VendingMachine.LastRestock = DateTime.Now;
        }

        var validationResult = await _validator.ValidateAsync(stock);

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
            await _context.SaveChangesAsync();
            return Ok(new
            {
                message = "Stock berhasil diperbarui.",
                currentQuantity = stock.Quantity,
                lastUpdated = stock.LastUpdated
            });
        }

        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Gagal memperbarui stok.", error = ex.Message });
        }

    }


}
