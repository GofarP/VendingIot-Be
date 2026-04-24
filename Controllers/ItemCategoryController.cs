using VendingIot.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VendingIot.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using FluentValidation;

namespace VendingIot.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ItemCategoryController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IValidator<ItemCategory> _validator;

        public ItemCategoryController(ApplicationDbContext context, IValidator<ItemCategory> validator)
        {
            _context = context;
            _validator = validator;
        }


        [HttpGet]
        public async Task<IActionResult> GetItemCategory([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                page = page < 1 ? 1 : page;
                pageSize = pageSize < 1 ? 10 : pageSize;

                var totalCount = await _context.ItemCategories.CountAsync();
                var ItemCategories = await _context.ItemCategories
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

                return Ok(new
                {
                    data = ItemCategories,
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
        public async Task<ActionResult<ItemCategory>> GetItemCategory(int id)
        {
            var itemCategory = await _context.ItemCategories.FindAsync(id);

            if (itemCategory == null)
            {
                return NotFound(new { message = $"Departemen with ID {id} not found." });
            }

            return Ok(itemCategory);
        }

        [HttpPost]
        public async Task<IActionResult> CreateItemCategory(ItemCategory itemCategory)
        {
            var validationResult = await _validator.ValidateAsync(itemCategory);

            if (!validationResult.IsValid)
            {
                return BadRequest(new
                {
                    message = "Validation failed",
                    errors = validationResult.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(e => e.ErrorMessage).ToArray()
                        )
                });
            }

            try
            {
                _context.ItemCategories.Add(itemCategory);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetItemCategory), new { id = itemCategory.Id }, new
                {
                    message = "Berhasil Membuat itemcategory baru",
                    data = itemCategory
                });
            }

            catch (Exception e)
            {
                return StatusCode(500, new { message = "Gagal menyimpan ke database.", error = e.Message });
            }
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
                return BadRequest(new
                {
                    message = "Validation Failed",
                    errors = validationResult.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(e => e.ErrorMessage).ToArray()
                        )
                });
            }

            _context.Entry(itemCategory).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Data Berhasil Diperbarui.", data = itemCategory });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Departments.Any(e => e.Id == id))
                {
                    return NotFound(new { message = "Data sudah tidak ada di database." });
                }
                throw;
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Gagal memperbarui data.", error = ex.Message });
            }
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
}