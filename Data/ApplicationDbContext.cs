using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VendingIot.Models;
using VendingIoT.Models;
namespace VendingIot.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {

    }

    public DbSet<Department> Departments { get; set; }
    public DbSet<PermissionCategory> PermissionCategories { get; set; }

    public DbSet<Permission> Permissions { get; set; }

    public DbSet<ItemCategory> ItemCategories { get; set; }

    public DbSet<Item> Items { get; set; }

    public DbSet<VendingMachine> VendingMachines { get; set; }

    public DbSet<VendingItem> VendingItems { get; set; }

    public DbSet<RefreshToken>RefreshTokens{get; set;}


    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<RefreshToken>()
        .HasIndex(r=>r.Token)
        .IsUnique();

        builder.Entity<VendingItem>(entity =>
        {
            entity.HasOne(vi => vi.VendingMachine)
                .WithMany(vm => vm.VendingItems)
                .HasForeignKey(vi => vi.VendingMachineId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(vi => vi.Item)
                  .WithMany(i => i.VendingItems)
                  .HasForeignKey(vi => vi.ItemId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Permission>(entity =>
        {
            entity.HasOne(p => p.PermissionCategory)
           .WithMany(c => c.Permissions)
           .HasForeignKey(p => p.PermissionCategoryId)
           .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Item>(entity =>
        {
            entity.HasOne(itemCategory => itemCategory.ItemCategory)
            .WithMany(item => item.Items)
            .HasForeignKey(itemCategory => itemCategory.ItemCategoryId)
            .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.FullName)
            .HasAnnotation("MySql:After", "NormalizedEmail");
        });
    }
}