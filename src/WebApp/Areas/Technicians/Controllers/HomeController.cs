using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZEGU.Core.Enums;
using ZEGU.Infrastructure.Data;
using ZEGU.WebApp.ViewModels.Technicians;

namespace ZEGU.WebApp.Areas.Technicians.Controllers
{
    [Area("Technicians")]
    [Authorize(Policy = "RequireTechnician")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var currentUserName = User.Identity?.Name;
            var technicianProfile = await _context.Technicians.FirstOrDefaultAsync(t => t.User!.UserName == currentUserName);
            if (technicianProfile == null)
            {
                TempData["ErrorMessage"] = "No technician profile is linked to your account yet. Please contact the Works Department.";
                return View(new TechnicianDashboardViewModel());
            }

            var assignments = await _context.Assignments
                .Include(a => a.Request)
                .ThenInclude(r => r.Category)
                .Include(a => a.Request)
                .ThenInclude(r => r.Location)
                .ThenInclude(l => l.Building)
                .Where(a => a.IsActive && a.TechnicianId == technicianProfile.Id)
                .ToListAsync();

            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var active = assignments
                .Where(a => a.Request.Status != MaintenanceRequestStatus.Completed &&
                            a.Request.Status != MaintenanceRequestStatus.Closed &&
                            a.Request.Status != MaintenanceRequestStatus.Rejected &&
                            a.Request.Status != MaintenanceRequestStatus.Cancelled)
                .OrderByDescending(a => a.Request.Priority)
                .ThenBy(a => a.Request.CreatedAt)
                .ToList();

            var model = new TechnicianDashboardViewModel
            {
                TotalAssigned = assignments.Count,
                InProgressCount = assignments.Count(a => a.Request.Status == MaintenanceRequestStatus.InProgress),
                CompletedThisMonth = assignments.Count(a => a.Request.Status == MaintenanceRequestStatus.Completed && a.Request.CompletedAt >= monthStart),
                OverdueCount = active.Count(a => a.Request.Category.SLAHours.HasValue &&
                                                  now > a.Request.CreatedAt.AddHours(a.Request.Category.SLAHours.Value)),
                ActiveAssignments = active,
                RecentlyCompleted = assignments
                    .Where(a => a.Request.Status == MaintenanceRequestStatus.Completed)
                    .OrderByDescending(a => a.Request.CompletedAt)
                    .Take(5)
                    .ToList()
            };

            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            var currentUserName = User.Identity?.Name;
            var technicianProfile = await _context.Technicians.FirstOrDefaultAsync(t => t.User!.UserName == currentUserName);
            if (technicianProfile == null) return NotFound();

            var request = await _context.MaintenanceRequests
                .Include(r => r.User)
                .Include(r => r.Category)
                .Include(r => r.Location)
                .ThenInclude(l => l.Building)
                .Include(r => r.Asset)
                .Include(r => r.Attachments)
                .Include(r => r.StatusHistory)
                .Include(r => r.Comments)
                .ThenInclude(c => c.User)
                .Include(r => r.Assignments)
                .FirstOrDefaultAsync(r => r.Id == id && r.IsActive);

            if (request == null) return NotFound();

            // View-only: technicians may only see requests actually assigned to them.
            var isAssignedToMe = request.Assignments.Any(a => a.IsActive && a.TechnicianId == technicianProfile.Id);
            if (!isAssignedToMe) return Forbid();

            return View(request);
        }
    }
}
