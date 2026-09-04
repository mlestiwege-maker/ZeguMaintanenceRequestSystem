using ZEGU.Core.Common;
using ZEGU.Core.Entities.Identity;
using ZEGU.Core.Entities.Maintenance;

namespace ZEGU.Core.Entities.Maintenance
{
    public class PreventiveMaintenanceRecord : BaseEntity
    {
        public int ScheduleId { get; set; }
        public PreventiveMaintenanceSchedule Schedule { get; set; } = null!;
        public int? TechnicianId { get; set; }
        public Technician? Technician { get; set; }
        public string? PerformedById { get; set; }
        public ApplicationUser? PerformedBy { get; set; }
        public string WorkPerformed { get; set; } = string.Empty;
        public DateTime PerformedAt { get; set; }
        public DateTime? NextDue { get; set; }
        public string? Notes { get; set; }
    }
}
