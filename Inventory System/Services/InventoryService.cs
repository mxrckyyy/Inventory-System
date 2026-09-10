using Inventory_System.Models;

namespace Inventory_System.Services;

public class InventoryService
{
    private List<Product> _products = new();
    private int _nextId = 1;

    public event Action? OnInventoryChanged;

    public InventoryService()
    {
        SeedData();
    }

    public string GenerateNextProductId()
    {
        var maxNum = _products
            .Select(p => p.ProductId)
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

    public List<Product> GetProducts() => _products.ToList();

    public List<Product> GetAll() => _products.ToList();

    public Product? GetProduct(int id) => _products.FirstOrDefault(p => p.Id == id);

    public Product? GetById(int id) => _products.FirstOrDefault(p => p.Id == id);

    public Product? GetByProductId(string productId) =>
        _products.FirstOrDefault(p => p.ProductId.Equals(productId.Trim(), StringComparison.OrdinalIgnoreCase));

    public bool IsProductIdExists(string productId, int excludeId = 0) =>
        _products.Any(p => p.ProductId.Equals(productId.Trim(), StringComparison.OrdinalIgnoreCase) && p.Id != excludeId);

    public Product AddProduct(Product product)
    {
        product.Id = _nextId++;
        product.ProductId = product.ProductId.Trim();
        product.ProductName = product.ProductName.Trim();
        product.Unit = product.Unit.Trim();
        product.Description = product.Description?.Trim() ?? string.Empty;
        product.DateAdded = DateTime.Now;
        product.LastUpdated = DateTime.Now;
        _products.Add(product);
        OnInventoryChanged?.Invoke();
        return product;
    }

    public Product Add(Product product) => AddProduct(product);

    public bool UpdateProduct(Product product)
    {
        var existing = _products.FirstOrDefault(p => p.Id == product.Id);
        if (existing == null) return false;

        existing.ProductId = product.ProductId.Trim();
        existing.ProductName = product.ProductName.Trim();
        existing.Category = product.Category;
        existing.Price = product.Price;
        existing.Quantity = product.Quantity;
        existing.MinimumStockLevel = product.MinimumStockLevel;
        existing.Unit = product.Unit.Trim();
        existing.Description = product.Description?.Trim() ?? string.Empty;
        existing.LastUpdated = DateTime.Now;
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool Update(Product product) => UpdateProduct(product);

    public bool DeleteProduct(int id)
    {
        var product = _products.FirstOrDefault(p => p.Id == id);
        if (product == null) return false;
        _products.Remove(product);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool Delete(int id) => DeleteProduct(id);

    public bool IncreaseStock(int id, int amount)
    {
        if (amount <= 0) return false;
        var product = _products.FirstOrDefault(p => p.Id == id);
        if (product == null) return false;

        product.Quantity += amount;
        product.LastUpdated = DateTime.Now;
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool AddStock(int id, int amount) => IncreaseStock(id, amount);

    public bool DecreaseStock(int id, int amount)
    {
        if (amount <= 0) return false;
        var product = _products.FirstOrDefault(p => p.Id == id);
        if (product == null) return false;
        if (product.Quantity < amount) return false;

        product.Quantity -= amount;
        product.LastUpdated = DateTime.Now;
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool RemoveStock(int id, int amount) => DecreaseStock(id, amount);

    public List<Product> SearchProducts(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return _products.ToList();
        var q = query.Trim().ToLower();
        return _products.Where(p =>
            p.ProductId.ToLower().Contains(q) ||
            p.ProductName.ToLower().Contains(q) ||
            p.Category.ToLower().Contains(q)).ToList();
    }

    public List<Product> Search(string query, string? category = null, string? status = null,
        string? sortBy = null, bool sortDescending = false)
    {
        var result = _products.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.Trim().ToLower();
            result = result.Where(p =>
                p.ProductId.ToLower().Contains(q) ||
                p.ProductName.ToLower().Contains(q) ||
                p.Category.ToLower().Contains(q));
        }

        if (!string.IsNullOrWhiteSpace(category))
            result = result.Where(p => p.Category == category);

        if (!string.IsNullOrWhiteSpace(status))
            result = result.Where(p => p.Status == status);

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

    public List<Product> FilterProducts(string? category = null, string? status = null)
    {
        var result = _products.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(category))
            result = result.Where(p => p.Category == category);
        if (!string.IsNullOrWhiteSpace(status))
            result = result.Where(p => p.Status == status);
        return result.ToList();
    }

    public List<string> GetCategories() => _products.Select(p => p.Category).Distinct().OrderBy(c => c).ToList();

    public List<Product> GetLowStockProducts() =>
        _products.Where(p => p.Status == "Low Stock").OrderBy(p => p.Quantity).ToList();

    public List<Product> GetOutOfStockProducts() =>
        _products.Where(p => p.Status == "Out of Stock").ToList();

    public decimal GetTotalInventoryValue() => _products.Sum(p => p.Price * p.Quantity);

    public int GetTotalQuantity() => _products.Sum(p => p.Quantity);

    public int GetProductCount() => _products.Count;

    public DashboardStats GetStats()
    {
        var totalQuantity = GetTotalQuantity();
        return new DashboardStats
        {
            TotalProducts = _products.Count,
            TotalQuantity = totalQuantity,
            InStock = _products.Count(p => p.Status == "In Stock"),
            LowStock = _products.Count(p => p.Status == "Low Stock"),
            OutOfStock = _products.Count(p => p.Status == "Out of Stock"),
            LowStockItems = _products.Count(p => p.Status == "Low Stock" || p.Status == "Out of Stock"),
            TotalValue = GetTotalInventoryValue(),
            Categories = GetCategories().Count,
            RecentProducts = GetRecentProducts(),
            LowStockProducts = GetLowStockProducts(),
            OutOfStockProducts = GetOutOfStockProducts()
        };
    }

    private List<Product> GetRecentProducts() =>
        _products.OrderByDescending(p => p.LastUpdated).Take(5).ToList();

    private void SeedData()
    {
        _products.Add(new Product
        {
            Id = _nextId++,
            ProductId = "P001",
            ProductName = "Chicken Adobo",
            Category = "Ready to Eat",
            Price = 120.00m,
            Quantity = 25,
            MinimumStockLevel = 10,
            Unit = "Piece",
            Description = "Classic Filipino chicken adobo, slow-cooked in soy sauce and vinegar.",
            DateAdded = DateTime.Now.AddDays(-30),
            LastUpdated = DateTime.Now.AddHours(-2)
        });

        _products.Add(new Product
        {
            Id = _nextId++,
            ProductId = "P002",
            ProductName = "Mineral Water",
            Category = "Beverages",
            Price = 15.00m,
            Quantity = 50,
            MinimumStockLevel = 10,
            Unit = "Bottle",
            Description = "Refreshing purified mineral water, 500ml bottle.",
            DateAdded = DateTime.Now.AddDays(-25),
            LastUpdated = DateTime.Now.AddHours(-5)
        });

        _products.Add(new Product
        {
            Id = _nextId++,
            ProductId = "P003",
            ProductName = "Instant Noodles",
            Category = "Dry Goods",
            Price = 25.00m,
            Quantity = 100,
            MinimumStockLevel = 20,
            Unit = "Pack",
            Description = "Instant ramen noodles, quick and easy meal.",
            DateAdded = DateTime.Now.AddDays(-20),
            LastUpdated = DateTime.Now.AddHours(-8)
        });

        _products.Add(new Product
        {
            Id = _nextId++,
            ProductId = "P004",
            ProductName = "White Rice",
            Category = "Grains",
            Price = 50.00m,
            Quantity = 80,
            MinimumStockLevel = 20,
            Unit = "Kilogram",
            Description = "Premium quality white rice, freshly harvested.",
            DateAdded = DateTime.Now.AddDays(-15),
            LastUpdated = DateTime.Now.AddHours(-1)
        });
    }
}
