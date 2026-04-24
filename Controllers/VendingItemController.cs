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

    [HttpGet("machine/{machineId}")]
    public async Task<IActionResult> GetItemsByMachine(int machineId)
    {
        var stocks = await _context.VendingItems
            .Include(v => v.Item)
            .Where(v => v.VendingMachineId == machineId)
            .ToListAsync();

        return Ok(stocks);
    }

    [HttpPost]
    public async Task<IActionResult> AssignItemToMachine(VendingItem vendingItem)
    {
        var validationResult = await _validator.ValidateAsync(vendingItem);
        if (!validationResult.IsValid)
        {
            return BadRequest(new { errors = validationResult.ToDictionary() });
        }

        // Cek apakah item sudah pernah di-assign ke mesin ini sebelumnya
        var exists = await _context.VendingItems
            .AnyAsync(v => v.VendingMachineId == vendingItem.VendingMachineId && v.ItemId == vendingItem.ItemId);
        
        if (exists) return BadRequest(new { message = "Item ini sudah terdaftar di mesin tersebut." });

        _context.VendingItems.Add(vendingItem);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Item berhasil ditambahkan ke mesin.", data = vendingItem });
    }

    [HttpPut("{id}/restock")]
    public async Task<IActionResult> Restock(int id, [FromBody] int newQuantity)
    {
        var stock = await _context.VendingItems.FindAsync(id);
        if (stock == null) return NotFound();

        stock.Quantity = newQuantity;
        stock.LastUpdated = DateTime.Now;

        var validationResult = await _validator.ValidateAsync(stock);
        if (!validationResult.IsValid) return BadRequest(new { errors = validationResult.ToDictionary() });

        await _context.SaveChangesAsync();
        return Ok(new { message = "Stok berhasil diperbarui.", currentQuantity = stock.Quantity });
    }
}