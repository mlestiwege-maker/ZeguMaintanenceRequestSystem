# Database Schema Design

## Technology
PostgreSQL 14+

## Entity Relationship Diagram (Conceptual)

```
Users ──────────────────┐
                        │
                        ├── MaintenanceRequests (many)
                        │
                        ├── RequestComments
                        │
                        ├── RequestAttachments
                        │
                        ├── RequestStatusHistory
                        │
                        └── Feedback

Roles ─── Users

Departments ─── Users

Campuses ─── Buildings ─── Rooms

MaintenanceCategories ─── MaintenanceRequests

Technicians ─── MaintenanceRequests (via Assignments)

Assignments ─── MaintenanceRequests
            ─── Technicians
            ─── WorkLogs

Materials ─── MaterialUsage ─── WorkLogs
```

## Tables

### Users
```sql
CREATE TABLE Users (
    UserID SERIAL PRIMARY KEY,
    Username VARCHAR(100) UNIQUE NOT NULL,
    Email VARCHAR(255) UNIQUE NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    FirstName VARCHAR(100) NOT NULL,
    LastName VARCHAR(100) NOT NULL,
    PhoneNumber VARCHAR(20),
    RoleID INT NOT NULL REFERENCES Roles(RoleID),
    DepartmentID INT REFERENCES Departments(DepartmentID),
    IsActive BOOLEAN DEFAULT TRUE,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

### Roles
```sql
CREATE TABLE Roles (
    RoleID SERIAL PRIMARY KEY,
    RoleName VARCHAR(50) UNIQUE NOT NULL,  -- Student, Staff, WorksOfficer, Technician, Manager, Admin
    Description TEXT,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

### Departments
```sql
CREATE TABLE Departments (
    DepartmentID SERIAL PRIMARY KEY,
    DepartmentName VARCHAR(255) NOT NULL,
    Code VARCHAR(50) UNIQUE,
    HeadOfDepartment VARCHAR(255),
    ContactEmail VARCHAR(255),
    ContactPhone VARCHAR(20),
    IsActive BOOLEAN DEFAULT TRUE,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

### Campuses
```sql
CREATE TABLE Campuses (
    CampusID SERIAL PRIMARY KEY,
    CampusName VARCHAR(255) NOT NULL,
    Code VARCHAR(50) UNIQUE,
    Address TEXT,
    IsActive BOOLEAN DEFAULT TRUE,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

### Buildings
```sql
CREATE TABLE Buildings (
    BuildingID SERIAL PRIMARY KEY,
    CampusID INT NOT NULL REFERENCES Campuses(CampusID),
    BuildingName VARCHAR(255) NOT NULL,
    Code VARCHAR(50),
    Floors INT DEFAULT 1,
    Description TEXT,
    IsActive BOOLEAN DEFAULT TRUE,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

### Rooms
```sql
CREATE TABLE Rooms (
    RoomID SERIAL PRIMARY KEY,
    BuildingID INT NOT NULL REFERENCES Buildings(BuildingID),
    Floor INT NOT NULL,
    RoomNumber VARCHAR(50) NOT NULL,
    RoomType VARCHAR(100),  -- Lecture Room, Office, Laboratory, etc.
    Description TEXT,
    IsActive BOOLEAN DEFAULT TRUE,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(BuildingID, Floor, RoomNumber)
);
```

### MaintenanceCategories
```sql
CREATE TABLE MaintenanceCategories (
    CategoryID SERIAL PRIMARY KEY,
    CategoryName VARCHAR(100) NOT NULL UNIQUE,
    Description TEXT,
    Icon VARCHAR(50),
    SLAHours INT,  -- Expected response time in hours
    IsActive BOOLEAN DEFAULT TRUE,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

### MaintenanceRequests
```sql
CREATE TABLE MaintenanceRequests (
    RequestID SERIAL PRIMARY KEY,
    RequestNumber VARCHAR(50) UNIQUE NOT NULL,  -- MRS-2024-0001
    UserID INT NOT NULL REFERENCES Users(UserID),
    DepartmentID INT REFERENCES Departments(DepartmentID),
    CategoryID INT NOT NULL REFERENCES MaintenanceCategories(CategoryID),
    LocationID INT NOT NULL REFERENCES Rooms(RoomID),
    Title VARCHAR(255) NOT NULL,
    Description TEXT NOT NULL,
    Priority VARCHAR(20) NOT NULL DEFAULT 'Normal',  -- Low, Normal, High, Emergency
    Status VARCHAR(50) NOT NULL DEFAULT 'Submitted',  -- Submitted, Received, Inspected, Approved, Assigned, InProgress, Completed, Verified, Closed, Rejected
    RejectionReason TEXT,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CompletedAt TIMESTAMP,
    ClosedAt TIMESTAMP
);
```

### RequestStatusHistory
```sql
CREATE TABLE RequestStatusHistory (
    HistoryID SERIAL PRIMARY KEY,
    RequestID INT NOT NULL REFERENCES MaintenanceRequests(RequestID),
    OldStatus VARCHAR(50),
    NewStatus VARCHAR(50) NOT NULL,
    ChangedBy INT REFERENCES Users(UserID),
    Comments TEXT,
    ChangedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

### RequestComments
```sql
CREATE TABLE RequestComments (
    CommentID SERIAL PRIMARY KEY,
    RequestID INT NOT NULL REFERENCES MaintenanceRequests(RequestID),
    UserID INT NOT NULL REFERENCES Users(UserID),
    CommentText TEXT NOT NULL,
    IsInternal BOOLEAN DEFAULT FALSE,  -- Internal notes vs. user-visible
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

### RequestAttachments
```sql
CREATE TABLE RequestAttachments (
    AttachmentID SERIAL PRIMARY KEY,
    RequestID INT NOT NULL REFERENCES MaintenanceRequests(RequestID),
    FileName VARCHAR(255) NOT NULL,
    FilePath VARCHAR(500) NOT NULL,
    FileSize BIGINT,
    FileType VARCHAR(100),  -- image/jpeg, image/png
    UploadedBy INT REFERENCES Users(UserID),
    UploadedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    IsBeforePhoto BOOLEAN DEFAULT FALSE
);
```

### Technicians
```sql
CREATE TABLE Technicians (
    TechnicianID SERIAL PRIMARY KEY,
    UserID INT REFERENCES Users(UserID),
    TechnicianType VARCHAR(100) NOT NULL,  -- Electrician, Plumber, etc.
    Specialization TEXT,
    PhoneNumber VARCHAR(20),
    IsAvailable BOOLEAN DEFAULT TRUE,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

### Assignments
```sql
CREATE TABLE Assignments (
    AssignmentID SERIAL PRIMARY KEY,
    RequestID INT NOT NULL REFERENCES MaintenanceRequests(RequestID),
    TechnicianID INT NOT NULL REFERENCES Technicians(TechnicianID),
    AssignedBy INT REFERENCES Users(UserID),
    AssignedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    AcceptedAt TIMESTAMP,
    StartedAt TIMESTAMP,
    Notes TEXT
);
```

### WorkLogs
```sql
CREATE TABLE WorkLogs (
    WorkLogID SERIAL PRIMARY KEY,
    RequestID INT NOT NULL REFERENCES MaintenanceRequests(RequestID),
    TechnicianID INT REFERENCES Technicians(TechnicianID),
    WorkPerformed TEXT NOT NULL,
    HoursSpent DECIMAL(5,2),
    StartedAt TIMESTAMP,
    CompletedAt TIMESTAMP,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

### Materials
```sql
CREATE TABLE Materials (
    MaterialID SERIAL PRIMARY KEY,
    MaterialName VARCHAR(255) NOT NULL,
    Category VARCHAR(100),
    Unit VARCHAR(50),  -- pieces, liters, meters
    UnitCost DECIMAL(10,2),
    Supplier VARCHAR(255),
    MinimumStock INT DEFAULT 0,
    CurrentStock INT DEFAULT 0,
    IsActive BOOLEAN DEFAULT TRUE,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

### MaterialUsage
```sql
CREATE TABLE MaterialUsage (
    UsageID SERIAL PRIMARY KEY,
    WorkLogID INT NOT NULL REFERENCES WorkLogs(WorkLogID),
    MaterialID INT NOT NULL REFERENCES Materials(MaterialID),
    QuantityUsed DECIMAL(10,2) NOT NULL,
    UnitCost DECIMAL(10,2),
    TotalCost DECIMAL(10,2),
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

### Feedback
```sql
CREATE TABLE Feedback (
    FeedbackID SERIAL PRIMARY KEY,
    RequestID INT NOT NULL REFERENCES MaintenanceRequests(RequestID),
    UserID INT NOT NULL REFERENCES Users(UserID),
    Rating INT CHECK (Rating >= 1 AND Rating <= 5),
    Comments TEXT,
    WorkSatisfactory BOOLEAN,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

### Notifications
```sql
CREATE TABLE Notifications (
    NotificationID SERIAL PRIMARY KEY,
    UserID INT NOT NULL REFERENCES Users(UserID),
    RequestID INT REFERENCES MaintenanceRequests(RequestID),
    Title VARCHAR(255) NOT NULL,
    Message TEXT NOT NULL,
    Type VARCHAR(50),  -- Email, SMS, System
    IsRead BOOLEAN DEFAULT FALSE,
    SentAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    ReadAt TIMESTAMP
);
```

### AuditLog
```sql
CREATE TABLE AuditLog (
    AuditID SERIAL PRIMARY KEY,
    TableName VARCHAR(100) NOT NULL,
    RecordID INT NOT NULL,
    Action VARCHAR(50) NOT NULL,  -- INSERT, UPDATE, DELETE
    ChangedBy INT REFERENCES Users(UserID),
    OldValues JSONB,
    NewValues JSONB,
    ChangedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

## Indexes

```sql
CREATE INDEX idx_requests_status ON MaintenanceRequests(Status);
CREATE INDEX idx_requests_priority ON MaintenanceRequests(Priority);
CREATE INDEX idx_requests_category ON MaintenanceRequests(CategoryID);
CREATE INDEX idx_requests_location ON MaintenanceRequests(LocationID);
CREATE INDEX idx_requests_user ON MaintenanceRequests(UserID);
CREATE INDEX idx_requests_created ON MaintenanceRequests(CreatedAt);
CREATE INDEX idx_assignments_technician ON Assignments(TechnicianID);
CREATE INDEX idx_notifications_user ON Notifications(UserID, IsRead);
```
