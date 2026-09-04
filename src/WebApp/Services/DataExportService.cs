using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using ZEGU.Core.Entities.Identity;
using ZEGU.Infrastructure.Data;

namespace ZEGU.WebApp.Services
{
    public class DataExportService
    {
        private readonly ApplicationDbContext _context;

        public DataExportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<byte[]> ExportUserDataAsync(string userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return Array.Empty<byte>();

            var data = new
            {
                ExportDate = DateTime.UtcNow,
                User = new
                {
                    user.Id,
                    user.FirstName,
                    user.LastName,
                    user.Email,
                    user.UserName,
                    user.PhoneNumber,
                    user.Role,
                    user.StudentNumber,
                    user.StaffNumber,
                    user.DepartmentId,
                    user.CreatedAt
                },
                Requests = await _context.MaintenanceRequests
                    .Where(r => r.UserId == userId)
                    .Select(r => new
                    {
                        r.RequestNumber,
                        r.Title,
                        r.Description,
                        r.Priority,
                        r.Status,
                        r.CreatedAt,
                        r.CompletedAt
                    })
                    .ToListAsync(),
                Comments = await _context.RequestComments
                    .Where(c => c.UserId == userId)
                    .Select(c => new
                    {
                        c.CommentText,
                        c.CreatedAt,
                        RequestId = c.RequestId
                    })
                    .ToListAsync(),
                Feedback = await _context.Feedbacks
                    .Where(f => f.UserId == userId)
                    .Select(f => new
                    {
                        f.Rating,
                        f.Comments,
                        f.WorkSatisfactory,
                        f.CreatedAt
                    })
                    .ToListAsync(),
                Notifications = await _context.Notifications
                    .Where(n => n.UserId == userId)
                    .Select(n => new
                    {
                        n.Title,
                        n.Message,
                        n.IsRead,
                        n.CreatedAt
                    })
                    .ToListAsync()
            };

            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            return Encoding.UTF8.GetBytes(json);
        }

        public async Task<bool> AnonymizeUserDataAsync(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            user.FirstName = "REDACTED";
            user.LastName = "REDACTED";
            user.Email = $"redacted_{userId}@anonymized.local";
            user.UserName = $"redacted_{userId}";
            user.NormalizedEmail = user.Email.ToUpper();
            user.NormalizedUserName = user.UserName.ToUpper();
            user.PhoneNumber = null;
            user.StudentNumber = null;
            user.StaffNumber = null;
            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;

            var comments = await _context.RequestComments.Where(c => c.UserId == userId).ToListAsync();
            foreach (var comment in comments)
            {
                comment.CommentText = "[REDACTED]";
                comment.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
