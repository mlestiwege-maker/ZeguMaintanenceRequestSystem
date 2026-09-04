using ZEGU.Core.Common;
using ZEGU.Core.Entities.Identity;

namespace ZEGU.Core.Entities.Maintenance
{
    public class Feedback : BaseEntity
    {
        public int RequestId { get; set; }
        public MaintenanceRequest Request { get; set; } = null!;
        
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;
        
        public int Rating { get; set; }
        public string? Comments { get; set; }
        public bool WorkSatisfactory { get; set; }
    }
}
