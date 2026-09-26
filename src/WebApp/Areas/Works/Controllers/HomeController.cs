using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZEGU.Core.Entities.Maintenance;
using ZEGU.Core.Entities.Shared;
using ZEGU.Core.Enums;
using ZEGU.Core.Entities.Identity;
using ZEGU.Infrastructure.Data;
using ZEGU.Infrastructure.Services;
using ZEGU.WebApp.Services;
using ZEGU.WebApp.ViewModels.Works;
using System.Security.Claims;

namespace ZEGU.WebApp.Areas.Works.Controllers
{
    [Area("Works")]
    [Authorize(Policy = "RequireWorksAccess")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly NotificationService _notificationService;
        private readonly SLAMonitoringService _slaMonitoringService;
        private readonly AuditService _auditService;
        private readonly EmailService _emailService;
        private readonly SmsService _smsService;
        private readonly WhatsAppService _whatsAppService;

        public HomeController(ApplicationDbContext context, NotificationService notificationService, SLAMonitoringService slaMonitoringService, AuditService auditService, EmailService emailService, SmsService smsService, WhatsAppService whatsAppService)
        {
            _context = context;
            _notificationService = notificationService;
            _slaMonitoringService = slaMonitoringService;
            _auditService = auditService;
            _emailService = emailService;
            _smsService = smsService;
            _whatsAppService = whatsAppService;
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
            int? categoryId = null, int? technicianId = null, DateTime? startDate = null, DateTime? endDate = null,
            int pageNumber = 1, int pageSize = 25)
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

            pageSize = Math.Clamp(pageSize, 1, 100);
            pageNumber = Math.Max(1, pageNumber);

            var orderedQuery = query.OrderByDescending(r => r.CreatedAt);
            var totalCount = await orderedQuery.CountAsync();
            var requests = await orderedQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Categories = await _context.MaintenanceCategories.Where(c => c.IsActive).ToListAsync();
            ViewBag.Technicians = await _context.Technicians.Include(t => t.User).Where(t => t.IsActive).ToListAsync();
            ViewBag.Statuses = Enum.GetValues(typeof(MaintenanceRequestStatus)).Cast<MaintenanceRequestStatus>().ToList();
            ViewBag.Priorities = Enum.GetValues(typeof(RequestPriority)).Cast<RequestPriority>().ToList();
            ViewBag.SearchTerm = search;
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            ViewBag.SelectedCategoryId = categoryId;
            ViewBag.SelectedTechnicianId = technicianId;

            return View(new ZEGU.WebApp.ViewModels.PagedResult<ZEGU.Core.Entities.Maintenance.MaintenanceRequest>
            {
                Items = requests,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            });
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

            var currentUserName = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == currentUserName);
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

            await _auditService.LogAsync(user.Id, user.UserName, "AssignTechnician", "MaintenanceRequest",
                entityId: request.Id, newValues: new { TechnicianId = technicianId, technician.TechnicianType },
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

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

            await SendRequesterStatusEmailAsync(request, "RequestAssigned",
                $"A {technician.TechnicianType} has been assigned to your request {request.RequestNumber} and will be in touch soon.");

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

            var currentUserName = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == currentUserName);
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
                    if (afterPhoto.Length > 5 * 1024 * 1024)
                    {
                        TempData["ErrorMessage"] = "File size must be less than 5MB";
                        return RedirectToAction(nameof(UpdateStatus), new { id });
                    }

                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
                    var afterPhotoExtension = Path.GetExtension(afterPhoto.FileName).ToLower();
                    if (!allowedExtensions.Contains(afterPhotoExtension))
                    {
                        TempData["ErrorMessage"] = "Only image files are allowed (jpg, jpeg, png, gif, bmp)";
                        return RedirectToAction(nameof(UpdateStatus), new { id });
                    }

                    var uploadsFolder = Path.Combine("wwwroot", "uploads", "after-photos");
                    Directory.CreateDirectory(uploadsFolder);

