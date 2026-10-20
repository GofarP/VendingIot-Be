using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VendingIot.Data;
using VendingIot.Extensions;
using VendingIot.Models;

namespace VendingIot.Controllers;

[Authorize]
public class ItemController : BaseApiController
{
    private readonly ApplicationDbContext _context;
    private readonly IValidator<Item> _validator;

    public ItemController(ApplicationDbContext context, IValidator<Item> validator)
    {
        _context = context;
        _validator = validator;
    }

    [HttpGet]
    public async Task<IActionResult> GetItem([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var query = _context.Items
            .Include(i => i.ItemCategory)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(i => i.Name.Contains(search) ||
                                     (i.ItemCategory != null && i.ItemCategory.Name.Contains(search)));
        }

        var result = await query
            .OrderByDescending(i => i.Id)
            .Select(i => new ItemResponseDto
            {
                Id = i.Id,
                Name = i.Name,
                Price = i.Price,
                Quantity = i.Quantity,
                ItemCategoryId = i.ItemCategoryId,
                ItemCategoryName = i.ItemCategory != null ? i.ItemCategory.Name : "-"
            })
            .ToPagedResponseAsync(page, pageSize, success: true);

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Item>> GetItem(int id)
    {
        var item = await _context.Items.FindAsync(id);
        if (item == null)
        {
            return NotFound(new { message = $"Item with ID {id} not found." });
        }

        return Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> CreateItem(Item item)
    {
        var validationResult = await _validator.ValidateAsync(item);
        if (!validationResult.IsValid)
        {
            return ValidationBadRequest(validationResult);
        }

        _context.Items.Add(item);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetItem), new { id = item.Id }, new
        {
            message = "Berhasil Membuat item baru",
            data = item
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateItem(int id, Item item)
    {
        if (id != item.Id)
        {
            return BadRequest(new { message = "ID di URL dan data tidak cocok." });
        }

        var validationResult = await _validator.ValidateAsync(item);
        if (!validationResult.IsValid)
        {
            return ValidationBadRequest(validationResult);
        }

        if (!await _context.Items.AnyAsync(e => e.Id == id))
        {
            return NotFound(new { message = "Data sudah tidak ada di database." });
        }

        _context.Entry(item).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Data Berhasil Diperbarui.", data = item });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteItem(int id)
    {
        var item = await _context.Items.FindAsync(id);
        if (item == null)
        {
            return NotFound(new { message = "Data tidak ditemukan." });
        }

        _context.Items.Remove(item);
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Item {item.Name} berhasil dihapus." });
    }
}