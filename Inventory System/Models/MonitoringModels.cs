namespace Inventory_System.Models;

public class UserAdminRow
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Roles { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ActivityCount { get; set; }
    public string LastAction { get; set; } = string.Empty;
    public DateTime? LastActivity { get; set; }
}

public class UserInventorySummary
{
    public string Username { get; set; } = string.Empty;
    public int TotalChanges { get; set; }
    public int ProductsCreated { get; set; }
    public int StockChanges { get; set; }
    public int Edits { get; set; }
    public int Deletions { get; set; }
    public DateTime LastChange { get; set; }
}