using System.ComponentModel.DataAnnotations;

namespace Inventory_System.Models;

public class Role
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [StringLength(200)]
    public string Description { get; set; } = string.Empty;

    public List<UserRole> UserRoles { get; set; } = new();
}