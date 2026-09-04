using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ZEGU.Core.Entities.Maintenance;
using ZEGU.Core.Entities.Shared;
using ZEGU.Infrastructure.Data;
using ZEGU.WebApp.Services;

namespace ZEGU.WebApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "RequireManagerOrAdmin")]
    public class AssetsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly QrCodeService _qrCodeService;

        public AssetsController(ApplicationDbContext context, QrCodeService qrCodeService)
        {
            _context = context;
            _qrCodeService = qrCodeService;
        }

        public async Task<IActionResult> Index(string? searchString, int? categoryId, int? locationId, string? status)
        {
            var query = _context.Assets
                .Include(a => a.Category)
                .Include(a => a.Location)
                .ThenInclude(r => r.Building)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(a =>
                    a.AssetName.Contains(searchString) ||
                    a.AssetCode.Contains(searchString) ||
                    a.SerialNumber.Contains(searchString) ||
                    a.Manufacturer.Contains(searchString));
            }

            if (categoryId.HasValue)
                query = query.Where(a => a.CategoryId == categoryId);

            if (locationId.HasValue)
                query = query.Where(a => a.LocationId == locationId);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(a => a.Status == status);

            ViewBag.Categories = new SelectList(await _context.MaintenanceCategories.Where(c => c.IsActive).ToListAsync(), "Id", "CategoryName", categoryId);
            ViewBag.Locations = new SelectList(await _context.Rooms.Where(r => r.IsActive).ToListAsync(), "Id", "RoomNumber", locationId);
            ViewBag.Statuses = new SelectList(new[] { "Operational", "UnderMaintenance", "OutOfService", "Disposed" }, status);
            ViewBag.SearchString = searchString;

            var assets = await query.OrderByDescending(a => a.CreatedAt).ToListAsync();
            return View(assets);
        }

        public async Task<IActionResult> Details(int id)
        {
            var asset = await _context.Assets
                .Include(a => a.Category)
                .Include(a => a.Location)
                .ThenInclude(r => r.Building)
                .Include(a => a.MaintenanceRequests)
                .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (asset == null)
                return NotFound();

            return View(asset);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = new SelectList(await _context.MaintenanceCategories.Where(c => c.IsActive).ToListAsync(), "Id", "CategoryName");
            ViewBag.Locations = new SelectList(await _context.Rooms.Where(r => r.IsActive).ToListAsync(), "Id", "RoomNumber");
            ViewBag.Statuses = new SelectList(new[] { "Operational", "UnderMaintenance", "OutOfService", "Disposed" });
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Asset asset)
        {
            if (ModelState.IsValid)
            {
                _context.Assets.Add(asset);
                await _context.SaveChangesAsync();

                if (!string.IsNullOrEmpty(asset.AssetCode))
                {
                    try
                    {
                        var qrCodePath = await _qrCodeService.SaveQrCodeAsync(asset.Id, asset.AssetCode);
                        asset.QrCodePath = qrCodePath;
                        await _context.SaveChangesAsync();
                    }
                    catch
                    {
                    }
                }

                TempData["SuccessMessage"] = $"Asset '{asset.AssetName}' created successfully.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = new SelectList(await _context.MaintenanceCategories.Where(c => c.IsActive).ToListAsync(), "Id", "CategoryName", asset.CategoryId);
            ViewBag.Locations = new SelectList(await _context.Rooms.Where(r => r.IsActive).ToListAsync(), "Id", "RoomNumber", asset.LocationId);
            ViewBag.Statuses = new SelectList(new[] { "Operational", "UnderMaintenance", "OutOfService", "Disposed" }, asset.Status);
            return View(asset);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var asset = await _context.Assets.FindAsync(id);
            if (asset == null)
                return NotFound();

            ViewBag.Categories = new SelectList(await _context.MaintenanceCategories.Where(c => c.IsActive).ToListAsync(), "Id", "CategoryName", asset.CategoryId);
            ViewBag.Locations = new SelectList(await _context.Rooms.Where(r => r.IsActive).ToListAsync(), "Id", "RoomNumber", asset.LocationId);
            ViewBag.Statuses = new SelectList(new[] { "Operational", "UnderMaintenance", "OutOfService", "Disposed" }, asset.Status);
            return View(asset);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Asset asset)
        {
            if (id != asset.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(asset);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Asset '{asset.AssetName}' updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Assets.AnyAsync(a => a.Id == id))
                        return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = new SelectList(await _context.MaintenanceCategories.Where(c => c.IsActive).ToListAsync(), "Id", "CategoryName", asset.CategoryId);
            ViewBag.Locations = new SelectList(await _context.Rooms.Where(r => r.IsActive).ToListAsync(), "Id", "RoomNumber", asset.LocationId);
            ViewBag.Statuses = new SelectList(new[] { "Operational", "UnderMaintenance", "OutOfService", "Disposed" }, asset.Status);
            return View(asset);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var asset = await _context.Assets.FindAsync(id);
            if (asset == null)
                return NotFound();

            asset.IsActive = !asset.IsActive;
            await _context.SaveChangesAsync();

            var status = asset.IsActive ? "activated" : "deactivated";
            TempData["SuccessMessage"] = $"Asset '{asset.AssetName}' {status} successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> DownloadQrCode(int id)
        {
            var asset = await _context.Assets.FindAsync(id);
            if (asset == null || string.IsNullOrEmpty(asset.QrCodePath))
                return NotFound();

            var filePath = Path.Combine("wwwroot", asset.QrCodePath.TrimStart('/'));
            if (!System.IO.File.Exists(filePath))
                return NotFound();

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(fileBytes, "image/png", $"QR-{asset.AssetCode}.png");
        }
    }
}
