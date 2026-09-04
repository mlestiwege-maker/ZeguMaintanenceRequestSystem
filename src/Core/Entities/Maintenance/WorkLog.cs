using ZEGU.Core.Common;
using ZEGU.Core.Entities.Identity;
using ZEGU.Core.Entities.Maintenance;

namespace ZEGU.Core.Entities.Maintenance
{
    public class WorkLog : BaseEntity
    {
        public int RequestId { get; set; }
        public MaintenanceRequest Request { get; set; } = null!;
        
        public int? TechnicianId { get; set; }
        public Technician? Technician { get; set; }
        
        public int? AssignmentId { get; set; }
        public Assignment? Assignment { get; set; }
        
        public string WorkPerformed { get; set; } = string.Empty;
        public decimal? HoursSpent { get; set; }
        public decimal? LaborRate { get; set; }
        public decimal? LaborCost => HoursSpent * LaborRate;
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        
        public ICollection<MaterialUsage> MaterialUsage { get; set; } = new List<MaterialUsage>();
    }
}
