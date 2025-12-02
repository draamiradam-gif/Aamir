using Microsoft.AspNetCore.Identity;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;
using Microsoft.EntityFrameworkCore; // Add this for Include, ToListAsync, etc.

namespace StudentManagementSystem.Services
{
    public interface IPermissionService
    {
        Task<bool> UserHasPermissionAsync(string userId, string permissionName);
        Task<List<string>> GetUserPermissionsAsync(string userId);
        Task<List<Permission>> GetAllPermissionsAsync();
        Task<bool> AssignPermissionToRoleAsync(string roleId, int permissionId);
        Task<bool> RemovePermissionFromRoleAsync(string roleId, int permissionId);
        Task SeedDefaultPermissionsAsync();
    }

    //public class PermissionService : IPermissionService
    //{
    //    private readonly ApplicationDbContext _context;
    //    private readonly UserManager<IdentityUser> _userManager;
    //    private readonly RoleManager<ApplicationRole> _roleManager;

    //    public PermissionService(ApplicationDbContext context, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
    //    {
    //        _context = context;
    //        _userManager = userManager;
    //        _roleManager = roleManager;
    //    }

    //    public async Task<bool> UserHasPermissionAsync(string userId, string permissionName)
    //    {
    //        var user = await _userManager.FindByIdAsync(userId);
    //        if (user == null) return false;

    //        // Get user roles
    //        var userRoles = await _userManager.GetRolesAsync(user);

    //        // Check if any of user's roles have this permission
    //        var hasPermission = await _context.RolePermissions
    //            .Include(rp => rp.Role)
    //            .Include(rp => rp.Permission)
    //            .AnyAsync(rp => userRoles.Contains(rp.Role.Name!) &&
    //                           rp.Permission.Name == permissionName &&
    //                           rp.Permission.IsActive);

    //        return hasPermission;
    //    }

    //    public async Task<List<string>> GetUserPermissionsAsync(string userId)
    //    {
    //        var user = await _userManager.FindByIdAsync(userId);
    //        if (user == null) return new List<string>();

    //        var userRoles = await _userManager.GetRolesAsync(user);

    //        var permissions = await _context.RolePermissions
    //            .Include(rp => rp.Role)
    //            .Include(rp => rp.Permission)
    //            .Where(rp => userRoles.Contains(rp.Role.Name!) && rp.Permission.IsActive)
    //            .Select(rp => rp.Permission.Name)
    //            .Distinct()
    //            .ToListAsync();

    //        return permissions;
    //    }

    //    public async Task<List<Permission>> GetAllPermissionsAsync()
    //    {
    //        return await _context.Permissions
    //            .Where(p => p.IsActive)
    //            .OrderBy(p => p.Category)
    //            .ThenBy(p => p.Name)
    //            .ToListAsync();
    //    }

    //    public async Task<bool> AssignPermissionToRoleAsync(string roleId, int permissionId)
    //    {
    //        try
    //        {
    //            // Check if already exists
    //            var exists = await _context.RolePermissions
    //                .AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

    //            if (!exists)
    //            {
    //                var rolePermission = new RolePermission
    //                {
    //                    RoleId = roleId,
    //                    PermissionId = permissionId
    //                };
    //                _context.RolePermissions.Add(rolePermission);
    //                await _context.SaveChangesAsync();
    //            }
    //            return true;
    //        }
    //        catch
    //        {
    //            return false;
    //        }
    //    }

    //    public async Task<bool> RemovePermissionFromRoleAsync(string roleId, int permissionId)
    //    {
    //        try
    //        {
    //            var rolePermission = await _context.RolePermissions
    //                .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

    //            if (rolePermission != null)
    //            {
    //                _context.RolePermissions.Remove(rolePermission);
    //                await _context.SaveChangesAsync();
    //            }
    //            return true;
    //        }
    //        catch
    //        {
    //            return false;
    //        }
    //    }

    //    public async Task SeedDefaultPermissionsAsync()
    //    {
    //        // Define default permissions
    //        var defaultPermissions = new List<Permission>
    //        {
    //            // Course Permissions
    //            new Permission { Name = "Courses.View", Description = "View courses", Category = "Courses" },
    //            new Permission { Name = "Courses.Create", Description = "Create courses", Category = "Courses" },
    //            new Permission { Name = "Courses.Edit", Description = "Edit courses", Category = "Courses" },
    //            new Permission { Name = "Courses.Delete", Description = "Delete courses", Category = "Courses" },
    //            new Permission { Name = "Courses.Enroll", Description = "Enroll students in courses", Category = "Courses" },
                
    //            // Student Permissions
    //            new Permission { Name = "Students.View", Description = "View students", Category = "Students" },
    //            new Permission { Name = "Students.Create", Description = "Create students", Category = "Students" },
    //            new Permission { Name = "Students.Edit", Description = "Edit students", Category = "Students" },
    //            new Permission { Name = "Students.Delete", Description = "Delete students", Category = "Students" },
                
    //            // Registration Permissions
    //            new Permission { Name = "Registration.View", Description = "View registration", Category = "Registration" },
    //            new Permission { Name = "Registration.Manage", Description = "Manage registration", Category = "Registration" },
    //            new Permission { Name = "Registration.Approve", Description = "Approve registration", Category = "Registration" },
                
    //            // Admin Permissions
    //            new Permission { Name = "Admin.Dashboard", Description = "Access admin dashboard", Category = "Admin" },
    //            new Permission { Name = "Admin.Users", Description = "Manage users", Category = "Admin" },
    //            new Permission { Name = "Admin.Roles", Description = "Manage roles", Category = "Admin" },
    //            new Permission { Name = "Admin.System", Description = "System administration", Category = "Admin" }
    //        };

    //        foreach (var permission in defaultPermissions)
    //        {
    //            // Check if permission already exists
    //            var existing = await _context.Permissions
    //                .FirstOrDefaultAsync(p => p.Name == permission.Name);

    //            if (existing == null)
    //            {
    //                _context.Permissions.Add(permission);
    //            }
    //        }

    //        await _context.SaveChangesAsync();
    //    }
    //}
}