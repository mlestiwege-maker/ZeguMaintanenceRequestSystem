using System.ComponentModel.DataAnnotations;
using ZEGU.Core.Common;
using ZEGU.Core.Entities.Maintenance;

namespace ZEGU.Core.Entities.Shared
{
    public class Room : BaseEntity
    {
        public int BuildingId { get; set; }
        public Building? Building { get; set; }
        
        public int Floor { get; set; }
        
        [MaxLength(50)]
        public string RoomNumber { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string? RoomType { get; set; }
        
        public string? Description { get; set; }
        
        public ICollection<MaintenanceRequest> MaintenanceRequests { get; set; } = new List<MaintenanceRequest>();
        public ICollection<PreventiveMaintenanceSchedule> PreventiveMaintenanceSchedules { get; set; } = new List<PreventiveMaintenanceSchedule>();
        public ICollection<Asset> Assets { get; set; } = new List<Asset>();
    }
}
