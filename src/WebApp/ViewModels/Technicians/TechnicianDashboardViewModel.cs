using ZEGU.Core.Entities.Maintenance;

namespace ZEGU.WebApp.ViewModels.Technicians
{
    public class TechnicianDashboardViewModel
    {
        public int TotalAssigned { get; set; }
        public int InProgressCount { get; set; }
        public int CompletedThisMonth { get; set; }
        public int OverdueCount { get; set; }
        public List<Assignment> ActiveAssignments { get; set; } = new();
        public List<Assignment> RecentlyCompleted { get; set; } = new();
    }
}
