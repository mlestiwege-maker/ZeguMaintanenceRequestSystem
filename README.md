# ZEGU Maintenance Request System (MRS)

A comprehensive web-based maintenance request management system for Zimbabwe Ezekiel Guti University (ZEGU), built with ASP.NET Core 8.0 MVC.

## 🚀 Features

### Core Functionality
- **User Authentication & Authorization** with role-based access (6 roles: Student, Staff, WorksOfficer, Technician, Manager, Admin)
- **Maintenance Request Management** - Submit, track, and resolve maintenance issues
- **Asset Management** with QR code generation
- **Technician Assignment & Work Logs** with labor tracking
- **Material Usage Tracking** with automatic stock deduction
- **Cost Tracking** - Labor, material, and other costs per request

### Advanced Features
- **Before/After Photo Documentation** for completed work
- **Preventive Maintenance Scheduling** with automated due date calculation
- **Reports & Analytics** with date range filtering and CSV export
- **Technician Workload Dashboard** for resource allocation
- **Advanced Search & Filtering** across all data
- **Audit Logging** for compliance and tracking

### Production-Ready
- **Security Hardening** - Strong password policy, account lockout, 2FA support
- **Rate Limiting** - Protection against abuse
- **GDPR Compliance** - Data export, account deletion, privacy policy
- **Docker Containerization** - Ready for production deployment
- **Health Checks** at `/health` and `/health/ready`
- **API Documentation** via Swagger at `/api-docs`
- **Pagination** for all list views
- **Notification Templates** - Manage email/SMS/WhatsApp templates

## 🛠️ Tech Stack

- **Framework:** ASP.NET Core 8.0 MVC
- **Database:** PostgreSQL with Entity Framework Core
- **Authentication:** ASP.NET Core Identity
- **Notifications:** MailKit (Email), Twilio (SMS/WhatsApp)
- **QR Codes:** QRCoder + API integration
- **API Documentation:** Swashbuckle/Swagger

## 📋 Prerequisites

- .NET 8.0 SDK
- PostgreSQL 15+ (or use Docker)
- Docker & Docker Compose (for containerized deployment)

## 🏃 Quick Start

### Option 1: Docker (Recommended)

```bash
# Clone the repository
git clone <repository-url>
cd MRS

# Start the application
docker-compose up -d

# Access the application
# Web: http://localhost:8080
# pgAdmin: http://localhost:5050
# Health: http://localhost:8080/health
```

### Option 2: Local Development

```bash
# Install PostgreSQL and create database
createdb ZEGU

# Update connection string in appsettings.json
# Default: Host=localhost;Port=5432;Database=ZEGU;Username=postgres;Password=YOUR_PASSWORD

# Run migrations
dotnet ef database update --project src/Infrastructure --startup-project src/WebApp

# Start the application
dotnet run --project src/WebApp/ZEGU.WebApp.csproj

# Access at http://localhost:5259
```

## 👥 Default Users (after seeding)

| Role | Email | Password |
|------|-------|----------|
| Admin | admin@university.edu | Admin123!@# |
| Manager | manager@staff.zegu.ac.zw | Manager123! |
| WorksOfficer | works@staff.zegu.ac.zw | Works123! |
| Technician | technician@staff.zegu.ac.zw | Tech123! |
| Staff | lmufutumari@staff.zegu.ac.zw | Staff123! |
| Student | student@student.zegu.ac.zw | Student123! |

⚠️ **Change all default passwords before production deployment!**

## 📁 Project Structure

```
MRS/
├── src/
│   ├── Core/                # Domain entities and enums
│   │   ├── Entities/        # Database entities
│   │   └── Enums/           # Application enums
│   ├── Infrastructure/      # Data access, services, migrations
│   │   ├── Data/            # DbContext, seed data
│   │   ├── Services/        # Domain services
│   │   └── Migrations/      # EF Core migrations
│   └── WebApp/              # ASP.NET Core MVC application
│       ├── Areas/           # Admin, Requests, Works, Mobile
│       ├── Controllers/     # Web API controllers
│       ├── Views/           # Razor views
│       ├── Services/        # Application services
│       └── wwwroot/         # Static files (CSS, JS, uploads)
├── Dockerfile               # Docker build configuration
├── docker-compose.yml       # Multi-container setup
└── .dockerignore            # Docker ignore patterns
```

## 🔐 Security Features

- **Password Policy:** Min 10 chars, requires digit, uppercase, lowercase, and non-alphanumeric
- **Account Lockout:** 5 failed attempts = 15-minute lockout
- **HTTPS Only** in production with HSTS
- **Secure Cookies:** HttpOnly, SameSite=Strict, SecurePolicy=Always
- **CSRF Protection** via antiforgery tokens
- **Rate Limiting:** 100 req/min global, 5 login attempts per 5 min
- **Audit Logging** of all sensitive operations
- **Data Anonymization** for GDPR compliance

## 🌐 API Endpoints

- **Web UI:** `/` (role-based dashboards)
- **Health Check:** `/health` and `/health/ready`
- **API Documentation:** `/api-docs` (Swagger UI in development)
- **Swagger JSON:** `/swagger/v1/swagger.json`

## 📊 Module Overview

### Admin Area (`/Admin`)
- Dashboard with system statistics
- User management (CRUD)
- Asset management with QR codes
- Department/Category/Location management
- Campus/Building/Room hierarchy
- Technician and Material management
- Preventive Maintenance scheduling
- Reports & Analytics with export
- Notification Templates
- System Settings (SLA configuration)
- Audit Logs viewer

### Requests Area (`/Requests`)
- Student/Staff dashboard
- Submit new maintenance request with photos
- View "My Requests" with status tracking
- Add comments to requests
- Submit feedback on completed work
- Cancel pending requests

### Works Area (`/Works`)
- Works Officer/Manager dashboard
- All Requests view with advanced filtering
- Assign technicians to requests
- Update status and priority
- Reject requests with reason
- SLA Dashboard with overdue alerts
- Work Logs with labor tracking
- Material Usage recording
- Technician Workload monitoring

## 🚀 Deployment

### Production Checklist

- [ ] Change all default passwords
- [ ] Configure production database connection string
- [ ] Set up SSL/TLS certificates
- [ ] Configure SMTP/Twilio credentials
- [ ] Set up automated database backups
- [ ] Configure logging and monitoring
- [ ] Set up reverse proxy (nginx/IIS)
- [ ] Review and adjust rate limiting
- [ ] Enable HSTS
- [ ] Configure firewall rules

### Docker Deployment

```bash
# Build and start
docker-compose up -d --build

# View logs
docker-compose logs -f zegu-web

# Stop
docker-compose down

# Backup database
docker exec zegu-db pg_dump -U zegu ZEGU > backup.sql
```

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `ASPNETCORE_ENVIRONMENT` | Runtime environment | `Production` |
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | - |
| `AdminSettings__Email` | Admin email | `admin@zegu.ac.zw` |
| `AdminSettings__Password` | Admin password | (change!) |
| `AppUrl` | Application URL | `http://localhost:8080` |

## 📝 License

Proprietary - Zimbabwe Ezekiel Guti University

## 🆘 Support

For issues and support:
- Email: support@zegu.ac.zw
- Documentation: `/api-docs` (Swagger)
- Health Status: `/health`

---

**Version:** 1.0.0  
**Last Updated:** September 2026  
**Maintained by:** ZEGU IT Department
