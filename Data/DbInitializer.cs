using Microsoft.AspNetCore.Identity;
using VendingIot.Models;
using VendingIot.Data; 

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
        
        // PENTING: Ambil context untuk seeding tabel biasa (seperti Department)
        var context = services.GetRequiredService<ApplicationDbContext>();

        if (!context.Departments.Any())
        {
            context.Departments.AddRange(
                new Department { Name = "IT Support", Description = "Technical and Infrastructure" },
                new Department { Name = "Human Resources", Description = "HR and Recruitment" },
                new Department { Name = "Logistics", Description = "Warehouse and Inventory" }
            );
            
            // WAJIB panggil SaveChanges untuk tabel biasa!
            await context.SaveChangesAsync(); 
            Console.WriteLine("---> Berhasil memasukkan data awal Departemen!");
        }

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

        var adminEmail = "admin@vending.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            var user = new ApplicationUser
            {
                UserName = adminEmail, 
                Email = adminEmail,
                FullName = "Admin",
                EmailConfirmed = true
            };

            var createUser = await userManager.CreateAsync(user, adminPassword);
            
            if (createUser.Succeeded)
            {
                await userManager.AddToRoleAsync(user, "Admin");
                Console.WriteLine("---> Berhasil membuat akun Admin!");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("---> GAGAL MEMBUAT AKUN ADMIN. ALASAN:");
                foreach (var error in createUser.Errors)
                {
                    Console.WriteLine($"- {error.Code}: {error.Description}");
                }
                Console.ResetColor();
            }
        }
    }
}