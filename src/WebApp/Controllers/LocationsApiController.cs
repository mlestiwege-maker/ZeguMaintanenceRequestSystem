using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZEGU.Core.Entities.Shared;
using ZEGU.Infrastructure.Data;

namespace ZEGU.WebApp.Controllers
{
    [ApiController]
    [Route("api/locations")]
    [AllowAnonymous]
    public class LocationsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LocationsApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("rooms")]
        public async Task<IActionResult> GetRooms([FromQuery] int buildingId)
        {
            var rooms = await _context.Rooms
                .Where(r => r.BuildingId == buildingId && r.IsActive)
                .Select(r => new
                {
                    r.Id,
                    r.RoomNumber,
                    r.RoomType
                })
                .ToListAsync();

            return Ok(rooms);
        }

        [HttpGet("assets")]
        public async Task<IActionResult> GetAssets([FromQuery] int locationId)
        {
            var assets = await _context.Assets
                .Where(a => a.LocationId == locationId && a.IsActive)
                .Select(a => new
                {
                    a.Id,
                    a.AssetName,
                    a.AssetCode
                })
                .ToListAsync();

            return Ok(assets);
        }
    }
}
