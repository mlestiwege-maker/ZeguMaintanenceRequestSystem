using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ZEGU.Core.Entities.Maintenance;
using ZEGU.Core.Enums;
using ZEGU.Infrastructure.Data;
using ZEGU.Infrastructure.Services;
using ZEGU.WebApp.Services;
using ZEGU.WebApp.ViewModels.Technicians;

namespace ZEGU.WebApp.Areas.Technicians.Controllers
{
    [Area("Technicians")]
    [Authorize(Policy = "RequireTechnician")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly NotificationService _notificationService;
        private readonly EmailService _emailService;
        private readonly SmsService _smsService;
        private readonly WhatsAppService _whatsAppService;

        public HomeController(ApplicationDbContext context, NotificationService notificationService, EmailService emailService, SmsService smsService, WhatsAppService whatsAppService)
        {
            _context = context;
            _notificationService = notificationService;
            _emailService = emailService;
            _smsService = smsService;
            _whatsAppService = whatsAppService;
        }

        private Task<Technician?> GetOwnTechnicianProfileAsync()
        {
            var currentUserName = User.Identity?.Name;
            return _context.Technicians.Include(t => t.User).FirstOrDefaultAsync(t => t.User!.UserName == currentUserName);
        }

        public async Task<IActionResult> Index()
        {
            var technicianProfile = await GetOwnTechnicianProfileAsync();
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
            var technicianProfile = await GetOwnTechnicianProfileAsync();
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
                .Include(r => r.MaterialRequests)
                .ThenInclude(m => m.Material)
                .FirstOrDefaultAsync(r => r.Id == id && r.IsActive);

            if (request == null) return NotFound();

            // View-only: technicians may only see requests actually assigned to them.
            var isAssignedToMe = request.Assignments.Any(a => a.IsActive && a.TechnicianId == technicianProfile.Id);
            if (!isAssignedToMe) return Forbid();

            ViewBag.TechnicianId = technicianProfile.Id;
            ViewBag.Materials = new SelectList(await _context.Materials.Where(m => m.IsActive).OrderBy(m => m.MaterialName).ToListAsync(), "Id", "MaterialName");
            ViewBag.CanMarkComplete = request.Status == MaintenanceRequestStatus.Assigned || request.Status == MaintenanceRequestStatus.InProgress;

            return View(request);
        }

        [HttpPost]
        public async Task<IActionResult> RequestMaterial(int requestId, int materialId, decimal quantity, string? notes)
        {
            var technicianProfile = await GetOwnTechnicianProfileAsync();
            if (technicianProfile == null) return NotFound();

            var request = await _context.MaintenanceRequests
                .Include(r => r.Assignments)
                .FirstOrDefaultAsync(r => r.Id == requestId && r.IsActive);
            if (request == null) return NotFound();

            var isAssignedToMe = request.Assignments.Any(a => a.IsActive && a.TechnicianId == technicianProfile.Id);
            if (!isAssignedToMe) return Forbid();

            if (quantity <= 0)
            {
                TempData["ErrorMessage"] = "Quantity must be greater than zero.";
                return RedirectToAction(nameof(Details), new { id = requestId });
            }

            var material = await _context.Materials.FirstOrDefaultAsync(m => m.Id == materialId && m.IsActive);
            if (material == null)
            {
                TempData["ErrorMessage"] = "Selected material is invalid.";
                return RedirectToAction(nameof(Details), new { id = requestId });
            }

            _context.MaterialRequests.Add(new MaterialRequest
            {
                RequestId = requestId,
                TechnicianId = technicianProfile.Id,
                MaterialId = materialId,
                QuantityRequested = quantity,
                Notes = notes,
                Status = MaterialRequestStatus.Pending
            });
            await _context.SaveChangesAsync();

            var techName = technicianProfile.User != null ? $"{technicianProfile.User.FirstName} {technicianProfile.User.LastName}" : "A technician";
            var message = $"{techName} requested {quantity} {material.Unit ?? "unit(s)"} of {material.MaterialName} for request {request.RequestNumber}.";
            await NotifyWorksAsync("Material Request", message, requestId);

            TempData["SuccessMessage"] = $"Material request for {material.MaterialName} submitted for approval.";
            return RedirectToAction(nameof(Details), new { id = requestId });
        }

        [HttpPost]
        public async Task<IActionResult> MarkComplete(int requestId, string workPerformed)
        {
            var technicianProfile = await GetOwnTechnicianProfileAsync();
            if (technicianProfile == null) return NotFound();

            var request = await _context.MaintenanceRequests
                .Include(r => r.Assignments)
                .FirstOrDefaultAsync(r => r.Id == requestId && r.IsActive);
            if (request == null) return NotFound();

            var assignment = request.Assignments.FirstOrDefault(a => a.IsActive && a.TechnicianId == technicianProfile.Id);
            if (assignment == null) return Forbid();

            if (request.Status != MaintenanceRequestStatus.Assigned && request.Status != MaintenanceRequestStatus.InProgress)
            {
                TempData["ErrorMessage"] = "This request cannot be marked complete from its current status.";
                return RedirectToAction(nameof(Details), new { id = requestId });
            }

            if (string.IsNullOrWhiteSpace(workPerformed))
            {
                TempData["ErrorMessage"] = "Please describe the work performed before marking this complete.";
                return RedirectToAction(nameof(Details), new { id = requestId });
            }

            var oldStatus = request.Status;
            request.Status = MaintenanceRequestStatus.Completed;
            request.CompletedAt = DateTime.UtcNow;
            request.UpdatedAt = DateTime.UtcNow;

            _context.WorkLogs.Add(new WorkLog
            {
                RequestId = requestId,
                TechnicianId = technicianProfile.Id,
                AssignmentId = assignment.Id,
                WorkPerformed = workPerformed,
                CompletedAt = DateTime.UtcNow
            });

            _context.RequestStatusHistory.Add(new RequestStatusHistory
            {
                RequestId = requestId,
                OldStatus = oldStatus,
                NewStatus = MaintenanceRequestStatus.Completed,
                ChangedById = technicianProfile.UserId,
                Comments = $"Marked complete by technician: {workPerformed}"
            });

            await _context.SaveChangesAsync();

            await NotifyWorksAsync("Job Marked Complete - Awaiting Review",
                $"Request {request.RequestNumber} has been marked complete by the assigned technician and is awaiting your review.",
                requestId);

            TempData["SuccessMessage"] = "Marked as complete. A Works Officer or Manager will review and confirm.";
            return RedirectToAction(nameof(Details), new { id = requestId });
        }

        private async Task NotifyWorksAsync(string title, string message, int requestId)
        {
            var worksUsers = await _context.Users
                .Where(u => (u.Role == UserRole.WorksOfficer || u.Role == UserRole.Manager) && u.IsActive)
                .ToListAsync();

            foreach (var worksUser in worksUsers)
            {
                await _notificationService.CreateNotificationAsync(worksUser.Id, title, message, requestId);

                if (!string.IsNullOrEmpty(worksUser.Email))
                {
                    _ = _emailService.SendEmailAsync(worksUser.Email, title, $"<p>{message}</p>", isHtml: true);
                }

                if (!string.IsNullOrEmpty(worksUser.PhoneNumber))
                {
                    _ = _smsService.SendSmsAsync(worksUser.PhoneNumber, message);
                    _ = _whatsAppService.SendWhatsAppAsync(worksUser.PhoneNumber, message);
                }
            }
        }
    }
}
