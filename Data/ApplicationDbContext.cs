using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VendingIot.Models;
namespace VendingIot.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {

    }

    public DbSet<Department> Departments { get; set; }


    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // builder.ApplyConfiguration(new RoleConfiguration());

        // builder.ApplyConfiguration(new UserRoleConfiguration());

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.FullName)
            .HasAnnotation("MySql:After", "NormalizedEmail");
        });
    }
}