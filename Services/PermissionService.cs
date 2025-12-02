using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;

namespace StudentManagementSystem.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public PermissionService(ApplicationDbContext context, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<bool> UserHasPermissionAsync(string userId, string permissionName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            var userRoles = await _userManager.GetRolesAsync(user);

            // FIX: Added null check for rp.Role.Name
            var hasPermission = await _context.RolePermissions
                .Include(rp => rp.Role)
                .Include(rp => rp.Permission)
                .AnyAsync(rp => rp.Role != null &&
                               rp.Permission != null &&
                               !string.IsNullOrEmpty(rp.Role.Name) &&
                               userRoles.Contains(rp.Role.Name) &&
                               rp.Permission.Name == permissionName &&
                               rp.Permission.IsActive);

            return hasPermission;
        }

        public async Task<List<string>> GetUserPermissionsAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return new List<string>();

            var userRoles = await _userManager.GetRolesAsync(user);

            // FIX: Added null checks
            var permissions = await _context.RolePermissions
                .Include(rp => rp.Role)
                .Include(rp => rp.Permission)
                .Where(rp => rp.Role != null &&
                           rp.Permission != null &&
                           !string.IsNullOrEmpty(rp.Role.Name) &&
                           userRoles.Contains(rp.Role.Name) &&
                           rp.Permission.IsActive)
                .Select(rp => rp.Permission.Name ?? string.Empty)
                .Distinct()
                .Where(name => !string.IsNullOrEmpty(name))
                .ToListAsync();

            return permissions;
        }

        public async Task<List<Permission>> GetAllPermissionsAsync()
        {
            return await _context.Permissions
                .Where(p => p.IsActive)
                .OrderBy(p => p.Category)
                .ThenBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<bool> AssignPermissionToRoleAsync(string roleId, int permissionId)
        {
            try
            {
                var exists = await _context.RolePermissions
                    .AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

                if (!exists)
                {
                    var rolePermission = new RolePermission
                    {
                        RoleId = roleId,
                        PermissionId = permissionId
                    };
                    _context.RolePermissions.Add(rolePermission);
                    await _context.SaveChangesAsync();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> RemovePermissionFromRoleAsync(string roleId, int permissionId)
        {
            try
            {
                var rolePermission = await _context.RolePermissions
                    .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

                if (rolePermission != null)
                {
                    _context.RolePermissions.Remove(rolePermission);
                    await _context.SaveChangesAsync();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task SeedDefaultPermissionsAsync()
        {
            var defaultPermissions = new List<Permission>
            {
                new Permission { Name = "Courses.View", Description = "View courses", Category = "Courses", IsActive = true },
                new Permission { Name = "Courses.Create", Description = "Create courses", Category = "Courses", IsActive = true },
                new Permission { Name = "Courses.Edit", Description = "Edit courses", Category = "Courses", IsActive = true },
                new Permission { Name = "Courses.Delete", Description = "Delete courses", Category = "Courses", IsActive = true },
                new Permission { Name = "Courses.Enroll", Description = "Enroll students in courses", Category = "Courses", IsActive = true },
                new Permission { Name = "Courses.Export", Description = "Export course data", Category = "Courses", IsActive = true },

                new Permission { Name = "Students.View", Description = "View students", Category = "Students", IsActive = true },
                new Permission { Name = "Students.Create", Description = "Create students", Category = "Students", IsActive = true },
                new Permission { Name = "Students.Edit", Description = "Edit students", Category = "Students", IsActive = true },
                new Permission { Name = "Students.Delete", Description = "Delete students", Category = "Students", IsActive = true },
                new Permission { Name = "Students.Export", Description = "Export student data", Category = "Students", IsActive = true },

                new Permission { Name = "Registration.View", Description = "View registration", Category = "Registration", IsActive = true },
                new Permission { Name = "Registration.Manage", Description = "Manage registration", Category = "Registration", IsActive = true },
                new Permission { Name = "Registration.Approve", Description = "Approve registration", Category = "Registration", IsActive = true },

                new Permission { Name = "Grades.View", Description = "View grades", Category = "Grades", IsActive = true },
                new Permission { Name = "Grades.Manage", Description = "Manage grades", Category = "Grades", IsActive = true },
                new Permission { Name = "Grades.Export", Description = "Export grade reports", Category = "Grades", IsActive = true },

                new Permission { Name = "Admin.Dashboard", Description = "Access admin dashboard", Category = "Admin", IsActive = true },
                new Permission { Name = "Admin.Users", Description = "Manage users", Category = "Admin", IsActive = true },
                new Permission { Name = "Admin.Roles", Description = "Manage roles", Category = "Admin", IsActive = true },
                new Permission { Name = "Admin.System", Description = "System administration", Category = "Admin", IsActive = true },
                new Permission { Name = "Admin.Reports", Description = "Generate system reports", Category = "Admin", IsActive = true },

                new Permission { Name = "Faculty.Dashboard", Description = "Access faculty dashboard", Category = "Faculty", IsActive = true },
                new Permission { Name = "Faculty.Courses", Description = "Manage assigned courses", Category = "Faculty", IsActive = true },
                new Permission { Name = "Faculty.Grades", Description = "Manage student grades", Category = "Faculty", IsActive = true },
                new Permission { Name = "Faculty.Attendance", Description = "Manage attendance", Category = "Faculty", IsActive = true },

                new Permission { Name = "Student.Dashboard", Description = "Access student dashboard", Category = "Student", IsActive = true },
                new Permission { Name = "Student.Grades.View", Description = "View grades", Category = "Student", IsActive = true },
                new Permission { Name = "Student.Schedule.View", Description = "View schedule", Category = "Student", IsActive = true },
                new Permission { Name = "Student.Courses.Register", Description = "Register for courses", Category = "Student", IsActive = true },
                new Permission { Name = "Student.Profile.View", Description = "View student profile", Category = "Student", IsActive = true },
                new Permission { Name = "Student.Transcript.View", Description = "View transcript", Category = "Student", IsActive = true }
            };

            foreach (var permission in defaultPermissions)
            {
                var existing = await _context.Permissions
                    .FirstOrDefaultAsync(p => p.Name == permission.Name);

                if (existing == null)
                {
                    _context.Permissions.Add(permission);
                }
                else
                {
                    existing.Description = permission.Description;
                    existing.Category = permission.Category;
                    existing.IsActive = true;
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}