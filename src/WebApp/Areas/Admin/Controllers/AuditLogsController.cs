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

        public async Task<IActionResult> Index(string? entityType = null, string? userId = null,
            int pageNumber = 1, int pageSize = 50)
        {
            pageSize = Math.Clamp(pageSize, 1, 200);
            pageNumber = Math.Max(1, pageNumber);

            var (items, totalCount) = await _auditService.GetPagedLogsAsync(pageNumber, pageSize, entityType, userId);

            return View(new ZEGU.WebApp.ViewModels.PagedResult<ZEGU.Core.Entities.Maintenance.AuditLog>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            });
        }
    }
}
