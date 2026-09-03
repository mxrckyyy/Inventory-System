using Inventory_System.Models;

namespace Inventory_System.Services;

public class AuthService
{
    private User? _currentUser;
    private bool _isLoggedIn;

    public User? CurrentUser => _currentUser;
    public bool IsLoggedIn => _isLoggedIn;

    public event Action? OnAuthStateChanged;

    public (bool Success, string Error) Login(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username))
            return (false, "Username is required.");

        if (string.IsNullOrWhiteSpace(password))
            return (false, "Password is required.");

        if (username == "admin" && password == "admin123")
        {
            _currentUser = new User
            {
                Username = "admin",
                FullName = "Admin User",
                Role = "Administrator",
                Avatar = "AU"
            };
            _isLoggedIn = true;
            OnAuthStateChanged?.Invoke();
            return (true, string.Empty);
        }

        return (false, "Invalid username or password.");
    }

    public void Logout()
    {
        _currentUser = null;
        _isLoggedIn = false;
        OnAuthStateChanged?.Invoke();
    }
}
