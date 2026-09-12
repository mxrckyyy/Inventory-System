using Inventory_System.Data;
using Inventory_System.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Inventory_System.Services;

public class AuthService
{
    private readonly IDbContextFactory<InventoryDbContext> _dbFactory;
    private readonly ILogger<AuthService> _logger;
    private SessionUser? _currentUser;
    private bool _isLoggedIn;

    public event Action? OnAuthStateChanged;

    public bool IsLoggedIn => _isLoggedIn;

    public SessionUser? CurrentUser => _currentUser;

    public AuthService(IDbContextFactory<InventoryDbContext> dbFactory, ILogger<AuthService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task<(bool Success, string Error)> LoginAsync(string username, string password)
    {
        var trimmedUsername = InputValidator.TrimSpaces(username);

        if (InputValidator.RequiredError(trimmedUsername, "Username").Length > 0)
            return (false, "Username is required.");

        if (InputValidator.RequiredError(password, "Password").Length > 0)
            return (false, "Password is required.");

        await using var db = await _dbFactory.CreateDbContextAsync();

        try
        {
            var user = await db.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username == trimmedUsername && u.IsActive);

            if (user == null || !PasswordHasher.Verify(user.PasswordHash, password))
                return (false, "Invalid username or password.");

            _currentUser = new SessionUser
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Avatar = user.Avatar,
                Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList()
            };
            _isLoggedIn = true;
            OnAuthStateChanged?.Invoke();

            db.ActivityLogs.Add(new ActivityLog
            {
                UserId = user.Id,
                Username = user.Username,
                Action = "LOGIN",
                EntityType = "Auth",
                EntityId = user.Id,
                Details = $"User '{user.Username}' signed in.",
                Timestamp = DateTime.Now
            });
            await db.SaveChangesAsync();

            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed due to a database error for user '{Username}'.", trimmedUsername);
            _isLoggedIn = false;
            _currentUser = null;
            return (false, "Unable to connect to the database or invalid credentials. Please try again.");
        }
    }

    public Dictionary<string, string> Register(string username, string password, string confirmPassword, string fullName, string roleName)
    {
        var errors = new Dictionary<string, string>();

        var trimmedUsername = InputValidator.TrimSpaces(username);
        var trimmedFullName = InputValidator.TrimSpaces(fullName);
        var trimmedRole = InputValidator.TrimSpaces(roleName);

        var usernameError = InputValidator.RequiredError(trimmedUsername, "Username");
        var fullNameError = InputValidator.RequiredError(trimmedFullName, "Full name");
        var roleError = InputValidator.RequiredError(trimmedRole, "Role");
        var passwordError = InputValidator.RequiredError(password, "Password");
        var confirmError = InputValidator.RequiredError(confirmPassword, "Confirm password");

        if (usernameError.Length > 0) { errors["username"] = usernameError; }
        if (fullNameError.Length > 0) { errors["fullName"] = fullNameError; }
        if (roleError.Length > 0) { errors["role"] = roleError; }
        if (passwordError.Length > 0) { errors["password"] = passwordError; }
        if (confirmError.Length > 0) { errors["confirmPassword"] = confirmError; }

        if (errors.Count == 0)
        {
            var matchError = InputValidator.PasswordMatchError(password, confirmPassword);
            if (matchError.Length > 0)
            {
                errors["confirmPassword"] = matchError;
                return errors;
            }
        }

        if (errors.Count > 0) return errors;

        using var db = _dbFactory.CreateDbContext();

        var existingCount = db.Users.Count(u => u.Username == trimmedUsername);
        if (existingCount > 0)
        {
            errors["username"] = "That username is already taken.";
            return errors;
        }

        var role = db.Roles.FirstOrDefault(r => r.Name == trimmedRole);
        if (role == null)
        {
            errors["role"] = "Selected role is not available.";
            return errors;
        }

        var user = new User
        {
            Username = trimmedUsername,
            PasswordHash = PasswordHasher.Hash(password),
            FullName = trimmedFullName,
            Avatar = BuildAvatar(trimmedFullName),
            IsActive = true,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
        db.Users.Add(user);
        db.SaveChanges();

        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        db.ActivityLogs.Add(new ActivityLog
        {
            UserId = user.Id,
            Username = user.Username,
            Action = "USER_REGISTERED",
            EntityType = "User",
            EntityId = user.Id,
            Details = $"New account '{user.Username}' registered with role '{role.Name}'.",
            Timestamp = DateTime.Now
        });
        db.SaveChanges();

        _currentUser = new SessionUser
        {
            Id = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            Avatar = user.Avatar,
            Roles = new List<string> { role.Name }
        };
        _isLoggedIn = true;
        OnAuthStateChanged?.Invoke();

        return errors;
    }

    private static string BuildAvatar(string fullName)
    {
        var avatar = string.Empty;
        var parts = fullName.Split(' ');

        if (parts.Length > 0 && parts[0].Length > 0)
        {
            avatar = avatar + char.ToUpper(parts[0][0]);
        }
        if (parts.Length > 1 && parts[parts.Length - 1].Length > 0)
        {
            avatar = avatar + char.ToUpper(parts[parts.Length - 1][0]);
        }

        return avatar.Length > 0 ? avatar : "U";
    }

    public void Logout()
    {
        if (_currentUser != null)
        {
            using var db = _dbFactory.CreateDbContext();
            db.ActivityLogs.Add(new ActivityLog
            {
                UserId = _currentUser.Id,
                Username = _currentUser.Username,
                Action = "LOGOUT",
                EntityType = "Auth",
                EntityId = _currentUser.Id,
                Details = $"User '{_currentUser.Username}' signed out.",
                Timestamp = DateTime.Now
            });
            db.SaveChanges();
        }

        _currentUser = null;
        _isLoggedIn = false;
        OnAuthStateChanged?.Invoke();
    }
}