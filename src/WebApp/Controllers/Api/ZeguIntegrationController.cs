using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using ZEGU.Core.Entities.Maintenance;
using ZEGU.Core.Entities.Ticketing;
using ZEGU.Core.Enums;
using ZEGU.Infrastructure.Data;

namespace ZEGU.WebApp.Controllers.Api
{
    [ApiController]
    [Route("api/zegu")]
    [Authorize]
    public class ZeguIntegrationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ZeguIntegrationController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("tickets/transfer-to-maintenance")]
        public async Task<IActionResult> TransferTicketToMaintenance([FromBody] TransferTicketRequest request)
        {
            var ticket = await _context.Tickets
                .FirstOrDefaultAsync(t => t.Id == request.TicketId && t.IsActive);

            if (ticket == null) return NotFound(new { message = "Ticket not found" });

            if (!ticket.CanTransferToMaintenance)
                return BadRequest(new { message = "This ticket cannot be transferred to maintenance" });

            var requestNumber = $"MRS-{DateTime.UtcNow.Year}-TEMP";
            var maintenanceRequest = new MaintenanceRequest
            {
                RequestNumber = requestNumber,
                UserId = ticket.UserId,
                CategoryId = request.CategoryId,
                LocationId = ticket.LocationId ?? 1,
                Title = ticket.Subject,
                Description = $"[Transferred from ZEGU Ticket {ticket.TicketNumber}]\n\n{ticket.Description}",
                Priority = ticket.Priority == TicketPriority.Critical ? RequestPriority.Emergency :
                           ticket.Priority == TicketPriority.High ? RequestPriority.High : RequestPriority.Normal,
                Status = MaintenanceRequestStatus.Submitted
            };

            _context.MaintenanceRequests.Add(maintenanceRequest);
            await _context.SaveChangesAsync();

            maintenanceRequest.RequestNumber = $"MRS-{DateTime.UtcNow.Year}-{maintenanceRequest.Id:D6}";
            ticket.TransferredToRequestId = maintenanceRequest.Id;
            ticket.Status = TicketStatus.Resolved;
            ticket.ResolvedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Ticket transferred to maintenance successfully",
                maintenanceRequestId = maintenanceRequest.Id,
                requestNumber = maintenanceRequest.RequestNumber
            });
        }

        [HttpGet("tickets")]
        public async Task<IActionResult> GetTickets([FromQuery] int? status = null)
        {
            var query = _context.Tickets
                .Include(t => t.User)
                .Include(t => t.Location)
                .Include(t => t.Location.Building)
                .Where(t => t.IsActive && t.CanTransferToMaintenance)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(t => t.Status == (TicketStatus)status.Value);
            }

            var tickets = await query
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new
                {
                    t.Id,
                    t.TicketNumber,
                    t.Subject,
                    t.Description,
                    t.Status,
                    t.Priority,
                    t.Type,
                    t.CreatedAt,
                    UserName = t.User.FirstName + " " + t.User.LastName,
                    Location = t.Location != null ? t.Location.Building.BuildingName + " - " + t.Location.RoomNumber : null
                })
                .ToListAsync();

            return Ok(tickets);
        }

        [HttpGet("requests")]
        public async Task<IActionResult> GetMaintenanceRequests([FromQuery] int? status = null)
        {
            var query = _context.MaintenanceRequests
                .Include(r => r.User)
                .Include(r => r.Category)
                .Include(r => r.Location)
                .Include(r => r.Location.Building)
                .Where(r => r.IsActive)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(r => r.Status == (MaintenanceRequestStatus)status);
            }

            var requests = await query
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
                    UserName = r.User.FirstName + " " + r.User.LastName,
                    CategoryName = r.Category.CategoryName,
                    Location = r.Location.Building.BuildingName + " - " + r.Location.RoomNumber
                })
                .ToListAsync();

            return Ok(requests);
        }

        [HttpGet("requests/{requestNumber}")]
        public async Task<IActionResult> GetRequestByNumber(string requestNumber)
        {
            var request = await _context.MaintenanceRequests
                .Include(r => r.User)
                .Include(r => r.Category)
                .Include(r => r.Location)
                .Include(r => r.Location.Building)
                .FirstOrDefaultAsync(r => r.RequestNumber == requestNumber && r.IsActive);

            if (request == null) return NotFound();

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
                UserName = request.User.FirstName + " " + request.User.LastName,
                CategoryName = request.Category.CategoryName,
                Location = request.Location.Building.BuildingName + " - " + request.Location.RoomNumber
            });
        }
    }

    public class TransferTicketRequest
    {
        public int TicketId { get; set; }
        public int CategoryId { get; set; }
        public int? LocationId { get; set; }
    }
}
