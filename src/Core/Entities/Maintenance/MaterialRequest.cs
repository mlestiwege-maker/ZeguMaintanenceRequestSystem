using ZEGU.Core.Common;
using ZEGU.Core.Entities.Identity;
using ZEGU.Core.Enums;

namespace ZEGU.Core.Entities.Maintenance
{
    public class MaterialRequest : BaseEntity
    {
        public int RequestId { get; set; }
        public MaintenanceRequest Request { get; set; } = null!;

        public int TechnicianId { get; set; }
        public Technician Technician { get; set; } = null!;

        public int MaterialId { get; set; }
        public Material Material { get; set; } = null!;

        public decimal QuantityRequested { get; set; }
        public string? Notes { get; set; }

        public MaterialRequestStatus Status { get; set; } = MaterialRequestStatus.Pending;

        public string? ReviewedById { get; set; }
        public ApplicationUser? ReviewedBy { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewNotes { get; set; }
    }
}
