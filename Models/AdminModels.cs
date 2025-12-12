using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementSystem.Models
{
    public class AdminApplication
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string ApplicantName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        [Required]
        public AdminType AppliedAdminType { get; set; }
        public AdminType? AssignedAdminType { get; set; }

        public int? UniversityId { get; set; }
        public int? FacultyId { get; set; }
        public int? DepartmentId { get; set; }
        public string? ApplicantId { get; set; }

        [Required]
        [StringLength(1000)]
        public string Justification { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Experience { get; set; }

        [StringLength(500)]
        public string? Qualifications { get; set; }

        public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;

        [Required]
        public DateTime AppliedDate { get; set; } = DateTime.Now;

        public DateTime? ReviewedDate { get; set; }

        [StringLength(100)]
        public string? ReviewedBy { get; set; }

        [StringLength(500)]
        public string? ReviewNotes { get; set; }
        public bool IsBlocked { get; set; } = false;

        [StringLength(500)]
        public string? BlockReason { get; set; }

        public DateTime? BlockedDate { get; set; }

        [StringLength(100)]
        public string? BlockedBy { get; set; }

        // Navigation properties
        [ForeignKey("UniversityId")]
        public virtual University? University { get; set; }

        [ForeignKey("FacultyId")]
        public virtual College? Faculty { get; set; }

        [ForeignKey("DepartmentId")]
        public virtual Department? Department { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;        
        


    }

    public class AdminPrivilege
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string AdminId { get; set; } = string.Empty;

        [Required]
        public AdminType AdminType { get; set; }

        // Scope limitations
        public int? UniversityScope { get; set; }
        public int? FacultyScope { get; set; }
        public int? DepartmentScope { get; set; }

        // Store as string in database
        public string PermissionsData { get; set; } = string.Empty;

        [NotMapped]
        public List<PermissionModule> Permissions
        {
            get => string.IsNullOrEmpty(PermissionsData)
                ? new List<PermissionModule>()
                : PermissionsData.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Select(p => Enum.Parse<PermissionModule>(p))
                                .ToList();
            set => PermissionsData = string.Join(',', value);
        }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? ModifiedDate { get; set; }

        [StringLength(100)]
        public string CreatedBy { get; set; } = "System";

        [StringLength(100)]
        public string? UpdatedBy { get; set; }

        // Navigation properties
        [ForeignKey("AdminId")]
        public virtual IdentityUser Admin { get; set; } = null!;

        [ForeignKey("UniversityScope")]
        public virtual University? University { get; set; }

        [ForeignKey("FacultyScope")]
        public virtual College? Faculty { get; set; }

        [ForeignKey("DepartmentScope")]
        public virtual Department? Department { get; set; }
        
        // ADD THESE if missing:
        public int? UniversityId { get; set; }
        public int? FacultyId { get; set; }
        public int? DepartmentId { get; set; }
        
        public string? ModifiedBy { get; set; } 

        
    }

    public class AdminPrivilegeTemplate
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string TemplateName { get; set; } = string.Empty;

        [Required]
        public AdminType AdminType { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        // Database column - THIS IS WHAT EF Core MAPS
        [Required]
        public string DefaultPermissionsData { get; set; } = string.Empty;

        // Computed property - THIS SHOULD BE IGNORED BY EF Core
        [NotMapped]
        public List<PermissionModule> DefaultPermissions
        {
            get => string.IsNullOrEmpty(DefaultPermissionsData)
                ? new List<PermissionModule>()
                : DefaultPermissionsData.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                      .Select(p => Enum.Parse<PermissionModule>(p))
                                      .ToList();
            set => DefaultPermissionsData = string.Join(',', value);
        }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }


}