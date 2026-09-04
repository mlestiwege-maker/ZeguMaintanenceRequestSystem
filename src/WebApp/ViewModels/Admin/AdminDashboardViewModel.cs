namespace ZEGU.WebApp.ViewModels.Admin
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalRequests { get; set; }
        public int TotalTechnicians { get; set; }
        public int TotalCategories { get; set; }
        public int TotalDepartments { get; set; }
        public int TotalCampuses { get; set; }
        public int TotalBuildings { get; set; }
        public int TotalRooms { get; set; }
        public int PendingRequests { get; set; }
        public int OpenRequests { get; set; }
        public int CompletedRequests { get; set; }
        public List<ZEGU.Core.Entities.Maintenance.MaintenanceRequest> RecentRequests { get; set; } = new();
    }
}
