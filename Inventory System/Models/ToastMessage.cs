namespace Inventory_System.Models;

public class ToastMessage
{
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "success";
    public DateTime Timestamp { get; set; } = DateTime.Now;
}
