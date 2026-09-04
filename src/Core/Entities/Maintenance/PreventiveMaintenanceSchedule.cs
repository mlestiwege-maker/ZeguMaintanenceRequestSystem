using ZEGU.Core.Common;
using ZEGU.Core.Entities.Shared;

namespace ZEGU.Core.Entities.Maintenance
{
    public class PreventiveMaintenanceSchedule : BaseEntity
    {
        public string ScheduleName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int CategoryId { get; set; }
        public MaintenanceCategory Category { get; set; } = null!;
        public int LocationId { get; set; }
        public Room Location { get; set; } = null!;
        public int? TechnicianId { get; set; }
        public Technician? Technician { get; set; }
        public string Frequency { get; set; } = string.Empty;
        public int FrequencyDays { get; set; }
        public DateTime? LastPerformed { get; set; }
        public DateTime NextDue { get; set; }
        public bool IsActive { get; set; } = true;
        public ICollection<PreventiveMaintenanceRecord> Records { get; set; } = new List<PreventiveMaintenanceRecord>();
    }
}
