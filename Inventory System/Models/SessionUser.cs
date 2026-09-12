namespace Inventory_System.Models;

public class SessionUser
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Avatar { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();

    public string Role => Roles.FirstOrDefault() ?? string.Empty;

    public bool IsInRole(string role) =>
        Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
}