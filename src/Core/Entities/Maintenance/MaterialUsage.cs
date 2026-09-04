using ZEGU.Core.Common;
using ZEGU.Core.Entities.Maintenance;

namespace ZEGU.Core.Entities.Maintenance
{
    public class MaterialUsage : BaseEntity
    {
        public int WorkLogId { get; set; }
        public WorkLog WorkLog { get; set; } = null!;
        
        public int MaterialId { get; set; }
        public Material Material { get; set; } = null!;
        
        public decimal QuantityUsed { get; set; }
        public decimal? UnitCost { get; set; }
        public decimal? TotalCost { get; set; }
    }
}
