namespace Inventory_System.Models;

public class DashboardStats
{
    public int TotalProducts { get; set; }
    public int TotalQuantity { get; set; }
    public int TotalCategories { get; set; }
    public int LowStockItems { get; set; }
    public decimal TotalValue { get; set; }
    public int InStock { get; set; }
    public int OutOfStock { get; set; }
    public int LowStock { get; set; }
    public int Categories { get; set; }
    public List<Product> RecentProducts { get; set; } = new();
    public List<Product> LowStockProducts { get; set; } = new();
    public List<Product> OutOfStockProducts { get; set; } = new();
}
