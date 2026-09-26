using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ZEGU.Core.Common;
using ZEGU.Core.Entities.Identity;
using ZEGU.Core.Entities.Maintenance;
using ZEGU.Core.Entities.Shared;
using ZEGU.Core.Entities.Ticketing;

namespace ZEGU.Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Department> Departments { get; set; }
        public DbSet<Campus> Campuses { get; set; }
        public DbSet<Building> Buildings { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<MaintenanceCategory> MaintenanceCategories { get; set; }
        public DbSet<MaintenanceRequest> MaintenanceRequests { get; set; }
        public DbSet<RequestStatusHistory> RequestStatusHistory { get; set; }
        public DbSet<RequestComment> RequestComments { get; set; }
        public DbSet<RequestAttachment> RequestAttachments { get; set; }
        public DbSet<Technician> Technicians { get; set; }
        public DbSet<Assignment> Assignments { get; set; }
        public DbSet<WorkLog> WorkLogs { get; set; }
        public DbSet<Material> Materials { get; set; }
        public DbSet<MaterialUsage> MaterialUsage { get; set; }
        public DbSet<MaterialRequest> MaterialRequests { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Asset> Assets { get; set; }
        public DbSet<PreventiveMaintenanceSchedule> PreventiveMaintenanceSchedules { get; set; }
        public DbSet<PreventiveMaintenanceRecord> PreventiveMaintenanceRecords { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<NotificationTemplate> NotificationTemplates { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Department
            builder.Entity<Department>()
                .HasIndex(d => d.Code)
                .IsUnique();

            // Campus
            builder.Entity<Campus>()
                .HasIndex(c => c.Code)
                .IsUnique();

            // Building
            builder.Entity<Building>()
                .HasIndex(b => new { b.CampusId, b.Code })
                .IsUnique();

            // Room
            builder.Entity<Room>()
                .HasIndex(r => new { r.BuildingId, r.Floor, r.RoomNumber })
                .IsUnique();

            // MaintenanceCategory
            builder.Entity<MaintenanceCategory>()
                .HasIndex(c => c.CategoryName)
                .IsUnique();

            // MaintenanceRequest
            builder.Entity<MaintenanceRequest>()
                .HasIndex(r => r.RequestNumber)
                .IsUnique();

            builder.Entity<MaintenanceRequest>()
                .Property(r => r.Priority)
                .HasConversion<string>();

            builder.Entity<MaintenanceRequest>()
                .Property(r => r.Status)
                .HasConversion<string>();

            // Ticket
            builder.Entity<Ticket>()
                .HasIndex(t => t.TicketNumber)
                .IsUnique();

            builder.Entity<Ticket>()
                .Property(t => t.Type)
                .HasConversion<string>();

            builder.Entity<Ticket>()
                .Property(t => t.Priority)
                .HasConversion<string>();

            builder.Entity<Ticket>()
                .Property(t => t.Status)
                .HasConversion<string>();

            // Technician
            builder.Entity<Technician>()
                .Property(t => t.TechnicianType)
                .HasConversion<string>();

            // ApplicationUser
            builder.Entity<ApplicationUser>()
                .Property(u => u.Role)
                .HasConversion<string>();

            // Relationships
            builder.Entity<Department>()
                .HasMany(d => d.Users)
                .WithOne(u => u.Department)
                .HasForeignKey(u => u.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Building>()
                .HasMany(b => b.Rooms)
                .WithOne(r => r.Building)
                .HasForeignKey(r => r.BuildingId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Campus>()
                .HasMany(c => c.Buildings)
                .WithOne(b => b.Campus)
                .HasForeignKey(b => b.CampusId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<MaintenanceCategory>()
                .HasMany(c => c.MaintenanceRequests)
                .WithOne(r => r.Category)
                .HasForeignKey(r => r.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Room>()
                .HasMany(r => r.MaintenanceRequests)
                .WithOne(r => r.Location)
                .HasForeignKey(r => r.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<MaintenanceRequest>()
                .HasMany(r => r.StatusHistory)
                .WithOne(h => h.Request)
                .HasForeignKey(h => h.RequestId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<MaintenanceRequest>()
                .HasMany(r => r.Comments)
                .WithOne(c => c.Request)
                .HasForeignKey(c => c.RequestId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<MaintenanceRequest>()
                .HasMany(r => r.Attachments)
                .WithOne(a => a.Request)
                .HasForeignKey(a => a.RequestId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<MaintenanceRequest>()
                .HasMany(r => r.Assignments)
                .WithOne(a => a.Request)
                .HasForeignKey(a => a.RequestId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<MaintenanceRequest>()
                .HasMany(r => r.WorkLogs)
                .WithOne(w => w.Request)
                .HasForeignKey(w => w.RequestId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Technician>()
                .HasMany(t => t.Assignments)
                .WithOne(a => a.Technician)
                .HasForeignKey(a => a.TechnicianId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Technician>()
                .HasMany(t => t.WorkLogs)
                .WithOne(w => w.Technician)
                .HasForeignKey(w => w.TechnicianId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Assignment>()
                .HasMany(a => a.WorkLogs)
                .WithOne(w => w.Assignment)
                .HasForeignKey(w => w.AssignmentId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<WorkLog>()
                .HasMany(w => w.MaterialUsage)
                .WithOne(m => m.WorkLog)
                .HasForeignKey(m => m.WorkLogId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Material>()
                .HasMany(m => m.MaterialUsage)
                .WithOne(m => m.Material)
                .HasForeignKey(m => m.MaterialId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<MaintenanceRequest>()
                .HasMany(r => r.MaterialRequests)
                .WithOne(mr => mr.Request)
                .HasForeignKey(mr => mr.RequestId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Technician>()
                .HasMany(t => t.MaterialRequests)
                .WithOne(mr => mr.Technician)
                .HasForeignKey(mr => mr.TechnicianId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Material>()
                .HasMany(m => m.MaterialRequests)
                .WithOne(mr => mr.Material)
                .HasForeignKey(mr => mr.MaterialId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<MaterialRequest>()
                .HasOne(mr => mr.ReviewedBy)
                .WithMany()
                .HasForeignKey(mr => mr.ReviewedById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<MaintenanceRequest>()
                .HasOne(r => r.Feedback)
                .WithOne(f => f.Request)
                .HasForeignKey<Feedback>(f => f.RequestId)
                .OnDelete(DeleteBehavior.Cascade);

            // Asset
            builder.Entity<Asset>()
                .HasIndex(a => a.AssetCode)
                .IsUnique();

            builder.Entity<Asset>()
                .HasOne(a => a.Category)
                .WithMany()
                .HasForeignKey(a => a.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Asset>()
                .HasOne(a => a.Location)
                .WithMany()
                .HasForeignKey(a => a.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Asset>()
                .HasMany(a => a.MaintenanceRequests)
                .WithOne(r => r.Asset)
                .HasForeignKey(r => r.AssetId)
                .OnDelete(DeleteBehavior.Restrict);

            // PreventiveMaintenanceSchedule
            builder.Entity<PreventiveMaintenanceSchedule>()
                .HasOne(s => s.Category)
                .WithMany()
                .HasForeignKey(s => s.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PreventiveMaintenanceSchedule>()
                .HasOne(s => s.Location)
                .WithMany()
                .HasForeignKey(s => s.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PreventiveMaintenanceSchedule>()
                .HasOne(s => s.Technician)
                .WithMany()
                .HasForeignKey(s => s.TechnicianId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<PreventiveMaintenanceSchedule>()
                .HasMany(s => s.Records)
                .WithOne(r => r.Schedule)
                .HasForeignKey(r => r.ScheduleId)
                .OnDelete(DeleteBehavior.Cascade);

            // PreventiveMaintenanceRecord
            builder.Entity<PreventiveMaintenanceRecord>()
                .HasOne(r => r.Technician)
                .WithMany()
                .HasForeignKey(r => r.TechnicianId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<PreventiveMaintenanceRecord>()
                .HasOne(r => r.PerformedBy)
                .WithMany()
                .HasForeignKey(r => r.PerformedById)
                .OnDelete(DeleteBehavior.SetNull);

            // Soft delete filter
            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
                {
                    var method = typeof(ApplicationDbContext)
                        .GetMethod(nameof(SetSoftDeleteFilter), 
                            System.Reflection.BindingFlags.NonPublic | 
                            System.Reflection.BindingFlags.Static)?
                        .MakeGenericMethod(entityType.ClrType);
                    
                    method?.Invoke(null, new object[] { builder });
                }
            }
        }

        private static void SetSoftDeleteFilter<T>(ModelBuilder builder) where T : BaseEntity
        {
            builder.Entity<T>().HasQueryFilter(e => e.IsActive);
        }
    }
}
