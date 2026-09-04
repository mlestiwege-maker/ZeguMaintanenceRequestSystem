using ZEGU.Core.Common;
using ZEGU.Core.Entities.Identity;

namespace ZEGU.Core.Entities.Maintenance
{
    public class RequestComment : BaseEntity
    {
        public int RequestId { get; set; }
        public MaintenanceRequest Request { get; set; } = null!;
        
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;
        
        public string CommentText { get; set; } = string.Empty;
        
        public bool IsInternal { get; set; } = false;
    }
}
