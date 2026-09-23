using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ZEGU.Core.Entities.Maintenance;
using ZEGU.Core.Enums;
using ZEGU.Infrastructure.Data;

namespace ZEGU.WebApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "RequireManagerOrAdmin")]
    public class PreventiveMaintenanceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PreventiveMaintenanceController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var schedules = await _context.PreventiveMaintenanceSchedules
                .Include(s => s.Category)
                .Include(s => s.Location)
                .Include(s => s.Location.Building)
                .Include(s => s.Technician)
                .ThenInclude(t => t!.User)
                .OrderBy(s => s.NextDue)
                .ToListAsync();

            return View(schedules);
        }

        public async Task<IActionResult> Details(int id)
        {
            var schedule = await _context.PreventiveMaintenanceSchedules
                .Include(s => s.Category)
                .Include(s => s.Location)
                .ThenInclude(l => l.Building)
                .Include(s => s.Technician)
                .ThenInclude(t => t!.User)
                .Include(s => s.Records)
                .ThenInclude(r => r.PerformedBy)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (schedule == null) return NotFound();

            return View(schedule);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = new SelectList(await _context.MaintenanceCategories.Where(c => c.IsActive).ToListAsync(), "Id", "CategoryName");
            ViewBag.Locations = new SelectList(await _context.Rooms.Include(r => r.Building).Where(r => r.IsActive).ToListAsync(), "Id", "RoomNumber");
            ViewBag.Technicians = new SelectList(await _context.Technicians.Include(t => t.User).Where(t => t.IsActive).ToListAsync(), "Id", "User.FirstName");
            ViewBag.Frequencies = new SelectList(new[] { "Daily", "Weekly", "BiWeekly", "Monthly", "Quarterly", "SemiAnnually", "Annually" });
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PreventiveMaintenanceSchedule schedule)
        {
            if (ModelState.IsValid)
            {
                _context.PreventiveMaintenanceSchedules.Add(schedule);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Preventive maintenance schedule created successfully!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = new SelectList(await _context.MaintenanceCategories.Where(c => c.IsActive).ToListAsync(), "Id", "CategoryName", schedule.CategoryId);
            ViewBag.Locations = new SelectList(await _context.Rooms.Where(r => r.IsActive).ToListAsync(), "Id", "RoomNumber", schedule.LocationId);
            ViewBag.Technicians = new SelectList(await _context.Technicians.Where(t => t.IsActive).ToListAsync(), "Id", "User.FirstName", schedule.TechnicianId);
            ViewBag.Frequencies = new SelectList(new[] { "Daily", "Weekly", "BiWeekly", "Monthly", "Quarterly", "SemiAnnually", "Annually" }, schedule.Frequency);
            return View(schedule);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var schedule = await _context.PreventiveMaintenanceSchedules.FindAsync(id);
            if (schedule == null) return NotFound();

            ViewBag.Categories = new SelectList(await _context.MaintenanceCategories.Where(c => c.IsActive).ToListAsync(), "Id", "CategoryName", schedule.CategoryId);
            ViewBag.Locations = new SelectList(await _context.Rooms.Where(r => r.IsActive).ToListAsync(), "Id", "RoomNumber", schedule.LocationId);
            ViewBag.Technicians = new SelectList(await _context.Technicians.Where(t => t.IsActive).ToListAsync(), "Id", "User.FirstName", schedule.TechnicianId);
            ViewBag.Frequencies = new SelectList(new[] { "Daily", "Weekly", "BiWeekly", "Monthly", "Quarterly", "SemiAnnually", "Annually" }, schedule.Frequency);
            return View(schedule);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PreventiveMaintenanceSchedule schedule)
        {
            if (id != schedule.Id) return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(schedule);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Schedule updated successfully!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = new SelectList(await _context.MaintenanceCategories.Where(c => c.IsActive).ToListAsync(), "Id", "CategoryName", schedule.CategoryId);
            ViewBag.Locations = new SelectList(await _context.Rooms.Where(r => r.IsActive).ToListAsync(), "Id", "RoomNumber", schedule.LocationId);
            ViewBag.Technicians = new SelectList(await _context.Technicians.Where(t => t.IsActive).ToListAsync(), "Id", "User.FirstName", schedule.TechnicianId);
            ViewBag.Frequencies = new SelectList(new[] { "Daily", "Weekly", "BiWeekly", "Monthly", "Quarterly", "SemiAnnually", "Annually" }, schedule.Frequency);
            return View(schedule);
        }

        [HttpPost]
        public async Task<IActionResult> MarkPerformed(int scheduleId, string workPerformed, string? notes)
        {
            var schedule = await _context.PreventiveMaintenanceSchedules
                .Include(s => s.Category)
                .Include(s => s.Location)
                .FirstOrDefaultAsync(s => s.Id == scheduleId);

            if (schedule == null) return NotFound();

            var currentUserName = User.Identity?.Name ?? string.Empty;
            var userId = _context.Users
                .Where(u => u.UserName == currentUserName)
                .Select(u => u.Id)
                .FirstOrDefault();

            var record = new PreventiveMaintenanceRecord
            {
                ScheduleId = scheduleId,
                TechnicianId = schedule.TechnicianId,
                PerformedById = userId,
                WorkPerformed = workPerformed,
                PerformedAt = DateTime.UtcNow,
                NextDue = DateTime.UtcNow.AddDays(schedule.FrequencyDays),
                Notes = notes
            };

            _context.PreventiveMaintenanceRecords.Add(record);
            schedule.LastPerformed = DateTime.UtcNow;
            schedule.NextDue = record.NextDue.Value;
            schedule.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Maintenance record added successfully!";
            return RedirectToAction(nameof(Details), new { id = scheduleId });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var schedule = await _context.PreventiveMaintenanceSchedules.FindAsync(id);
            if (schedule == null) return NotFound();

            schedule.IsActive = !schedule.IsActive;
            schedule.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = schedule.IsActive ? "Schedule activated!" : "Schedule deactivated!";
            return RedirectToAction(nameof(Index));
        }
    }
}
