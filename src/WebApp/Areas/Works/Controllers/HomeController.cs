using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZEGU.Core.Entities.Maintenance;
using ZEGU.Core.Entities.Shared;
using ZEGU.Core.Enums;
using ZEGU.Core.Entities.Identity;
using ZEGU.Infrastructure.Data;
using ZEGU.Infrastructure.Services;
using ZEGU.WebApp.ViewModels.Works;

namespace ZEGU.WebApp.Areas.Works.Controllers
{
    [Area("Works")]
    [Authorize(Policy = "RequireWorksAccess")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly NotificationService _notificationService;
        private readonly SLAMonitoringService _slaMonitoringService;

        public HomeController(ApplicationDbContext context, NotificationService notificationService, SLAMonitoringService slaMonitoringService)
        {
            _context = context;
            _notificationService = notificationService;
            _slaMonitoringService = slaMonitoringService;
        }

        public async Task<IActionResult> Index()
        {
            var stats = new WorksDashboardViewModel
            {
                TotalRequests = await _context.MaintenanceRequests.CountAsync(r => r.IsActive),
                PendingRequests = await _context.MaintenanceRequests.CountAsync(r => r.IsActive && r.Status == MaintenanceRequestStatus.Submitted),
                InProgressRequests = await _context.MaintenanceRequests.CountAsync(r => r.IsActive && r.Status == MaintenanceRequestStatus.InProgress),
                CompletedRequests = await _context.MaintenanceRequests.CountAsync(r => r.IsActive && r.Status == MaintenanceRequestStatus.Completed),
                EmergencyRequests = await _context.MaintenanceRequests.CountAsync(r => r.IsActive && r.Priority == RequestPriority.Emergency)
            };

            var recentRequests = await _context.MaintenanceRequests
                .Include(r => r.User)
                .Include(r => r.Category)
                .Include(r => r.Location)
                .Include(r => r.Location.Building)
                .Where(r => r.IsActive)
                .OrderByDescending(r => r.CreatedAt)
                .Take(20)
                .ToListAsync();

            stats.RecentRequests = recentRequests;
            return View(stats);
        }

        public async Task<IActionResult> AllRequests(string? search = null, string? status = null, string? priority = null, 
            int? categoryId = null, int? technicianId = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.MaintenanceRequests
                .Include(r => r.User)
                .Include(r => r.Category)
                .Include(r => r.Location)
                .Include(r => r.Location.Building)
                .Include(r => r.Assignments)
                .ThenInclude(a => a.Technician)
                .ThenInclude(t => t.User)
                .Where(r => r.IsActive)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(r => r.RequestNumber.Contains(search) ||
                                        r.Title.Contains(search) ||
                                        r.Description.Contains(search) ||
                                        r.User.FirstName.Contains(search) ||
                                        r.User.LastName.Contains(search));
            }

            if (!string.IsNullOrEmpty(status))
            {
                if (Enum.TryParse<MaintenanceRequestStatus>(status, out var statusEnum))
                {
                    query = query.Where(r => r.Status == statusEnum);
                }
            }

            if (!string.IsNullOrEmpty(priority))
            {
                if (Enum.TryParse<RequestPriority>(priority, out var priorityEnum))
                {
                    query = query.Where(r => r.Priority == priorityEnum);
                }
            }

            if (categoryId.HasValue)
            {
                query = query.Where(r => r.CategoryId == categoryId.Value);
            }

            if (technicianId.HasValue)
            {
                query = query.Where(r => r.Assignments.Any(a => a.TechnicianId == technicianId.Value && a.IsActive));
            }

            if (startDate.HasValue)
            {
                var start = DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc);
                query = query.Where(r => r.CreatedAt >= start);
            }

            if (endDate.HasValue)
            {
                var end = DateTime.SpecifyKind(endDate.Value.AddDays(1), DateTimeKind.Utc);
                query = query.Where(r => r.CreatedAt <= end);
            }

