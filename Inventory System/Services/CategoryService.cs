using Inventory_System.Data;
using Inventory_System.Models;
using Microsoft.EntityFrameworkCore;

namespace Inventory_System.Services;

public class CategoryService
{
    private readonly IDbContextFactory<InventoryDbContext> _dbFactory;
    private readonly AuthService _authService;

    public event Action? OnCategoriesChanged;

    public CategoryService(IDbContextFactory<InventoryDbContext> dbFactory, AuthService authService)
    {
        _dbFactory = dbFactory;
        _authService = authService;
    }

    public List<string> GetAll()
    {
        using var db = _dbFactory.CreateDbContext();
        return db.Categories.Select(c => c.Name).OrderBy(c => c).ToList();
    }

    public bool AddCategory(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        var trimmed = name.Trim();

        using var db = _dbFactory.CreateDbContext();

        if (db.Categories.Any(c => c.Name == trimmed)) return false;

        var category = new Category
        {
            Name = trimmed,
            CreatedByUserId = _authService.CurrentUser?.Id,
            UpdatedByUserId = _authService.CurrentUser?.Id
        };
        db.Categories.Add(category);
        db.SaveChanges();

        var actor = _authService.CurrentUser;
        db.ActivityLogs.Add(new ActivityLog
        {
            UserId = actor?.Id,
            Username = actor?.Username ?? "System",
            Action = "CATEGORY_CREATED",
            EntityType = "Category",
            EntityId = category.Id,
            Details = $"Created category '{trimmed}'.",
            Timestamp = DateTime.Now
        });
        db.SaveChanges();

        OnCategoriesChanged?.Invoke();
        return true;
    }

    public bool UpdateCategory(string oldName, string newName)
    {
        if (string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName)) return false;
        var trimmedNew = newName.Trim();

        using var db = _dbFactory.CreateDbContext();

        if (db.Categories.Any(c => c.Name == trimmedNew && c.Name != oldName)) return false;

        var category = db.Categories.FirstOrDefault(c => c.Name == oldName);
        if (category == null) return false;

        category.Name = trimmedNew;
        category.UpdatedAt = DateTime.Now;
        category.UpdatedByUserId = _authService.CurrentUser?.Id;

        if (db.Entry(category).State != EntityState.Modified)
            db.Entry(category).State = EntityState.Modified;

        db.SaveChanges();

        var actor = _authService.CurrentUser;
        db.ActivityLogs.Add(new ActivityLog
        {
            UserId = actor?.Id,
            Username = actor?.Username ?? "System",
            Action = "CATEGORY_RENAMED",
            EntityType = "Category",
            EntityId = category.Id,
            Details = $"Renamed category '{oldName}' -> '{trimmedNew}'.",
            Timestamp = DateTime.Now
        });
        db.SaveChanges();

        OnCategoriesChanged?.Invoke();
        return true;
    }

    public bool DeleteCategory(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;

        using var db = _dbFactory.CreateDbContext();

        var category = db.Categories.FirstOrDefault(c => c.Name == name);
        if (category == null) return false;

        if (db.Products.Any(p => p.CategoryId == category.Id)) return false;

        db.Categories.Remove(category);
        db.SaveChanges();

        var actor = _authService.CurrentUser;
        db.ActivityLogs.Add(new ActivityLog
        {
            UserId = actor?.Id,
            Username = actor?.Username ?? "System",
            Action = "CATEGORY_DELETED",
            EntityType = "Category",
            Details = $"Deleted category '{name}'.",
            Timestamp = DateTime.Now
        });
        db.SaveChanges();

        OnCategoriesChanged?.Invoke();
        return true;
    }

    public bool IsCategoryInUse(string name, InventoryService inventoryService)
    {
        using var db = _dbFactory.CreateDbContext();
        var categoryId = db.Categories.Where(c => c.Name == name).Select(c => c.Id).FirstOrDefault();
        if (categoryId == 0) return false;
        return db.Products.Any(p => p.CategoryId == categoryId);
    }
}