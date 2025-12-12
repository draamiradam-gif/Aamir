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
        private readonly ILogger<CachedAdminService> _logger;

        public CachedAdminService(IAdminService adminService, IMemoryCache cache, ApplicationDbContext context, ILogger<CachedAdminService> logger)
        {
            _adminService = adminService;
            _cache = cache;
            _context = context;
            _logger = logger;
        }

        // ========== CACHED METHODS ==========

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

        public async Task<List<AdminApplication>> GetApprovedApplicationsAsync()
        {
            var cacheKey = "approved_applications_cache";

            // Try to get from cache first
            if (_cache.TryGetValue(cacheKey, out List<AdminApplication>? cachedApplications))
            {
                return cachedApplications ?? new List<AdminApplication>();
            }

            // Cache miss - fetch from database
            _logger.LogInformation("Cache miss for approved applications, fetching from database...");

            var applications = await _adminService.GetApprovedApplicationsAsync()
                ?? new List<AdminApplication>();

            // Cache the results
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(10))
                .SetAbsoluteExpiration(TimeSpan.FromHours(1));

            _cache.Set(cacheKey, applications, cacheOptions);

            return applications;
        }

        public async Task<AdminPrivilegeTemplate?> GetTemplateAsync(int templateId)
        {
            var cacheKey = $"Template_{templateId}";
            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
                return await _adminService.GetTemplateAsync(templateId);
            });
        }

        public async Task<AdminPrivilege?> GetAdminPrivilegeAsync(string adminId)
        {
            var cacheKey = $"admin-privilege-{adminId}";
            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                return await _adminService.GetAdminPrivilegeAsync(adminId);
            });
        }

        public async Task<AdminDashboardViewModel> GetDashboardDataAsync()
        {
            var cacheKey = "admin-dashboard-data";
            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2);
                return await _adminService.GetDashboardDataAsync();
            }) ?? new AdminDashboardViewModel();
        }

        // ========== UPDATE METHODS (CLEAR CACHE) ==========

        public async Task<bool> UpdateAdminPrivilegesAsync(string adminId,
            List<PermissionModule> permissions, string updatedBy, AdminType adminType,
            string? universityScope, string? facultyScope, string? departmentScope,
            string? newPassword = null)
        {
            // Clear relevant cache entries
            _cache.Remove($"admin-privilege-{adminId}");
            _cache.Remove("AllAdminPrivileges");
            _cache.Remove("admin-dashboard-data");

            // Delegate to the underlying service
            return await _adminService.UpdateAdminPrivilegesAsync(
                adminId, permissions, updatedBy, adminType,
                universityScope, facultyScope, departmentScope, newPassword);
        }

        public async Task<bool> CreateAdminWithPrivilegesAsync(CreateAdminViewModel model, string createdBy)
        {
            // Clear cache when creating new admin
            _cache.Remove("AllAdminPrivileges");
            _cache.Remove("admin-dashboard-data");
            _cache.Remove("approved_applications_cache");

            return await _adminService.CreateAdminWithPrivilegesAsync(model, createdBy);
        }

        public async Task<bool> DeactivateAdminAsync(string adminId, string deactivatedBy)
        {
            // Clear cache for this admin
            _cache.Remove($"admin-privilege-{adminId}");
            _cache.Remove("AllAdminPrivileges");
            _cache.Remove("admin-dashboard-data");

            return await _adminService.DeactivateAdminAsync(adminId, deactivatedBy);
        }

        public async Task<bool> ActivateAdminAsync(string adminId, string activatedBy)
        {
            // Clear cache for this admin
            _cache.Remove($"admin-privilege-{adminId}");
            _cache.Remove("AllAdminPrivileges");
            _cache.Remove("admin-dashboard-data");

            return await _adminService.ActivateAdminAsync(adminId, activatedBy);
        }

        public async Task<bool> SubmitApplicationAsync(AdminApplication application)
        {
            // Clear applications cache
            _cache.Remove("approved_applications_cache");

            return await _adminService.SubmitApplicationAsync(application);
        }

        public async Task<bool> ReviewApplicationAsync(int applicationId, ApplicationStatus status, string reviewedBy, string notes)
        {
            // Clear applications cache
            _cache.Remove("approved_applications_cache");

            return await _adminService.ReviewApplicationAsync(applicationId, status, reviewedBy, notes);
        }

        public async Task<bool> CreateAdminFromApplicationAsync(int applicationId, string createdBy)
        {
            // Clear cache when creating admin from application
            _cache.Remove("AllAdminPrivileges");
            _cache.Remove("admin-dashboard-data");
            _cache.Remove("approved_applications_cache");

            return await _adminService.CreateAdminFromApplicationAsync(applicationId, createdBy);
        }

        public async Task<bool> BulkUpdatePermissionsAsync(List<string> adminIds, List<PermissionModule> permissions, string updatedBy)
        {
            // Clear cache for all affected admins
            foreach (var adminId in adminIds)
            {
                _cache.Remove($"admin-privilege-{adminId}");
            }
            _cache.Remove("AllAdminPrivileges");

            return await _adminService.BulkUpdatePermissionsAsync(adminIds, permissions, updatedBy);
        }

        public async Task<bool> BulkChangeAdminTypeAsync(List<string> adminIds, AdminType newAdminType, string updatedBy)
        {
            // Clear cache for all affected admins
            foreach (var adminId in adminIds)
            {
                _cache.Remove($"admin-privilege-{adminId}");
            }
            _cache.Remove("AllAdminPrivileges");

            return await _adminService.BulkChangeAdminTypeAsync(adminIds, newAdminType, updatedBy);
        }

        public async Task<bool> ImportAdminsFromExcelAsync(string filePath, string importedBy)
        {
            // Clear all admin caches when importing
            _cache.Remove("AllAdminPrivileges");
            _cache.Remove("admin-dashboard-data");

            return await _adminService.ImportAdminsFromExcelAsync(filePath, importedBy);
        }

        // ========== PASSTHROUGH METHODS (NO CACHING) ==========

        public Task<List<AdminApplication>> GetPendingApplicationsAsync() => _adminService.GetPendingApplicationsAsync();
        public Task<List<AdminApplication>> GetAllApplicationsAsync() => _adminService.GetAllApplicationsAsync();
        public Task<AdminApplication?> GetApplicationByIdAsync(int id) => _adminService.GetApplicationByIdAsync(id);
        public Task<AdminPrivilege?> GetAdminPrivilegeByEmailAsync(string email) => _adminService.GetAdminPrivilegeByEmailAsync(email);
        public Task<bool> HasPermissionAsync(string adminId, PermissionModule permission) => _adminService.HasPermissionAsync(adminId, permission);
        public Task<List<PermissionModule>> GetAdminPermissionsAsync(string adminId) => _adminService.GetAdminPermissionsAsync(adminId);
        public Task<bool> IsSuperAdminAsync(string adminId) => _adminService.IsSuperAdminAsync(adminId);
        public Task<bool> ExportAdminsToExcelAsync(string filePath) => _adminService.ExportAdminsToExcelAsync(filePath);
        public Task<bool> ExportApplicationsToExcelAsync(string filePath) => _adminService.ExportApplicationsToExcelAsync(filePath);
        public Task<bool> ExportPrivilegesToExcelAsync(string filePath) => _adminService.ExportPrivilegesToExcelAsync(filePath);
    }
}