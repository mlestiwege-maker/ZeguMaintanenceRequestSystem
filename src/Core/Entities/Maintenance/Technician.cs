using ZEGU.Core.Common;
using ZEGU.Core.Entities.Identity;
using ZEGU.Core.Enums;

namespace ZEGU.Core.Entities.Maintenance
{
    public class Technician : BaseEntity
    {
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }
        
        public TechnicianType TechnicianType { get; set; }
        public string? Specialization { get; set; }
        public string? PhoneNumber { get; set; }
        public bool IsAvailable { get; set; } = true;
        
        public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
        public ICollection<WorkLog> WorkLogs { get; set; } = new List<WorkLog>();
    }
}
