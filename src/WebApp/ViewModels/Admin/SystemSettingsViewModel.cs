namespace ZEGU.WebApp.ViewModels.Admin
{
    public class SystemSettingsViewModel
    {
        public string AdminEmail { get; set; } = string.Empty;
        public List<CategorySlaSummary> CategorySlaHours { get; set; } = new();
        public int MaxFileSize { get; set; }
        public string AppUrl { get; set; } = string.Empty;
        public string QrCodeApiUrl { get; set; } = string.Empty;
        public int TotalUsers { get; set; }
        public int TotalRequests { get; set; }
        public int TotalCategories { get; set; }
        public int TotalTechnicians { get; set; }
        public int TotalAssets { get; set; }
        public int TotalSchedules { get; set; }
        public int UnreadNotifications { get; set; }
    }

    public class CategorySlaSummary
    {
        public int Id { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int SLAHours { get; set; }
    }
}
