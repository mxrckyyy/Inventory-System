using System.ComponentModel.DataAnnotations;

namespace Inventory_System.Models;

public class Category
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [StringLength(9)]
    public string Color { get; set; } = "#2563EB";

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public int? CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }

    public int? UpdatedByUserId { get; set; }
    public User? UpdatedByUser { get; set; }

    public List<Product> Products { get; set; } = new();
}