using Inventory_System.Models;
using Inventory_System.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Inventory_System.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Inventory.Data.DatabaseInitializer");
        var factory = services.GetRequiredService<IDbContextFactory<InventoryDbContext>>();

        await using var db = await factory.CreateDbContextAsync();

        if (!await CanConnectAsync(db, logger)) return;

        await RunStepAsync(() => db.Database.MigrateAsync(), logger, "database migrations");
        await RunStepAsync(() => SeedRolesAsync(db), logger, "role seeding");
        await RunStepAsync(() => SeedUsersAsync(db), logger, "user seeding");
        await RunStepAsync(() => SeedCategoriesAsync(db), logger, "category seeding");
        await RunStepAsync(() => SeedProductsAsync(db), logger, "product seeding");
    }

    private static async Task<bool> CanConnectAsync(InventoryDbContext db, ILogger logger)
    {
        try
        {
            if (await db.Database.CanConnectAsync())
            {
                logger.LogInformation("Database connection succeeded. Applying startup initialization.");
                return true;
            }

            logger.LogWarning(
                "Database is not reachable at startup. The application will keep running so health checks pass, " +
                "but database features are unavailable. Configure ConnectionStrings:DefaultConnection " +
                "(or ConnectionStrings__DefaultConnection / DATABASE_URL on the host) and redeploy.");
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to connect to the database at startup. The application will keep running so health checks pass, " +
                "but database features are unavailable.");
            return false;
        }
    }

    private static async Task RunStepAsync(Func<Task> step, ILogger logger, string stepName)
    {
        try
        {
            await step();
            logger.LogInformation("Startup step completed: {StepName}.", stepName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Startup step failed: {StepName}. The application will continue running.", stepName);
        }
    }

    private static async Task SeedRolesAsync(InventoryDbContext db)
    {
        var roleNames = new[] { "Administrator", "Manager", "Staff", "Employee" };
        foreach (var name in roleNames)
        {
            if (!await db.Roles.AnyAsync(r => r.Name == name))
            {
                db.Roles.Add(new Role { Name = name });
            }
        }
        await db.SaveChangesAsync();
    }

    private static async Task SeedUsersAsync(InventoryDbContext db)
    {
        if (await db.Users.AnyAsync()) return;

        var adminRole = await db.Roles.SingleAsync(r => r.Name == "Administrator");
        var staffRole = await db.Roles.SingleAsync(r => r.Name == "Staff");

        var admin = new User
        {
            Username = "admin",
            PasswordHash = PasswordHasher.Hash("admin123"),
            FullName = "Administrator",
            Avatar = "AU",
            CreatedAt = DateTime.Now
        };
        var staff = new User
        {
            Username = "staff",
            PasswordHash = PasswordHasher.Hash("staff123"),
            FullName = "Staff User",
            Avatar = "SU",
            CreatedAt = DateTime.Now
        };

        db.Users.AddRange(admin, staff);
        await db.SaveChangesAsync();

        db.UserRoles.AddRange(new UserRole { UserId = admin.Id, RoleId = adminRole.Id },
                              new UserRole { UserId = staff.Id, RoleId = staffRole.Id });
        await db.SaveChangesAsync();
    }

    private static async Task SeedCategoriesAsync(InventoryDbContext db)
    {
        var admin = await db.Users.FirstOrDefaultAsync(u => u.Username == "admin");

        var names = new[]
        {
            "Ready to Eat",
            "Beverages",
            "Dry Goods",
            "Grains",
            "Canned Goods",
            "Other"
        };

        foreach (var name in names)
        {
            if (!await db.Categories.AnyAsync(c => c.Name == name))
            {
                db.Categories.Add(new Category
                {
                    Name = name,
                    CreatedByUserId = admin?.Id,
                    UpdatedByUserId = admin?.Id
                });
            }
        }
        await db.SaveChangesAsync();
    }

    private static async Task SeedProductsAsync(InventoryDbContext db)
    {
        if (await db.Products.AnyAsync()) return;

        var admin = await db.Users.FirstOrDefaultAsync(u => u.Username == "admin");

        var readyToEat = await db.Categories.SingleAsync(c => c.Name == "Ready to Eat");
        var beverages = await db.Categories.SingleAsync(c => c.Name == "Beverages");
        var dryGoods = await db.Categories.SingleAsync(c => c.Name == "Dry Goods");
        var grains = await db.Categories.SingleAsync(c => c.Name == "Grains");

        var products = new List<Product>
        {
            new()
            {
                ProductId = "P001",
                ProductName = "Chicken Adobo",
                CategoryId = readyToEat.Id,
                Price = 120.00m,
                Quantity = 25,
                MinimumStockLevel = 10,
                Unit = "Piece",
                Description = "Classic Filipino chicken adobo, slow-cooked in soy sauce and vinegar.",
                DateAdded = DateTime.Now.AddDays(-30),
                LastUpdated = DateTime.Now.AddHours(-2),
                CreatedByUserId = admin?.Id,
                UpdatedByUserId = admin?.Id
            },
            new()
            {
                ProductId = "P002",
                ProductName = "Mineral Water",
                CategoryId = beverages.Id,
                Price = 15.00m,
                Quantity = 50,
                MinimumStockLevel = 10,
                Unit = "Bottle",
                Description = "Refreshing purified mineral water, 500ml bottle.",
                DateAdded = DateTime.Now.AddDays(-25),
                LastUpdated = DateTime.Now.AddHours(-5),
                CreatedByUserId = admin?.Id,
                UpdatedByUserId = admin?.Id
            },
            new()
            {
                ProductId = "P003",
                ProductName = "Instant Noodles",
                CategoryId = dryGoods.Id,
                Price = 25.00m,
                Quantity = 100,
                MinimumStockLevel = 20,
                Unit = "Pack",
                Description = "Instant ramen noodles, quick and easy meal.",
                DateAdded = DateTime.Now.AddDays(-20),
                LastUpdated = DateTime.Now.AddHours(-8),
                CreatedByUserId = admin?.Id,
                UpdatedByUserId = admin?.Id
            },
            new()
            {
                ProductId = "P004",
                ProductName = "White Rice",
                CategoryId = grains.Id,
                Price = 50.00m,
                Quantity = 80,
                MinimumStockLevel = 20,
                Unit = "Kilogram",
                Description = "Premium quality white rice, freshly harvested.",
                DateAdded = DateTime.Now.AddDays(-15),
                LastUpdated = DateTime.Now.AddHours(-1),
                CreatedByUserId = admin?.Id,
                UpdatedByUserId = admin?.Id
            }
        };

        db.Products.AddRange(products);
        await db.SaveChangesAsync();
    }
}