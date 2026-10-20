using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VendingIot.Data;
using VendingIot.Extensions;
using VendingIot.Models;

namespace VendingIot.Controllers;

[Authorize]
public class VendingItemController : BaseApiController
{
    private readonly ApplicationDbContext _context;
    private readonly IValidator<VendingItem> _validator;

    public VendingItemController(ApplicationDbContext context, IValidator<VendingItem> validator)
    {
        _context = context;
        _validator = validator;
    }

    [HttpGet("vendingwithstock")]
    public async Task<IActionResult> GetVendingMachineWithStock(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        var query = _context.VendingMachines.AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(v => v.Name.Contains(search) ||
                                     v.MachineCode.Contains(search) ||
                                     v.Location.Contains(search));
        }

        var result = await query
            .OrderByDescending(v => v.Id)
            .Select(v => new
            {
                v.Id,
                v.Name,
                v.MachineCode,
                v.Location,
                TotalItemTypes = v.VendingItems.Count(),
                TotalStock = v.VendingItems.Sum(vi => vi.Quantity),
                TotalCategories = v.VendingItems
                    .Where(vi => vi.Item != null)
                    .Select(vi => vi.Item!.ItemCategoryId)
                    .Distinct()
                    .Count()
            })
            .ToPagedResponseAsync(page, pageSize, message: "Success retrieve vending machine statistics");

        return Ok(result);
    }

    [HttpGet("getitembymachine/{machineId}")]
    public async Task<IActionResult> GetItemsByMachine(
        int machineId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        var query = _context.VendingItems
            .AsNoTracking()
            .Where(v => v.VendingMachineId == machineId);

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(v => (v.Item != null && v.Item.Name.Contains(search)) ||
                                     (v.Item != null && v.Item.ItemCategory != null && v.Item.ItemCategory.Name.Contains(search)));
        }

        var result = await query
            .OrderBy(v => v.Id)
            .Select(v => new
            {
                v.Id,
                v.VendingMachineId,
                v.Quantity,
                v.Capacity,
                Price = v.Item != null ? v.Item.Price : 0,
                ItemName = v.Item != null ? v.Item.Name : "-",
                CategoryName = (v.Item != null && v.Item.ItemCategory != null) ? v.Item.ItemCategory.Name : "-"
            })
            .ToPagedResponseAsync(page, pageSize, message: "Success retrieve machine items");

        return Ok(result);
    }

    [HttpPost("assignitemtomachine")]
    public async Task<IActionResult> AssignItemToMachine(VendingItem vendingItem)
    {
        var validationResult = await _validator.ValidateAsync(vendingItem);
        if (!validationResult.IsValid)
        {
            return ValidationBadRequest(validationResult);
        }

        var exists = await _context.VendingItems
            .AnyAsync(v => v.VendingMachineId == vendingItem.VendingMachineId && v.ItemId == vendingItem.ItemId);

        if (exists)
        {
            return BadRequest(new { message = "This item is registered" });
        }

        _context.VendingItems.Add(vendingItem);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Item successfully added to machine", data = vendingItem });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> RemoveItemFromMachine(int id)
    {
        var vendingItem = await _context.VendingItems.FindAsync(id);
        if (vendingItem == null)
        {
            return NotFound(new { message = "Item not found in this machine" });
        }

        _context.VendingItems.Remove(vendingItem);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Item successfully removed from machine" });
    }

    [HttpPut("{id}/restock")]
    public async Task<IActionResult> Restock(int id, [FromBody] RestockRequestDto request)
    {
        var item = await _context.VendingItems
            .Include(v => v.VendingMachine)
            .FirstOrDefaultAsync(v => v.Id == id);

        if (item == null)
        {
            return NotFound(new { message = "Item is not found." });
        }

        item.Quantity = request.Quantity;
        item.Capacity = request.Capacity;
        item.LastUpdated = DateTime.Now;

        if (item.VendingMachine != null)
        {
            item.VendingMachine.LastRestock = DateTime.Now;
        }

        var validationResult = await _validator.ValidateAsync(item);
        if (!validationResult.IsValid)
        {
            return ValidationBadRequest(validationResult);
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Stock berhasil diperbarui.",
            currentQuantity = item.Quantity,
            currentStock = item.Capacity,
            lastUpdated = item.LastUpdated
        });
    }
}
