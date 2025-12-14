using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace StudentManagementSystem.Models
{
    public enum AdminType
    {
        SuperAdmin = 1,
        UniversityAdmin = 2,
        FacultyAdmin = 3,
        DepartmentAdmin = 4,
        EmployeeAdmin = 5,
        FinanceAdmin = 6,
        StudentAdmin = 7,
        CustomAdmin = 8
    }

    public enum PermissionModule
    {
        // User Management
        UserManagement_View,
        UserManagement_Create,
        UserManagement_Edit,
        UserManagement_Delete,
        UserManagement_Export,

        // Student Management
        Student_View,
        Student_Create,
        Student_Edit,
        Student_Delete,
        Student_Export,
        Student_Import,

        // Course Management
        Course_View,
        Course_Create,
        Course_Edit,
        Course_Delete,
        Course_Export,
        Course_Import,

        // Grade Management
        Grade_View,
        Grade_Edit,
        Grade_Delete,
        Grade_Export,
        Grade_Import,

        // Finance Management
        Finance_View,
        Finance_Create,
        Finance_Edit,
        Finance_Delete,
        Finance_Export,
        Finance_Reports,

        // University Structure
        University_View,
        University_Create,
        University_Edit,
        University_Delete,

        // College Management
        College_View,
        College_Create,
        College_Edit,
        College_Delete,

        // Department Management
        Department_View,
        Department_Create,
        Department_Edit,
        Department_Delete,

        // System Administration
        System_Backup,
        System_Restore,
        System_Logs,
        System_Settings,

        // Admin Management
        Admin_View,
        Admin_Create,
        Admin_Edit,
        Admin_Delete,
        Admin_Permissions,

        // Application Management
        Application_View,
        Application_Approve,
        Application_Reject,
        Application_Export
    }

    public enum ApplicationStatus
    {
        Pending,
        Approved,
        Rejected,
        Revoked,
        Blocked,
        UnderReview
    }

    // Main Admin Creation ViewModel
    public class CreateAdminViewModel
    {
        public string AdminId { get; set; } = string.Empty;
        public string AdminName { get; set; } = string.Empty;
        public int ApplicationId { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Admin Type")]
        public AdminType AdminType { get; set; }

        // Scope limitations
        [Display(Name = "University Scope")]
        public string? UniversityScope { get; set; }

        [Display(Name = "Faculty Scope")]
        public string? FacultyScope { get; set; }

        [Display(Name = "Department Scope")]
        public string? DepartmentScope { get; set; }

        // Permissions selection
        [Display(Name = "Permissions")]
        public List<PermissionModule> SelectedPermissions { get; set; } = new List<PermissionModule>();

        [Required]
        [DataType(DataType.Password)]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        // For templates
        [Display(Name = "Use Template")]
        public int? TemplateId { get; set; }

        // Navigation properties for dropdowns
        public List<AdminPrivilegeTemplate> AvailableTemplates { get; set; } = new List<AdminPrivilegeTemplate>();

        // Select List for UI
        public SelectList TemplateList => new SelectList(AvailableTemplates, "Id", "TemplateName", TemplateId);
        public List<University> Universities { get; set; } = new List<University>();
        public List<College> Colleges { get; set; } = new List<College>();
        public List<Department> Departments { get; set; } = new List<Department>();

        // Select Lists for UI
        public SelectList UniversityList => new SelectList(Universities, "Id", "Name", UniversityScope);
        public SelectList CollegeList => new SelectList(Colleges, "Id", "Name", FacultyScope);
        public SelectList DepartmentList => new SelectList(Departments, "Id", "Name", DepartmentScope);
       // public SelectList TemplateList => new SelectList(AvailableTemplates, "Id", "TemplateName", TemplateId);
        public SelectList AdminTypeList => new SelectList(Enum.GetValues<AdminType>().Select(at => new { Value = at, Text = at.ToString() }), "Value", "Text");

        // All available permissions for checkbox list
        public List<PermissionModule> AllPermissions => Enum.GetValues<PermissionModule>().ToList();

        // ADD THIS for Edit view compatibility:
        public List<PermissionModule> CurrentPermissions { get; set; } = new List<PermissionModule>();
    }

    // Admin Application Form ViewModel
    public class AdminApplicationFormViewModel
    {
        [Required]
        [Display(Name = "Full Name")]
        public string ApplicantName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Phone]
        [Display(Name = "Phone Number")]
        public string Phone { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Requested Admin Type")]
        public AdminType AppliedAdminType { get; set; }

        [Display(Name = "University")]
        public string? UniversityId { get; set; }

        [Display(Name = "Faculty/College")]
        public string? FacultyId { get; set; }

        [Display(Name = "Department")]
        public string? DepartmentId { get; set; }

        [Required]
        [StringLength(1000, ErrorMessage = "Justification must be between 10 and 1000 characters.", MinimumLength = 10)]
        [Display(Name = "Justification for Admin Access")]
        public string Justification { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Relevant Experience")]
        public string? Experience { get; set; }

        [StringLength(500)]
        [Display(Name = "Qualifications")]
        public string? Qualifications { get; set; }

        // Navigation properties for dropdowns
        public List<University> Universities { get; set; } = new List<University>();
        public List<College> Colleges { get; set; } = new List<College>();
        public List<Department> Departments { get; set; } = new List<Department>();

        // Select Lists
        public SelectList UniversityList => new SelectList(Universities, "Id", "Name", UniversityId);
        public SelectList CollegeList => new SelectList(Colleges, "Id", "Name", FacultyId);
        public SelectList DepartmentList => new SelectList(Departments, "Id", "Name", DepartmentId);
        public SelectList AdminTypeList => new SelectList(Enum.GetValues<AdminType>()
            .Where(at => at != AdminType.SuperAdmin) // Don't allow SuperAdmin applications
            .Select(at => new { Value = at, Text = at.ToString() }), "Value", "Text");
    }

    // Admin Privilege View Model for Display
    //public class AdminPrivilegeViewModel
    //{
    //    public string AdminId { get; set; } = string.Empty;
    //    public string AdminName { get; set; } = string.Empty;
    //    public string Email { get; set; } = string.Empty;
    //    public AdminType AdminType { get; set; }
    //    public string AdminTypeName => AdminType.ToString();
    //    public List<PermissionModule> Permissions { get; set; } = new List<PermissionModule>();
    //    public string? UniversityScope { get; set; }
    //    public string? FacultyScope { get; set; }
    //    public string? DepartmentScope { get; set; }
    //    public bool IsActive { get; set; }
    //    public DateTime CreatedDate { get; set; }
    //    public string CreatedBy { get; set; } = string.Empty;


    //    // Helper properties for UI
    //    public string PermissionCount => $"{Permissions.Count} permissions";
    //    public string ScopeInfo
    //    {
    //        get
    //        {
    //            var scopes = new List<string>();
    //            if (!string.IsNullOrEmpty(UniversityScope)) scopes.Add($"Univ: {UniversityScope}");
    //            if (!string.IsNullOrEmpty(FacultyScope)) scopes.Add($"Faculty: {FacultyScope}");
    //            if (!string.IsNullOrEmpty(DepartmentScope)) scopes.Add($"Dept: {DepartmentScope}");
    //            return scopes.Any() ? string.Join(" | ", scopes) : "Global Access";
    //        }
    //    }

    //    public bool IsFromApplication { get; set; }
    //    public int? ApplicationId { get; set; }

    //    // Helper properties for UI
    //    public bool IsNewApproval => IsFromApplication && !IsActive;
    //    public string StatusBadgeClass
    //    {
    //        get
    //        {
    //            if (IsFromApplication) return "badge bg-warning";
    //            return IsActive ? "badge bg-success" : "badge bg-secondary";
    //        }
    //    }

    //    public string StatusText
    //    {
    //        get
    //        {
    //            if (IsFromApplication) return "Awaiting Setup";
    //            return IsActive ? "Active" : "Inactive";
    //        }
    //    }
    //}
    public class AdminPrivilegeViewModel
    {
        // Core properties
        public string AdminId { get; set; } = string.Empty;
        public string AdminName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public AdminType AdminType { get; set; }
        public List<PermissionModule> Permissions { get; set; } = new List<PermissionModule>();
        public string? UniversityScope { get; set; }
        public string? FacultyScope { get; set; }
        public string? DepartmentScope { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public bool IsFromApplication { get; set; }
        public int? ApplicationId { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }
        public AdminApplication? ApplicationData { get; set; }
        //public int PermissionCount => Permissions?.Count ?? 0;


        // Helper properties
        public string AdminTypeName => AdminType.ToString();

        public string StatusBadgeClass
        {
            get
            {
                if (IsFromApplication) return "bg-warning text-dark";
                return IsActive ? "bg-success" : "bg-secondary";
            }
        }

        public string StatusText
        {
            get
            {
                if (IsFromApplication) return "Awaiting Setup";
                return IsActive ? "Active" : "Inactive";
            }
        }

        public string AdminTypeBadgeClass => AdminType switch
        {
            AdminType.SuperAdmin => "bg-danger",
            AdminType.UniversityAdmin => "bg-primary",
            AdminType.FacultyAdmin => "bg-info",
            AdminType.DepartmentAdmin => "bg-success",
            AdminType.FinanceAdmin => "bg-warning text-dark",
            AdminType.StudentAdmin => "bg-purple",
            AdminType.CustomAdmin => "bg-dark",
            _ => "bg-secondary"
        };

        public string ScopeInfo
        {
            get
            {
                var scopes = new List<string>();
                if (!string.IsNullOrEmpty(UniversityScope)) scopes.Add($"Univ: {UniversityScope}");
                if (!string.IsNullOrEmpty(FacultyScope)) scopes.Add($"Faculty: {FacultyScope}");
                if (!string.IsNullOrEmpty(DepartmentScope)) scopes.Add($"Dept: {DepartmentScope}");
                return scopes.Any() ? string.Join(" | ", scopes) : "Global Access";
            }
        }

        public string PermissionSummary
        {
            get
            {
                if (!Permissions.Any()) return "No permissions";

                var grouped = Permissions
                    .Select(p => PermissionHelper.GetPermissionCategory(p))
                    .Distinct()
                    .ToList();

                return $"{Permissions.Count} permissions ({string.Join(", ", grouped.Take(3))})";
            }
        }

        public string TimeAgoCreated => GetTimeAgo(CreatedDate);
        public string TimeAgoModified => ModifiedDate.HasValue ? GetTimeAgo(ModifiedDate.Value) : "Never";
        public string TimeAgoLastLogin => LastLoginDate.HasValue ? GetTimeAgo(LastLoginDate.Value) : "Never";

        private string GetTimeAgo(DateTime date)
        {
            var timeSpan = DateTime.Now - date;

            if (timeSpan.TotalDays >= 365)
                return $"{(int)(timeSpan.TotalDays / 365)}y ago";
            if (timeSpan.TotalDays >= 30)
                return $"{(int)(timeSpan.TotalDays / 30)}mo ago";
            if (timeSpan.TotalDays >= 7)
                return $"{(int)(timeSpan.TotalDays / 7)}w ago";
            if (timeSpan.TotalDays >= 1)
                return $"{(int)timeSpan.TotalDays}d ago";
            if (timeSpan.TotalHours >= 1)
                return $"{(int)timeSpan.TotalHours}h ago";
            if (timeSpan.TotalMinutes >= 1)
                return $"{(int)timeSpan.TotalMinutes}m ago";

            return "Just now";
        }
    }

    // Admin Application View Model for Display
    public class AdminApplicationViewModel
    {
        public int Id { get; set; }
        public string ApplicantName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public AdminType AppliedAdminType { get; set; }
        public string AppliedAdminTypeName => AppliedAdminType.ToString();
        public string Justification { get; set; } = string.Empty;
        public string? Experience { get; set; }
        public string? Qualifications { get; set; }
        public ApplicationStatus Status { get; set; }
        public DateTime AppliedDate { get; set; }
        public DateTime? ReviewedDate { get; set; }
        public string? ReviewedBy { get; set; }
        public string? ReviewNotes { get; set; }
        public string? UniversityName { get; set; }
        public string? FacultyName { get; set; }
        public string? DepartmentName { get; set; }

        // Helper properties
        public string StatusBadgeClass => Status switch
        {
            ApplicationStatus.Approved => "badge bg-success",
            ApplicationStatus.Rejected => "badge bg-danger",
            ApplicationStatus.UnderReview => "badge bg-warning",
            _ => "badge bg-secondary"
        };

        public string DaysSinceApplied => $"{(DateTime.Now - AppliedDate).Days} days ago";
    }

    //// Edit Admin Privileges ViewModel
    //public class EditAdminPrivilegesViewModel
    //{
    //    public string AdminId { get; set; } = string.Empty;
    //    public string AdminName { get; set; } = string.Empty;
    //    public string Email { get; set; } = string.Empty;
    //    public AdminType AdminType { get; set; }

    //    // ADD THESE PROPERTIES:
    //    [Display(Name = "University Scope")]
    //    public string? UniversityScope { get; set; }

    //    [Display(Name = "Faculty Scope")]
    //    public string? FacultyScope { get; set; }

    //    [Display(Name = "Department Scope")]
    //    public string? DepartmentScope { get; set; }

    //    // ADD THESE FOR DROPDOWNS:
    //    public List<AdminPrivilegeTemplate> AvailableTemplates { get; set; } = new List<AdminPrivilegeTemplate>();
    //    public List<University> Universities { get; set; } = new List<University>();
    //    public List<College> Colleges { get; set; } = new List<College>();
    //    public List<Department> Departments { get; set; } = new List<Department>();

    //    // ADD THESE FOR UI SELECT LISTS:
    //    public SelectList TemplateList => new SelectList(AvailableTemplates, "Id", "TemplateName");
    //    public SelectList UniversityList => new SelectList(Universities, "Id", "Name", UniversityScope);
    //    public SelectList CollegeList => new SelectList(Colleges, "Id", "Name", FacultyScope);
    //    public SelectList DepartmentList => new SelectList(Departments, "Id", "Name", DepartmentScope);
    //    public SelectList AdminTypeList => new SelectList(Enum.GetValues<AdminType>().Select(at => new { Value = at, Text = at.ToString() }), "Value", "Text");

    //    // Existing properties:
    //    public List<PermissionModule> CurrentPermissions { get; set; } = new List<PermissionModule>();
    //    public List<PermissionModule> AllPermissions { get; set; } = new List<PermissionModule>();

    //    // Helper for grouped permissions
    //    public Dictionary<string, List<PermissionModule>> GroupedPermissions => AllPermissions
    //        .GroupBy(p => p.ToString().Split('_')[0])
    //        .ToDictionary(g => g.Key, g => g.ToList());
    //}

    // Admin Dashboard ViewModel
    public class AdminDashboardViewModel
    {
        public int TotalAdmins { get; set; }
        public int PendingApplications { get; set; }
        public int ActiveAdmins { get; set; }
        public int TotalUsers { get; set; }
        public List<AdminApplicationViewModel> RecentApplications { get; set; } = new List<AdminApplicationViewModel>();
        public List<AdminPrivilegeViewModel> RecentAdmins { get; set; } = new List<AdminPrivilegeViewModel>();

        // Statistics by admin type - ensure proper initialization
        public Dictionary<string, int> AdminsByType { get; set; } = new Dictionary<string, int>();
    }

    // Import/Export ViewModels
    public class AdminImportViewModel
    {
        [Required(ErrorMessage = "Please select a file to upload")]
        [Display(Name = "Import File")]
        public IFormFile ImportFile { get; set; } = null!;

        [Display(Name = "Overwrite Existing Admins")]
        public bool OverwriteExisting { get; set; }

        [Display(Name = "Send Welcome Email")]
        public bool SendWelcomeEmail { get; set; }
    }

    public class AdminExportViewModel
    {
        [Display(Name = "Export Format")]
        public ExportFormat Format { get; set; } = ExportFormat.Excel;

        [Display(Name = "Include Permissions")]
        public bool IncludePermissions { get; set; } = true;

        [Display(Name = "Include Scope Information")]
        public bool IncludeScope { get; set; } = true;

        [Display(Name = "Admin Types to Export")]
        public List<AdminType> SelectedAdminTypes { get; set; } = new List<AdminType>();

        public List<AdminType> AllAdminTypes => Enum.GetValues<AdminType>().ToList();
    }

    public enum ExportFormat
    {
        Excel,
        CSV,
        PDF,
        JSON
    }

    // Permission Group ViewModel for UI
    public class PermissionGroupViewModel
    {
        public string GroupName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public List<PermissionItemViewModel> Permissions { get; set; } = new List<PermissionItemViewModel>();
    }

    public class PermissionItemViewModel
    {
        public PermissionModule Permission { get; set; }

        // Add getters and setters for these properties
        public string Name
        {
            get => Permission.ToString();
            set { /* optional setter if needed */ }
        }

        public string DisplayName
        {
            get => Permission.ToString().Replace('_', ' ');
            set { /* optional setter if needed */ }
        }

        public string Description { get; set; } = string.Empty;
        public bool IsSelected { get; set; }

        // Alternative: Use computed properties without setters
        // public string Name => Permission.ToString();
        // public string DisplayName => Permission.ToString().Replace('_', ' ');
    }

    // Bulk Operations ViewModel (FIXED - removed duplicates)
    public class BulkAdminOperationViewModel
    {
        [Required(ErrorMessage = "Please select an operation type")]
        [Display(Name = "Operation Type")]
        public BulkOperationType OperationType { get; set; }

        [Required(ErrorMessage = "Please select at least one admin")]
        [Display(Name = "Select Admins")]
        public List<string> SelectedAdminIds { get; set; } = new List<string>();

        [Display(Name = "New Permissions")]
        public List<PermissionModule> NewPermissions { get; set; } = new List<PermissionModule>();

        [Display(Name = "New Admin Type")]
        public AdminType? NewAdminType { get; set; }

        [Display(Name = "Available Admins")]
        public List<AdminPrivilegeViewModel> AvailableAdmins { get; set; } = new List<AdminPrivilegeViewModel>();
    }

    public enum BulkOperationType
    {
        UpdatePermissions,
        ChangeAdminType,
        Activate,
        Deactivate
    }

    // Audit Log ViewModel
    public class AuditLogViewModel
    {
        public int Id { get; set; }
        public string AdminEmail { get; set; } = string.Empty;
        public AdminType AdminType { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public string PerformedBy { get; set; } = string.Empty;
        public string OldValue { get; set; } = string.Empty;
        public string NewValue { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string TimeAgo => GetTimeAgo(Timestamp);

        private string GetTimeAgo(DateTime timestamp)
        {
            var timeSpan = DateTime.Now - timestamp;
            if (timeSpan.TotalDays >= 1)
                return $"{(int)timeSpan.TotalDays} days ago";
            if (timeSpan.TotalHours >= 1)
                return $"{(int)timeSpan.TotalHours} hours ago";
            if (timeSpan.TotalMinutes >= 1)
                return $"{(int)timeSpan.TotalMinutes} minutes ago";
            return "Just now";
        }
    }

    // Import Result ViewModel
    public class AdminImportResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public int ImportedCount { get; set; }
        public int SkippedCount { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }

    // Helper method for getting permission descriptions
    public static class PermissionHelper
    {
        public static string GetPermissionDescription(PermissionModule permission)
        {
            return permission switch
            {
                // User Management
                PermissionModule.UserManagement_View => "View user accounts and profiles",
                PermissionModule.UserManagement_Create => "Create new user accounts",
                PermissionModule.UserManagement_Edit => "Edit user information and settings",
                PermissionModule.UserManagement_Delete => "Delete user accounts",
                PermissionModule.UserManagement_Export => "Export user data to files",

                // Student Management
                PermissionModule.Student_View => "View student profiles and information",
                PermissionModule.Student_Create => "Create new student records",
                PermissionModule.Student_Edit => "Edit student information",
                PermissionModule.Student_Delete => "Delete student records",
                PermissionModule.Student_Export => "Export student data",
                PermissionModule.Student_Import => "Import student data from files",

                // Course Management
                PermissionModule.Course_View => "View course catalog and details",
                PermissionModule.Course_Create => "Create new courses",
                PermissionModule.Course_Edit => "Edit course information",
                PermissionModule.Course_Delete => "Delete courses",
                PermissionModule.Course_Export => "Export course data",
                PermissionModule.Course_Import => "Import course data",

                // Grade Management
                PermissionModule.Grade_View => "View student grades and transcripts",
                PermissionModule.Grade_Edit => "Enter and modify grades",
                PermissionModule.Grade_Delete => "Delete grade records",
                PermissionModule.Grade_Export => "Export grade reports",
                PermissionModule.Grade_Import => "Import grade data",

                // Finance Management
                PermissionModule.Finance_View => "View financial records and transactions",
                PermissionModule.Finance_Create => "Create financial records",
                PermissionModule.Finance_Edit => "Edit financial information",
                PermissionModule.Finance_Delete => "Delete financial records",
                PermissionModule.Finance_Export => "Export financial data",
                PermissionModule.Finance_Reports => "Generate financial reports",

                // University Structure
                PermissionModule.University_View => "View university information",
                PermissionModule.University_Create => "Create new university records",
                PermissionModule.University_Edit => "Edit university details",
                PermissionModule.University_Delete => "Delete university records",

                // College Management
                PermissionModule.College_View => "View college/faculty information",
                PermissionModule.College_Create => "Create new college records",
                PermissionModule.College_Edit => "Edit college details",
                PermissionModule.College_Delete => "Delete college records",

                // Department Management
                PermissionModule.Department_View => "View department information",
                PermissionModule.Department_Create => "Create new department records",
                PermissionModule.Department_Edit => "Edit department details",
                PermissionModule.Department_Delete => "Delete department records",

                // System Administration
                PermissionModule.System_Backup => "Create system backups",
                PermissionModule.System_Restore => "Restore from backups",
                PermissionModule.System_Logs => "View system logs",
                PermissionModule.System_Settings => "Configure system settings",

                // Admin Management
                PermissionModule.Admin_View => "View admin accounts and privileges",
                PermissionModule.Admin_Create => "Create new admin accounts",
                PermissionModule.Admin_Edit => "Edit admin privileges",
                PermissionModule.Admin_Delete => "Delete admin accounts",
                PermissionModule.Admin_Permissions => "Manage permission assignments",

                // Application Management
                PermissionModule.Application_View => "View admin applications",
                PermissionModule.Application_Approve => "Approve admin applications",
                PermissionModule.Application_Reject => "Reject admin applications",
                PermissionModule.Application_Export => "Export application data",

                _ => permission.ToString().Replace('_', ' ')
            };
        }

        public static string GetCategoryDisplayName(string category)
        {
            return category switch
            {
                "UserManagement" => "User Management",
                "Student" => "Student Management",
                "Course" => "Course Management",
                "Grade" => "Grade Management",
                "Finance" => "Finance Management",
                "University" => "University Management",
                "College" => "College Management",
                "Department" => "Department Management",
                "System" => "System Administration",
                "Admin" => "Admin Management",
                "Application" => "Application Management",
                _ => category
            };
        }

        public static string GetPermissionAction(PermissionModule permission)
        {
            var name = permission.ToString();
            return name.Contains('_') ? name.Split('_')[1] : name;
        }

        public static string GetPermissionCategory(PermissionModule permission)
        {
            var name = permission.ToString();
            return name.Split('_')[0];
        }

        public static List<PermissionGroupViewModel> GetGroupedPermissions(List<PermissionModule>? selectedPermissions = null)
        {
            var allPermissions = Enum.GetValues<PermissionModule>().ToList();
            var groups = allPermissions
                .GroupBy(p => GetPermissionCategory(p))
                .Select(g => new PermissionGroupViewModel
                {
                    GroupName = g.Key,
                    DisplayName = GetCategoryDisplayName(g.Key),
                    Permissions = g.Select(p => new PermissionItemViewModel
                    {
                        Permission = p,
                        Name = p.ToString(),
                        DisplayName = p.ToString().Replace('_', ' '),
                        Description = GetPermissionDescription(p),
                        IsSelected = selectedPermissions != null && selectedPermissions.Contains(p)
                    }).ToList()
                })
                .OrderBy(g => g.DisplayName)
                .ToList();

            return groups;
        }

        public static Dictionary<string, List<PermissionModule>> GetPermissionsByCategory()
        {
            return Enum.GetValues<PermissionModule>()
                .GroupBy(p => GetPermissionCategory(p))
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        public static string GetPermissionBadgeClass(PermissionModule permission)
        {
            var action = GetPermissionAction(permission);

            return action switch
            {
                "View" => "badge bg-info",
                "Create" => "badge bg-success",
                "Edit" => "badge bg-warning",
                "Delete" => "badge bg-danger",
                "Export" => "badge bg-info",
                "Import" => "badge bg-warning",
                "Backup" => "badge bg-dark",
                "Restore" => "badge bg-dark",
                "Logs" => "badge bg-secondary",
                "Settings" => "badge bg-dark",
                "Approve" => "badge bg-success",
                "Reject" => "badge bg-danger",
                "Reports" => "badge bg-primary",
                "Permissions" => "badge bg-primary",
                _ => "badge bg-secondary"
            };
        }

        public static List<PermissionModule> GetDefaultPermissionsForAdminType(AdminType adminType)
        {
            return adminType switch
            {
                AdminType.SuperAdmin => Enum.GetValues<PermissionModule>().ToList(),

                AdminType.UniversityAdmin => new List<PermissionModule>
                {
                    // University scope
                    PermissionModule.University_View,
                    PermissionModule.University_Edit,
                    
                    // User Management
                    PermissionModule.UserManagement_View,
                    PermissionModule.UserManagement_Create,
                    PermissionModule.UserManagement_Edit,
                    
                    // Student Management
                    PermissionModule.Student_View,
                    PermissionModule.Student_Create,
                    PermissionModule.Student_Edit,
                    PermissionModule.Student_Export,
                    
                    // Course Management
                    PermissionModule.Course_View,
                    PermissionModule.Course_Create,
                    PermissionModule.Course_Edit,
                    
                    // Grade Management
                    PermissionModule.Grade_View,
                    PermissionModule.Grade_Edit,
                    PermissionModule.Grade_Export,
                },

                AdminType.FacultyAdmin => new List<PermissionModule>
                {
                    // College scope
                    PermissionModule.College_View,
                    PermissionModule.College_Edit,
                    
                    // User Management
                    PermissionModule.UserManagement_View,
                    PermissionModule.UserManagement_Edit,
                    
                    // Student Management
                    PermissionModule.Student_View,
                    PermissionModule.Student_Edit,
                    PermissionModule.Student_Export,
                    
                    // Course Management
                    PermissionModule.Course_View,
                    PermissionModule.Course_Edit,
                    
                    // Grade Management
                    PermissionModule.Grade_View,
                    PermissionModule.Grade_Edit,
                },

                AdminType.DepartmentAdmin => new List<PermissionModule>
                {
                    // Department scope
                    PermissionModule.Department_View,
                    PermissionModule.Department_Edit,
                    
                    // Student Management
                    PermissionModule.Student_View,
                    PermissionModule.Student_Edit,
                    
                    // Course Management
                    PermissionModule.Course_View,
                    PermissionModule.Course_Edit,
                    
                    // Grade Management
                    PermissionModule.Grade_View,
                    PermissionModule.Grade_Edit,
                },

                AdminType.FinanceAdmin => new List<PermissionModule>
                {
                    // Finance Management
                    PermissionModule.Finance_View,
                    PermissionModule.Finance_Create,
                    PermissionModule.Finance_Edit,
                    PermissionModule.Finance_Export,
                    PermissionModule.Finance_Reports,
                    
                    // Student Management (for billing)
                    PermissionModule.Student_View,
                    
                    // Export capabilities
                    PermissionModule.UserManagement_Export,
                    PermissionModule.Student_Export,
                    PermissionModule.Course_Export,
                },

                AdminType.StudentAdmin => new List<PermissionModule>
                {
                    // Student Management
                    PermissionModule.Student_View,
                    PermissionModule.Student_Create,
                    PermissionModule.Student_Edit,
                    PermissionModule.Student_Export,
                    PermissionModule.Student_Import,
                    
                    // Course Management
                    PermissionModule.Course_View,
                    
                    // Grade Management
                    PermissionModule.Grade_View,
                    PermissionModule.Grade_Export,
                    
                    // Application Management
                    PermissionModule.Application_View,
                    PermissionModule.Application_Approve,
                    PermissionModule.Application_Reject,
                },

                _ => new List<PermissionModule>
                {
                    PermissionModule.UserManagement_View,
                    PermissionModule.Student_View,
                    PermissionModule.Course_View,
                    PermissionModule.Grade_View,
                }
            };
        }
    }
}