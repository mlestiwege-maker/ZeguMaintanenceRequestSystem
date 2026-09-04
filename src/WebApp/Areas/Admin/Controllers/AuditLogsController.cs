using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZEGU.Infrastructure.Services;

namespace ZEGU.WebApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "RequireAdmin")]
    public class AuditLogsController : Controller
    {
        private readonly AuditService _auditService;

        public AuditLogsController(AuditService auditService)
        {
            _auditService = auditService;
        }

        public async Task<IActionResult> Index(string? entityType = null, string? userId = null, int count = 100)
        {
            var logs = await _auditService.GetRecentLogsAsync(count, entityType, userId);
            return View(logs);
        }
    }
}
