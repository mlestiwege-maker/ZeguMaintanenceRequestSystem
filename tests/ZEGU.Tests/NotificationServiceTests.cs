using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ZEGU.Core.Entities.Maintenance;
using ZEGU.Infrastructure.Data;
using ZEGU.Infrastructure.Services;

namespace ZEGU.Tests;

public class NotificationServiceTests
{
    private static ApplicationDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task RenderTemplateAsync_ReturnsNull_WhenNoActiveTemplateMatches()
    {
        await using var context = NewContext();
        var service = new NotificationService(context, NullLogger<NotificationService>.Instance);

        var result = await service.RenderTemplateAsync("DoesNotExist", new Dictionary<string, string>());

        Assert.Null(result);
    }

    [Fact]
    public async Task RenderTemplateAsync_IgnoresInactiveTemplate()
    {
        await using var context = NewContext();
        context.NotificationTemplates.Add(new NotificationTemplate
        {
            Name = "RequestSubmitted",
            Subject = "New {RequestNumber}",
            Body = "Hi {UserName}",
            Type = "Email",
            IsActive = false
        });
        await context.SaveChangesAsync();
        var service = new NotificationService(context, NullLogger<NotificationService>.Instance);

        var result = await service.RenderTemplateAsync("RequestSubmitted", new Dictionary<string, string>
        {
            ["RequestNumber"] = "MRS-2026-000001",
            ["UserName"] = "Jane"
        });

        Assert.Null(result);
    }

    [Fact]
    public async Task RenderTemplateAsync_SubstitutesAllPlaceholders()
    {
        await using var context = NewContext();
        context.NotificationTemplates.Add(new NotificationTemplate
        {
            Name = "RequestSubmitted",
            Subject = "New request {RequestNumber}",
            Body = "Hi {UserName}, your request '{Title}' is {Status}.",
            Type = "Email",
            IsActive = true
        });
        await context.SaveChangesAsync();
        var service = new NotificationService(context, NullLogger<NotificationService>.Instance);

        var result = await service.RenderTemplateAsync("RequestSubmitted", new Dictionary<string, string>
        {
            ["RequestNumber"] = "MRS-2026-000001",
            ["UserName"] = "Jane",
            ["Title"] = "Broken tap",
            ["Status"] = "Submitted"
        });

        Assert.NotNull(result);
        Assert.Equal("New request MRS-2026-000001", result.Value.Subject);
        Assert.Equal("Hi Jane, your request 'Broken tap' is Submitted.", result.Value.Body);
    }

    [Fact]
    public async Task CreateNotificationAsync_PersistsUnreadNotification()
    {
        await using var context = NewContext();
        var service = new NotificationService(context, NullLogger<NotificationService>.Instance);

        await service.CreateNotificationAsync("user-1", "Title", "Message");

        var count = await service.GetUnreadCountAsync("user-1");
        Assert.Equal(1, count);
    }
}
