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

    [HttpGet("vendingwithstock")]
    public async Task<IActionResult> GetVendingMachineWithStock([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        try
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 10 : pageSize;

            var query = _context.VendingMachines.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(v => v.Name.Contains(search) ||
                                       v.MachineCode.Contains(search) ||
                                       v.Location.Contains(search));
            }

            var totalCount = await query.CountAsync();

            var data = await query
                .OrderByDescending(v => v.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(v => new
                {
                    v.Id,
                    v.Name,
                    v.MachineCode,
                    v.Location,
                    TotalItemTypes = v.VendingItems.Count(),
                    TotalStock = v.VendingItems.Sum(vi => vi.Quantity),
                    TotalCategories = v.VendingItems
                        .Select(vi => vi.Item.ItemCategoryId)
                        .Distinct()
                        .Count()
                })
                .ToListAsync();

            return Ok(new
            {
                message = "Success retrieve vending machine statistics",
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

    [HttpGet("getitembymachine/{machineId}")]
    public async Task<IActionResult> GetItemsByMachine(
     int machineId,
     [FromQuery] int page = 1,
     [FromQuery] int pageSize = 10,
     [FromQuery] string? search = null)
    {
        try
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 10 : pageSize;

            var query = _context.VendingItems
                .AsNoTracking()
                .Where(v => v.VendingMachineId == machineId);

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(v => v.Item.Name.Contains(search) ||
                                       v.Item.ItemCategory.Name.Contains(search));
            }

            var totalCount = await query.CountAsync();

            var stocks = await query
                .OrderBy(v => v.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(v => new
                {
                    v.Id,
                    v.VendingMachineId,
                    v.Quantity,
                    v.Capacity,
                    Price = v.Item.Price,
                    ItemName = v.Item.Name,
                    CategoryName = v.Item.ItemCategory.Name
                })
                .ToListAsync();

            return Ok(new
            {
                message = "Success retrieve machine items",
                data = stocks,
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

    [HttpPost("assignitemtomachine")]
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

        if (item == null) return NotFound(new { message = "Item is not found." });

        // Update field dari DTO
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
                currentQuantity = item.Quantity,
                currentStock = item.Capacity,
                lastUpdated = item.LastUpdated
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Gagal memperbarui stok.", error = ex.Message });
        }
    }


}
