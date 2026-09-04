namespace ZEGU.WebApp.ViewModels.Works
{
    public class WorksDashboardViewModel
    {
        public int TotalRequests { get; set; }
        public int PendingRequests { get; set; }
        public int InProgressRequests { get; set; }
        public int CompletedRequests { get; set; }
        public int EmergencyRequests { get; set; }
        public List<ZEGU.Core.Entities.Maintenance.MaintenanceRequest> RecentRequests { get; set; } = new();
    }

    public class TechnicianWorkloadItem
    {
        public int TechnicianId { get; set; }
        public string TechnicianName { get; set; } = string.Empty;
        public string TechnicianType { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
        public int ActiveAssignments { get; set; }
        public int CompletedAssignments { get; set; }
        public int PendingAssignments { get; set; }
        public decimal? TotalHoursThisMonth { get; set; }
        public decimal? TotalLaborCost { get; set; }
        public double? AvgCompletionDays { get; set; }
        public int WorkLogCount { get; set; }
        public List<ZEGU.Core.Entities.Maintenance.MaintenanceRequest> CurrentRequests { get; set; } = new();
    }

    public class TechnicianWorkloadViewModel
    {
        public List<TechnicianWorkloadItem> Technicians { get; set; } = new();
        public int TotalTechnicians { get; set; }
        public int AvailableTechnicians { get; set; }
        public int BusyTechnicians { get; set; }
        public int TotalActiveAssignments { get; set; }
    }
}
