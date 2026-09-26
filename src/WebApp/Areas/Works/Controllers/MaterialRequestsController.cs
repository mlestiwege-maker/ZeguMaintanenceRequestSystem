using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZEGU.Core.Enums;
using ZEGU.Infrastructure.Data;
using ZEGU.Infrastructure.Services;
using ZEGU.WebApp.Services;

namespace ZEGU.WebApp.Areas.Works.Controllers
{
    [Area("Works")]
    [Authorize(Policy = "RequireWorksAccess")]
    public class MaterialRequestsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly NotificationService _notificationService;
        private readonly AuditService _auditService;
        private readonly SmsService _smsService;
        private readonly WhatsAppService _whatsAppService;

        public MaterialRequestsController(ApplicationDbContext context, NotificationService notificationService, AuditService auditService, SmsService smsService, WhatsAppService whatsAppService)
        {
            _context = context;
            _notificationService = notificationService;
            _auditService = auditService;
            _smsService = smsService;
            _whatsAppService = whatsAppService;
        }

        public async Task<IActionResult> Index(string? status = null)
        {
            var query = _context.MaterialRequests
                .Include(m => m.Material)
                .Include(m => m.Request)
                .Include(m => m.Technician)
                .ThenInclude(t => t.User)
                .Where(m => m.IsActive)
                .AsQueryable();

            if (Enum.TryParse<MaterialRequestStatus>(status, out var statusEnum))
            {
                query = query.Where(m => m.Status == statusEnum);
            }
            else
            {
                query = query.Where(m => m.Status == MaterialRequestStatus.Pending);
            }

            var materialRequests = await query.OrderByDescending(m => m.CreatedAt).ToListAsync();
            ViewBag.SelectedStatus = status ?? "Pending";
            return View(materialRequests);
        }

        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            var materialRequest = await _context.MaterialRequests
                .Include(m => m.Material)
                .Include(m => m.Request)
                .Include(m => m.Technician)
                .ThenInclude(t => t.User)
                .FirstOrDefaultAsync(m => m.Id == id && m.IsActive);

            if (materialRequest == null) return NotFound();

            if (materialRequest.Status != MaterialRequestStatus.Pending)
            {
                TempData["ErrorMessage"] = "This material request has already been reviewed.";
                return RedirectToAction(nameof(Index));
            }

            var currentUserName = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == currentUserName);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            var oldStock = materialRequest.Material.CurrentStock;
            materialRequest.Material.CurrentStock = Math.Max(0, oldStock - (int)Math.Ceiling(materialRequest.QuantityRequested));
            materialRequest.Material.UpdatedAt = DateTime.UtcNow;

            materialRequest.Status = MaterialRequestStatus.Approved;
            materialRequest.ReviewedById = user.Id;
            materialRequest.ReviewedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(user.Id, user.UserName, "ApproveMaterialRequest", "MaterialRequest",
                entityId: materialRequest.Id,
                oldValues: new { MaterialStock = oldStock },
                newValues: new { MaterialStock = materialRequest.Material.CurrentStock, materialRequest.QuantityRequested },
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

            var techUser = materialRequest.Technician.User;
            if (techUser != null)
            {
                var message = $"Your request for {materialRequest.QuantityRequested} {materialRequest.Material.Unit} of {materialRequest.Material.MaterialName} for {materialRequest.Request.RequestNumber} was approved.";
                await _notificationService.CreateNotificationAsync(techUser.Id, "Material Request Approved", message, materialRequest.RequestId);

                if (!string.IsNullOrEmpty(techUser.PhoneNumber))
                {
                    _ = _smsService.SendSmsAsync(techUser.PhoneNumber, message);
                    _ = _whatsAppService.SendWhatsAppAsync(techUser.PhoneNumber, message);
                }
            }

            TempData["SuccessMessage"] = "Material request approved and stock updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Reject(int id, string reviewNotes)
        {
            var materialRequest = await _context.MaterialRequests
                .Include(m => m.Material)
                .Include(m => m.Request)
                .Include(m => m.Technician)
                .ThenInclude(t => t.User)
                .FirstOrDefaultAsync(m => m.Id == id && m.IsActive);

            if (materialRequest == null) return NotFound();

            if (materialRequest.Status != MaterialRequestStatus.Pending)
            {
                TempData["ErrorMessage"] = "This material request has already been reviewed.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(reviewNotes))
            {
                TempData["ErrorMessage"] = "Please provide a reason for rejecting this request.";
                return RedirectToAction(nameof(Index));
            }

            var currentUserName = User.Identity?.Name;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == currentUserName);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            materialRequest.Status = MaterialRequestStatus.Rejected;
            materialRequest.ReviewedById = user.Id;
            materialRequest.ReviewedAt = DateTime.UtcNow;
            materialRequest.ReviewNotes = reviewNotes;
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(user.Id, user.UserName, "RejectMaterialRequest", "MaterialRequest",
                entityId: materialRequest.Id, newValues: new { reviewNotes },
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

            var techUser = materialRequest.Technician.User;
            if (techUser != null)
            {
                var message = $"Your request for {materialRequest.Material.MaterialName} for {materialRequest.Request.RequestNumber} was rejected: {reviewNotes}";
                await _notificationService.CreateNotificationAsync(techUser.Id, "Material Request Rejected", message, materialRequest.RequestId);

                if (!string.IsNullOrEmpty(techUser.PhoneNumber))
                {
                    _ = _smsService.SendSmsAsync(techUser.PhoneNumber, message);
                    _ = _whatsAppService.SendWhatsAppAsync(techUser.PhoneNumber, message);
                }
            }

            TempData["SuccessMessage"] = "Material request rejected.";
            return RedirectToAction(nameof(Index));
        }
    }
}
