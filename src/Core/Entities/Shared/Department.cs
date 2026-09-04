using ZEGU.Core.Common;
using ZEGU.Core.Entities.Identity;
using ZEGU.Core.Entities.Maintenance;

namespace ZEGU.Core.Entities.Shared
{
    public class Department : BaseEntity
    {
        public string DepartmentName { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? HeadOfDepartment { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        
        public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
        public ICollection<MaintenanceRequest> MaintenanceRequests { get; set; } = new List<MaintenanceRequest>();
    }
}
