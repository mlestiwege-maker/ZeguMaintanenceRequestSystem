using ZEGU.Core.Common;

namespace ZEGU.Core.Entities.Maintenance
{
    public class Material : BaseEntity
    {
        public string MaterialName { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? Unit { get; set; }
        public decimal? UnitCost { get; set; }
        public string? Supplier { get; set; }
        public int MinimumStock { get; set; } = 0;
        public int CurrentStock { get; set; } = 0;
        
        public ICollection<MaterialUsage> MaterialUsage { get; set; } = new List<MaterialUsage>();
    }
}
