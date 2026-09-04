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

        public HomeController(ApplicationDbContext context, NotificationService notificationService, EmailService emailService, SmsService smsService, WhatsAppService whatsAppService)
        {
            _context = context;
            _notificationService = notificationService;
            _emailService = emailService;
            _smsService = smsService;
            _whatsAppService = whatsAppService;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
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
        public async Task<IActionResult> Create()
        {
            var model = new CreateRequestViewModel
            {
                Categories = await _context.MaintenanceCategories.Where(c => c.IsActive).ToListAsync(),
                Buildings = await _context.Buildings.Include(b => b.Campus).Where(b => b.IsActive).ToListAsync(),
                Departments = await _context.Departments.Where(d => d.IsActive).ToListAsync()
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Categories = await _context.MaintenanceCategories.Where(c => c.IsActive).ToListAsync();
                model.Buildings = await _context.Buildings.Include(b => b.Campus).Where(b => b.IsActive).ToListAsync();
                model.Departments = await _context.Departments.Where(d => d.IsActive).ToListAsync();
                return View(model);
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
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

            var request = new MaintenanceRequest
            {
                RequestNumber = $"MRS-{DateTime.UtcNow.Year}-TEMP",
                UserId = user.Id,
                DepartmentId = model.DepartmentId,
                CategoryId = model.CategoryId.Value,
                LocationId = model.LocationId.Value,
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
                    if (photo.Length > 0)
                    {
                        if (photo.Length > 5 * 1024 * 1024)
                        {
                            TempData["ErrorMessage"] = "File size must be less than 5MB";
                            return RedirectToAction(nameof(Create));
                        }

                        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
                        var fileExtension = Path.GetExtension(photo.FileName).ToLower();
                        if (!allowedExtensions.Contains(fileExtension))
                        {
                            TempData["ErrorMessage"] = "Only image files are allowed (jpg, jpeg, png, gif, bmp)";
                            return RedirectToAction(nameof(Create));
                        }

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

            foreach (var worksUser in worksUsers)
            {
                if (!string.IsNullOrEmpty(worksUser.Email))
                {
                    _ = _emailService.SendMaintenanceNotificationAsync(worksUser.Email, worksUser.FirstName, request.RequestNumber, "Submitted");
                }
            }

            TempData["SuccessMessage"] = $"Maintenance request {request.RequestNumber} submitted successfully!";
            return RedirectToAction(nameof(MyRequests));
        }

        public async Task<IActionResult> MyRequests()
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            var requests = await _context.MaintenanceRequests
                .Include(r => r.Category)
                .Include(r => r.Location)
                .Include(r => r.Location.Building)
                .Where(r => r.UserId == user.Id && r.IsActive)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(requests);
        }

        public async Task<IActionResult> Details(int id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            var request = await _context.MaintenanceRequests
                .Include(r => r.Category)
                .Include(r => r.Location)
                .Include(r => r.Location.Building)
                .Include(r => r.StatusHistory)
                .Include(r => r.Comments)
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
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
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

            TempData["SuccessMessage"] = "Comment added successfully";
            return RedirectToAction(nameof(Details), new { id = requestId });
        }

        [HttpPost]
        public async Task<IActionResult> SubmitFeedback(int requestId, int rating, string? comments, bool workSatisfactory)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
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
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
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
