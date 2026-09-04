using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZEGU.Core.Enums;
using ZEGU.Infrastructure.Data;
using ZEGU.WebApp.ViewModels.Works;

namespace ZEGU.WebApp.Areas.Works.Controllers
{
    [Area("Works")]
    [Authorize(Policy = "RequireWorksAccess")]
    public class TechniciansController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TechniciansController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Workload()
        {
            var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

            var technicians = await _context.Technicians
                .Include(t => t.User)
                .Include(t => t.Assignments)
                .ThenInclude(a => a.Request)
                .Include(t => t.WorkLogs)
                .Where(t => t.IsActive)
                .ToListAsync();

            var workloadItems = technicians.Select(t => new TechnicianWorkloadItem
            {
                TechnicianId = t.Id,
                TechnicianName = $"{t.User.FirstName} {t.User.LastName}",
                TechnicianType = t.TechnicianType.ToString(),
                IsAvailable = t.IsAvailable,
                ActiveAssignments = t.Assignments.Count(a => a.IsActive && 
                    a.Request.Status != MaintenanceRequestStatus.Completed && 
                    a.Request.Status != MaintenanceRequestStatus.Closed &&
                    a.Request.Status != MaintenanceRequestStatus.Cancelled),
                PendingAssignments = t.Assignments.Count(a => a.IsActive && a.Request.Status == MaintenanceRequestStatus.Assigned),
                CompletedAssignments = t.Assignments.Count(a => a.IsActive && 
                    (a.Request.Status == MaintenanceRequestStatus.Completed || a.Request.Status == MaintenanceRequestStatus.Closed)),
                WorkLogCount = t.WorkLogs.Count(w => w.IsActive),
                TotalHoursThisMonth = t.WorkLogs
                    .Where(w => w.IsActive && w.CreatedAt >= startOfMonth)
                    .Sum(w => w.HoursSpent ?? 0),
                TotalLaborCost = t.WorkLogs
                    .Where(w => w.IsActive)
                    .Sum(w => (w.HoursSpent ?? 0) * (w.LaborRate ?? 0)),
                AvgCompletionDays = t.Assignments
                    .Where(a => a.IsActive && a.Request.CompletedAt.HasValue)
                    .Select(a => (a.Request.CompletedAt.Value - a.Request.CreatedAt).TotalDays)
                    .DefaultIfEmpty()
                    .Average(),
                CurrentRequests = t.Assignments
                    .Where(a => a.IsActive && 
                        a.Request.Status != MaintenanceRequestStatus.Completed && 
                        a.Request.Status != MaintenanceRequestStatus.Closed)
                    .Select(a => a.Request)
                    .OrderByDescending(r => r.Priority)
                    .Take(5)
                    .ToList()
            }).ToList();

            var viewModel = new TechnicianWorkloadViewModel
            {
                Technicians = workloadItems,
                TotalTechnicians = workloadItems.Count,
                AvailableTechnicians = workloadItems.Count(t => t.IsAvailable),
                BusyTechnicians = workloadItems.Count(t => t.ActiveAssignments > 0),
                TotalActiveAssignments = workloadItems.Sum(t => t.ActiveAssignments)
            };

            return View(viewModel);
        }
    }
}
