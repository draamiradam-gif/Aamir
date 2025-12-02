using Microsoft.Extensions.Caching.Memory;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;

namespace StudentManagementSystem.Services
{
    public class CachedAdminService : IAdminService
    {
        private readonly IAdminService _adminService;
        private readonly IMemoryCache _cache;
        private readonly ApplicationDbContext _context;

        public CachedAdminService(IAdminService adminService, IMemoryCache cache, ApplicationDbContext context)
        {
            _adminService = adminService;
            _cache = cache;
            _context = context;
        }

        // Cached methods
        public async Task<List<AdminPrivilege>> GetAllAdminPrivilegesAsync()
        {
            return await _cache.GetOrCreateAsync("AllAdminPrivileges", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                return await _adminService.GetAllAdminPrivilegesAsync();
            }) ?? new List<AdminPrivilege>();
        }

        public async Task<List<AdminPrivilegeTemplate>> GetTemplatesAsync()
        {
            return await _cache.GetOrCreateAsync("AdminTemplates", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                return await _adminService.GetTemplatesAsync();
            }) ?? new List<AdminPrivilegeTemplate>();
        }

        public async Task<AdminPrivilegeTemplate?> GetTemplateByAdminTypeAsync(AdminType adminType)
        {
            var cacheKey = $"Template_{adminType}";
            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                return await _adminService.GetTemplateByAdminTypeAsync(adminType);
            });
        }

        // Non-cached methods - pass through to underlying service
        public Task<List<AdminApplication>> GetPendingApplicationsAsync() => _adminService.GetPendingApplicationsAsync();
        public Task<List<AdminApplication>> GetAllApplicationsAsync() => _adminService.GetAllApplicationsAsync();
        public Task<AdminApplication?> GetApplicationByIdAsync(int id) => _adminService.GetApplicationByIdAsync(id);
        public Task<bool> SubmitApplicationAsync(AdminApplication application) => _adminService.SubmitApplicationAsync(application);
        public Task<bool> ReviewApplicationAsync(int applicationId, ApplicationStatus status, string reviewedBy, string notes) => _adminService.ReviewApplicationAsync(applicationId, status, reviewedBy, notes);
        public Task<bool> CreateAdminFromApplicationAsync(int applicationId, string createdBy) => _adminService.CreateAdminFromApplicationAsync(applicationId, createdBy);
        public Task<AdminPrivilege?> GetAdminPrivilegeAsync(string adminId) => _adminService.GetAdminPrivilegeAsync(adminId);
        public Task<AdminPrivilege?> GetAdminPrivilegeByEmailAsync(string email) => _adminService.GetAdminPrivilegeByEmailAsync(email);
        public Task<bool> UpdateAdminPrivilegesAsync(string adminId, List<PermissionModule> permissions, string updatedBy) => _adminService.UpdateAdminPrivilegesAsync(adminId, permissions, updatedBy);
        public Task<bool> CreateAdminWithPrivilegesAsync(CreateAdminViewModel model, string createdBy) => _adminService.CreateAdminWithPrivilegesAsync(model, createdBy);
        public Task<bool> DeactivateAdminAsync(string adminId, string deactivatedBy) => _adminService.DeactivateAdminAsync(adminId, deactivatedBy);
        public Task<bool> ActivateAdminAsync(string adminId, string activatedBy) => _adminService.ActivateAdminAsync(adminId, activatedBy);
        public Task<AdminPrivilegeTemplate?> GetTemplateAsync(int templateId) => _adminService.GetTemplateAsync(templateId);
        public Task<bool> HasPermissionAsync(string adminId, PermissionModule permission) => _adminService.HasPermissionAsync(adminId, permission);
        public Task<List<PermissionModule>> GetAdminPermissionsAsync(string adminId) => _adminService.GetAdminPermissionsAsync(adminId);
        public Task<bool> IsSuperAdminAsync(string adminId) => _adminService.IsSuperAdminAsync(adminId);
        public Task<bool> ExportAdminsToExcelAsync(string filePath) => _adminService.ExportAdminsToExcelAsync(filePath);
        public Task<bool> ImportAdminsFromExcelAsync(string filePath, string importedBy) => _adminService.ImportAdminsFromExcelAsync(filePath, importedBy);
        public Task<bool> ExportApplicationsToExcelAsync(string filePath) => _adminService.ExportApplicationsToExcelAsync(filePath);
        public Task<bool> ExportPrivilegesToExcelAsync(string filePath) => _adminService.ExportPrivilegesToExcelAsync(filePath);
        public Task<AdminDashboardViewModel> GetDashboardDataAsync() => _adminService.GetDashboardDataAsync();
        public Task<bool> BulkUpdatePermissionsAsync(List<string> adminIds, List<PermissionModule> permissions, string updatedBy) => _adminService.BulkUpdatePermissionsAsync(adminIds, permissions, updatedBy);
        public Task<bool> BulkChangeAdminTypeAsync(List<string> adminIds, AdminType newAdminType, string updatedBy) => _adminService.BulkChangeAdminTypeAsync(adminIds, newAdminType, updatedBy);
    }
}