            var requests = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
            ViewBag.Categories = await _context.MaintenanceCategories.Where(c => c.IsActive).ToListAsync();
            ViewBag.Technicians = await _context.Technicians.Include(t => t.User).Where(t => t.IsActive).ToListAsync();
            ViewBag.Statuses = Enum.GetValues(typeof(MaintenanceRequestStatus)).Cast<MaintenanceRequestStatus>().ToList();
            ViewBag.Priorities = Enum.GetValues(typeof(RequestPriority)).Cast<RequestPriority>().ToList();
            ViewBag.SearchTerm = search;
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            ViewBag.SelectedCategoryId = categoryId;
            ViewBag.SelectedTechnicianId = technicianId;
            return View(requests);
        }

        public async Task<IActionResult> ExportRequestsCsv(string? search = null, string? status = null, string? priority = null, 
            int? categoryId = null, int? technicianId = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.MaintenanceRequests
                .Include(r => r.User)
                .Include(r => r.Category)
                .Include(r => r.Location)
                .Where(r => r.IsActive)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(r => r.RequestNumber.Contains(search) || r.Title.Contains(search) || r.User.FirstName.Contains(search));
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<MaintenanceRequestStatus>(status, out var statusEnum))
                query = query.Where(r => r.Status == statusEnum);
            if (!string.IsNullOrEmpty(priority) && Enum.TryParse<RequestPriority>(priority, out var priorityEnum))
                query = query.Where(r => r.Priority == priorityEnum);
            if (categoryId.HasValue)
                query = query.Where(r => r.CategoryId == categoryId.Value);
            if (technicianId.HasValue)
                query = query.Where(r => r.Assignments.Any(a => a.TechnicianId == technicianId.Value && a.IsActive));
            if (startDate.HasValue)
            {
                var start = DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc);
                query = query.Where(r => r.CreatedAt >= start);
            }
            if (endDate.HasValue)
            {
                var end = DateTime.SpecifyKind(endDate.Value.AddDays(1), DateTimeKind.Utc);
                query = query.Where(r => r.CreatedAt <= end);
            }

            var requests = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();

            var csv = new System.Text.StringBuilder();
            csv.AppendLine("RequestNumber,Title,Category,Requester,Email,Location,Status,Priority,LaborCost,MaterialCost,TotalCost,CreatedAt,CompletedAt");

            foreach (var r in requests)
            {
                csv.AppendLine($"{r.RequestNumber},\"{r.Title}\",{r.Category.CategoryName},\"{r.User.FirstName} {r.User.LastName}\",{r.User.Email},{r.Location.RoomNumber},{r.Status},{r.Priority},{r.LaborCost},{r.MaterialCost},{r.TotalCost},{r.CreatedAt:yyyy-MM-dd},{r.CompletedAt:yyyy-MM-dd}");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"requests-{DateTime.UtcNow:yyyyMMdd}.csv");
        }

        public async Task<IActionResult> AssignTechnician(int id)
        {
            var request = await _context.MaintenanceRequests
                .Include(r => r.Category)
                .Include(r => r.Location)
                .FirstOrDefaultAsync(r => r.Id == id && r.IsActive);

            if (request == null) return NotFound();

            var technicians = await _context.Technicians
                .Include(t => t.User)
                .Where(t => t.IsActive && t.IsAvailable)
                .ToListAsync();

            ViewBag.Request = request;
            ViewBag.Technicians = technicians;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AssignTechnician(int id, int technicianId, string? notes = null)
        {
            var request = await _context.MaintenanceRequests.FindAsync(id);
            if (request == null) return NotFound();

            var technician = await _context.Technicians.FindAsync(technicianId);
            if (technician == null) return NotFound();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            request.Status = MaintenanceRequestStatus.Assigned;

            _context.Assignments.Add(new Assignment
            {
                RequestId = id,
                TechnicianId = technicianId,
                AssignedById = user.Id,
                Notes = notes
            });

            _context.RequestStatusHistory.Add(new RequestStatusHistory
            {
                RequestId = id,
                OldStatus = MaintenanceRequestStatus.Submitted,
                NewStatus = MaintenanceRequestStatus.Assigned,
                ChangedById = user.Id,
                Comments = $"Assigned to {technician.TechnicianType}"
            });

            await _context.SaveChangesAsync();

            if (technician.UserId != null)
            {
                var techUser = await _context.Users.FindAsync(technician.UserId);
                if (techUser != null)
                {
                    await _notificationService.CreateNotificationAsync(techUser.Id, 
                        "New Assignment", 
                        $"You have been assigned to request {request.RequestNumber}: {request.Title}", 
                        request.Id);
                }
            }

            await _notificationService.CreateNotificationAsync(request.UserId, 
                "Request Assigned", 
                $"Your request {request.RequestNumber} has been assigned to {technician.TechnicianType}", 
                request.Id);

            TempData["SuccessMessage"] = "Technician assigned successfully!";
            return RedirectToAction(nameof(AllRequests));
        }

        public async Task<IActionResult> UpdateStatus(int id)
        {
            var request = await _context.MaintenanceRequests
                .Include(r => r.Category)
                .Include(r => r.Location)
                .Include(r => r.Location.Building)
                .Include(r => r.Assignments)
                .ThenInclude(a => a.Technician)
                .FirstOrDefaultAsync(r => r.Id == id && r.IsActive);
            
            if (request == null) return NotFound();
            return View(request);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, MaintenanceRequestStatus newStatus, string? comments = null, 
            decimal? laborCost = null, decimal? materialCost = null, decimal? otherCost = null, 
            IFormFile? afterPhoto = null)
        {
            var request = await _context.MaintenanceRequests.FindAsync(id);
            if (request == null) return NotFound();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            var oldStatus = request.Status;
            request.Status = newStatus;
            request.UpdatedAt = DateTime.UtcNow;

            if (laborCost.HasValue)
                request.LaborCost = laborCost;
            if (materialCost.HasValue)
                request.MaterialCost = materialCost;
            if (otherCost.HasValue)
                request.OtherCost = otherCost;

            if (newStatus == MaintenanceRequestStatus.Completed)
            {
                request.CompletedAt = DateTime.UtcNow;

                if (afterPhoto != null && afterPhoto.Length > 0)
                {
                    var uploadsFolder = Path.Combine("wwwroot", "uploads", "after-photos");
                    Directory.CreateDirectory(uploadsFolder);

                    var fileName = $"after-{id}-{Guid.NewGuid()}{Path.GetExtension(afterPhoto.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await afterPhoto.CopyToAsync(stream);
                    }

                    request.AfterPhotoPath = $"/uploads/after-photos/{fileName}";

                    _context.RequestAttachments.Add(new RequestAttachment
                    {
                        RequestId = id,
                        FileName = afterPhoto.FileName,
                        FilePath = request.AfterPhotoPath,
                        FileSize = afterPhoto.Length,
                        FileType = afterPhoto.ContentType,
                        UploadedById = user.Id,
                        IsBeforePhoto = false
                    });
                }
            }
            if (newStatus == MaintenanceRequestStatus.Closed)
            {
                request.ClosedAt = DateTime.UtcNow;
            }

            _context.RequestStatusHistory.Add(new RequestStatusHistory
            {
                RequestId = id,
                OldStatus = oldStatus,
                NewStatus = newStatus,
                ChangedById = user.Id,
                Comments = comments
            });

            await _context.SaveChangesAsync();

            await _notificationService.CreateNotificationAsync(request.UserId,
                "Request Status Updated",
                $"Your request {request.RequestNumber} status has been changed to {newStatus}",
                request.Id);

            TempData["SuccessMessage"] = "Request status updated successfully!";
            return RedirectToAction(nameof(AllRequests));
        }

        [HttpGet]
        public async Task<IActionResult> Reject(int id)
        {
            var request = await _context.MaintenanceRequests
                .Include(r => r.Category)
                .Include(r => r.Location)
                .Include(r => r.Location.Building)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id && r.IsActive);

            if (request == null) return NotFound();
            return View(request);
        }

        [HttpPost]
        public async Task<IActionResult> Reject(int id, string rejectionReason)
        {
            var request = await _context.MaintenanceRequests.FindAsync(id);
            if (request == null) return NotFound();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            request.Status = MaintenanceRequestStatus.Rejected;
            request.RejectionReason = rejectionReason;
            request.UpdatedAt = DateTime.UtcNow;

            _context.RequestStatusHistory.Add(new RequestStatusHistory
            {
                RequestId = id,
                OldStatus = MaintenanceRequestStatus.Submitted,
                NewStatus = MaintenanceRequestStatus.Rejected,
                ChangedById = user.Id,
                Comments = $"Rejected: {rejectionReason}"
            });

            await _context.SaveChangesAsync();

            await _notificationService.CreateNotificationAsync(request.UserId, 
                "Request Rejected", 
                $"Your request {request.RequestNumber} has been rejected. Reason: {rejectionReason}", 
                request.Id);

            TempData["SuccessMessage"] = "Request rejected successfully!";
            return RedirectToAction(nameof(AllRequests));
        }

        public async Task<IActionResult> Details(int id)
        {
            var request = await _context.MaintenanceRequests
                .Include(r => r.User)
                .Include(r => r.Category)
                .Include(r => r.Location)
                .Include(r => r.Location.Building)
                .Include(r => r.StatusHistory)
                .Include(r => r.Comments)
                .Include(r => r.Attachments)
                .Include(r => r.Assignments)
                .ThenInclude(a => a.Technician)
                .FirstOrDefaultAsync(r => r.Id == id && r.IsActive);
            
            if (request == null) return NotFound();
            return View(request);
        }

        public async Task<IActionResult> SLADashboard()
        {
            var overdueRequests = await _slaMonitoringService.GetOverdueRequestsAsync();
            var totalActive = await _context.MaintenanceRequests.CountAsync(r => r.IsActive);
            var overdueCount = overdueRequests.Count;

            var slaStats = new SLADashboardViewModel
            {
                TotalActiveRequests = totalActive,
                OverdueRequests = overdueCount,
                OnTrackRequests = totalActive - overdueCount,
                OverdueRequestList = overdueRequests
            };

            return View(slaStats);
        }

        [HttpGet]
        public async Task<IActionResult> UpdatePriority(int id)
        {
            var request = await _context.MaintenanceRequests
                .Include(r => r.Category)
                .Include(r => r.Location)
                .Include(r => r.Location.Building)
                .FirstOrDefaultAsync(r => r.Id == id && r.IsActive);
            
            if (request == null) return NotFound();
            return View(request);
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePriority(int id, RequestPriority newPriority, string? comments = null)
        {
            var request = await _context.MaintenanceRequests.FindAsync(id);
            if (request == null) return NotFound();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            var oldPriority = request.Priority;
            request.Priority = newPriority;
            request.UpdatedAt = DateTime.UtcNow;

            _context.RequestStatusHistory.Add(new RequestStatusHistory
            {
                RequestId = id,
                OldStatus = (MaintenanceRequestStatus)(int)oldPriority,
                NewStatus = (MaintenanceRequestStatus)(int)newPriority,
                ChangedById = user.Id,
                Comments = $"Priority changed from {oldPriority} to {newPriority}" + (string.IsNullOrEmpty(comments) ? "" : $": {comments}")
            });

            await _context.SaveChangesAsync();

            await _notificationService.CreateNotificationAsync(request.UserId, 
                "Request Priority Updated", 
                $"Your request {request.RequestNumber} priority has been changed to {newPriority}", 
                request.Id);

            TempData["SuccessMessage"] = "Request priority updated successfully!";
            return RedirectToAction(nameof(AllRequests));
        }
    }
}
