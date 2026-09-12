using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inventory_System.Models;

public class Product
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Product ID is required.")]
    [Display(Name = "Product ID")]
    [StringLength(20)]
    public string ProductId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Product name is required.")]
    [Display(Name = "Product Name")]
    [StringLength(200)]
    public string ProductName { get; set; } = string.Empty;

    public int CategoryId { get; set; }
    public Category? CategoryNav { get; set; }

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
    [StringLength(30)]
    public string Unit { get; set; } = string.Empty;

    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    public DateTime DateAdded { get; set; } = DateTime.Now;

    public DateTime LastUpdated { get; set; } = DateTime.Now;

    public int? CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }

    public int? UpdatedByUserId { get; set; }
    public User? UpdatedByUser { get; set; }

    [NotMapped]
    private string? _categoryName;

    [NotMapped]
    [Required(ErrorMessage = "Category is required.")]
    public string Category
    {
        get => _categoryName ?? CategoryNav?.Name ?? string.Empty;
        set => _categoryName = value;
    }

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