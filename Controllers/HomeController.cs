using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VendingIot.Data;
using VendingIot.Models;
using Microsoft.AspNetCore.Authorization;

namespace VendingIot.Controllers
{
    [Authorize]
    [ApiController] 
    [Route("api/[controller]")] 
    public class HomeController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("stats")] 
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var dashboardData = new DashboardStatsDTO
                {
                    TotalUsers = await _context.Users.CountAsync(),

                    TotalMachines = await _context.VendingMachines.CountAsync(),

                    TotalDepartments = await _context.Departments.CountAsync()
                };

                return Ok(dashboardData);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = "Terjadi kesalahan saat mengambil data", error = ex.Message });
            }
        }
    }
}