using System.ComponentModel.DataAnnotations;

namespace Inventory_System.Models;

public class Product
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Product ID is required.")]
    [Display(Name = "Product ID")]
    public string ProductId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Product name is required.")]
    [Display(Name = "Product Name")]
    public string ProductName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Category is required.")]
    public string Category { get; set; } = string.Empty;

    [Required(ErrorMessage = "Price is required.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0.")]
    [Display(Name = "Price")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Quantity is required.")]
    [Range(0, int.MaxValue, ErrorMessage = "Quantity must be 0 or more.")]
    public int Quantity { get; set; }

    [Required(ErrorMessage = "Minimum stock level is required.")]
    [Range(0, int.MaxValue, ErrorMessage = "Minimum stock level must be 0 or more.")]
    [Display(Name = "Minimum Stock Level")]
    public int MinimumStockLevel { get; set; }

    [Required(ErrorMessage = "Unit is required.")]
    public string Unit { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime DateAdded { get; set; } = DateTime.Now;

    public DateTime LastUpdated { get; set; } = DateTime.Now;

    public string Status
    {
        get
        {
            if (Quantity == 0) return "Out of Stock";
            if (Quantity <= MinimumStockLevel) return "Low Stock";
            return "In Stock";
        }
    }
}