                    var fileName = $"after-{id}-{Guid.NewGuid()}{afterPhotoExtension}";
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

            await _auditService.LogAsync(user.Id, user.UserName, "UpdateStatus", "MaintenanceRequest",
                entityId: request.Id, oldValues: new { Status = oldStatus }, newValues: new { Status = newStatus, comments },
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

            await _notificationService.CreateNotificationAsync(request.UserId,
                "Request Status Updated",
                $"Your request {request.RequestNumber} status has been changed to {newStatus}",
                request.Id);

            await SendRequesterStatusEmailAsync(request, "RequestStatusUpdated",
                $"Your request {request.RequestNumber} status has been changed to <strong>{newStatus}</strong>." +
                (string.IsNullOrEmpty(comments) ? "" : $"<br/>Notes: {comments}"));

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

            var currentUserName = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == currentUserName);
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

            await _auditService.LogAsync(user.Id, user.UserName, "Reject", "MaintenanceRequest",
                entityId: request.Id, newValues: new { rejectionReason },
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

            await _notificationService.CreateNotificationAsync(request.UserId,
                "Request Rejected",
                $"Your request {request.RequestNumber} has been rejected. Reason: {rejectionReason}",
                request.Id);

            await SendRequesterStatusEmailAsync(request, "RequestRejected",
                $"Your request {request.RequestNumber} has been rejected.<br/>Reason: {rejectionReason}");

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
                .Include(r => r.Asset)
                .Include(r => r.StatusHistory)
                .Include(r => r.Comments)
                .ThenInclude(c => c.User)
                .Include(r => r.Attachments)
                .Include(r => r.Assignments)
                .ThenInclude(a => a.Technician)
                .FirstOrDefaultAsync(r => r.Id == id && r.IsActive);

            if (request == null) return NotFound();

            ViewBag.LocationAssets = await _context.Assets
                .Where(a => a.LocationId == request.LocationId && a.IsActive)
                .OrderBy(a => a.AssetName)
                .ToListAsync();

            return View(request);
        }

        [HttpPost]
        public async Task<IActionResult> SetAsset(int requestId, int? assetId)
        {
            var request = await _context.MaintenanceRequests.FindAsync(requestId);
            if (request == null) return NotFound();

            if (assetId.HasValue)
            {
                var asset = await _context.Assets.FirstOrDefaultAsync(a => a.Id == assetId.Value && a.IsActive);
                if (asset == null)
                {
                    TempData["ErrorMessage"] = "Selected asset is invalid.";
                    return RedirectToAction(nameof(Details), new { id = requestId });
                }
            }

            request.AssetId = assetId;
            request.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = assetId.HasValue ? "Asset linked to this request." : "Asset link removed.";
            return RedirectToAction(nameof(Details), new { id = requestId });
        }

