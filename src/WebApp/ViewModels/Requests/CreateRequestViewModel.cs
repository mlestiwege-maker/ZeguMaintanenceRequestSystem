using System.ComponentModel.DataAnnotations;
using ZEGU.Core.Entities.Shared;
using ZEGU.Core.Enums;

namespace ZEGU.WebApp.ViewModels.Requests
{
    public class CreateRequestViewModel
    {
        [Required(ErrorMessage = "Title is required")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a category")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid category")]
        public int? CategoryId { get; set; }

        [Required(ErrorMessage = "Please select a room")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid room")]
        public int? LocationId { get; set; }

        public int? DepartmentId { get; set; }
        public int SelectedBuildingId { get; set; }
        public RequestPriority Priority { get; set; } = RequestPriority.Normal;
        public List<MaintenanceCategory> Categories { get; set; } = new();
        public List<Building> Buildings { get; set; } = new();
        public List<Department> Departments { get; set; } = new();
        public List<IFormFile> Photos { get; set; } = new();
    }
}
