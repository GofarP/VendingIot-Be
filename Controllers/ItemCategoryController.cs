using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VendingIot.Data;
using VendingIot.Extensions;
using VendingIot.Models;

namespace VendingIot.Controllers;

[Authorize]
public class ItemCategoryController : BaseApiController
{
    private readonly ApplicationDbContext _context;
    private readonly IValidator<ItemCategory> _validator;

    public ItemCategoryController(ApplicationDbContext context, IValidator<ItemCategory> validator)
    {
        _context = context;
        _validator = validator;
    }

    [HttpGet]
    public async Task<IActionResult> GetItemCategory([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var query = _context.ItemCategories.AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(x => x.Name.Contains(search));
        }

        var result = await query
            .OrderByDescending(x => x.Id)
            .ToPagedResponseAsync(page, pageSize);

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ItemCategory>> GetItemCategory(int id)
    {
        var itemCategory = await _context.ItemCategories.FindAsync(id);
        if (itemCategory == null)
        {
            return NotFound(new { message = $"ItemCategory with ID {id} not found." });
        }

        return Ok(itemCategory);
    }

    [HttpPost]
    public async Task<IActionResult> CreateItemCategory(ItemCategory itemCategory)
    {
        var validationResult = await _validator.ValidateAsync(itemCategory);
        if (!validationResult.IsValid)
        {
            return ValidationBadRequest(validationResult);
        }

        _context.ItemCategories.Add(itemCategory);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetItemCategory), new { id = itemCategory.Id }, new
        {
            message = "Berhasil Membuat itemcategory baru",
            data = itemCategory
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateItemCategory(int id, ItemCategory itemCategory)
    {
        if (id != itemCategory.Id)
        {
            return BadRequest(new { message = "ID di URL dan data tidak cocok." });
        }

        var validationResult = await _validator.ValidateAsync(itemCategory);
        if (!validationResult.IsValid)
        {
            return ValidationBadRequest(validationResult);
        }

        if (!await _context.ItemCategories.AnyAsync(e => e.Id == id))
        {
            return NotFound(new { message = "Data sudah tidak ada di database." });
        }

        _context.Entry(itemCategory).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Data Berhasil Diperbarui.", data = itemCategory });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteItemCategory(int id)
    {
        var itemCategory = await _context.ItemCategories.FindAsync(id);
        if (itemCategory == null)
        {
            return NotFound(new { message = "Data tidak ditemukan." });
        }

        _context.ItemCategories.Remove(itemCategory);
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Item Category {itemCategory.Name} berhasil dihapus." });
    }
}