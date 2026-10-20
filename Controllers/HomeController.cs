using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VendingIot.Data;
using VendingIot.Models;

namespace VendingIot.Controllers;

[Authorize]
public class HomeController : BaseApiController
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetDashboardStats()
    {
        var dashboardData = new DashboardStatsDTO
        {
            TotalUsers = await _context.Users.CountAsync(),
            TotalMachines = await _context.VendingMachines.CountAsync(),
            TotalDepartments = await _context.Departments.CountAsync()
        };

        return Ok(dashboardData);
    }
}