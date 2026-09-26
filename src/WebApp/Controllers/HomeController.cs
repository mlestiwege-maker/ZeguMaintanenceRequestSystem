using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZEGU.Core.Entities.Identity;
using ZEGU.Core.Enums;
using ZEGU.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;

namespace ZEGU.WebApp.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var currentUserName = User.Identity?.Name;
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == currentUserName);
                if (user != null)
                {
                    if (user.Role == UserRole.Student || user.Role == UserRole.Staff)
                    {
                        return RedirectToAction("Index", "Home", new { area = "Requests" });
                    }
                    else if (user.Role == UserRole.WorksOfficer || user.Role == UserRole.Manager)
                    {
                        return RedirectToAction("Index", "Home", new { area = "Works" });
                    }
                    else if (user.Role == UserRole.Technician)
                    {
                        return RedirectToAction("Index", "Home", new { area = "Technicians" });
                    }
                    else if (user.Role == UserRole.Admin)
                    {
                        return RedirectToAction("Index", "Home", new { area = "Admin" });
                    }
                }
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}
