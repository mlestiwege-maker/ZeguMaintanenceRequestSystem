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
using ZEGU.WebApp.ViewModels.Requests;

namespace ZEGU.WebApp.Areas.Requests.Controllers
{
    [Area("Requests")]
    [Authorize(Policy = "RequireStudentOrStaff")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly NotificationService _notificationService;
        private readonly EmailService _emailService;
        private readonly SmsService _smsService;
        private readonly WhatsAppService _whatsAppService;
        private readonly IConfiguration _configuration;

        public HomeController(ApplicationDbContext context, NotificationService notificationService, EmailService emailService, SmsService smsService, WhatsAppService whatsAppService, IConfiguration configuration)
        {
            _context = context;
            _notificationService = notificationService;
            _emailService = emailService;
            _smsService = smsService;
            _whatsAppService = whatsAppService;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index()
        {
            var currentUserName = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == currentUserName);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            var myRequests = await _context.MaintenanceRequests
                .Include(r => r.Category)
                .Include(r => r.Location)
                .Include(r => r.Location.Building)
                .Where(r => r.UserId == user.Id && r.IsActive)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var stats = new RequestsDashboardViewModel
            {
                TotalRequests = myRequests.Count,
                PendingRequests = myRequests.Count(r => r.Status == MaintenanceRequestStatus.Submitted || r.Status == MaintenanceRequestStatus.Assigned),
                InProgressRequests = myRequests.Count(r => r.Status == MaintenanceRequestStatus.InProgress),
                CompletedRequests = myRequests.Count(r => r.Status == MaintenanceRequestStatus.Completed),
                RecentRequests = myRequests.Take(10).ToList()
            };

            return View(stats);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? assetId = null)
        {
            var model = new CreateRequestViewModel
            {
                Categories = await _context.MaintenanceCategories.Where(c => c.IsActive).ToListAsync(),
                Buildings = await _context.Buildings.Include(b => b.Campus).Where(b => b.IsActive).ToListAsync(),
                Departments = await _context.Departments.Where(d => d.IsActive).ToListAsync()
            };

            if (assetId.HasValue)
            {
                var asset = await _context.Assets
                    .Include(a => a.Location)
                    .FirstOrDefaultAsync(a => a.Id == assetId.Value && a.IsActive);

                if (asset != null)
                {
                    model.AssetId = asset.Id;
                    model.LocationId = asset.LocationId;
                    model.CategoryId = asset.CategoryId;
                    model.SelectedBuildingId = asset.Location.BuildingId;
                    model.Title = $"Issue with {asset.AssetName}";
                }
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateRequestViewModel? model)
        {
            if (model == null)
            {
                TempData["ErrorMessage"] = "Your upload was too large or the request could not be read. Please try again with smaller files.";
                return RedirectToAction(nameof(Create));
            }

            if (!ModelState.IsValid)
            {
                model.Categories = await _context.MaintenanceCategories.Where(c => c.IsActive).ToListAsync();
                model.Buildings = await _context.Buildings.Include(b => b.Campus).Where(b => b.IsActive).ToListAsync();
                model.Departments = await _context.Departments.Where(d => d.IsActive).ToListAsync();
                return View(model);
            }

            var currentUserName = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == currentUserName);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            if (!model.CategoryId.HasValue || !model.LocationId.HasValue)
            {
                ModelState.AddModelError("", "Please select a category and location");
                model.Categories = await _context.MaintenanceCategories.Where(c => c.IsActive).ToListAsync();
                model.Buildings = await _context.Buildings.Include(b => b.Campus).Where(b => b.IsActive).ToListAsync();
                model.Departments = await _context.Departments.Where(d => d.IsActive).ToListAsync();
                return View(model);
            }

            var category = await _context.MaintenanceCategories.FirstOrDefaultAsync(c => c.Id == model.CategoryId.Value);
            if (category == null)
            {
                ModelState.AddModelError("", "Selected category is invalid");
                model.Categories = await _context.MaintenanceCategories.Where(c => c.IsActive).ToListAsync();
                model.Buildings = await _context.Buildings.Include(b => b.Campus).Where(b => b.IsActive).ToListAsync();
                model.Departments = await _context.Departments.Where(d => d.IsActive).ToListAsync();
                return View(model);
            }

            int? validatedAssetId = null;
            if (model.AssetId.HasValue)
            {
                var asset = await _context.Assets.FirstOrDefaultAsync(a => a.Id == model.AssetId.Value && a.IsActive && a.LocationId == model.LocationId.Value);
                if (asset != null) validatedAssetId = asset.Id;
            }

            var maxFileSizeMb = _configuration.GetValue<int>("Uploads:MaxFileSize", 5);
            var maxFileSizeBytes = maxFileSizeMb * 1024L * 1024L;
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".pdf", ".doc", ".docx" };
            if (model.Photos != null)
            {
                foreach (var photo in model.Photos)
                {
                    if (photo.Length == 0) continue;

                    if (photo.Length > maxFileSizeBytes)
                    {
                        TempData["ErrorMessage"] = $"File size must be less than {maxFileSizeMb}MB";
                        model.Categories = await _context.MaintenanceCategories.Where(c => c.IsActive).ToListAsync();
                        model.Buildings = await _context.Buildings.Include(b => b.Campus).Where(b => b.IsActive).ToListAsync();
                        model.Departments = await _context.Departments.Where(d => d.IsActive).ToListAsync();
                        return View(model);
                    }

                    var fileExtension = Path.GetExtension(photo.FileName).ToLower();
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        TempData["ErrorMessage"] = "Only images (jpg, jpeg, png, gif, bmp) or documents (pdf, doc, docx) are allowed";
                        model.Categories = await _context.MaintenanceCategories.Where(c => c.IsActive).ToListAsync();
                        model.Buildings = await _context.Buildings.Include(b => b.Campus).Where(b => b.IsActive).ToListAsync();
                        model.Departments = await _context.Departments.Where(d => d.IsActive).ToListAsync();
                        return View(model);
                    }
                }
            }

