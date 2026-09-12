using System.ComponentModel.DataAnnotations;

namespace Inventory_System.Models;

public class InventoryLog
{
    public int Id { get; set; }

    public int? ProductId { get; set; }
    public Product? Product { get; set; }

    [StringLength(200)]
    public string ProductName { get; set; } = string.Empty;

    public int? UserId { get; set; }
    public User? User { get; set; }

    [Required]
    [StringLength(20)]
    public string ChangeType { get; set; } = string.Empty;

    public int PreviousQuantity { get; set; }

    public int NewQuantity { get; set; }

    public int QuantityChange { get; set; }

    public decimal? PreviousPrice { get; set; }

    public decimal? NewPrice { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.Now;
}