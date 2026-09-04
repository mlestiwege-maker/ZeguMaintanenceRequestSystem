using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using ZEGU.Core.Common;
using ZEGU.Core.Entities.Identity;
using ZEGU.Core.Entities.Shared;
using ZEGU.Core.Enums;

namespace ZEGU.Core.Entities.Maintenance
{
    public class MaintenanceRequest : BaseEntity
    {
        [MaxLength(50)]
        public string RequestNumber { get; set; } = string.Empty;
        
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;
        
        public int? DepartmentId { get; set; }
        public Department? Department { get; set; }
        
        public int CategoryId { get; set; }
        public MaintenanceCategory Category { get; set; } = null!;
        
        public int LocationId { get; set; }
        public Room Location { get; set; } = null!;
        
        public int? AssetId { get; set; }
        public Asset? Asset { get; set; }
        
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;
        
        public RequestPriority Priority { get; set; } = RequestPriority.Normal;
        
        public MaintenanceRequestStatus Status { get; set; } = MaintenanceRequestStatus.Submitted;
        
        public string? RejectionReason { get; set; }
        
        public DateTime? CompletedAt { get; set; }
        public DateTime? ClosedAt { get; set; }

        public decimal? LaborCost { get; set; }
        public decimal? MaterialCost { get; set; }
        public decimal? OtherCost { get; set; }
        public decimal? TotalCost => (LaborCost ?? 0) + (MaterialCost ?? 0) + (OtherCost ?? 0);

        public string? AfterPhotoPath { get; set; }

        public ICollection<RequestStatusHistory> StatusHistory { get; set; } = new List<RequestStatusHistory>();
        public ICollection<RequestComment> Comments { get; set; } = new List<RequestComment>();
        public ICollection<RequestAttachment> Attachments { get; set; } = new List<RequestAttachment>();
        public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
        public ICollection<WorkLog> WorkLogs { get; set; } = new List<WorkLog>();
        public Feedback? Feedback { get; set; }
    }
}
