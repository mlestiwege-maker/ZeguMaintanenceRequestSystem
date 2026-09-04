using ZEGU.Core.Entities.Identity;
using ZEGU.Core.Entities.Shared;
using ZEGU.Core.Entities.Maintenance;
using ZEGU.Core.Enums;
using ZEGU.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ZEGU.Infrastructure.Services
{
    public class SLAMonitoringService
    {
        private readonly ApplicationDbContext _context;

        public SLAMonitoringService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task CheckOverdueRequestsAsync()
        {
            var now = DateTime.UtcNow;

            var overdueRequests = await _context.MaintenanceRequests
                .Include(r => r.Category)
                .Include(r => r.User)
                .Where(r => r.IsActive && 
                       (r.Status == MaintenanceRequestStatus.Submitted || 
                        r.Status == MaintenanceRequestStatus.Assigned ||
                        r.Status == MaintenanceRequestStatus.InProgress))
                .ToListAsync();

            foreach (var request in overdueRequests)
            {
                if (request.Category.SLAHours.HasValue)
                {
                    var slaDeadline = request.CreatedAt.AddHours(request.Category.SLAHours.Value);
                    
                    if (now > slaDeadline && request.Status != MaintenanceRequestStatus.Rejected)
                    {
                        var existingNotification = await _context.Notifications
                            .FirstOrDefaultAsync(n => n.RequestId == request.Id && 
                                                     n.Title == "SLA Breach" && 
                                                     !n.IsRead);

                        if (existingNotification == null)
                        {
                            _context.Notifications.Add(new Notification
                            {
                                UserId = request.UserId,
                                Title = "SLA Breach Alert",
                                Message = $"Request {request.RequestNumber} has exceeded the SLA of {request.Category.SLAHours} hours. Current status: {request.Status}",
                                RequestId = request.Id,
                                Type = NotificationType.System,
                                IsRead = false
                            });

                            var worksUsers = await _context.Users
                                .Where(u => (u.Role == UserRole.WorksOfficer || u.Role == UserRole.Manager) && u.IsActive)
                                .ToListAsync();

                            foreach (var user in worksUsers)
                            {
                                _context.Notifications.Add(new Notification
                                {
                                    UserId = user.Id,
                                    Title = "SLA Breach Alert",
                                    Message = $"Request {request.RequestNumber} has exceeded SLA. Category: {request.Category.CategoryName}",
                                    RequestId = request.Id,
                                    Type = NotificationType.System,
                                    IsRead = false
                                });
                            }
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task<List<MaintenanceRequest>> GetOverdueRequestsAsync()
        {
            var now = DateTime.UtcNow;

            var requests = await _context.MaintenanceRequests
                .Include(r => r.Category)
                .Include(r => r.User)
                .Include(r => r.Location)
                .Include(r => r.Location.Building)
                .Where(r => r.IsActive && 
                       (r.Status == MaintenanceRequestStatus.Submitted || 
                        r.Status == MaintenanceRequestStatus.Assigned ||
                        r.Status == MaintenanceRequestStatus.InProgress))
                .ToListAsync();

            var overdue = new List<MaintenanceRequest>();
            foreach (var request in requests)
            {
                if (request.Category.SLAHours.HasValue)
                {
                    var slaDeadline = request.CreatedAt.AddHours(request.Category.SLAHours.Value);
                    if (now > slaDeadline && request.Status != MaintenanceRequestStatus.Rejected)
                    {
                        overdue.Add(request);
                    }
                }
            }

            return overdue;
        }

        public async Task<int> GetOverdueCountAsync()
        {
            var overdueRequests = await GetOverdueRequestsAsync();
            return overdueRequests.Count;
        }
    }
}
