using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZEGU.Core.Entities.Maintenance;
using ZEGU.Core.Enums;
using ZEGU.Infrastructure.Data;
using ZEGU.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using ZEGU.Core.Entities.Identity;

namespace ZEGU.WebApp.Controllers
{
[Authorize]
    public class NotificationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly NotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;

        private string? CurrentUserName => User.Identity?.Name;

        public NotificationsController(ApplicationDbContext context, NotificationService notificationService, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _notificationService = notificationService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userName = CurrentUserName;
            if (string.IsNullOrEmpty(userName)) return RedirectToAction("Login", "Account", new { area = "" });
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            var notifications = await _context.Notifications
                .Where(n => n.UserId == user.Id)
                .OrderByDescending(n => n.CreatedAt)
                .Take(50)
                .ToListAsync();

            return View(notifications);
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userName = CurrentUserName;
            if (string.IsNullOrEmpty(userName)) return RedirectToAction("Login", "Account", new { area = "" });
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            await _notificationService.MarkAsReadAsync(id, user.Id);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userName = CurrentUserName;
            if (string.IsNullOrEmpty(userName)) return RedirectToAction("Login", "Account", new { area = "" });
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            await _notificationService.MarkAllAsReadAsync(user.Id);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> UnreadCount()
        {
            var userName = CurrentUserName;
            if (string.IsNullOrEmpty(userName)) return Json(0);
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null) return Json(0);

            var count = await _notificationService.GetUnreadCountAsync(user.Id);
            return Json(count);
        }

        [HttpGet]
        public async Task<IActionResult> GetUnread()
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            if (user == null) return Json(new List<object>());

            var notifications = await _notificationService.GetUnreadNotificationsAsync(user.Id);
            var result = notifications.Select(n => new
            {
                n.Id,
                n.Title,
                n.Message,
                n.CreatedAt,
                RequestId = n.RequestId,
                Type = n.Type.ToString()
            }).ToList();

            return Json(result);
        }
    }
}
