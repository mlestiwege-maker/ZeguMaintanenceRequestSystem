using ZEGU.Core.Common;
using ZEGU.Core.Entities.Identity;
using ZEGU.Core.Entities.Maintenance;

namespace ZEGU.Core.Entities.Maintenance
{
    public class Assignment : BaseEntity
    {
        public int RequestId { get; set; }
        public MaintenanceRequest Request { get; set; } = null!;
        
        public int TechnicianId { get; set; }
        public Technician Technician { get; set; } = null!;
        
        public string? AssignedById { get; set; }
        public ApplicationUser? AssignedBy { get; set; }
        
        public DateTime? AcceptedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        
        public string? Notes { get; set; }
        
        public ICollection<WorkLog> WorkLogs { get; set; } = new List<WorkLog>();
    }
}
