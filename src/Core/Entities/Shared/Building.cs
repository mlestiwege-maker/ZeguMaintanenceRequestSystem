using ZEGU.Core.Common;

namespace ZEGU.Core.Entities.Shared
{
    public class Building : BaseEntity
    {
        public int CampusId { get; set; }
        public Campus? Campus { get; set; }
        
        public string BuildingName { get; set; } = string.Empty;
        public string? Code { get; set; }
        public int Floors { get; set; } = 1;
        public string? Description { get; set; }
        
        public ICollection<Room> Rooms { get; set; } = new List<Room>();
    }
}
