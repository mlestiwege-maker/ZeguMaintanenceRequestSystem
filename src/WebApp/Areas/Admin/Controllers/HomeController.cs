using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZEGU.Core.Entities.Identity;
using ZEGU.Core.Entities.Shared;
using ZEGU.Core.Entities.Maintenance;
using ZEGU.Core.Enums;
using ZEGU.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using ZEGU.WebApp.ViewModels.Admin;
using System.Linq;
using System.Security.Claims;
using ZEGU.Infrastructure.Services;

namespace ZEGU.WebApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "RequireManagerOrAdmin")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var stats = new AdminDashboardViewModel
            {
                TotalUsers = await _context.Users.CountAsync(),
                TotalRequests = await _context.MaintenanceRequests.CountAsync(),
                TotalTechnicians = await _context.Technicians.CountAsync(),
                TotalCategories = await _context.MaintenanceCategories.CountAsync(),
                TotalDepartments = await _context.Departments.CountAsync(),
                TotalCampuses = await _context.Campuses.CountAsync(),
                TotalBuildings = await _context.Buildings.CountAsync(),
                TotalRooms = await _context.Rooms.CountAsync(),
                PendingRequests = await _context.MaintenanceRequests.CountAsync(r => r.IsActive && r.Status == MaintenanceRequestStatus.Submitted),
                OpenRequests = await _context.MaintenanceRequests.CountAsync(r => r.IsActive && r.Status == MaintenanceRequestStatus.InProgress),
                CompletedRequests = await _context.MaintenanceRequests.CountAsync(r => r.IsActive && r.Status == MaintenanceRequestStatus.Completed)
            };

            var recentRequests = await _context.MaintenanceRequests
                .Include(r => r.User)
                .Include(r => r.Category)
                .OrderByDescending(r => r.CreatedAt)
                .Take(10)
                .ToListAsync();

            stats.RecentRequests = recentRequests;
            return View(stats);
        }
    }

    [Area("Admin")]
    [Authorize(Policy = "RequireAdmin")]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AuditService _auditService;

        public UsersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, AuditService auditService)
        {
            _context = context;
            _userManager = userManager;
            _auditService = auditService;
        }

        public async Task<IActionResult> Index(string? search = null, int pageNumber = 1, int pageSize = 25)
        {
            pageSize = Math.Clamp(pageSize, 1, 100);
            pageNumber = Math.Max(1, pageNumber);

            var query = _context.Users
                .Include(u => u.Department)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u => u.FirstName.Contains(search) ||
                                          u.LastName.Contains(search) ||
                                          (u.Email != null && u.Email.Contains(search)) ||
                                          (u.UserName != null && u.UserName.Contains(search)));
            }

            var orderedQuery = query.OrderByDescending(u => u.CreatedAt);
            var totalCount = await orderedQuery.CountAsync();
            var users = await orderedQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.SearchTerm = search;

            return View(new ZEGU.WebApp.ViewModels.PagedResult<ApplicationUser>
            {
                Items = users,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            });
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Departments = await _context.Departments.Where(d => d.IsActive).ToListAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(string firstName, string lastName, string email, string userName, string password, UserRole role, int? departmentId = null, string? studentNumber = null, string? staffNumber = null)
        {
            var user = new ApplicationUser
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                UserName = userName,
                Role = role,
                DepartmentId = departmentId,
                StudentNumber = studentNumber,
                StaffNumber = staffNumber,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, role.ToString());
                await _auditService.LogAsync(_userManager.GetUserId(User), User.Identity?.Name, "Create", "User",
                    entityId: null, newValues: new { user.UserName, user.Email, user.Role },
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());
                TempData["SuccessMessage"] = "User created successfully!";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            ViewBag.Departments = await _context.Departments.Where(d => d.IsActive).ToListAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ToggleActive(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);

            await _auditService.LogAsync(_userManager.GetUserId(User), User.Identity?.Name,
                user.IsActive ? "Activate" : "Deactivate", "User",
                entityId: null, newValues: new { user.UserName, user.IsActive },
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["SuccessMessage"] = user.IsActive ? "User activated successfully!" : "User deactivated successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            
            ViewBag.Departments = await _context.Departments.Where(d => d.IsActive).ToListAsync();
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string id, string firstName, string lastName, string email, UserRole role, int? departmentId = null, string? studentNumber = null, string? staffNumber = null)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var oldRole = user.Role;
            user.FirstName = firstName;
            user.LastName = lastName;
            user.Email = email;
            user.UserName = email;
            user.Role = role;
            user.DepartmentId = departmentId;
            user.StudentNumber = studentNumber;
            user.StaffNumber = staffNumber;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                await _userManager.RemoveFromRolesAsync(user, await _userManager.GetRolesAsync(user));
                await _userManager.AddToRoleAsync(user, role.ToString());
                await _auditService.LogAsync(_userManager.GetUserId(User), User.Identity?.Name, "Update", "User",
                    entityId: null, oldValues: new { Role = oldRole }, newValues: new { user.UserName, user.Role },
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());
                TempData["SuccessMessage"] = "User updated successfully!";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            ViewBag.Departments = await _context.Departments.Where(d => d.IsActive).ToListAsync();
            return View(user);
        }
    }

    [Area("Admin")]
    [Authorize(Policy = "RequireAdmin")]
    public class DepartmentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DepartmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var departments = await _context.Departments.OrderBy(d => d.DepartmentName).ToListAsync();
            return View(departments);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Department model)
        {
            if (ModelState.IsValid)
            {
                _context.Departments.Add(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Department created successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var dept = await _context.Departments.FindAsync(id);
            if (dept == null) return NotFound();
            
            dept.IsActive = !dept.IsActive;
            dept.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = dept.IsActive ? "Department activated!" : "Department deactivated!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department == null) return NotFound();
            return View(department);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Department model)
        {
            if (ModelState.IsValid)
            {
                var department = await _context.Departments.FindAsync(model.Id);
                if (department == null) return NotFound();

                department.DepartmentName = model.DepartmentName;
                department.Code = model.Code;
                department.HeadOfDepartment = model.HeadOfDepartment;
                department.ContactEmail = model.ContactEmail;
                department.ContactPhone = model.ContactPhone;
                department.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Department updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }
    }

    [Area("Admin")]
    [Authorize(Policy = "RequireAdmin")]
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _context.MaintenanceCategories.OrderBy(c => c.CategoryName).ToListAsync();
            return View(categories);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(MaintenanceCategory model)
        {
            if (ModelState.IsValid)
            {
                _context.MaintenanceCategories.Add(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Category created successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var category = await _context.MaintenanceCategories.FindAsync(id);
            if (category == null) return NotFound();
            
            category.IsActive = !category.IsActive;
            category.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = category.IsActive ? "Category activated!" : "Category deactivated!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _context.MaintenanceCategories.FindAsync(id);
            if (category == null) return NotFound();
            return View(category);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(MaintenanceCategory model)
        {
            if (ModelState.IsValid)
            {
                var category = await _context.MaintenanceCategories.FindAsync(model.Id);
                if (category == null) return NotFound();

                category.CategoryName = model.CategoryName;
                category.Description = model.Description;
                category.Icon = model.Icon;
                category.SLAHours = model.SLAHours;
                category.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Category updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }
    }

    [Area("Admin")]
    [Authorize(Policy = "RequireAdmin")]
    public class LocationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LocationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var buildings = await _context.Buildings
                .Include(b => b.Campus)
                .Include(b => b.Rooms)
                .OrderBy(b => b.BuildingName)
                .ToListAsync();
            return View(buildings);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Campuses = await _context.Campuses.Where(c => c.IsActive).ToListAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Building model, int campusId)
        {
            model.CampusId = campusId;
            if (ModelState.IsValid)
            {
                _context.Buildings.Add(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Building created successfully!";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Campuses = await _context.Campuses.Where(c => c.IsActive).ToListAsync();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var building = await _context.Buildings.FindAsync(id);
            if (building == null) return NotFound();
            
            building.IsActive = !building.IsActive;
            building.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = building.IsActive ? "Building activated!" : "Building deactivated!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var building = await _context.Buildings.FindAsync(id);
            if (building == null) return NotFound();
            
            ViewBag.Campuses = await _context.Campuses.Where(c => c.IsActive).ToListAsync();
            return View(building);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Building model, int campusId)
        {
            model.CampusId = campusId;
            if (ModelState.IsValid)
            {
                var building = await _context.Buildings.FindAsync(model.Id);
                if (building == null) return NotFound();

                building.BuildingName = model.BuildingName;
                building.Code = model.Code;
                building.Floors = model.Floors;
                building.Description = model.Description;
                building.CampusId = campusId;
                building.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Building updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Campuses = await _context.Campuses.Where(c => c.IsActive).ToListAsync();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Rooms(int buildingId)
        {
            var building = await _context.Buildings
                .Include(b => b.Rooms)
                .FirstOrDefaultAsync(b => b.Id == buildingId);
            
            if (building == null) return NotFound();
            return View(building);
        }

        [HttpGet]
        public IActionResult CreateRoom(int buildingId)
        {
            ViewBag.BuildingId = buildingId;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateRoom(int buildingId, int floor, string roomNumber, string? roomType = null)
        {
            var building = await _context.Buildings.FindAsync(buildingId);
            if (building == null) return NotFound();

            var room = new ZEGU.Core.Entities.Shared.Room
            {
                BuildingId = buildingId,
                Floor = floor,
                RoomNumber = roomNumber,
                RoomType = roomType,
                IsActive = true
            };

            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Room created successfully!";
            return RedirectToAction(nameof(Rooms), new { buildingId });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleRoomActive(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room == null) return NotFound();

            room.IsActive = !room.IsActive;
            room.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = room.IsActive ? "Room activated!" : "Room deactivated!";
            return RedirectToAction(nameof(Rooms), new { buildingId = room.BuildingId });
        }
    }

    [Area("Admin")]
    [Authorize(Policy = "RequireAdmin")]
    public class TechniciansController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TechniciansController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var technicians = await _context.Technicians
                .Include(t => t.User)
                .OrderBy(t => t.TechnicianType)
                .ToListAsync();
            return View(technicians);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Users = await _context.Users.Where(u => u.IsActive).ToListAsync();
            ViewBag.Types = Enum.GetValues(typeof(TechnicianType)).Cast<TechnicianType>().ToList();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(string userId, TechnicianType technicianType, string? specialization = null, string? phoneNumber = null)
        {
            if (ModelState.IsValid)
            {
                _context.Technicians.Add(new Technician
                {
                    UserId = userId,
                    TechnicianType = technicianType,
                    Specialization = specialization,
                    PhoneNumber = phoneNumber,
                    IsAvailable = true
                });
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Technician created successfully!";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Users = await _context.Users.Where(u => u.IsActive).ToListAsync();
            ViewBag.Types = Enum.GetValues(typeof(TechnicianType)).Cast<TechnicianType>().ToList();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ToggleAvailability(int id)
        {
            var technician = await _context.Technicians.FindAsync(id);
            if (technician == null) return NotFound();
            
            technician.IsAvailable = !technician.IsAvailable;
            technician.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = technician.IsAvailable ? "Technician marked as available!" : "Technician marked as unavailable!";
            return RedirectToAction(nameof(Index));
        }
    }

    [Area("Admin")]
    [Authorize(Policy = "RequireAdmin")]
    public class MaterialsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditService _auditService;

        public MaterialsController(ApplicationDbContext context, AuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        public async Task<IActionResult> Index()
        {
            var materials = await _context.Materials.OrderBy(m => m.MaterialName).ToListAsync();
            return View(materials);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Material model)
        {
            if (ModelState.IsValid)
            {
                _context.Materials.Add(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Material created successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var material = await _context.Materials.FindAsync(id);
            if (material == null) return NotFound();

            material.IsActive = !material.IsActive;
            material.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = material.IsActive ? "Material activated!" : "Material deactivated!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> AdjustStock(int id)
        {
            var material = await _context.Materials.FindAsync(id);
            if (material == null) return NotFound();
            return View(material);
        }

        [HttpPost]
        public async Task<IActionResult> AdjustStock(int id, int quantity, string reason)
        {
            var material = await _context.Materials.FindAsync(id);
            if (material == null) return NotFound();

            if (quantity == 0)
            {
                ModelState.AddModelError("", "Adjustment quantity cannot be zero.");
                return View(material);
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                ModelState.AddModelError("", "A reason is required for stock adjustments.");
                return View(material);
            }

            var oldStock = material.CurrentStock;
            material.CurrentStock = Math.Max(0, material.CurrentStock + quantity);
            material.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(User.FindFirstValue(ClaimTypes.NameIdentifier), User.Identity?.Name,
                "AdjustStock", "Material", entityId: material.Id,
                oldValues: new { CurrentStock = oldStock },
                newValues: new { material.CurrentStock, quantity, reason },
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString());

            TempData["SuccessMessage"] = $"Stock adjusted from {oldStock} to {material.CurrentStock}.";
            return RedirectToAction(nameof(Index));
        }
    }

    [Area("Admin")]
    [Authorize(Policy = "RequireAdmin")]
    public class CampusesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CampusesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var campuses = await _context.Campuses.OrderBy(c => c.CampusName).ToListAsync();
            return View(campuses);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Campus model)
        {
            if (ModelState.IsValid)
            {
                _context.Campuses.Add(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Campus created successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var campus = await _context.Campuses.FindAsync(id);
            if (campus == null) return NotFound();
            return View(campus);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Campus model)
        {
            if (ModelState.IsValid)
            {
                var campus = await _context.Campuses.FindAsync(model.Id);
                if (campus == null) return NotFound();

                campus.CampusName = model.CampusName;
                campus.Code = model.Code;
                campus.Address = model.Address;
                campus.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Campus updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var campus = await _context.Campuses.FindAsync(id);
            if (campus == null) return NotFound();
            
            campus.IsActive = !campus.IsActive;
            campus.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = campus.IsActive ? "Campus activated!" : "Campus deactivated!";
            return RedirectToAction(nameof(Index));
        }
    }

    [Area("Admin")]
    [Authorize(Policy = "RequireManagerOrAdmin")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(DateTime? startDate = null, DateTime? endDate = null)
        {
            var start = startDate.HasValue ? DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc) : DateTime.UtcNow.AddMonths(-1);
            var end = endDate.HasValue ? DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc) : DateTime.UtcNow;

            var query = _context.MaintenanceRequests
                .Include(r => r.Category)
                .Include(r => r.Assignments)
                .Include(r => r.Department)
                .Include(r => r.Location)
                .ThenInclude(l => l.Building)
                .Where(r => r.IsActive && r.CreatedAt >= start && r.CreatedAt <= end);

            var requests = await query.ToListAsync();

            var report = new ReportsViewModel
            {
                StartDate = start,
                EndDate = end,
                TotalRequests = requests.Count,
                CompletedRequests = requests.Count(r => r.Status == MaintenanceRequestStatus.Completed),
                TotalCost = requests.Sum(r => r.TotalCost),
                AverageResolutionDays = requests
                    .Where(r => r.CompletedAt.HasValue)
                    .Select(r => (r.CompletedAt!.Value - r.CreatedAt).TotalDays)
                    .DefaultIfEmpty()
                    .Average(),

                RequestsByCategory = requests
                    .GroupBy(r => r.Category.CategoryName)
                    .Select(g => new CategoryReportItem
                    {
                        CategoryName = g.Key,
                        Count = g.Count(),
                        TotalCost = g.Sum(r => r.TotalCost)
                    })
                    .ToList(),

                RequestsByStatus = requests
                    .GroupBy(r => r.Status.ToString())
                    .Select(g => new StatusReportItem
                    {
                        Status = g.Key,
                        Count = g.Count()
                    })
                    .ToList(),

                RequestsByPriority = requests
                    .GroupBy(r => r.Priority.ToString())
                    .Select(g => new PriorityReportItem
                    {
                        Priority = g.Key,
                        Count = g.Count()
                    })
                    .ToList(),

                RequestsByDepartment = requests
                    .GroupBy(r => r.Department != null ? r.Department.DepartmentName : "Unassigned")
                    .Select(g => new DepartmentReportItem
                    {
                        DepartmentName = g.Key,
                        Count = g.Count(),
                        TotalCost = g.Sum(r => r.TotalCost)
                    })
                    .ToList(),

                RequestsByLocation = requests
                    .GroupBy(r => r.Location.Building!.BuildingName)
                    .Select(g => new LocationReportItem
                    {
                        BuildingName = g.Key,
                        Count = g.Count(),
                        TotalCost = g.Sum(r => r.TotalCost)
                    })
                    .ToList(),

                TechnicianPerformance = await _context.Technicians
                    .Include(t => t.User)
                    .Include(t => t.Assignments)
                    .Include(t => t.WorkLogs)
                    .Select(t => new TechnicianPerformanceItem
                    {
                        TechnicianName = t.User != null ? t.User.FirstName + " " + t.User.LastName : "Unassigned",
                        TechnicianType = t.TechnicianType.ToString(),
                        TotalAssignments = t.Assignments.Count(a => a.IsActive && a.Request.CreatedAt >= start && a.Request.CreatedAt <= end),
                        TotalWorkLogs = t.WorkLogs.Count(w => w.IsActive),
                        TotalHours = t.WorkLogs.Where(w => w.IsActive).Sum(w => w.HoursSpent),
                        CompletedRequests = t.Assignments.Count(a => a.IsActive && a.Request.Status == MaintenanceRequestStatus.Completed && a.Request.CreatedAt >= start && a.Request.CreatedAt <= end),
                        TotalLaborCost = t.WorkLogs.Where(w => w.IsActive).Sum(w => w.HoursSpent * w.LaborRate)
                    })
                    .ToListAsync(),

                MonthlyTrends = await Task.Run(() => requests
                    .GroupBy(r => r.CreatedAt.Year)
                    .Select(g => new MonthlyReportItem
                    {
                        Month = g.Key.ToString(),
                        RequestCount = g.Count(),
                        CompletedCount = g.Count(r => r.Status == MaintenanceRequestStatus.Completed),
                        TotalCost = g.Sum(r => r.TotalCost)
                    })
                    .OrderBy(m => m.Month)
                    .ToList())
            };

            return View(report);
        }

        public async Task<IActionResult> ExportCsv(DateTime? startDate = null, DateTime? endDate = null)
        {
            var start = startDate.HasValue ? DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc) : DateTime.UtcNow.AddMonths(-1);
            var end = endDate.HasValue ? DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc) : DateTime.UtcNow;

            var requests = await _context.MaintenanceRequests
                .Include(r => r.Category)
                .Include(r => r.User)
                .Include(r => r.Location)
                .Where(r => r.IsActive && r.CreatedAt >= start && r.CreatedAt <= end)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var csv = new System.Text.StringBuilder();
            csv.AppendLine("RequestNumber,Title,Category,Requester,Location,Status,Priority,LaborCost,MaterialCost,OtherCost,TotalCost,CreatedAt,CompletedAt");

            foreach (var r in requests)
            {
                csv.AppendLine($"{r.RequestNumber},{EscapeCsv(r.Title)},{r.Category.CategoryName},{r.User.FirstName} {r.User.LastName},{r.Location.RoomNumber},{r.Status},{r.Priority},{r.LaborCost},{r.MaterialCost},{r.OtherCost},{r.TotalCost},{r.CreatedAt:yyyy-MM-dd},{r.CompletedAt:yyyy-MM-dd}");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"reports-{start:yyyyMMdd}-{end:yyyyMMdd}.csv");
        }

        private static string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }
            return value;
        }
    }
}
