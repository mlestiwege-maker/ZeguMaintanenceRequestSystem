using ZEGU.Core.Common;

namespace ZEGU.Core.Entities.Maintenance
{
    public class NotificationTemplate : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string Type { get; set; } = "Email";
        public string? Description { get; set; }
        public List<string> Placeholders { get; set; } = new();
    }
}
