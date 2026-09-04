using System.ComponentModel.DataAnnotations;
using ZEGU.Core.Common;
using ZEGU.Core.Entities.Identity;
using ZEGU.Core.Entities.Shared;
using ZEGU.Core.Enums;

namespace ZEGU.Core.Entities.Ticketing
{
    public class Ticket : BaseEntity
    {
        [MaxLength(50)]
        public string TicketNumber { get; set; } = string.Empty;
        
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;
        
        public int? DepartmentId { get; set; }
        public Department? Department { get; set; }
        
        public TicketType Type { get; set; } = TicketType.IT;
        
        [MaxLength(255)]
        public string Subject { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;
        
        public TicketPriority Priority { get; set; } = TicketPriority.Normal;
        
        public TicketStatus Status { get; set; } = TicketStatus.Open;
        
        public int? LocationId { get; set; }
        public Room? Location { get; set; }
        
        public string? AssignedToId { get; set; }
        public ApplicationUser? AssignedTo { get; set; }
        
        public DateTime? ResolvedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        
        public bool CanTransferToMaintenance { get; set; } = true;
        
        public int? TransferredToRequestId { get; set; }
    }
}
