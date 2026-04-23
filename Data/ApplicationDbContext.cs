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
    public DbSet<PermissionCategory> PermissionCategories { get; set; }

    public DbSet<Permission> Permissions { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Permission>(entity =>
        {
            entity.HasOne(p => p.PermissionCategory)
           .WithMany(c => c.Permissions)
           .HasForeignKey(p => p.PermissionCategoryId)
           .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.FullName)
            .HasAnnotation("MySql:After", "NormalizedEmail");
        });
    }
}