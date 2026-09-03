namespace Inventory_System.Services;

public class CategoryService
{
    private List<string> _categories = new();

    public event Action? OnCategoriesChanged;

    public CategoryService()
    {
        SeedData();
    }

    public List<string> GetAll() => _categories.ToList();

    public bool AddCategory(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        var trimmed = name.Trim();
        if (_categories.Any(c => c.Equals(trimmed, StringComparison.OrdinalIgnoreCase)))
            return false;

        _categories.Add(trimmed);
        OnCategoriesChanged?.Invoke();
        return true;
    }

    public bool UpdateCategory(string oldName, string newName)
    {
        if (string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName)) return false;
        var trimmedNew = newName.Trim();

        if (_categories.Any(c => c.Equals(trimmedNew, StringComparison.OrdinalIgnoreCase) &&
                                  !c.Equals(oldName, StringComparison.OrdinalIgnoreCase)))
            return false;

        var index = _categories.FindIndex(c => c.Equals(oldName.Trim(), StringComparison.OrdinalIgnoreCase));
        if (index < 0) return false;

        _categories[index] = trimmedNew;
        OnCategoriesChanged?.Invoke();
        return true;
    }

    public bool DeleteCategory(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        var removed = _categories.RemoveAll(c => c.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase));
        if (removed > 0)
        {
            OnCategoriesChanged?.Invoke();
            return true;
        }
        return false;
    }

    public bool IsCategoryInUse(string name, InventoryService inventoryService)
    {
        return inventoryService.GetAll().Any(p =>
            p.Category.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private void SeedData()
    {
        _categories.AddRange(new[]
        {
            "Ready to Eat",
            "Beverages",
            "Dry Goods",
            "Grains",
            "Canned Goods",
            "Other"
        });
    }
}