        [HttpPost]
        public async Task<IActionResult> AddComment(int requestId, string commentText, bool isInternal = false)
        {
            var currentUserName = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == currentUserName);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            var request = await _context.MaintenanceRequests
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == requestId && r.IsActive);
            if (request == null) return NotFound();

            _context.RequestComments.Add(new RequestComment
            {
                RequestId = requestId,
                UserId = user.Id,
                CommentText = commentText,
                IsInternal = isInternal
            });
            await _context.SaveChangesAsync();

            if (!isInternal)
            {
                await _notificationService.CreateNotificationAsync(request.UserId,
                    "New Reply on Your Request",
                    $"{user.FirstName} {user.LastName} replied to your request {request.RequestNumber}: {commentText}",
                    request.Id);

                if (!string.IsNullOrEmpty(request.User.Email))
                {
                    var template = await _notificationService.RenderTemplateAsync("RequestReplied", new Dictionary<string, string>
                    {
                        ["RequestNumber"] = request.RequestNumber,
                        ["Title"] = request.Title,
                        ["UserName"] = $"{request.User.FirstName} {request.User.LastName}",
                        ["ReplyBy"] = $"{user.FirstName} {user.LastName}",
                        ["ReplyText"] = commentText
                    });

                    if (template.HasValue)
                    {
                        _ = _emailService.SendEmailAsync(request.User.Email, template.Value.Subject, template.Value.Body, isHtml: true);
                    }
                    else
                    {
                        var subject = $"New reply on request {request.RequestNumber}";
                        var body = $@"<p>Hi {request.User.FirstName},</p>
                            <p><strong>{user.FirstName} {user.LastName}</strong> replied to your maintenance request <strong>{request.RequestNumber}</strong> ({request.Title}):</p>
                            <blockquote style='border-left:3px solid #0d6efd;padding-left:12px;color:#333;'>{commentText}</blockquote>
                            <p>Log in to the system to view the full conversation.</p>";
                        _ = _emailService.SendEmailAsync(request.User.Email, subject, body, isHtml: true);
                    }
                }

                if (!string.IsNullOrEmpty(request.User.PhoneNumber))
                {
                    var smsBody = $"Reply on request {request.RequestNumber}: {commentText}";
                    _ = _smsService.SendSmsAsync(request.User.PhoneNumber, smsBody);
                    _ = _whatsAppService.SendWhatsAppAsync(request.User.PhoneNumber, smsBody);
                }
            }

            TempData["SuccessMessage"] = "Reply added successfully";
            return RedirectToAction(nameof(Details), new { id = requestId });
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

            var currentUserName = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == currentUserName);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            var oldPriority = request.Priority;
            request.Priority = newPriority;
            request.UpdatedAt = DateTime.UtcNow;

            _context.RequestStatusHistory.Add(new RequestStatusHistory
            {
                RequestId = id,
                OldStatus = request.Status,
                NewStatus = request.Status,
                ChangedById = user.Id,
                Comments = $"Priority changed from {oldPriority} to {newPriority}" + (string.IsNullOrEmpty(comments) ? "" : $": {comments}")
            });

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(user.Id, user.UserName, "UpdatePriority", "MaintenanceRequest",
                entityId: request.Id, oldValues: new { Priority = oldPriority }, newValues: new { Priority = newPriority, comments },
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

            await _notificationService.CreateNotificationAsync(request.UserId,
                "Request Priority Updated",
                $"Your request {request.RequestNumber} priority has been changed to {newPriority}", 
                request.Id);

            TempData["SuccessMessage"] = "Request priority updated successfully!";
            return RedirectToAction(nameof(AllRequests));
        }

        private async Task SendRequesterStatusEmailAsync(MaintenanceRequest request, string templateName, string fallbackHtmlMessage)
        {
            var requester = await _context.Users.FindAsync(request.UserId);
            if (requester == null) return;

            if (!string.IsNullOrEmpty(requester.Email))
            {
                var template = await _notificationService.RenderTemplateAsync(templateName, new Dictionary<string, string>
                {
                    ["RequestNumber"] = request.RequestNumber,
                    ["Title"] = request.Title,
                    ["UserName"] = $"{requester.FirstName} {requester.LastName}",
                    ["Status"] = request.Status.ToString()
                });

                if (template.HasValue)
                {
                    _ = _emailService.SendEmailAsync(requester.Email, template.Value.Subject, template.Value.Body, isHtml: true);
                }
                else
                {
                    var subject = $"Update on your request {request.RequestNumber}";
                    var body = $"<p>Hi {requester.FirstName},</p><p>{fallbackHtmlMessage}</p>";
                    _ = _emailService.SendEmailAsync(requester.Email, subject, body, isHtml: true);
                }
            }

            if (!string.IsNullOrEmpty(requester.PhoneNumber))
            {
                _ = _smsService.SendMaintenanceNotificationAsync(requester.PhoneNumber, requester.FirstName, request.RequestNumber, request.Status.ToString());
                _ = _whatsAppService.SendMaintenanceNotificationAsync(requester.PhoneNumber, requester.FirstName, request.RequestNumber, request.Status.ToString());
            }
        }
    }
}
