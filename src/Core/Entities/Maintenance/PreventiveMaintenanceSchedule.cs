using ZEGU.Core.Common;
using ZEGU.Core.Entities.Identity;
using ZEGU.Core.Entities.Shared;
using ZEGU.Core.Enums;

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
        public int? AssetId { get; set; }
        public Asset? Asset { get; set; }
        public int? TechnicianId { get; set; }
        public Technician? Technician { get; set; }
        public string Frequency { get; set; } = string.Empty;
        public int FrequencyDays { get; set; }
        public DateTime? LastPerformed { get; set; }
        public DateTime NextDue { get; set; }
        public DateTime? LastReminderSentAt { get; set; }
        public PreventiveMaintenanceStatus Status { get; set; } = PreventiveMaintenanceStatus.Scheduled;
        public DateTime? StartedAt { get; set; }
        public string? StartedById { get; set; }
        public ApplicationUser? StartedBy { get; set; }
        public ICollection<PreventiveMaintenanceRecord> Records { get; set; } = new List<PreventiveMaintenanceRecord>();
    }
}
