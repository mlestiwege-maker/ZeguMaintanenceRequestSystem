namespace ZEGU.WebApp.ViewModels.Admin
{
    public class CategoryReportItem
    {
        public string CategoryName { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal? TotalCost { get; set; }
    }

    public class StatusReportItem
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class PriorityReportItem
    {
        public string Priority { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class DepartmentReportItem
    {
        public string DepartmentName { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal? TotalCost { get; set; }
    }

    public class LocationReportItem
    {
        public string BuildingName { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal? TotalCost { get; set; }
    }

    public class TechnicianPerformanceItem
    {
        public string TechnicianName { get; set; } = string.Empty;
        public string TechnicianType { get; set; } = string.Empty;
        public int TotalAssignments { get; set; }
        public int TotalWorkLogs { get; set; }
        public decimal? TotalHours { get; set; }
        public int CompletedRequests { get; set; }
        public decimal? TotalLaborCost { get; set; }
    }

    public class MonthlyReportItem
    {
        public string Month { get; set; } = string.Empty;
        public int RequestCount { get; set; }
        public int CompletedCount { get; set; }
        public decimal? TotalCost { get; set; }
    }

    public class ReportsViewModel
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public List<CategoryReportItem> RequestsByCategory { get; set; } = new();
        public List<StatusReportItem> RequestsByStatus { get; set; } = new();
        public List<PriorityReportItem> RequestsByPriority { get; set; } = new();
        public List<DepartmentReportItem> RequestsByDepartment { get; set; } = new();
        public List<LocationReportItem> RequestsByLocation { get; set; } = new();
        public List<TechnicianPerformanceItem> TechnicianPerformance { get; set; } = new();
        public List<MonthlyReportItem> MonthlyTrends { get; set; } = new();
        public decimal? TotalCost { get; set; }
        public int TotalRequests { get; set; }
        public int CompletedRequests { get; set; }
        public double? AverageResolutionDays { get; set; }
    }
}
