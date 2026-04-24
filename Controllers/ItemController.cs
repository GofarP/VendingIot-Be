using VendingIot.Models;
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
    public class ItemController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IValidator<Item> _validator;

        public ItemController(ApplicationDbContext context, IValidator<Item> validator)
        {
            _context = context;
            _validator = validator;
        }

        [HttpGet]
        public async Task<IActionResult> GetItem([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                page = page < 1 ? 1 : page;
                pageSize = pageSize < 1 ? 10 : pageSize;

                var totalCount = await _context.Items.CountAsync();
                var items = await _context.Items
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return Ok(new
                {
                    data = items,
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
                _context.Items.Add(item);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(GetItem), new { id = item.Id }, new
                {
                    message = "Berhasil Membuat item baru",
                    data = item
                });
            }
            catch (Exception e)
            {
                return StatusCode(500, new { message = "Gagal menyimpan ke database.", error = e.Message });
            }
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

            _context.Entry(item).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Data Berhasil Diperbarui.", data = item });
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
}