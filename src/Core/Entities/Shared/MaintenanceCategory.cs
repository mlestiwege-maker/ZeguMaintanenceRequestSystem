using ZEGU.Core.Common;
using ZEGU.Core.Entities.Maintenance;

namespace ZEGU.Core.Entities.Shared
{
    public class MaintenanceCategory : BaseEntity
    {
        public string CategoryName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Icon { get; set; }
        public int? SLAHours { get; set; }
        
        public ICollection<MaintenanceRequest> MaintenanceRequests { get; set; } = new List<MaintenanceRequest>();
        public ICollection<PreventiveMaintenanceSchedule> PreventiveMaintenanceSchedules { get; set; } = new List<PreventiveMaintenanceSchedule>();
        public ICollection<Asset> Assets { get; set; } = new List<Asset>();
    }
}
