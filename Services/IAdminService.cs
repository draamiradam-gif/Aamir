using StudentManagementSystem.Models;

namespace StudentManagementSystem.Services
{
    public interface IAdminService
    {
        // Application Management
        Task<List<AdminApplication>> GetPendingApplicationsAsync();
        Task<List<AdminApplication>> GetAllApplicationsAsync();
        Task<AdminApplication?> GetApplicationByIdAsync(int id);
        Task<bool> SubmitApplicationAsync(AdminApplication application);
        Task<bool> ReviewApplicationAsync(int applicationId, ApplicationStatus status, string reviewedBy, string notes);
        Task<bool> CreateAdminFromApplicationAsync(int applicationId, string createdBy);

        // Admin Privilege Management
        Task<List<AdminPrivilege>> GetAllAdminPrivilegesAsync();
        Task<AdminPrivilege?> GetAdminPrivilegeAsync(string adminId);
        Task<AdminPrivilege?> GetAdminPrivilegeByEmailAsync(string email);
        Task<bool> CreateAdminWithPrivilegesAsync(CreateAdminViewModel model, string createdBy);
        Task<bool> DeactivateAdminAsync(string adminId, string deactivatedBy);
        Task<bool> ActivateAdminAsync(string adminId, string activatedBy);

        // Template Management
        Task<List<AdminPrivilegeTemplate>> GetTemplatesAsync();
        Task<AdminPrivilegeTemplate?> GetTemplateAsync(int templateId);
        Task<AdminPrivilegeTemplate?> GetTemplateByAdminTypeAsync(AdminType adminType);

        // Permission Checking
        Task<bool> HasPermissionAsync(string adminId, PermissionModule permission);
        Task<List<PermissionModule>> GetAdminPermissionsAsync(string adminId);
        Task<bool> IsSuperAdminAsync(string adminId);

        // Import/Export
        Task<bool> ExportAdminsToExcelAsync(string filePath);
        Task<bool> ImportAdminsFromExcelAsync(string filePath, string importedBy);
        Task<bool> ExportApplicationsToExcelAsync(string filePath);
        Task<bool> ExportPrivilegesToExcelAsync(string filePath);

        // Dashboard Data
        Task<AdminDashboardViewModel> GetDashboardDataAsync();

        // Bulk Operations
        Task<bool> BulkUpdatePermissionsAsync(List<string> adminIds, List<PermissionModule> permissions, string updatedBy);
        Task<bool> BulkChangeAdminTypeAsync(List<string> adminIds, AdminType newAdminType, string updatedBy);

        Task<List<AdminApplication>> GetApprovedApplicationsAsync();

        // CHOOSE ONE OF THESE - Don't have duplicates!

        // Option 1: Single comprehensive method (Recommended)
        Task<bool> UpdateAdminPrivilegesAsync(string adminId,
            List<PermissionModule> permissions, string updatedBy, AdminType adminType,
            string? universityScope, string? facultyScope, string? departmentScope,
            string? newPassword = null);

        // Option 2: Separate methods for password updates
        /*
        Task<bool> UpdateAdminPrivilegesAsync(string adminId, 
            List<PermissionModule> permissions, string updatedBy, AdminType adminType,
            string? universityScope, string? facultyScope, string? departmentScope);
            
        Task<bool> UpdateAdminPasswordAsync(string adminId, string newPassword, string updatedBy);
        */

    }
}