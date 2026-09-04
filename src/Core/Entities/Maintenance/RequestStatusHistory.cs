using ZEGU.Core.Common;
using ZEGU.Core.Entities.Identity;
using ZEGU.Core.Enums;

namespace ZEGU.Core.Entities.Maintenance
{
    public class RequestStatusHistory : BaseEntity
    {
        public int RequestId { get; set; }
        public MaintenanceRequest Request { get; set; } = null!;
        
        public MaintenanceRequestStatus? OldStatus { get; set; }
        public MaintenanceRequestStatus NewStatus { get; set; }
        
        public string? ChangedById { get; set; }
        public ApplicationUser? ChangedBy { get; set; }
        
        public string? Comments { get; set; }
    }
}
