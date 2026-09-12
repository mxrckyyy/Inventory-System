using System.ComponentModel.DataAnnotations;

namespace Inventory_System.Models;

public class ActivityLog
{
    public int Id { get; set; }

    public int? UserId { get; set; }
    public User? User { get; set; }

    [StringLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [StringLength(40)]
    public string Action { get; set; } = string.Empty;

    [StringLength(30)]
    public string EntityType { get; set; } = string.Empty;

    public int? EntityId { get; set; }

    [StringLength(1000)]
    public string Details { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.Now;
}