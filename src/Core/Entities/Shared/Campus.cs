using ZEGU.Core.Common;

namespace ZEGU.Core.Entities.Shared
{
    public class Campus : BaseEntity
    {
        public string CampusName { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Address { get; set; }
        
        public ICollection<Building> Buildings { get; set; } = new List<Building>();
    }
}
