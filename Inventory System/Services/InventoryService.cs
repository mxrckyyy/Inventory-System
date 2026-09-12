using Inventory_System.Data;
using Inventory_System.Models;
using Microsoft.EntityFrameworkCore;

namespace Inventory_System.Services;

public class InventoryService
{
    private readonly IDbContextFactory<InventoryDbContext> _dbFactory;
    private readonly AuthService _authService;

    public event Action? OnInventoryChanged;

    public InventoryService(IDbContextFactory<InventoryDbContext> dbFactory, AuthService authService)
    {
        _dbFactory = dbFactory;
        _authService = authService;
    }

    public string GenerateNextProductId()
    {
        using var db = _dbFactory.CreateDbContext();
        var maxNum = db.Products
            .Select(p => p.ProductId)
            .AsEnumerable()
            .Where(id => id.StartsWith("P", StringComparison.OrdinalIgnoreCase) && id.Length > 1)
            .Select(id =>
            {
                var numPart = id[1..];
                return int.TryParse(numPart, out var n) ? n : 0;
            })
            .DefaultIfEmpty(0)
            .Max();

        return $"P{(maxNum + 1):D3}";
    }

    public List<Product> GetAll()
    {
        using var db = _dbFactory.CreateDbContext();
        return db.Products.AsNoTracking()
            .Include(p => p.CategoryNav)
            .Include(p => p.CreatedByUser)
            .Include(p => p.UpdatedByUser)
            .OrderBy(p => p.ProductId)
            .ToList();
    }

    public List<Product> GetProducts() => GetAll();

    public Product? GetById(int id)
    {
        using var db = _dbFactory.CreateDbContext();
        return db.Products
            .Include(p => p.CategoryNav)
            .Include(p => p.CreatedByUser)
            .Include(p => p.UpdatedByUser)
            .FirstOrDefault(p => p.Id == id);
    }

    public Product? GetProduct(int id) => GetById(id);

    public Product? GetByProductId(string productId)
    {
        using var db = _dbFactory.CreateDbContext();
        return db.Products
            .Include(p => p.CategoryNav)
            .Include(p => p.CreatedByUser)
            .Include(p => p.UpdatedByUser)
            .FirstOrDefault(p => p.ProductId == productId.Trim());
    }

    public bool IsProductIdExists(string productId, int excludeId = 0)
    {
        using var db = _dbFactory.CreateDbContext();
        return db.Products.Any(p => p.ProductId == productId.Trim() && p.Id != excludeId);
    }

    public Product AddProduct(Product product)
    {
        using var db = _dbFactory.CreateDbContext();

        product.ProductId = product.ProductId.Trim();
        product.ProductName = product.ProductName.Trim();
        product.Unit = product.Unit.Trim();
        product.Description = product.Description?.Trim() ?? string.Empty;
        product.DateAdded = DateTime.Now;
        product.LastUpdated = DateTime.Now;
        product.CreatedByUserId = _authService.CurrentUser?.Id;
        product.UpdatedByUserId = _authService.CurrentUser?.Id;

        var category = ResolveCategory(db, product.Category);
        product.CategoryId = category.Id;
        product.CategoryNav = null;

        db.Products.Add(product);
        db.SaveChanges();

        var actor = _authService.CurrentUser;

        db.InventoryLogs.Add(new InventoryLog
        {
            ProductId = product.Id,
            ProductName = product.ProductName,
            UserId = actor?.Id,
            ChangeType = "ADD",
            PreviousQuantity = 0,
            NewQuantity = product.Quantity,
            QuantityChange = product.Quantity,
            PreviousPrice = null,
            NewPrice = product.Price,
            Note = $"Product '{product.ProductName}' created with {product.Quantity} {product.Unit}(s).",
            Timestamp = DateTime.Now
        });

        db.ActivityLogs.Add(NewActivityLog("PRODUCT_CREATED", product.Id,
            $"Created product '{product.ProductName}' ({product.ProductId}) in '{category.Name}' at ₱{product.Price:F2}."));

        db.SaveChanges();

        OnInventoryChanged?.Invoke();
        return product;
    }

    public Product Add(Product product) => AddProduct(product);

