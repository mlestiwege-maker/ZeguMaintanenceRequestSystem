using Microsoft.EntityFrameworkCore;
using ZEGU.Core.Entities.Identity;
using ZEGU.Core.Entities.Maintenance;
using ZEGU.Core.Entities.Shared;
using ZEGU.Core.Enums;
using ZEGU.Infrastructure.Data;
using ZEGU.Infrastructure.Services;

namespace ZEGU.Tests;

public class PreventiveMaintenanceReminderServiceTests
{
    private static ApplicationDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static async Task<(MaintenanceCategory category, Room room, ApplicationUser worksOfficer)> SeedBaseDataAsync(ApplicationDbContext context)
    {
        var category = new MaintenanceCategory { CategoryName = "HVAC" };
        var building = new Building { BuildingName = "Science Block" };
        var room = new Room { RoomNumber = "101", Building = building };
        var worksOfficer = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "works@staff.zegu.ac.zw",
            Email = "works@staff.zegu.ac.zw",
            PhoneNumber = "+263771234567",
            FirstName = "Mary",
            LastName = "WorksOfficer",
            Role = UserRole.WorksOfficer,
            IsActive = true
        };

        context.MaintenanceCategories.Add(category);
        context.Rooms.Add(room);
        context.Users.Add(worksOfficer);
        await context.SaveChangesAsync();

        return (category, room, worksOfficer);
    }

    [Fact]
    public async Task CheckDueSchedulesAsync_SkipsScheduleNotYetDue()
    {
        await using var context = NewContext();
        var (category, room, _) = await SeedBaseDataAsync(context);

        context.PreventiveMaintenanceSchedules.Add(new PreventiveMaintenanceSchedule
        {
            ScheduleName = "Quarterly HVAC Check",
            CategoryId = category.Id,
            LocationId = room.Id,
            Frequency = "Quarterly",
            FrequencyDays = 90,
            NextDue = DateTime.UtcNow.AddDays(30)
        });
        await context.SaveChangesAsync();

        var service = new PreventiveMaintenanceReminderService(context);
        var events = await service.CheckDueSchedulesAsync();

        Assert.Empty(events);
    }

    [Fact]
    public async Task CheckDueSchedulesAsync_FlagsDueSoonSchedule_AsNotOverdue()
    {
        await using var context = NewContext();
        var (category, room, worksOfficer) = await SeedBaseDataAsync(context);

        var schedule = new PreventiveMaintenanceSchedule
        {
            ScheduleName = "Quarterly HVAC Check",
            CategoryId = category.Id,
            LocationId = room.Id,
            Frequency = "Quarterly",
            FrequencyDays = 90,
            NextDue = DateTime.UtcNow.AddDays(2)
        };
        context.PreventiveMaintenanceSchedules.Add(schedule);
        await context.SaveChangesAsync();

        var service = new PreventiveMaintenanceReminderService(context);
        var events = await service.CheckDueSchedulesAsync();

        var evt = Assert.Single(events);
        Assert.False(evt.IsOverdue);
        Assert.Contains(evt.Recipients, r => r.Id == worksOfficer.Id);
        Assert.NotNull(schedule.LastReminderSentAt);

        var notification = await context.Notifications.SingleAsync(n => n.UserId == worksOfficer.Id);
        Assert.Equal("Preventive Maintenance Due Soon", notification.Title);
    }

    [Fact]
    public async Task CheckDueSchedulesAsync_FlagsOverdueSchedule()
    {
        await using var context = NewContext();
        var (category, room, _) = await SeedBaseDataAsync(context);

        context.PreventiveMaintenanceSchedules.Add(new PreventiveMaintenanceSchedule
        {
            ScheduleName = "Quarterly HVAC Check",
            CategoryId = category.Id,
            LocationId = room.Id,
            Frequency = "Quarterly",
            FrequencyDays = 90,
            NextDue = DateTime.UtcNow.AddDays(-5)
        });
        await context.SaveChangesAsync();

        var service = new PreventiveMaintenanceReminderService(context);
        var events = await service.CheckDueSchedulesAsync();

        var evt = Assert.Single(events);
        Assert.True(evt.IsOverdue);

        var notification = await context.Notifications.SingleAsync();
        Assert.Equal("Preventive Maintenance Overdue", notification.Title);
    }

    [Fact]
    public async Task CheckDueSchedulesAsync_DoesNotDuplicateReminder_WithinSameDueCycle()
    {
        await using var context = NewContext();
        var (category, room, _) = await SeedBaseDataAsync(context);

        context.PreventiveMaintenanceSchedules.Add(new PreventiveMaintenanceSchedule
        {
            ScheduleName = "Quarterly HVAC Check",
            CategoryId = category.Id,
            LocationId = room.Id,
            Frequency = "Quarterly",
            FrequencyDays = 90,
            NextDue = DateTime.UtcNow.AddDays(1)
        });
        await context.SaveChangesAsync();

        var service = new PreventiveMaintenanceReminderService(context);
        var firstRun = await service.CheckDueSchedulesAsync();
        var secondRun = await service.CheckDueSchedulesAsync();

        Assert.Single(firstRun);
        Assert.Empty(secondRun);
    }

    [Fact]
    public async Task CheckDueSchedulesAsync_RemindsAgain_AfterNextDueAdvancesAndBecomesDueSoonOnceMore()
    {
        await using var context = NewContext();
        var (category, room, _) = await SeedBaseDataAsync(context);

        var schedule = new PreventiveMaintenanceSchedule
        {
            ScheduleName = "Quarterly HVAC Check",
            CategoryId = category.Id,
            LocationId = room.Id,
            Frequency = "Quarterly",
            FrequencyDays = 90,
            NextDue = DateTime.UtcNow.AddDays(1)
        };
        context.PreventiveMaintenanceSchedules.Add(schedule);
        await context.SaveChangesAsync();

        var service = new PreventiveMaintenanceReminderService(context);
        await service.CheckDueSchedulesAsync();

        // Simulate MarkPerformed pushing NextDue far into the future.
        schedule.NextDue = DateTime.UtcNow.AddDays(90);
        await context.SaveChangesAsync();

        var afterPerform = await service.CheckDueSchedulesAsync();
        Assert.Empty(afterPerform);

        // Simulate ~89 days passing: the old reminder is now long before the new due-soon
        // window, and the new NextDue (set 90 days out) is approaching again.
        schedule.LastReminderSentAt = DateTime.UtcNow.AddDays(-100);
        schedule.NextDue = DateTime.UtcNow.AddDays(1);
        await context.SaveChangesAsync();

        var nextCycle = await service.CheckDueSchedulesAsync();
        Assert.Single(nextCycle);
    }

    [Fact]
    public async Task CheckDueSchedulesAsync_IgnoresInactiveSchedule()
    {
        await using var context = NewContext();
        var (category, room, _) = await SeedBaseDataAsync(context);

        context.PreventiveMaintenanceSchedules.Add(new PreventiveMaintenanceSchedule
        {
            ScheduleName = "Quarterly HVAC Check",
            CategoryId = category.Id,
            LocationId = room.Id,
            Frequency = "Quarterly",
            FrequencyDays = 90,
            NextDue = DateTime.UtcNow.AddDays(-5),
            IsActive = false
        });
        await context.SaveChangesAsync();

        var service = new PreventiveMaintenanceReminderService(context);
        var events = await service.CheckDueSchedulesAsync();

        Assert.Empty(events);
    }
}
