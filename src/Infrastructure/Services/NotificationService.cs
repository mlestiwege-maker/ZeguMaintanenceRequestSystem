using ZEGU.Core.Entities.Identity;
using ZEGU.Core.Entities.Maintenance;
using ZEGU.Core.Enums;
using ZEGU.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ZEGU.Infrastructure.Services
{
    public class NotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(ApplicationDbContext context, ILogger<NotificationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task CreateNotificationAsync(string userId, string title, string message, int? requestId = null, NotificationType type = NotificationType.System)
        {
            _context.Notifications.Add(new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                RequestId = requestId,
                Type = type,
                IsRead = false
            });
            await _context.SaveChangesAsync();
        }

        public async Task CreateNotificationForRoleAsync(UserRole role, string title, string message, int? requestId = null, NotificationType type = NotificationType.System)
        {
            var users = await _context.Users
                .Where(u => u.Role == role && u.IsActive)
                .ToListAsync();

            foreach (var user in users)
            {
                await CreateNotificationAsync(user.Id, title, message, requestId, type);
            }
        }

        public async Task<List<Notification>> GetUnreadNotificationsAsync(string userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .Take(10)
                .ToListAsync();
        }

        public async Task<(string Subject, string Body)?> RenderTemplateAsync(string templateName, Dictionary<string, string> placeholders)
        {
            var template = await _context.NotificationTemplates
                .FirstOrDefaultAsync(t => t.Name == templateName && t.IsActive);

            if (template == null) return null;

            var subject = template.Subject;
            var body = template.Body;
            foreach (var (key, value) in placeholders)
            {
                subject = subject.Replace("{" + key + "}", value);
                body = body.Replace("{" + key + "}", value);
            }

            return (subject, body);
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task MarkAsReadAsync(int notificationId, string userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
            
            if (notification != null)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(string userId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }
    }
}

