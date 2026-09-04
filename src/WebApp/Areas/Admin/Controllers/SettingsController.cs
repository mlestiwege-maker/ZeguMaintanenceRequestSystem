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
            var viewModel = new SystemSettingsViewModel
            {
                AdminEmail = _configuration["AdminSettings:Email"] ?? "admin@university.edu",
                SLAEmergency = _configuration.GetValue<int>("SLA:Emergency", 2),
                SLAHigh = _configuration.GetValue<int>("SLA:High", 4),
                SLANormal = _configuration.GetValue<int>("SLA:Normal", 24),
                SLALow = _configuration.GetValue<int>("SLA:Low", 48),
                MaxFileSize = _configuration.GetValue<int>("Uploads:MaxFileSize", 5),
                AppUrl = _configuration["AppUrl"] ?? "http://localhost:5259",
                QrCodeApiUrl = _configuration["QrCodeApiUrl"] ?? "https://api.qrserver.com/v1/create-qr-code/",
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

        [HttpPost]
        public async Task<IActionResult> UpdateSlaSettings(int emergency, int high, int normal, int low)
        {
            _configuration["SLA:Emergency"] = emergency.ToString();
            _configuration["SLA:High"] = high.ToString();
            _configuration["SLA:Normal"] = normal.ToString();
            _configuration["SLA:Low"] = low.ToString();

            TempData["SuccessMessage"] = "SLA settings updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> UpdateAdminEmail(string email, string password)
        {
            _configuration["AdminSettings:Email"] = email;
            _configuration["AdminSettings:Password"] = password;

            TempData["SuccessMessage"] = "Admin credentials updated. Please update database to take effect.";
            return RedirectToAction(nameof(Index));
        }
    }
}
