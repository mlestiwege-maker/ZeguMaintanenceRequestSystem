using ZEGU.Core.Common;
using ZEGU.Core.Entities.Maintenance;
using ZEGU.Core.Entities.Shared;

namespace ZEGU.Core.Entities.Maintenance
{
    public class Asset : BaseEntity
    {
        public string AssetName { get; set; } = string.Empty;
        public string? AssetCode { get; set; }
        public string? SerialNumber { get; set; }
        public int CategoryId { get; set; }
        public MaintenanceCategory Category { get; set; } = null!;
        public int LocationId { get; set; }
        public Room Location { get; set; } = null!;
        public DateTime? PurchaseDate { get; set; }
        public DateTime? WarrantyExpiry { get; set; }
        public string? Manufacturer { get; set; }
        public string? Model { get; set; }
        public string? Status { get; set; }
        public string? QrCodePath { get; set; }
        public string? BarcodePath { get; set; }
        public ICollection<MaintenanceRequest> MaintenanceRequests { get; set; } = new List<MaintenanceRequest>();
    }
}
