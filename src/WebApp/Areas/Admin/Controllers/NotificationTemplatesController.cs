using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZEGU.Core.Entities.Maintenance;
using ZEGU.Infrastructure.Data;

namespace ZEGU.WebApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "RequireAdmin")]
    public class NotificationTemplatesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NotificationTemplatesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var templates = await _context.NotificationTemplates
                .OrderBy(t => t.Type)
                .ThenBy(t => t.Name)
                .ToListAsync();
            return View(templates);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Types = new List<string> { "Email", "SMS", "WhatsApp", "System" };
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NotificationTemplate template)
        {
            if (ModelState.IsValid)
            {
                _context.NotificationTemplates.Add(template);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Notification template created successfully!";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Types = new List<string> { "Email", "SMS", "WhatsApp", "System" };
            return View(template);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var template = await _context.NotificationTemplates.FindAsync(id);
            if (template == null) return NotFound();
            ViewBag.Types = new List<string> { "Email", "SMS", "WhatsApp", "System" };
            return View(template);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, NotificationTemplate template)
        {
            if (id != template.Id) return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(template);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Template updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Types = new List<string> { "Email", "SMS", "WhatsApp", "System" };
            return View(template);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var template = await _context.NotificationTemplates.FindAsync(id);
            if (template == null) return NotFound();

            template.IsActive = false;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Template deleted successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}
