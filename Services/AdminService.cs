using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;
using StudentManagementSystem.Services;

namespace StudentManagementSystem.Services
{
    public class AdminService : IAdminService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminService(ApplicationDbContext context, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // Application Management
        public async Task<List<AdminApplication>> GetPendingApplicationsAsync()
        {
            return await _context.AdminApplications
                .Include(a => a.University)
                .Include(a => a.Faculty)
                .Include(a => a.Department)
                .Where(a => a.Status == ApplicationStatus.Pending)
                .OrderByDescending(a => a.AppliedDate)
                .ToListAsync();
        }

        public async Task<List<AdminApplication>> GetAllApplicationsAsync()
        {
            return await _context.AdminApplications
                .Include(a => a.University)
                .Include(a => a.Faculty)
                .Include(a => a.Department)
                .OrderByDescending(a => a.AppliedDate)
                .ToListAsync();
        }

        public async Task<AdminApplication?> GetApplicationByIdAsync(int id)
        {
            return await _context.AdminApplications
                .Include(a => a.University)
                .Include(a => a.Faculty)
                .Include(a => a.Department)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<bool> SubmitApplicationAsync(AdminApplication application)
        {
            try
            {
                _context.AdminApplications.Add(application);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ReviewApplicationAsync(int applicationId, ApplicationStatus status, string reviewedBy, string notes)
        {
            try
            {
                var application = await _context.AdminApplications.FindAsync(applicationId);
                if (application == null) return false;

                application.Status = status;
                application.ReviewedDate = DateTime.Now;
                application.ReviewedBy = reviewedBy;
                application.ReviewNotes = notes;

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> CreateAdminFromApplicationAsync(int applicationId, string createdBy)
        {
            try
            {
                var application = await _context.AdminApplications.FindAsync(applicationId);
                if (application == null || application.Status != ApplicationStatus.Approved) return false;

                // Create user account
                var user = new IdentityUser
                {
                    UserName = application.Email,
                    Email = application.Email,
                    EmailConfirmed = true
                };

                var password = GenerateRandomPassword();
                var result = await _userManager.CreateAsync(user, password);

                if (result.Succeeded)
                {
                    // Create admin privileges
                    var template = await GetTemplateForAdminType(application.AppliedAdminType);
                    var privileges = new AdminPrivilege
                    {
                        AdminId = user.Id,
                        AdminType = application.AppliedAdminType,
                        UniversityScope = application.UniversityId,
                        FacultyScope = application.FacultyId,
                        DepartmentScope = application.DepartmentId,
                        Permissions = template?.DefaultPermissions ?? new List<PermissionModule>(),
                        CreatedBy = createdBy
                    };

                    _context.AdminPrivileges.Add(privileges);
                    await _context.SaveChangesAsync();

                    // Send email with credentials
                    await SendAdminCredentialsEmail(application.Email, password, application.AppliedAdminType);

                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        // Admin Privilege Management
        public async Task<List<AdminPrivilege>> GetAllAdminPrivilegesAsync()
        {
            return await _context.AdminPrivileges
                .Include(p => p.Admin)
                .Include(p => p.University)
                .Include(p => p.Faculty)
                .Include(p => p.Department)
                .Where(p => p.IsActive)
                .OrderBy(p => p.AdminType)
                .ThenBy(p => p.Admin.UserName)
                .ToListAsync();
        }

        public async Task<AdminPrivilege?> GetAdminPrivilegeAsync(string adminId)
        {
            return await _context.AdminPrivileges
                .Include(p => p.Admin)
                .FirstOrDefaultAsync(p => p.AdminId == adminId && p.IsActive);
        }

        public async Task<AdminPrivilege?> GetAdminPrivilegeByEmailAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return null;

            return await _context.AdminPrivileges
                .Include(p => p.Admin)
                .FirstOrDefaultAsync(p => p.AdminId == user.Id && p.IsActive);
        }

        public async Task<bool> UpdateAdminPrivilegesAsync(string adminId, List<PermissionModule> permissions, string updatedBy)
        {
            try
            {
                var privilege = await _context.AdminPrivileges.FirstOrDefaultAsync(p => p.AdminId == adminId);
                if (privilege == null) return false;

                privilege.Permissions = permissions;
                privilege.ModifiedDate = DateTime.Now;

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> CreateAdminWithPrivilegesAsync(CreateAdminViewModel model, string createdBy)
        {
            try
            {
                // Create user account
                var user = new IdentityUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (!result.Succeeded) return false;

                // Create admin privileges
                var privileges = new AdminPrivilege
                {
                    AdminId = user.Id,
                    AdminType = model.AdminType,
                    // Convert string to int? for database
                    UniversityScope = string.IsNullOrEmpty(model.UniversityScope) ? null : int.Parse(model.UniversityScope),
                    FacultyScope = string.IsNullOrEmpty(model.FacultyScope) ? null : int.Parse(model.FacultyScope),
                    DepartmentScope = string.IsNullOrEmpty(model.DepartmentScope) ? null : int.Parse(model.DepartmentScope),
                    Permissions = model.SelectedPermissions,
                    CreatedBy = createdBy
                };

                _context.AdminPrivileges.Add(privileges);
                await _context.SaveChangesAsync();

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeactivateAdminAsync(string adminId, string deactivatedBy)
        {
            try
            {
                var privilege = await _context.AdminPrivileges.FirstOrDefaultAsync(p => p.AdminId == adminId);
                if (privilege == null) return false;

                privilege.IsActive = false;
                privilege.ModifiedDate = DateTime.Now;

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ActivateAdminAsync(string adminId, string activatedBy)
        {
            try
            {
                var privilege = await _context.AdminPrivileges.FirstOrDefaultAsync(p => p.AdminId == adminId);
                if (privilege == null) return false;

                privilege.IsActive = true;
                privilege.ModifiedDate = DateTime.Now;

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Template Management
        public async Task<List<AdminPrivilegeTemplate>> GetTemplatesAsync()
        {
            return await _context.AdminPrivilegeTemplates
                .Where(t => t.IsActive)
                .ToListAsync();
        }

        public async Task<AdminPrivilegeTemplate?> GetTemplateAsync(int templateId)
        {
            return await _context.AdminPrivilegeTemplates.FindAsync(templateId);
        }

        public async Task<AdminPrivilegeTemplate?> GetTemplateByAdminTypeAsync(AdminType adminType)
        {
            return await _context.AdminPrivilegeTemplates
                .FirstOrDefaultAsync(t => t.AdminType == adminType && t.IsActive);
        }

        // Permission Checking
        public async Task<bool> HasPermissionAsync(string adminId, PermissionModule permission)
        {
            var privilege = await _context.AdminPrivileges
                .FirstOrDefaultAsync(p => p.AdminId == adminId && p.IsActive);

            return privilege?.Permissions.Contains(permission) == true;
        }

        public async Task<List<PermissionModule>> GetAdminPermissionsAsync(string adminId)
        {
            var privilege = await _context.AdminPrivileges
                .FirstOrDefaultAsync(p => p.AdminId == adminId && p.IsActive);

            return privilege?.Permissions ?? new List<PermissionModule>();
        }

        public async Task<bool> IsSuperAdminAsync(string adminId)
        {
            var privilege = await _context.AdminPrivileges
                .FirstOrDefaultAsync(p => p.AdminId == adminId && p.IsActive);

            return privilege?.AdminType == AdminType.SuperAdmin;
        }

        // Dashboard Data
        public async Task<AdminDashboardViewModel> GetDashboardDataAsync()
        {
            var privileges = await GetAllAdminPrivilegesAsync();
            var applications = await GetPendingApplicationsAsync();

            var adminsByType = privileges
                .GroupBy(p => p.AdminType.ToString())
                .ToDictionary(g => g.Key, g => g.Count());

            return new AdminDashboardViewModel
            {
                TotalAdmins = privileges.Count,
                PendingApplications = applications.Count,
                ActiveAdmins = privileges.Count(p => p.IsActive),
                TotalUsers = await _userManager.Users.CountAsync(),
                RecentApplications = applications.Take(5).Select(a => new AdminApplicationViewModel
                {
                    Id = a.Id,
                    ApplicantName = a.ApplicantName,
                    Email = a.Email,
                    AppliedAdminType = a.AppliedAdminType,
                    Status = a.Status,
                    AppliedDate = a.AppliedDate
                }).ToList(),
                RecentAdmins = privileges.Take(5).Select(p => new AdminPrivilegeViewModel
                {
                    AdminId = p.AdminId,
                    AdminName = p.Admin.UserName ?? "N/A",
                    Email = p.Admin.Email ?? "N/A",
                    AdminType = p.AdminType,
                    CreatedDate = p.CreatedDate
                }).ToList(),
                AdminsByType = adminsByType // This ensures the dictionary is never null
            };
        }

        // Bulk Operations
        public async Task<bool> BulkUpdatePermissionsAsync(List<string> adminIds, List<PermissionModule> permissions, string updatedBy)
        {
            try
            {
                var privileges = await _context.AdminPrivileges
                    .Where(p => adminIds.Contains(p.AdminId))
                    .ToListAsync();

                foreach (var privilege in privileges)
                {
                    privilege.Permissions = permissions;
                    privilege.ModifiedDate = DateTime.Now;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> BulkChangeAdminTypeAsync(List<string> adminIds, AdminType newAdminType, string updatedBy)
        {
            try
            {
                var privileges = await _context.AdminPrivileges
                    .Where(p => adminIds.Contains(p.AdminId))
                    .ToListAsync();

                foreach (var privilege in privileges)
                {
                    privilege.AdminType = newAdminType;
                    privilege.ModifiedDate = DateTime.Now;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Import/Export (simplified implementations)
        public async Task<bool> ExportAdminsToExcelAsync(string filePath)
        {
            // Implement Excel export logic here
            await Task.CompletedTask;
            return true;
        }

        public async Task<bool> ImportAdminsFromExcelAsync(string filePath, string importedBy)
        {
            // Implement Excel import logic here
            await Task.CompletedTask;
            return true;
        }

        public async Task<bool> ExportApplicationsToExcelAsync(string filePath)
        {
            // Implement Excel export logic here
            await Task.CompletedTask;
            return true;
        }

        public async Task<bool> ExportPrivilegesToExcelAsync(string filePath)
        {
            // Implement Excel export logic here
            await Task.CompletedTask;
            return true;
        }

        // Helper methods
        private string GenerateRandomPassword()
        {
            const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*";
            var random = new Random();
            var chars = new char[12];
            for (int i = 0; i < chars.Length; i++)
            {
                chars[i] = validChars[random.Next(validChars.Length)];
            }
            return new string(chars);
        }

        private async Task<AdminPrivilegeTemplate?> GetTemplateForAdminType(AdminType adminType)
        {
            return await _context.AdminPrivilegeTemplates
                .FirstOrDefaultAsync(t => t.AdminType == adminType && t.IsActive);
        }

        private async Task SendAdminCredentialsEmail(string email, string password, AdminType adminType)
        {
            // Implement actual email sending logic here
            await Task.Delay(100); // Simulate email sending
            Console.WriteLine($"Credentials sent to {email}: {password} for role {adminType}");
        }
    }
}