using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZEGU.Core.Entities.Identity;
using ZEGU.Core.Enums;
using ZEGU.Infrastructure.Data;
using ZEGU.Infrastructure.Services;
using ZEGU.WebApp.ViewModels.Admin;

namespace ZEGU.WebApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "RequireAdmin")]
    public class SettingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public SettingsController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index()
        {
            var categorySla = await _context.MaintenanceCategories
                .Where(c => c.IsActive)
                .OrderBy(c => c.CategoryName)
                .Select(c => new CategorySlaSummary { Id = c.Id, CategoryName = c.CategoryName, SLAHours = c.SLAHours ?? 0 })
                .ToListAsync();

            var viewModel = new SystemSettingsViewModel
            {
                AdminEmail = _configuration["AdminSettings:Email"] ?? "",
                CategorySlaHours = categorySla,
                MaxFileSize = _configuration.GetValue<int>("Uploads:MaxFileSize", 5),
                AppUrl = _configuration["AppUrl"] ?? "http://localhost:5259",
                TotalUsers = await _context.Users.CountAsync(),
                TotalRequests = await _context.MaintenanceRequests.CountAsync(),
                TotalCategories = await _context.MaintenanceCategories.CountAsync(),
                TotalTechnicians = await _context.Technicians.CountAsync(),
                TotalAssets = await _context.Assets.CountAsync(),
                TotalSchedules = await _context.PreventiveMaintenanceSchedules.CountAsync(),
                UnreadNotifications = await _context.Notifications.CountAsync(n => !n.IsRead)
            };

            return View(viewModel);
        }
    }
}
