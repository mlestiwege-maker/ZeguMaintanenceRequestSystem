using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ZEGU.Core.Entities.Identity;
using ZEGU.Core.Entities.Maintenance;
using ZEGU.Core.Enums;

namespace ZEGU.Infrastructure.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            await context.Database.MigrateAsync();

            string[] roles = { "Student", "Staff", "WorksOfficer", "Technician", "Manager", "Admin" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            var adminEmail = configuration["AdminSettings:Email"] ?? "admin@university.edu";
            var adminPassword = configuration["AdminSettings:Password"] ?? "Admin123!";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "System",
                    LastName = "Administrator",
                    EmailConfirmed = true,
                    Role = UserRole.Admin
                };
                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            var staffUser = await userManager.FindByEmailAsync("lmufutumari@staff.zegu.ac.zw");
            if (staffUser == null)
            {
                staffUser = new ApplicationUser
                {
                    UserName = "lmufutumari@staff.zegu.ac.zw",
                    Email = "lmufutumari@staff.zegu.ac.zw",
                    FirstName = "Jane",
                    LastName = "Staff",
                    EmailConfirmed = true,
                    Role = UserRole.Staff,
                    StaffNumber = "STF-001"
                };
                await userManager.CreateAsync(staffUser, "Staff123!");
                await userManager.AddToRoleAsync(staffUser, "Staff");
            }

            var studentUser = await userManager.FindByEmailAsync("student@student.zegu.ac.zw");
            if (studentUser == null)
            {
                studentUser = new ApplicationUser
                {
                    UserName = "student@student.zegu.ac.zw",
                    Email = "student@student.zegu.ac.zw",
                    FirstName = "John",
                    LastName = "Student",
                    EmailConfirmed = true,
                    Role = UserRole.Student,
                    StudentNumber = "STU-001"
                };
                await userManager.CreateAsync(studentUser, "Student123!");
                await userManager.AddToRoleAsync(studentUser, "Student");
            }

            var worksOfficerUser = await userManager.FindByEmailAsync("works@staff.zegu.ac.zw");
            if (worksOfficerUser == null)
            {
                worksOfficerUser = new ApplicationUser
                {
                    UserName = "works@staff.zegu.ac.zw",
                    Email = "works@staff.zegu.ac.zw",
                    FirstName = "Mary",
                    LastName = "WorksOfficer",
                    EmailConfirmed = true,
                    Role = UserRole.WorksOfficer
                };
                await userManager.CreateAsync(worksOfficerUser, "Works123!");
                await userManager.AddToRoleAsync(worksOfficerUser, "WorksOfficer");
            }

            var managerUser = await userManager.FindByEmailAsync("manager@staff.zegu.ac.zw");
            if (managerUser == null)
            {
                managerUser = new ApplicationUser
                {
                    UserName = "manager@staff.zegu.ac.zw",
                    Email = "manager@staff.zegu.ac.zw",
                    FirstName = "Peter",
                    LastName = "Manager",
                    EmailConfirmed = true,
                    Role = UserRole.Manager
                };
                await userManager.CreateAsync(managerUser, "Manager123!");
                await userManager.AddToRoleAsync(managerUser, "Manager");
            }

            var technicianUser = await userManager.FindByEmailAsync("technician@staff.zegu.ac.zw");
            if (technicianUser == null)
            {
                technicianUser = new ApplicationUser
                {
                    UserName = "technician@staff.zegu.ac.zw",
                    Email = "technician@staff.zegu.ac.zw",
                    FirstName = "Alex",
                    LastName = "Technician",
                    EmailConfirmed = true,
                    Role = UserRole.Technician
                };
                await userManager.CreateAsync(technicianUser, "Tech123!");
                await userManager.AddToRoleAsync(technicianUser, "Technician");
            }

            if (!context.Departments.Any())
            {
                context.Departments.AddRange(
                    new ZEGU.Core.Entities.Shared.Department
                    {
                        DepartmentName = "Works and Maintenance",
                        Code = "WORKS",
                        IsActive = true
                    },
                    new ZEGU.Core.Entities.Shared.Department
                    {
                        DepartmentName = "ICT",
                        Code = "ICT",
                        IsActive = true
                    },
                    new ZEGU.Core.Entities.Shared.Department
                    {
                        DepartmentName = "Student Affairs",
                        Code = "SA",
                        IsActive = true
                    }
                );
                await context.SaveChangesAsync();
            }

            if (!context.MaintenanceCategories.Any())
            {
                context.MaintenanceCategories.AddRange(
                    new ZEGU.Core.Entities.Shared.MaintenanceCategory { CategoryName = "Electrical", SLAHours = 4, IsActive = true },
                    new ZEGU.Core.Entities.Shared.MaintenanceCategory { CategoryName = "Plumbing", SLAHours = 4, IsActive = true },
                    new ZEGU.Core.Entities.Shared.MaintenanceCategory { CategoryName = "Carpentry", SLAHours = 24, IsActive = true },
                    new ZEGU.Core.Entities.Shared.MaintenanceCategory { CategoryName = "Painting", SLAHours = 48, IsActive = true },
                    new ZEGU.Core.Entities.Shared.MaintenanceCategory { CategoryName = "HVAC", SLAHours = 8, IsActive = true },
                    new ZEGU.Core.Entities.Shared.MaintenanceCategory { CategoryName = "Furniture", SLAHours = 24, IsActive = true },
                    new ZEGU.Core.Entities.Shared.MaintenanceCategory { CategoryName = "Sanitation", SLAHours = 2, IsActive = true },
                    new ZEGU.Core.Entities.Shared.MaintenanceCategory { CategoryName = "Grounds", SLAHours = 24, IsActive = true },
                    new ZEGU.Core.Entities.Shared.MaintenanceCategory { CategoryName = "Generator", SLAHours = 4, IsActive = true },
                    new ZEGU.Core.Entities.Shared.MaintenanceCategory { CategoryName = "Other", SLAHours = 24, IsActive = true }
                );
                await context.SaveChangesAsync();
            }

            if (!context.Campuses.Any())
            {
                var campus = new ZEGU.Core.Entities.Shared.Campus
                {
                    CampusName = "Main Campus",
                    Code = "MAIN",
                    IsActive = true
                };
                context.Campuses.Add(campus);
                await context.SaveChangesAsync();

                var scienceBlock = new ZEGU.Core.Entities.Shared.Building
                {
                    CampusId = campus.Id,
                    BuildingName = "Science Block",
                    Code = "SCI",
                    Floors = 3,
                    IsActive = true
                };
                var adminBlock = new ZEGU.Core.Entities.Shared.Building
                {
                    CampusId = campus.Id,
                    BuildingName = "Administration Block",
                    Code = "ADMIN",
                    Floors = 2,
                    IsActive = true
                };
                var library = new ZEGU.Core.Entities.Shared.Building
                {
                    CampusId = campus.Id,
                    BuildingName = "Library",
                    Code = "LIB",
                    Floors = 4,
                    IsActive = true
                };

                context.Buildings.AddRange(scienceBlock, adminBlock, library);
                await context.SaveChangesAsync();
            }

            if (!context.Rooms.Any())
            {
                var scienceBlock = await context.Buildings.FirstOrDefaultAsync(b => b.Code == "SCI");
                var adminBlock = await context.Buildings.FirstOrDefaultAsync(b => b.Code == "ADMIN");
                var library = await context.Buildings.FirstOrDefaultAsync(b => b.Code == "LIB");

                if (scienceBlock != null && adminBlock != null && library != null)
                {
                    context.Rooms.AddRange(
                        new ZEGU.Core.Entities.Shared.Room { BuildingId = scienceBlock.Id, Floor = 1, RoomNumber = "Lab 101", RoomType = "Laboratory", IsActive = true },
                        new ZEGU.Core.Entities.Shared.Room { BuildingId = scienceBlock.Id, Floor = 1, RoomNumber = "Lab 102", RoomType = "Laboratory", IsActive = true },
                        new ZEGU.Core.Entities.Shared.Room { BuildingId = scienceBlock.Id, Floor = 1, RoomNumber = "Lecture B12", RoomType = "Lecture Room", IsActive = true },
                        new ZEGU.Core.Entities.Shared.Room { BuildingId = scienceBlock.Id, Floor = 2, RoomNumber = "Office 201", RoomType = "Office", IsActive = true },
                        new ZEGU.Core.Entities.Shared.Room { BuildingId = scienceBlock.Id, Floor = 2, RoomNumber = "Office 202", RoomType = "Office", IsActive = true },
                        new ZEGU.Core.Entities.Shared.Room { BuildingId = adminBlock.Id, Floor = 1, RoomNumber = "Reception", RoomType = "Office", IsActive = true },
                        new ZEGU.Core.Entities.Shared.Room { BuildingId = adminBlock.Id, Floor = 2, RoomNumber = "Board Room", RoomType = "Conference", IsActive = true },
                        new ZEGU.Core.Entities.Shared.Room { BuildingId = library.Id, Floor = 1, RoomNumber = "Main Hall", RoomType = "Library", IsActive = true },
                        new ZEGU.Core.Entities.Shared.Room { BuildingId = library.Id, Floor = 2, RoomNumber = "Office 201", RoomType = "Office", IsActive = true }
                    );
                    await context.SaveChangesAsync();
                }
            }

            if (!context.Technicians.Any() && technicianUser != null)
            {
                context.Technicians.Add(new Technician
                {
                    UserId = technicianUser.Id,
                    TechnicianType = TechnicianType.Electrician,
                    Specialization = "General Electrical",
                    PhoneNumber = "+1234567890",
                    IsAvailable = true
                });
                await context.SaveChangesAsync();
            }
        }
    }
}
