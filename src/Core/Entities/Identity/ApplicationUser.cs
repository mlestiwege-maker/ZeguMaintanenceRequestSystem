using Microsoft.AspNetCore.Identity;
using ZEGU.Core.Enums;
using ZEGU.Core.Entities.Shared;

namespace ZEGU.Core.Entities.Identity
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public new string? PhoneNumber { get; set; }
        public UserRole Role { get; set; }
        public int? DepartmentId { get; set; }
        public Department? Department { get; set; }
        public string? StudentNumber { get; set; }
        public string? StaffNumber { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
    }
}
