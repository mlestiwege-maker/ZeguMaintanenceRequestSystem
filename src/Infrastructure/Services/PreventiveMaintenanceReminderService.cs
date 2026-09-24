using ZEGU.Core.Entities.Identity;
using ZEGU.Core.Entities.Maintenance;
using ZEGU.Core.Enums;
using ZEGU.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ZEGU.Infrastructure.Services
{
    public class PmReminderEvent
    {
        public PreventiveMaintenanceSchedule Schedule { get; set; } = null!;
        public bool IsOverdue { get; set; }
        public List<ApplicationUser> Recipients { get; set; } = new();
    }

    public class PreventiveMaintenanceReminderService
    {
        private const int DueSoonWindowDays = 3;

        private readonly ApplicationDbContext _context;

        public PreventiveMaintenanceReminderService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<PmReminderEvent>> CheckDueSchedulesAsync()
        {
            var now = DateTime.UtcNow;
            var events = new List<PmReminderEvent>();

            var schedules = await _context.PreventiveMaintenanceSchedules
                .Include(s => s.Category)
                .Include(s => s.Location)
                .ThenInclude(l => l.Building)
                .Include(s => s.Technician)
                .ThenInclude(t => t!.User)
                .Where(s => s.IsActive)
                .ToListAsync();

            var managersAndWorksOfficers = await _context.Users
                .Where(u => (u.Role == UserRole.WorksOfficer || u.Role == UserRole.Manager) && u.IsActive)
                .ToListAsync();

            foreach (var schedule in schedules)
            {
                var dueSoonThreshold = schedule.NextDue.AddDays(-DueSoonWindowDays);
                var isDueOrSoon = now >= dueSoonThreshold;
                var alreadyReminded = schedule.LastReminderSentAt.HasValue && schedule.LastReminderSentAt.Value >= dueSoonThreshold;

                if (!isDueOrSoon || alreadyReminded) continue;

                var isOverdue = now > schedule.NextDue;
                var title = isOverdue ? "Preventive Maintenance Overdue" : "Preventive Maintenance Due Soon";
                var message = $"'{schedule.ScheduleName}' ({schedule.Category.CategoryName} - {schedule.Location.Building!.BuildingName} {schedule.Location.RoomNumber}) " +
                              (isOverdue
                                  ? $"was due on {schedule.NextDue:dd MMM yyyy} and has not been marked performed."
                                  : $"is due on {schedule.NextDue:dd MMM yyyy}.");

                var recipients = new List<ApplicationUser>(managersAndWorksOfficers);
                if (schedule.Technician?.User != null && !recipients.Any(r => r.Id == schedule.Technician.User.Id))
                {
                    recipients.Add(schedule.Technician.User);
                }

                foreach (var recipient in recipients)
                {
                    _context.Notifications.Add(new Notification
                    {
                        UserId = recipient.Id,
                        Title = title,
                        Message = message,
                        Type = NotificationType.System,
                        IsRead = false
                    });
                }

                schedule.LastReminderSentAt = now;
                schedule.UpdatedAt = now;

                events.Add(new PmReminderEvent
                {
                    Schedule = schedule,
                    IsOverdue = isOverdue,
                    Recipients = recipients
                });
            }

            await _context.SaveChangesAsync();

            return events;
        }
    }
}