    public bool UpdateProduct(Product product)
    {
        using var db = _dbFactory.CreateDbContext();

        var existing = db.Products.Include(p => p.CategoryNav).FirstOrDefault(p => p.Id == product.Id);
        if (existing == null) return false;

        var previousQuantity = existing.Quantity;
        var previousPrice = existing.Price;

        existing.ProductId = product.ProductId.Trim();
        existing.ProductName = product.ProductName.Trim();
        existing.Unit = product.Unit.Trim();
        existing.Description = product.Description?.Trim() ?? string.Empty;
        existing.Price = product.Price;
        existing.Quantity = product.Quantity;
        existing.MinimumStockLevel = product.MinimumStockLevel;
        existing.LastUpdated = DateTime.Now;
        existing.UpdatedByUserId = _authService.CurrentUser?.Id;

        var category = ResolveCategory(db, product.Category);
        existing.CategoryId = category.Id;

        db.SaveChanges();

        var quantityChanged = existing.Quantity != previousQuantity;
        var priceChanged = existing.Price != previousPrice;

        var changes = new List<string>();
        if (quantityChanged) changes.Add($"quantity {previousQuantity} -> {existing.Quantity}");
        if (priceChanged) changes.Add($"price ₱{previousPrice:F2} -> ₱{existing.Price:F2}");
        if (existing.CategoryNav != null && existing.CategoryNav.Name != product.Category)
            changes.Add($"category -> {category.Name}");
        var changeText = changes.Count == 0 ? "details updated" : string.Join(", ", changes);

        db.InventoryLogs.Add(new InventoryLog
        {
            ProductId = existing.Id,
            ProductName = existing.ProductName,
            UserId = _authService.CurrentUser?.Id,
            ChangeType = "EDIT",
            PreviousQuantity = previousQuantity,
            NewQuantity = existing.Quantity,
            QuantityChange = existing.Quantity - previousQuantity,
            PreviousPrice = previousPrice,
            NewPrice = existing.Price,
            Note = $"'{existing.ProductName}' updated ({changeText}).",
            Timestamp = DateTime.Now
        });

        db.ActivityLogs.Add(NewActivityLog("PRODUCT_EDITED", existing.Id,
            $"Edited product '{existing.ProductName}' ({existing.ProductId}) - {changeText}."));

        db.SaveChanges();

        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool Update(Product product) => UpdateProduct(product);

    public bool DeleteProduct(int id)
    {
        using var db = _dbFactory.CreateDbContext();

        var product = db.Products.FirstOrDefault(p => p.Id == id);
        if (product == null) return false;

        db.InventoryLogs.Add(new InventoryLog
        {
            ProductId = product.Id,
            ProductName = product.ProductName,
            UserId = _authService.CurrentUser?.Id,
            ChangeType = "DELETE",
            PreviousQuantity = product.Quantity,
            NewQuantity = 0,
            QuantityChange = -product.Quantity,
            PreviousPrice = product.Price,
            NewPrice = null,
            Note = $"Product '{product.ProductName}' deleted.",
            Timestamp = DateTime.Now
        });

        db.ActivityLogs.Add(NewActivityLog("PRODUCT_DELETED", product.Id,
            $"Deleted product '{product.ProductName}' ({product.ProductId})."));

        db.Products.Remove(product);
        db.SaveChanges();

        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool Delete(int id) => DeleteProduct(id);

    public bool IncreaseStock(int id, int amount)
    {
        if (amount <= 0) return false;

        using var db = _dbFactory.CreateDbContext();

        var product = db.Products.FirstOrDefault(p => p.Id == id);
        if (product == null) return false;

        var previous = product.Quantity;
        product.Quantity += amount;
        product.LastUpdated = DateTime.Now;
        product.UpdatedByUserId = _authService.CurrentUser?.Id;

        db.InventoryLogs.Add(new InventoryLog
        {
            ProductId = product.Id,
            ProductName = product.ProductName,
            UserId = _authService.CurrentUser?.Id,
            ChangeType = "STOCK_IN",
            PreviousQuantity = previous,
            NewQuantity = product.Quantity,
            QuantityChange = amount,
            PreviousPrice = product.Price,
            NewPrice = product.Price,
            Note = $"Added {amount} {product.Unit}(s) to stock of '{product.ProductName}'.",
            Timestamp = DateTime.Now
        });

        db.ActivityLogs.Add(NewActivityLog("STOCK_IN", product.Id,
            $"Added {amount} {product.Unit}(s) to '{product.ProductName}' (stock {previous} -> {product.Quantity})."));

        db.SaveChanges();
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool AddStock(int id, int amount) => IncreaseStock(id, amount);

    public bool DecreaseStock(int id, int amount)
    {
        if (amount <= 0) return false;

        using var db = _dbFactory.CreateDbContext();

        var product = db.Products.FirstOrDefault(p => p.Id == id);
        if (product == null) return false;
        if (product.Quantity < amount) return false;

        var previous = product.Quantity;
        product.Quantity -= amount;
        product.LastUpdated = DateTime.Now;
        product.UpdatedByUserId = _authService.CurrentUser?.Id;

        db.InventoryLogs.Add(new InventoryLog
        {
            ProductId = product.Id,
            ProductName = product.ProductName,
            UserId = _authService.CurrentUser?.Id,
            ChangeType = "STOCK_OUT",
            PreviousQuantity = previous,
            NewQuantity = product.Quantity,
            QuantityChange = -amount,
            PreviousPrice = product.Price,
            NewPrice = product.Price,
            Note = $"Removed {amount} {product.Unit}(s) from stock of '{product.ProductName}'.",
            Timestamp = DateTime.Now
        });

        db.ActivityLogs.Add(NewActivityLog("STOCK_OUT", product.Id,
            $"Removed {amount} {product.Unit}(s) from '{product.ProductName}' (stock {previous} -> {product.Quantity})."));

        db.SaveChanges();
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool RemoveStock(int id, int amount) => DecreaseStock(id, amount);

    public List<Product> SearchProducts(string query)
    {
        using var db = _dbFactory.CreateDbContext();
        IQueryable<Product> result = QueryAll(db);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.Trim();
            result = result.Where(p =>
                p.ProductId.Contains(q) ||
                p.ProductName.Contains(q) ||
                (p.CategoryNav != null && p.CategoryNav.Name.Contains(q)) ||
                p.Unit.Contains(q));
        }

        return result.OrderBy(p => p.ProductId).ToList();
    }

    public List<Product> FilterProducts(string? category = null, string? status = null)
    {
        using var db = _dbFactory.CreateDbContext();
        var result = ApplyFilters(QueryAll(db), category, status);
        return result.OrderBy(p => p.ProductId).ToList();
    }

    public List<Product> Search(string query, string? category = null, string? status = null,
        string? sortBy = null, bool sortDescending = false)
    {
        using var db = _dbFactory.CreateDbContext();
        IQueryable<Product> result = ApplyFilters(QueryAll(db), category, status);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.Trim();
            result = result.Where(p =>
                p.ProductId.Contains(q) ||
                p.ProductName.Contains(q) ||
                (p.CategoryNav != null && p.CategoryNav.Name.Contains(q)));
        }

        result = sortBy?.ToLower() switch
        {
            "name" => sortDescending ? result.OrderByDescending(p => p.ProductName) : result.OrderBy(p => p.ProductName),
            "price" => sortDescending ? result.OrderByDescending(p => p.Price) : result.OrderBy(p => p.Price),
            "quantity" => sortDescending ? result.OrderByDescending(p => p.Quantity) : result.OrderBy(p => p.Quantity),
            "date" => sortDescending ? result.OrderByDescending(p => p.DateAdded) : result.OrderBy(p => p.DateAdded),
            "productid" => sortDescending ? result.OrderByDescending(p => p.ProductId) : result.OrderBy(p => p.ProductId),
            _ => result.OrderBy(p => p.ProductId)
        };

        return result.ToList();
    }

    private static IQueryable<Product> QueryAll(InventoryDbContext db) =>
        db.Products.AsNoTracking()
            .Include(p => p.CategoryNav)
            .Include(p => p.CreatedByUser)
            .Include(p => p.UpdatedByUser);

    private static IQueryable<Product> ApplyFilters(IQueryable<Product> source, string? category, string? status)
    {
        IQueryable<Product> result = source;

        if (!string.IsNullOrWhiteSpace(category))
            result = result.Where(p => p.CategoryNav != null && p.CategoryNav.Name == category);

        if (!string.IsNullOrWhiteSpace(status))
        {
            result = status switch
            {
                "In Stock" => result.Where(p => p.Quantity > p.MinimumStockLevel),
                "Low Stock" => result.Where(p => p.Quantity > 0 && p.Quantity <= p.MinimumStockLevel),
                "Out of Stock" => result.Where(p => p.Quantity == 0),
                _ => result
            };
        }

        return result;
    }

    public List<string> GetCategories()
    {
        using var db = _dbFactory.CreateDbContext();
        return db.Categories.AsNoTracking().Select(c => c.Name).OrderBy(c => c).ToList();
    }

    public List<Product> GetLowStockProducts()
    {
        using var db = _dbFactory.CreateDbContext();
        return db.Products.AsNoTracking()
            .Include(p => p.CategoryNav)
            .Where(p => p.Quantity > 0 && p.Quantity <= p.MinimumStockLevel)
            .OrderBy(p => p.Quantity)
            .ToList();
    }

    public List<Product> GetOutOfStockProducts()
    {
        using var db = _dbFactory.CreateDbContext();
        return db.Products.AsNoTracking()
            .Include(p => p.CategoryNav)
            .Where(p => p.Quantity == 0)
            .ToList();
    }

    public decimal GetTotalInventoryValue()
    {
        using var db = _dbFactory.CreateDbContext();
        return db.Products.Sum(p => p.Price * p.Quantity);
    }

    public int GetTotalQuantity()
    {
        using var db = _dbFactory.CreateDbContext();
        return db.Products.Sum(p => p.Quantity);
    }

    public int GetProductCount()
    {
        using var db = _dbFactory.CreateDbContext();
        return db.Products.Count();
    }

    public DashboardStats GetStats()
    {
        using var db = _dbFactory.CreateDbContext();

        var aggregate = db.Products
            .AsNoTracking()
            .GroupBy(p => 1)
            .Select(g => new
            {
                TotalProducts = g.Count(),
                TotalQuantity = g.Sum(p => p.Quantity),
                TotalValue = g.Sum(p => p.Price * p.Quantity),
                InStock = g.Count(p => p.Quantity > p.MinimumStockLevel),
                LowStock = g.Count(p => p.Quantity > 0 && p.Quantity <= p.MinimumStockLevel),
                OutOfStock = g.Count(p => p.Quantity == 0),
                RecentActivity = g.Max(p => p.LastUpdated)
            })
            .FirstOrDefault();

        var categories = db.Categories.Count();

        var productRows = db.Products
            .AsNoTracking()
            .Select(p => new Product
            {
                Id = p.Id,
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                Unit = p.Unit,
                Quantity = p.Quantity,
                Price = p.Price,
                Category = p.Category,
                MinimumStockLevel = p.MinimumStockLevel,
                LastUpdated = p.LastUpdated
            })
            .ToList();

        var lowStock = aggregate?.LowStock ?? 0;
        var outOfStock = aggregate?.OutOfStock ?? 0;

        return new DashboardStats
        {
            TotalProducts = aggregate?.TotalProducts ?? 0,
            TotalQuantity = aggregate?.TotalQuantity ?? 0,
            InStock = aggregate?.InStock ?? 0,
            LowStock = lowStock,
            OutOfStock = outOfStock,
            LowStockItems = lowStock + outOfStock,
            TotalValue = aggregate?.TotalValue ?? 0,
            Categories = categories,
            TotalCategories = categories,
            RecentProducts = productRows
                .OrderByDescending(p => p.LastUpdated)
                .Take(5)
                .ToList(),
            LowStockProducts = productRows
                .Where(p => p.Quantity > 0 && p.Quantity <= p.MinimumStockLevel)
                .OrderBy(p => p.Quantity)
                .ToList(),
            OutOfStockProducts = productRows
                .Where(p => p.Quantity == 0)
                .ToList()
        };
    }

    public List<InventoryLog> GetRecentLogs(int count = 20)
    {
        using var db = _dbFactory.CreateDbContext();
        return db.InventoryLogs.AsNoTracking()
            .Include(l => l.Product)
            .Include(l => l.User)
            .OrderByDescending(l => l.Timestamp)
            .Take(count)
            .ToList();
    }

    private ActivityLog NewActivityLog(string action, int? entityId, string details)
    {
        var actor = _authService.CurrentUser;
        return new ActivityLog
        {
            UserId = actor?.Id,
            Username = actor?.Username ?? "System",
            Action = action,
            EntityType = "Product",
            EntityId = entityId,
            Details = details,
            Timestamp = DateTime.Now
        };
    }

    private static Category ResolveCategory(InventoryDbContext db, string categoryName)
    {
        var name = string.IsNullOrWhiteSpace(categoryName) ? "Other" : categoryName.Trim();

        var category = db.Categories.FirstOrDefault(c => c.Name == name);
        if (category != null) return category;

        category = new Category { Name = name };
        db.Categories.Add(category);
        db.SaveChanges();
        return category;
    }
}