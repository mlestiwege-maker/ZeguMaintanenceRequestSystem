using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZEGU.Core.Entities.Maintenance;
using ZEGU.Core.Enums;
using ZEGU.Infrastructure.Data;
using ZEGU.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using ZEGU.Core.Entities.Identity;

namespace ZEGU.WebApp.Areas.Works.Controllers
{
[Area("Works")]
    [Authorize(Policy = "RequireWorksAccess")]
    public class WorkLogsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        private string? CurrentUserName => User.Identity?.Name;

        public WorkLogsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(int requestId)
        {
            var workLogs = await _context.WorkLogs
                .Include(w => w.Technician)
                .Include(w => w.Technician.User)
                .Where(w => w.RequestId == requestId && w.IsActive)
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync();

            ViewBag.RequestId = requestId;
            return View(workLogs);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int requestId)
        {
            var request = await _context.MaintenanceRequests
                .Include(r => r.Assignments)
                .ThenInclude(a => a.Technician)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null) return NotFound();

            ViewBag.Request = request;
            ViewBag.Technicians = await _context.Technicians
                .Include(t => t.User)
                .Where(t => t.IsActive)
                .ToListAsync();

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(int requestId, string workPerformed, decimal? hoursSpent, int? technicianId, 
            DateTime? startedAt, DateTime? completedAt, decimal? laborRate = null)
        {
            var userName = CurrentUserName;
            if (string.IsNullOrEmpty(userName)) return RedirectToAction("Login", "Account", new { area = "" });
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            var workLog = new WorkLog
            {
                RequestId = requestId,
                TechnicianId = technicianId,
                WorkPerformed = workPerformed,
                HoursSpent = hoursSpent,
                LaborRate = laborRate,
                StartedAt = startedAt,
                CompletedAt = completedAt,
                CreatedAt = DateTime.UtcNow
            };

            _context.WorkLogs.Add(workLog);
            await _context.SaveChangesAsync();

            var request = await _context.MaintenanceRequests.FindAsync(requestId);
            if (request != null && hoursSpent.HasValue && laborRate.HasValue)
            {
                request.LaborCost = (request.LaborCost ?? 0) + (hoursSpent.Value * laborRate.Value);
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Work log added successfully!";
            return RedirectToAction(nameof(Index), new { requestId });
        }
    }
}
