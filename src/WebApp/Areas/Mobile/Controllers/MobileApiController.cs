using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ZEGU.Core.Entities.Maintenance;
using ZEGU.Core.Enums;
using ZEGU.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using ZEGU.Core.Entities.Identity;

namespace ZEGU.WebApp.Areas.Mobile.Controllers
{
    [Area("Mobile")]
    [ApiController]
    [Route("api/mobile/[action]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class MobileApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;

        public MobileApiController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _configuration = configuration;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Authenticated request is missing a user id claim.");

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _userManager.FindByNameAsync(request.Username);
            if (user == null) return Unauthorized(new { message = "Invalid username or password" });

            var result = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!result) return Unauthorized(new { message = "Invalid username or password" });

            if (!user.IsActive) return Unauthorized(new { message = "Account is deactivated" });

            var jwtKey = _configuration["Jwt:Key"]!;
            var jwtIssuer = _configuration["Jwt:Issuer"] ?? "ZEGU.MRS";
            var jwtAudience = _configuration["Jwt:Audience"] ?? "ZEGU.MRS.Mobile";

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName ?? user.Email ?? user.Id),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddHours(12);

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: expires,
                signingCredentials: credentials);

            return Ok(new
            {
                accessToken = new JwtSecurityTokenHandler().WriteToken(token),
                expiresAt = expires,
                user.Id,
                user.UserName,
                user.Email,
                user.FirstName,
                user.LastName,
                Role = user.Role.ToString()
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _context.MaintenanceCategories
                .Where(c => c.IsActive)
                .Select(c => new { c.Id, c.CategoryName, c.Description, c.SLAHours })
                .ToListAsync();
            return Ok(categories);
        }

        [HttpGet]
        public async Task<IActionResult> GetBuildings()
        {
            var buildings = await _context.Buildings
                .Include(b => b.Campus)
                .Where(b => b.IsActive)
                .Select(b => new { b.Id, b.BuildingName, b.Code, b.CampusId, CampusName = b.Campus!.CampusName })
                .ToListAsync();
            return Ok(buildings);
        }

        [HttpGet]
        public async Task<IActionResult> GetRooms(int buildingId)
        {
            var rooms = await _context.Rooms
                .Where(r => r.BuildingId == buildingId && r.IsActive)
                .Select(r => new { r.Id, r.RoomNumber, r.RoomType, r.Floor })
                .ToListAsync();
            return Ok(rooms);
        }

        [HttpGet]
        public async Task<IActionResult> GetMyRequests()
        {
            var requests = await _context.MaintenanceRequests
                .Include(r => r.Category)
                .Include(r => r.Location)
                .Include(r => r.Location.Building)
                .Where(r => r.UserId == CurrentUserId && r.IsActive)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    r.Id,
                    r.RequestNumber,
                    r.Title,
                    r.Description,
                    r.Status,
                    r.Priority,
                    r.CreatedAt,
                    CategoryName = r.Category.CategoryName,
                    BuildingName = r.Location.Building!.BuildingName,
                    RoomNumber = r.Location.RoomNumber
                })
                .ToListAsync();
            return Ok(requests);
        }

        [HttpGet]
        public async Task<IActionResult> GetRequestDetails(int id)
        {
            var request = await _context.MaintenanceRequests
                .Include(r => r.Category)
                .Include(r => r.Location)
                .Include(r => r.Location.Building)
                .Include(r => r.StatusHistory)
                .Include(r => r.Comments)
                .Include(r => r.Attachments)
                .FirstOrDefaultAsync(r => r.Id == id && r.IsActive);

            if (request == null) return NotFound();
            if (request.UserId != CurrentUserId) return Forbid(JwtBearerDefaults.AuthenticationScheme);

            return Ok(new
            {
                request.Id,
                request.RequestNumber,
                request.Title,
                request.Description,
                request.Status,
                request.Priority,
                request.CreatedAt,
                request.CompletedAt,
                request.ClosedAt,
                CategoryName = request.Category.CategoryName,
                BuildingName = request.Location.Building!.BuildingName,
                RoomNumber = request.Location.RoomNumber,
                StatusHistory = request.StatusHistory.Select(h => new { h.NewStatus, CreatedAt = h.CreatedAt, h.Comments }),
                Comments = request.Comments.Select(c => new { c.CommentText, c.CreatedAt }),
                Attachments = request.Attachments.Select(a => new { a.FileName, a.FilePath })
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateRequest([FromBody] MobileCreateRequestRequest request)
        {
            var user = await _userManager.FindByIdAsync(CurrentUserId);
            if (user == null) return Unauthorized();

            int? validatedAssetId = null;
            if (request.AssetId.HasValue)
            {
                var asset = await _context.Assets.FirstOrDefaultAsync(a => a.Id == request.AssetId.Value && a.IsActive && a.LocationId == request.LocationId);
                if (asset != null) validatedAssetId = asset.Id;
            }

            var requestNumber = $"MRS-{DateTime.UtcNow.Year}-TEMP";
            var maintenanceRequest = new MaintenanceRequest
            {
                RequestNumber = requestNumber,
                UserId = user.Id,
                CategoryId = request.CategoryId,
                LocationId = request.LocationId,
                AssetId = validatedAssetId,
                Title = request.Title,
                Description = request.Description,
                Priority = request.Priority,
                Status = MaintenanceRequestStatus.Submitted
            };

            _context.MaintenanceRequests.Add(maintenanceRequest);
            await _context.SaveChangesAsync();

            maintenanceRequest.RequestNumber = $"MRS-{DateTime.UtcNow.Year}-{maintenanceRequest.Id:D6}";
            await _context.SaveChangesAsync();

            return Ok(new { message = "Request created successfully", requestNumber = maintenanceRequest.RequestNumber, id = maintenanceRequest.Id });
        }

        [HttpGet]
        public async Task<IActionResult> ScanAsset(int assetId)
        {
            var asset = await _context.Assets
                .Include(a => a.Category)
                .Include(a => a.Location)
                .Include(a => a.Location.Building)
                .FirstOrDefaultAsync(a => a.Id == assetId && a.IsActive);

            if (asset == null) return NotFound(new { message = "Asset not found" });

            return Ok(new
            {
                asset.Id,
                asset.AssetName,
                asset.AssetCode,
                asset.SerialNumber,
                asset.Manufacturer,
                asset.Model,
                asset.Status,
                asset.Condition,
                CategoryName = asset.Category.CategoryName,
                CategoryId = asset.CategoryId,
                BuildingName = asset.Location.Building!.BuildingName,
                RoomNumber = asset.Location.RoomNumber,
                LocationId = asset.LocationId,
                asset.PurchaseDate,
                asset.WarrantyExpiry
            });
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class MobileCreateRequestRequest
    {
        public int CategoryId { get; set; }
        public int LocationId { get; set; }
        public int? AssetId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public RequestPriority Priority { get; set; } = RequestPriority.Normal;
    }
}
