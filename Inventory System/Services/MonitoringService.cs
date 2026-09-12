using Inventory_System.Data;
using Inventory_System.Models;
using Microsoft.EntityFrameworkCore;

namespace Inventory_System.Services;

public class MonitoringService
{
    private readonly IDbContextFactory<InventoryDbContext> _dbFactory;

    public MonitoringService(IDbContextFactory<InventoryDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public List<UserAdminRow> GetUsers()
    {
        using var db = _dbFactory.CreateDbContext();

        var users = db.Users
            .AsNoTracking()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .ToList();

        var activitySummary = db.ActivityLogs
            .AsNoTracking()
            .GroupBy(a => a.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                Count = g.Count(),
                LastAction = g.OrderByDescending(x => x.Timestamp).Select(x => x.Action).FirstOrDefault(),
                LastActivity = g.Max(x => x.Timestamp)
            })
            .ToList();

        return users.Select(u =>
        {
            var summary = activitySummary.FirstOrDefault(x => x.UserId == u.Id);
            return new UserAdminRow
            {
                UserId = u.Id,
                Username = u.Username,
                FullName = u.FullName,
                Roles = string.Join(", ", u.UserRoles.Select(ur => ur.Role.Name)),
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                ActivityCount = summary?.Count ?? 0,
                LastAction = summary?.LastAction ?? "-",
                LastActivity = summary?.LastActivity
            };
        }).ToList();
    }

    public List<ActivityLog> GetRecentActivityLogs(int count = 60)
    {
        using var db = _dbFactory.CreateDbContext();
        return db.ActivityLogs
            .AsNoTracking()
            .Include(a => a.User)
            .OrderByDescending(a => a.Timestamp)
            .Take(count)
            .ToList();
    }

    public List<InventoryLog> GetRecentInventoryLogs(int count = 60)
    {
        using var db = _dbFactory.CreateDbContext();
        return db.InventoryLogs
            .AsNoTracking()
            .Include(l => l.Product)
            .Include(l => l.User)
            .OrderByDescending(l => l.Timestamp)
            .Take(count)
            .ToList();
    }

    public List<UserInventorySummary> GetUserInventorySummaries()
    {
        using var db = _dbFactory.CreateDbContext();

        var logs = db.InventoryLogs.AsNoTracking().Include(l => l.User).ToList();

        return logs
            .GroupBy(l => l.UserId)
            .Select(g => new UserInventorySummary
            {
                Username = g.FirstOrDefault(l => l.User != null)?.User?.Username ?? "Unknown",
                TotalChanges = g.Count(),
                ProductsCreated = g.Count(l => l.ChangeType == "ADD"),
                StockChanges = g.Count(l => l.ChangeType == "STOCK_IN" || l.ChangeType == "STOCK_OUT"),
                Edits = g.Count(l => l.ChangeType == "EDIT"),
                Deletions = g.Count(l => l.ChangeType == "DELETE"),
                LastChange = g.Max(l => l.Timestamp)
            })
            .OrderByDescending(s => s.TotalChanges)
            .ToList();
    }
}