            var request = new MaintenanceRequest
            {
                RequestNumber = $"MRS-{DateTime.UtcNow.Year}-TEMP",
                UserId = user.Id,
                DepartmentId = model.DepartmentId,
                CategoryId = model.CategoryId.Value,
                LocationId = model.LocationId.Value,
                AssetId = validatedAssetId,
                Title = model.Title,
                Description = model.Description,
                Priority = model.Priority,
                Status = MaintenanceRequestStatus.Submitted
            };

            _context.MaintenanceRequests.Add(request);
            await _context.SaveChangesAsync();

            request.RequestNumber = $"MRS-{DateTime.UtcNow.Year}-{request.Id:D6}";
            await _context.SaveChangesAsync();

            if (model.Photos != null && model.Photos.Count > 0)
            {
                var uploadsFolder = Path.Combine("wwwroot", "uploads", "maintenance", request.RequestNumber);
                Directory.CreateDirectory(uploadsFolder);

                foreach (var photo in model.Photos)
                {
                    if (photo.Length == 0) continue;

                    var fileExtension = Path.GetExtension(photo.FileName).ToLower();
                    var fileName = $"{Guid.NewGuid()}{fileExtension}";
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await photo.CopyToAsync(stream);
                    }

                    _context.RequestAttachments.Add(new RequestAttachment
                    {
                        RequestId = request.Id,
                        FileName = photo.FileName,
                        FilePath = $"/uploads/maintenance/{request.RequestNumber}/{fileName}",
                        FileSize = photo.Length,
                        FileType = photo.ContentType,
                        UploadedById = user.Id,
                        IsBeforePhoto = true
                    });
                }
                await _context.SaveChangesAsync();
            }

            await _notificationService.CreateNotificationForRoleAsync(UserRole.WorksOfficer, 
                "New Maintenance Request", 
                $"New request {request.RequestNumber} submitted by {user.FirstName} {user.LastName}", 
                request.Id);

            var worksUsers = await _context.Users
                .Where(u => (u.Role == UserRole.WorksOfficer || u.Role == UserRole.Manager) && u.IsActive)
                .ToListAsync();

            var template = await _notificationService.RenderTemplateAsync("RequestSubmitted", new Dictionary<string, string>
            {
                ["RequestNumber"] = request.RequestNumber,
                ["Title"] = request.Title,
                ["UserName"] = $"{user.FirstName} {user.LastName}",
                ["Status"] = "Submitted",
                ["Priority"] = request.Priority.ToString(),
                ["Date"] = request.CreatedAt.ToString("dd MMM yyyy")
            });

            foreach (var worksUser in worksUsers)
            {
                if (!string.IsNullOrEmpty(worksUser.Email))
                {
                    if (template.HasValue)
                    {
                        _ = _emailService.SendEmailAsync(worksUser.Email, template.Value.Subject, template.Value.Body, isHtml: true);
                    }
                    else
                    {
                        _ = _emailService.SendMaintenanceNotificationAsync(worksUser.Email, worksUser.FirstName, request.RequestNumber, "Submitted");
                    }
                }
            }

            if (!string.IsNullOrEmpty(user.Email))
            {
                var confirmationTemplate = await _notificationService.RenderTemplateAsync("RequestSubmittedConfirmation", new Dictionary<string, string>
                {
                    ["RequestNumber"] = request.RequestNumber,
                    ["Title"] = request.Title,
                    ["UserName"] = $"{user.FirstName} {user.LastName}",
                    ["Status"] = "Submitted",
                    ["Priority"] = request.Priority.ToString(),
                    ["Date"] = request.CreatedAt.ToString("dd MMM yyyy")
                });

                if (confirmationTemplate.HasValue)
                {
                    _ = _emailService.SendEmailAsync(user.Email, confirmationTemplate.Value.Subject, confirmationTemplate.Value.Body, isHtml: true);
                }
                else
                {
                    var subject = $"Request {request.RequestNumber} received";
                    var body = $@"<p>Hi {user.FirstName},</p>
                        <p>Your maintenance request has been successfully submitted and is now with our Works team.</p>
                        <ul>
                            <li><strong>Request #:</strong> {request.RequestNumber}</li>
                            <li><strong>Title:</strong> {request.Title}</li>
                            <li><strong>Priority:</strong> {request.Priority}</li>
                        </ul>
                        <p>We'll email you again as soon as there's an update or reply on this request.</p>";
                    _ = _emailService.SendEmailAsync(user.Email, subject, body, isHtml: true);
                }
            }

            if (!string.IsNullOrEmpty(user.PhoneNumber))
            {
                _ = _smsService.SendMaintenanceNotificationAsync(user.PhoneNumber, user.FirstName, request.RequestNumber, "Submitted");
                _ = _whatsAppService.SendMaintenanceNotificationAsync(user.PhoneNumber, user.FirstName, request.RequestNumber, "Submitted");
            }

            TempData["SuccessMessage"] = $"Maintenance request {request.RequestNumber} submitted successfully!";
            return RedirectToAction(nameof(MyRequests));
        }

        public async Task<IActionResult> MyRequests(string? search = null, string? status = null, int pageNumber = 1, int pageSize = 25)
        {
            var currentUserName = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == currentUserName);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            pageSize = Math.Clamp(pageSize, 1, 100);
            pageNumber = Math.Max(1, pageNumber);

            var query = _context.MaintenanceRequests
                .Include(r => r.Category)
                .Include(r => r.Location)
                .Include(r => r.Location.Building)
                .Where(r => r.UserId == user.Id && r.IsActive)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(r => r.RequestNumber.Contains(search) ||
                                        r.Title.Contains(search) ||
                                        r.Description.Contains(search));
            }

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<MaintenanceRequestStatus>(status, out var statusEnum))
            {
                query = query.Where(r => r.Status == statusEnum);
            }

            var orderedQuery = query.OrderByDescending(r => r.CreatedAt);

            ViewBag.SearchTerm = search;
            ViewBag.Statuses = Enum.GetValues(typeof(MaintenanceRequestStatus)).Cast<MaintenanceRequestStatus>().ToList();

            var totalCount = await orderedQuery.CountAsync();
            var requests = await orderedQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return View(new ZEGU.WebApp.ViewModels.PagedResult<ZEGU.Core.Entities.Maintenance.MaintenanceRequest>
            {
                Items = requests,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            });
        }

        public async Task<IActionResult> Details(int id)
        {
            var currentUserName = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == currentUserName);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            var request = await _context.MaintenanceRequests
                .Include(r => r.Category)
                .Include(r => r.Location)
                .Include(r => r.Location.Building)
                .Include(r => r.StatusHistory)
                .Include(r => r.Comments)
                .ThenInclude(c => c.User)
                .Include(r => r.Attachments)
                .Include(r => r.Assignments)
                .ThenInclude(a => a.Technician)
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == user.Id);

            if (request == null) return NotFound();
            return View(request);
        }

        [HttpPost]
        public async Task<IActionResult> AddComment(int requestId, string commentText)
        {
            var currentUserName = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == currentUserName);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            var request = await _context.MaintenanceRequests.FindAsync(requestId);
            if (request == null || request.UserId != user.Id) return NotFound();

            _context.RequestComments.Add(new RequestComment
            {
                RequestId = requestId,
                UserId = user.Id,
                CommentText = commentText,
                IsInternal = false
            });
            await _context.SaveChangesAsync();

            await _notificationService.CreateNotificationForRoleAsync(UserRole.WorksOfficer,
                "New Reply from Requester",
                $"{user.FirstName} {user.LastName} replied to request {request.RequestNumber}: {commentText}",
                request.Id);

            var worksUsers = await _context.Users
                .Where(u => (u.Role == UserRole.WorksOfficer || u.Role == UserRole.Manager) && u.IsActive)
                .ToListAsync();

            var replyTemplate = await _notificationService.RenderTemplateAsync("RequestCommentReceived", new Dictionary<string, string>
            {
                ["RequestNumber"] = request.RequestNumber,
                ["Title"] = request.Title,
                ["UserName"] = $"{user.FirstName} {user.LastName}",
                ["ReplyText"] = commentText
            });

            foreach (var worksUser in worksUsers)
            {
                if (string.IsNullOrEmpty(worksUser.Email)) continue;

                if (replyTemplate.HasValue)
                {
                    _ = _emailService.SendEmailAsync(worksUser.Email, replyTemplate.Value.Subject, replyTemplate.Value.Body, isHtml: true);
                }
                else
                {
                    var subject = $"New reply from requester on {request.RequestNumber}";
                    var body = $@"<p>Hi {worksUser.FirstName},</p>
                        <p><strong>{user.FirstName} {user.LastName}</strong> replied on maintenance request <strong>{request.RequestNumber}</strong> ({request.Title}):</p>
                        <blockquote style='border-left:3px solid #0d6efd;padding-left:12px;color:#333;'>{commentText}</blockquote>";
                    _ = _emailService.SendEmailAsync(worksUser.Email, subject, body, isHtml: true);
                }
            }

            TempData["SuccessMessage"] = "Comment added successfully";
            return RedirectToAction(nameof(Details), new { id = requestId });
        }

        [HttpPost]
        public async Task<IActionResult> SubmitFeedback(int requestId, int rating, string? comments, bool workSatisfactory)
        {
            var currentUserName = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == currentUserName);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            var request = await _context.MaintenanceRequests
                .FirstOrDefaultAsync(r => r.Id == requestId && r.UserId == user.Id && r.Status == MaintenanceRequestStatus.Completed);

            if (request == null) return NotFound();

            var existingFeedback = await _context.Feedbacks.FirstOrDefaultAsync(f => f.RequestId == requestId);
            if (existingFeedback != null)
            {
                ModelState.AddModelError("", "Feedback has already been submitted for this request");
                return RedirectToAction(nameof(Details), new { id = requestId });
            }

            _context.Feedbacks.Add(new Feedback
            {
                RequestId = requestId,
                UserId = user.Id,
                Rating = rating,
                Comments = comments,
                WorkSatisfactory = workSatisfactory
            });

            request.Status = MaintenanceRequestStatus.Verified;
            request.ClosedAt = DateTime.UtcNow;
            
            _context.RequestStatusHistory.Add(new RequestStatusHistory
            {
                RequestId = requestId,
                OldStatus = MaintenanceRequestStatus.Completed,
                NewStatus = MaintenanceRequestStatus.Verified,
                ChangedById = user.Id,
                Comments = "Work verified by requester"
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Thank you for your feedback! The request has been closed.";
            return RedirectToAction(nameof(MyRequests));
        }

        [HttpPost]
        public async Task<IActionResult> Cancel(int requestId)
        {
            var currentUserName = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == currentUserName);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            var request = await _context.MaintenanceRequests
                .FirstOrDefaultAsync(r => r.Id == requestId && r.UserId == user.Id);

            if (request == null) return NotFound();

            if (request.Status != MaintenanceRequestStatus.Submitted && request.Status != MaintenanceRequestStatus.Assigned)
            {
                TempData["ErrorMessage"] = "Only pending requests can be cancelled.";
                return RedirectToAction(nameof(MyRequests));
            }

            request.Status = MaintenanceRequestStatus.Cancelled;
            request.IsActive = false;
            request.UpdatedAt = DateTime.UtcNow;

            _context.RequestStatusHistory.Add(new RequestStatusHistory
            {
                RequestId = requestId,
                OldStatus = MaintenanceRequestStatus.Submitted,
                NewStatus = MaintenanceRequestStatus.Cancelled,
                ChangedById = user.Id,
                Comments = "Cancelled by requester"
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Request cancelled successfully!";
            return RedirectToAction(nameof(MyRequests));
        }
    }
}
