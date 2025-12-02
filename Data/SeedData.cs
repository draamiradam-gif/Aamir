using Microsoft.AspNetCore.Identity;
using StudentManagementSystem.Services;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;

namespace StudentManagementSystem.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Create roles
            string[] roleNames = { "SuperAdmin", "Admin", "UniversityAdmin", "FacultyAdmin", "DepartmentAdmin" };

            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Create super admin user
            var superAdminEmail = "superadmin@localhost";
            var superAdmin = await userManager.FindByEmailAsync(superAdminEmail);

            if (superAdmin == null)
            {
                superAdmin = new IdentityUser
                {
                    UserName = superAdminEmail,
                    Email = superAdminEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(superAdmin, "SuperAdmin123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(superAdmin, "SuperAdmin");
                }
            }

            // Create regular admin
            var adminEmail = "admin@localhost";
            var admin = await userManager.FindByEmailAsync(adminEmail);

            if (admin == null)
            {
                admin = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(admin, "Admin123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Admin");
                }
            }
        }
    }

    public static class DataSeeder
    {
        public static async Task SeedPermissionsAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var permissionService = scope.ServiceProvider.GetService<IPermissionService>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // If permission service is not available, skip permission seeding
            if (permissionService == null)
            {
                Console.WriteLine("❌ PermissionService not available, skipping permission seeding.");
                return;
            }

            try
            {
                Console.WriteLine("🔄 Seeding default permissions...");

                // Seed permissions first
                await permissionService.SeedDefaultPermissionsAsync();

                // Get all permissions
                var allPermissions = await permissionService.GetAllPermissionsAsync();

                if (!allPermissions.Any())
                {
                    Console.WriteLine("❌ No permissions found to assign.");
                    return;
                }

                Console.WriteLine($"📋 Found {allPermissions.Count} permissions to assign.");

                // Assign all permissions to SuperAdmin role
                var superAdminRole = await roleManager.FindByNameAsync("SuperAdmin");
                if (superAdminRole != null)
                {
                    foreach (var permission in allPermissions)
                    {
                        await permissionService.AssignPermissionToRoleAsync(superAdminRole.Id, permission.Id);
                    }
                    Console.WriteLine($"✅ Assigned {allPermissions.Count} permissions to SuperAdmin role.");
                }
                else
                {
                    Console.WriteLine("❌ SuperAdmin role not found.");
                }

                // Assign basic permissions to Admin role
                var adminRole = await roleManager.FindByNameAsync("Admin");
                if (adminRole != null)
                {
                    var adminPermissions = allPermissions.Where(p =>
                        !p.Name.StartsWith("Admin.System") &&
                        !p.Name.StartsWith("Admin.Roles")
                    ).ToList();

                    foreach (var permission in adminPermissions)
                    {
                        await permissionService.AssignPermissionToRoleAsync(adminRole.Id, permission.Id);
                    }
                    Console.WriteLine($"✅ Assigned {adminPermissions.Count} permissions to Admin role.");
                }
                else
                {
                    Console.WriteLine("❌ Admin role not found.");
                }

                // Assign course-related permissions to Faculty role
                var facultyRole = await roleManager.FindByNameAsync("Faculty");
                if (facultyRole != null)
                {
                    var facultyPermissions = allPermissions.Where(p =>
                        p.Name.StartsWith("Courses.") ||
                        p.Name.StartsWith("Students.View") ||
                        p.Name.StartsWith("Grades.") ||
                        p.Name.StartsWith("Faculty.") ||
                        p.Name.StartsWith("Registration.")
                    ).ToList();

                    foreach (var permission in facultyPermissions)
                    {
                        await permissionService.AssignPermissionToRoleAsync(facultyRole.Id, permission.Id);
                    }
                    Console.WriteLine($"✅ Assigned {facultyPermissions.Count} permissions to Faculty role.");
                }
                else
                {
                    Console.WriteLine("❌ Faculty role not found.");
                }

                // Assign student permissions to Student role
                var studentRole = await roleManager.FindByNameAsync("Student");
                if (studentRole != null)
                {
                    var studentPermissions = allPermissions.Where(p =>
                        p.Name.StartsWith("Student.")
                    ).ToList();

                    foreach (var permission in studentPermissions)
                    {
                        await permissionService.AssignPermissionToRoleAsync(studentRole.Id, permission.Id);
                    }
                    Console.WriteLine($"✅ Assigned {studentPermissions.Count} permissions to Student role.");
                }
                else
                {
                    Console.WriteLine("❌ Student role not found.");
                }

                Console.WriteLine("🎉 Permission seeding completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 Permission seeding failed: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"💥 Inner exception: {ex.InnerException.Message}");
                }
            }
        }

        public static async Task SeedDefaultTemplates(ApplicationDbContext context)
        {
            try
            {
                var existingCount = await context.AdminPrivilegeTemplates.CountAsync();
                Console.WriteLine($"ℹ️ Currently {existingCount} templates in database");

                if (existingCount == 0)
                {
                    Console.WriteLine("🔄 Seeding default templates...");

                    var templates = new List<AdminPrivilegeTemplate>
            {
            // 1. SuperAdmin Template
            new AdminPrivilegeTemplate
            {
                TemplateName = "System Administrator",
                AdminType = AdminType.SuperAdmin,
                Description = "Complete system access with all permissions",
                DefaultPermissionsData = string.Join(',', Enum.GetValues<PermissionModule>()
                    .Select(p => p.ToString())),
                IsActive = true,
                CreatedDate = DateTime.Now
            },

            // 2. UniversityAdmin Templates
            new AdminPrivilegeTemplate
            {
                TemplateName = "University Full Access",
                AdminType = AdminType.UniversityAdmin,
                Description = "Complete university-level administration access",
                DefaultPermissionsData = string.Join(',', new List<string>
                {
                    "UserManagement_View", "UserManagement_Create", "UserManagement_Edit", "UserManagement_Export",
                    "Student_View", "Student_Create", "Student_Edit", "Student_Export", "Student_Import",
                    "Course_View", "Course_Create", "Course_Edit", "Course_Export",
                    "Grade_View", "Grade_Edit", "Grade_Export",
                    "University_View", "University_Edit",
                    "College_View", "College_Edit",
                    "Department_View", "Department_Edit",
                    "Application_View", "Application_Approve", "Application_Reject", "Application_Export"
                }),
                IsActive = true,
                CreatedDate = DateTime.Now
            },

            new AdminPrivilegeTemplate
            {
                TemplateName = "University Read-Only",
                AdminType = AdminType.UniversityAdmin,
                Description = "View-only access for university monitoring",
                DefaultPermissionsData = string.Join(',', new List<string>
                {
                    "UserManagement_View", "Student_View", "Course_View", "Grade_View", "Finance_View",
                    "University_View", "College_View", "Department_View", "Application_View"
                }),
                IsActive = true,
                CreatedDate = DateTime.Now
            },

            // 3. FacultyAdmin Template
            new AdminPrivilegeTemplate
            {
                TemplateName = "Faculty Administrator",
                AdminType = AdminType.FacultyAdmin,
                Description = "Full faculty/college level administration",
                DefaultPermissionsData = string.Join(',', new List<string>
                {
                    "UserManagement_View", "UserManagement_Edit",
                    "Student_View", "Student_Edit", "Student_Export",
                    "Course_View", "Course_Edit", "Course_Export",
                    "Grade_View", "Grade_Edit",
                    "College_View", "College_Edit",
                    "Department_View", "Department_Edit",
                    "Application_View", "Application_Approve"
                }),
                IsActive = true,
                CreatedDate = DateTime.Now
            },

            // 4. DepartmentAdmin Templates
            new AdminPrivilegeTemplate
            {
                TemplateName = "Department Head",
                AdminType = AdminType.DepartmentAdmin,
                Description = "Department-level administration access",
                DefaultPermissionsData = string.Join(',', new List<string>
                {
                    "UserManagement_View",
                    "Student_View", "Student_Edit",
                    "Course_View", "Course_Edit", "Course_Export",
                    "Grade_View", "Grade_Edit",
                    "Department_View", "Department_Edit"
                }),
                IsActive = true,
                CreatedDate = DateTime.Now
            },

            new AdminPrivilegeTemplate
            {
                TemplateName = "Department Coordinator",
                AdminType = AdminType.DepartmentAdmin,
                Description = "Department coordination and management",
                DefaultPermissionsData = string.Join(',', new List<string>
                {
                    "Student_View", "Course_View", "Course_Edit", "Grade_View",
                    "Department_View", "Department_Edit"
                }),
                IsActive = true,
                CreatedDate = DateTime.Now
            },

            // 5. EmployeeAdmin Template
            new AdminPrivilegeTemplate
            {
                TemplateName = "Employee Manager",
                AdminType = AdminType.EmployeeAdmin,
                Description = "Employee and staff management",
                DefaultPermissionsData = string.Join(',', new List<string>
                {
                    "UserManagement_View", "UserManagement_Create", "UserManagement_Edit", "UserManagement_Export",
                    "Finance_View", "Finance_Export"
                }),
                IsActive = true,
                CreatedDate = DateTime.Now
            },

            // 6. FinanceAdmin Templates
            new AdminPrivilegeTemplate
            {
                TemplateName = "Finance Full Access",
                AdminType = AdminType.FinanceAdmin,
                Description = "Complete financial management access",
                DefaultPermissionsData = string.Join(',', new List<string>
                {
                    "Finance_View", "Finance_Create", "Finance_Edit", "Finance_Delete",
                    "Finance_Export", "Finance_Reports",
                    "Student_View",
                    "UserManagement_View", "UserManagement_Export"
                }),
                IsActive = true,
                CreatedDate = DateTime.Now
            },

            new AdminPrivilegeTemplate
            {
                TemplateName = "Finance View Only",
                AdminType = AdminType.FinanceAdmin,
                Description = "Financial reporting and viewing access",
                DefaultPermissionsData = string.Join(',', new List<string>
                {
                    "Finance_View", "Finance_Export", "Finance_Reports",
                    "Student_View"
                }),
                IsActive = true,
                CreatedDate = DateTime.Now
            },

            // 7. StudentAdmin Templates
            new AdminPrivilegeTemplate
            {
                TemplateName = "Student Affairs",
                AdminType = AdminType.StudentAdmin,
                Description = "Student management and support services",
                DefaultPermissionsData = string.Join(',', new List<string>
                {
                    "Student_View", "Student_Create", "Student_Edit", "Student_Export", "Student_Import",
                    "Course_View", "Grade_View",
                    "Application_View", "Application_Approve", "Application_Reject", "Application_Export"
                }),
                IsActive = true,
                CreatedDate = DateTime.Now
            },

            new AdminPrivilegeTemplate
            {
                TemplateName = "Admissions Officer",
                AdminType = AdminType.StudentAdmin,
                Description = "Student admissions and enrollment management",
                DefaultPermissionsData = string.Join(',', new List<string>
                {
                    "Student_View", "Student_Create", "Student_Edit", "Student_Import",
                    "Application_View", "Application_Approve", "Application_Reject"
                }),
                IsActive = true,
                CreatedDate = DateTime.Now
            },

            // 8. CustomAdmin Templates
            new AdminPrivilegeTemplate
            {
                TemplateName = "Custom: Auditor",
                AdminType = AdminType.CustomAdmin,
                Description = "Audit and compliance review access",
                DefaultPermissionsData = string.Join(',', new List<string>
                {
                    "UserManagement_View", "Student_View", "Course_View", "Grade_View", "Finance_View",
                    "System_Logs", "Application_View"
                }),
                IsActive = true,
                CreatedDate = DateTime.Now
            },

            new AdminPrivilegeTemplate
            {
                TemplateName = "Custom: Reporting Analyst",
                AdminType = AdminType.CustomAdmin,
                Description = "Data analysis and reporting access",
                DefaultPermissionsData = string.Join(',', new List<string>
                {
                    "UserManagement_View", "UserManagement_Export",
                    "Student_View", "Student_Export",
                    "Course_View", "Course_Export",
                    "Grade_View", "Grade_Export",
                    "Finance_View", "Finance_Export", "Finance_Reports",
                    "Application_Export"
                }),
                IsActive = true,
                CreatedDate = DateTime.Now
            },

            new AdminPrivilegeTemplate
            {
                TemplateName = "Custom: System Monitor",
                AdminType = AdminType.CustomAdmin,
                Description = "System monitoring and basic administration",
                DefaultPermissionsData = string.Join(',', new List<string>
                {
                    "System_Logs", "System_Backup",
                    "UserManagement_View", "Student_View", "Application_View"
                }),
                IsActive = true,
                CreatedDate = DateTime.Now
            }
        };

                    context.AdminPrivilegeTemplates.AddRange(templates);
                    await context.SaveChangesAsync();

                    Console.WriteLine("✅ 13 default templates seeded successfully!");
                    Console.WriteLine("   - 1 SuperAdmin template");
                    Console.WriteLine("   - 2 UniversityAdmin templates");
                    Console.WriteLine("   - 1 FacultyAdmin template");
                    Console.WriteLine("   - 2 DepartmentAdmin templates");
                    Console.WriteLine("   - 1 EmployeeAdmin template");
                    Console.WriteLine("   - 2 FinanceAdmin templates");
                    Console.WriteLine("   - 2 StudentAdmin templates");
                    Console.WriteLine("   - 3 CustomAdmin templates");

                    // Verify they were saved
                    var newCount = await context.AdminPrivilegeTemplates.CountAsync();
                    Console.WriteLine($"✅ Total templates after seeding: {newCount}");
                }
                else
                {
                    Console.WriteLine($"ℹ️ Templates already exist in database ({existingCount} templates). Skipping seeding.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error seeding templates: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"❌ Inner exception: {ex.InnerException.Message}");
                }
                throw;
            }
        }
    }
}