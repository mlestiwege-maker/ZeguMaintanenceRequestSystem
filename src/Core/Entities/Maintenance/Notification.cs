using ZEGU.Core.Common;
using ZEGU.Core.Entities.Identity;
using ZEGU.Core.Enums;

namespace ZEGU.Core.Entities.Maintenance
{
    public class Notification : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;
        
        public int? RequestId { get; set; }
        
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        
        public NotificationType Type { get; set; } = NotificationType.System;
        
        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; }
    }
}
