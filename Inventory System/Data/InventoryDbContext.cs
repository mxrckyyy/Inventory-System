using Inventory_System.Models;
using Microsoft.EntityFrameworkCore;

namespace Inventory_System.Data;

public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<InventoryLog> InventoryLogs => Set<InventoryLog>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.HasKey(u => u.Id);
            e.Property(u => u.Username).HasMaxLength(50).IsRequired();
            e.Property(u => u.PasswordHash).HasMaxLength(200).IsRequired();
            e.Property(u => u.FullName).HasMaxLength(100).IsRequired();
            e.Property(u => u.Avatar).HasMaxLength(10);
            e.HasIndex(u => u.Username).IsUnique();
        });

        modelBuilder.Entity<Role>(e =>
        {
            e.ToTable("Roles");
            e.HasKey(r => r.Id);
            e.Property(r => r.Name).HasMaxLength(50).IsRequired();
            e.Property(r => r.Description).HasMaxLength(200);
            e.HasIndex(r => r.Name).IsUnique();
        });

        modelBuilder.Entity<UserRole>(e =>
        {
            e.ToTable("UserRoles");
            e.HasKey(ur => new { ur.UserId, ur.RoleId });
            e.HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Category>(e =>
        {
            e.ToTable("Categories");
            e.HasKey(c => c.Id);
            e.Property(c => c.Name).HasMaxLength(100).IsRequired();
            e.Property(c => c.Description).HasMaxLength(500);
            e.Property(c => c.Color).HasMaxLength(9).HasDefaultValue("#2563EB");
            e.HasIndex(c => c.Name).IsUnique();
            e.HasOne(c => c.CreatedByUser)
                .WithMany()
                .HasForeignKey(c => c.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(c => c.UpdatedByUser)
                .WithMany()
                .HasForeignKey(c => c.UpdatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Product>(e =>
        {
            e.ToTable("Products");
            e.HasKey(p => p.Id);
            e.Property(p => p.ProductId).HasMaxLength(20).IsRequired();
            e.Property(p => p.ProductName).HasMaxLength(200).IsRequired();
            e.Property(p => p.Price).HasPrecision(18, 2);
            e.Property(p => p.Unit).HasMaxLength(30).IsRequired();
            e.Property(p => p.Description).HasMaxLength(1000);
            e.HasIndex(p => p.ProductId).IsUnique();
            e.HasIndex(p => new { p.CategoryId, p.ProductName });
            e.HasIndex(p => p.CreatedByUserId);
            e.HasIndex(p => p.UpdatedByUserId);
            e.HasOne(p => p.CategoryNav)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.CreatedByUser)
                .WithMany()
                .HasForeignKey(p => p.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(p => p.UpdatedByUser)
                .WithMany()
                .HasForeignKey(p => p.UpdatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<InventoryLog>(e =>
        {
            e.ToTable("InventoryLogs");
            e.HasKey(l => l.Id);
            e.Property(l => l.ChangeType).HasMaxLength(20).IsRequired();
            e.Property(l => l.Note).HasMaxLength(500);
            e.Property(l => l.ProductName).HasMaxLength(200);
            e.Property(l => l.PreviousPrice).HasPrecision(18, 2);
            e.Property(l => l.NewPrice).HasPrecision(18, 2);
            e.HasIndex(l => l.Timestamp);
            e.HasIndex(l => l.ChangeType);
            e.HasOne(l => l.Product)
                .WithMany()
                .HasForeignKey(l => l.ProductId)
                .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(l => l.User)
                .WithMany()
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ActivityLog>(e =>
        {
            e.ToTable("ActivityLogs");
            e.HasKey(l => l.Id);
            e.Property(l => l.Username).HasMaxLength(50);
            e.Property(l => l.Action).HasMaxLength(40).IsRequired();
            e.Property(l => l.EntityType).HasMaxLength(30);
            e.Property(l => l.Details).HasMaxLength(1000);
            e.HasIndex(l => l.Timestamp);
            e.HasIndex(l => l.UserId);
            e.HasIndex(l => l.Action);
            e.HasOne(l => l.User)
                .WithMany()
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        base.OnModelCreating(modelBuilder);
    }
}