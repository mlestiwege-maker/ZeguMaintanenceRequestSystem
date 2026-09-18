using Microsoft.EntityFrameworkCore;
using ZEGU.Core.Entities.Maintenance;
using ZEGU.Infrastructure.Data;
using ZEGU.Infrastructure.Services;

namespace ZEGU.Tests;

public class AuditServiceTests
{
    private static ApplicationDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task LogAsync_PersistsActionWithSerializedValues()
    {
        await using var context = NewContext();
        var service = new AuditService(context);

        await service.LogAsync("user-1", "admin@university.edu", "Deactivate", "User",
            entityId: null, newValues: new { UserName = "tech@university.edu", IsActive = false });

        var logs = await service.GetRecentLogsAsync();
        var log = Assert.Single(logs);
        Assert.Equal("Deactivate", log.Action);
        Assert.Equal("User", log.EntityType);
        Assert.Contains("tech@university.edu", log.NewValues);
    }

    [Fact]
    public async Task GetRecentLogsAsync_FiltersByEntityType()
    {
        await using var context = NewContext();
        var service = new AuditService(context);

        await service.LogAsync("user-1", "admin", "Update", "User");
        await service.LogAsync("user-1", "admin", "UpdateStatus", "MaintenanceRequest", entityId: 5);

        var userLogs = await service.GetRecentLogsAsync(entityType: "User");

        var log = Assert.Single(userLogs);
        Assert.Equal("User", log.EntityType);
    }

    [Fact]
    public async Task GetRecentLogsAsync_OrdersNewestFirst()
    {
        await using var context = NewContext();
        var now = DateTime.UtcNow;
        context.AuditLogs.AddRange(
            new AuditLog { Action = "First", EntityType = "User", CreatedAt = now.AddMinutes(-1) },
            new AuditLog { Action = "Second", EntityType = "User", CreatedAt = now });
        await context.SaveChangesAsync();
        var service = new AuditService(context);

        var logs = await service.GetRecentLogsAsync();

        Assert.Equal("Second", logs[0].Action);
        Assert.Equal("First", logs[1].Action);
    }
}
