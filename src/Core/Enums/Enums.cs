namespace ZEGU.Core.Enums
{
    public enum RequestPriority
    {
        Low = 1,
        Normal = 2,
        High = 3,
        Emergency = 4
    }

    public enum MaintenanceRequestStatus
    {
        Submitted = 1,
        Received = 2,
        Inspected = 3,
        Approved = 4,
        Assigned = 5,
        InProgress = 6,
        Completed = 7,
        Verified = 8,
        Closed = 9,
        Rejected = 10,
        Cancelled = 11
    }

    public enum TicketStatus
    {
        Open = 1,
        InProgress = 2,
        OnHold = 3,
        Resolved = 4,
        Closed = 5,
        Escalated = 6
    }

    public enum TicketPriority
    {
        Low = 1,
        Normal = 2,
        High = 3,
        Critical = 4
    }

    public enum TicketType
    {
        IT = 1,
        Maintenance = 2,
        General = 3,
        Other = 4
    }

    public enum UserRole
    {
        Student = 1,
        Staff = 2,
        WorksOfficer = 3,
        Technician = 4,
        Manager = 5,
        Admin = 6
    }

    public enum TechnicianType
    {
        Electrician = 1,
        Plumber = 2,
        Carpenter = 3,
        Painter = 4,
        HVAC = 5,
        GeneralMaintenance = 6,
        ICT = 7,
        Welder = 8,
        Other = 9
    }

    public enum NotificationType
    {
        Email = 1,
        SMS = 2,
        System = 3,
        WhatsApp = 4
    }
}
