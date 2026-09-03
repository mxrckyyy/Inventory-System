using Inventory_System.Models;

namespace Inventory_System.Services;

public class ToastService
{
    private readonly List<ToastMessage> _messages = new();

    public event Action? OnToast;

    public IReadOnlyList<ToastMessage> Messages => _messages.AsReadOnly();

    public void ShowSuccess(string message) => Show(message, "success");
    public void ShowError(string message) => Show(message, "error");
    public void ShowWarning(string message) => Show(message, "warning");
    public void ShowInfo(string message) => Show(message, "info");

    public void Show(string message, string type = "success")
    {
        var toast = new ToastMessage { Message = message, Type = type };
        _messages.Add(toast);
        OnToast?.Invoke();
    }

    public void Remove(ToastMessage message)
    {
        _messages.Remove(message);
        OnToast?.Invoke();
    }
}
