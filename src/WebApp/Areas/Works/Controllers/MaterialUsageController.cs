using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZEGU.Core.Entities.Maintenance;
using ZEGU.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using ZEGU.Core.Entities.Identity;

namespace ZEGU.WebApp.Areas.Works.Controllers
{
    [Area("Works")]
    [Authorize(Policy = "RequireWorksAccess")]
    public class MaterialUsageController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MaterialUsageController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Create(int workLogId)
        {
            var workLog = await _context.WorkLogs
                .Include(w => w.Request)
                .FirstOrDefaultAsync(w => w.Id == workLogId && w.IsActive);

            if (workLog == null) return NotFound();

            ViewBag.WorkLog = workLog;
            ViewBag.Materials = await _context.Materials.Where(m => m.IsActive && m.CurrentStock > 0).ToListAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(int workLogId, int materialId, decimal quantityUsed, decimal? unitCost = null)
        {
            var workLog = await _context.WorkLogs
                .Include(w => w.Request)
                .FirstOrDefaultAsync(w => w.Id == workLogId && w.IsActive);

            if (workLog == null) return NotFound();

            var material = await _context.Materials.FindAsync(materialId);
            if (material == null) return NotFound();

            if (material.CurrentStock < quantityUsed)
            {
                TempData["ErrorMessage"] = "Insufficient stock available!";
                return RedirectToAction(nameof(Create), new { workLogId });
            }

            var totalCost = (unitCost ?? material.UnitCost ?? 0) * quantityUsed;

            var materialUsage = new MaterialUsage
            {
                WorkLogId = workLogId,
                MaterialId = materialId,
                QuantityUsed = quantityUsed,
                UnitCost = unitCost ?? material.UnitCost,
                TotalCost = totalCost
            };

            _context.MaterialUsage.Add(materialUsage);

            material.CurrentStock = (int)Math.Max(0, material.CurrentStock - quantityUsed);
            material.UpdatedAt = DateTime.UtcNow;

            if (workLog.Request != null)
            {
                workLog.Request.MaterialCost = (workLog.Request.MaterialCost ?? 0) + totalCost;
                workLog.Request.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Material usage recorded: {quantityUsed} x {material.MaterialName}";
            return RedirectToAction("Details", "Home", new { id = workLog.RequestId, area = "Works" });
        }
    }
}
