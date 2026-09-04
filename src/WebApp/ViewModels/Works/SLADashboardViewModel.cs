namespace ZEGU.WebApp.ViewModels.Works
{
    public class SLADashboardViewModel
    {
        public int TotalActiveRequests { get; set; }
        public int OverdueRequests { get; set; }
        public int OnTrackRequests { get; set; }
        public List<ZEGU.Core.Entities.Maintenance.MaintenanceRequest> OverdueRequestList { get; set; } = new();
    }
}
