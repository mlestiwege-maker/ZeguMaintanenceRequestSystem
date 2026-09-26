using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZEGU.Infrastructure.Data;

namespace ZEGU.WebApp.Areas.Mobile.Controllers
{
    [Area("Mobile")]
    [Authorize]
    [Route("Mobile/Scan")]
    public class ScanController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ScanController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Index(int id)
        {
            var asset = await _context.Assets
                .Include(a => a.Category)
                .Include(a => a.Location)
                .ThenInclude(l => l.Building)
                .FirstOrDefaultAsync(a => a.Id == id && a.IsActive);

            if (asset == null) return NotFound();

            ViewBag.RecentHistory = await _context.MaintenanceRequests
                .Where(r => r.AssetId == id && r.IsActive)
                .OrderByDescending(r => r.CreatedAt)
                .Take(5)
                .ToListAsync();

            return View(asset);
        }
    }
}
