using Microsoft.AspNetCore.Identity;
using VendingIot.Models;
namespace VendingIot.Data;

public static class DbInitializer
{
    public static async Task Seed(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var config = services.GetRequiredService<IConfiguration>();

        string[] roleNames = { "Admin", "Manager", "Staff" };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        string? adminPassword = config["SeedData:AdminPassword"];

        if (string.IsNullOrEmpty(adminPassword))
        {
            adminPassword = "Admin123!Default"; 
        }

        var adminEmail="admin@vending.com";
        var adminUser=await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            var user=new ApplicationUser
            {
                UserName=adminEmail,
                Email=adminEmail,
                FullName="Admin",
                EmailConfirmed=true
            };

            var createUser=await userManager.CreateAsync(user, adminPassword);
            if (createUser.Succeeded)
            {
                await userManager.AddToRoleAsync(user, "Admin");
            }
        }

    }
}