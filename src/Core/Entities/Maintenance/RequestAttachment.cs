using ZEGU.Core.Common;
using ZEGU.Core.Entities.Identity;

namespace ZEGU.Core.Entities.Maintenance
{
    public class RequestAttachment : BaseEntity
    {
        public int RequestId { get; set; }
        public MaintenanceRequest Request { get; set; } = null!;
        
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long? FileSize { get; set; }
        public string? FileType { get; set; }
        
        public string? UploadedById { get; set; }
        public ApplicationUser? UploadedBy { get; set; }
        
        public bool IsBeforePhoto { get; set; } = false;
    }
}
