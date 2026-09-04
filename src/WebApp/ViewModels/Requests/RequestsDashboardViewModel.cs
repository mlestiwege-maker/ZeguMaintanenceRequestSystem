namespace ZEGU.WebApp.ViewModels.Requests
{
    public class RequestsDashboardViewModel
    {
        public int TotalRequests { get; set; }
        public int PendingRequests { get; set; }
        public int InProgressRequests { get; set; }
        public int CompletedRequests { get; set; }
        public List<ZEGU.Core.Entities.Maintenance.MaintenanceRequest> RecentRequests { get; set; } = new();
    }
}
