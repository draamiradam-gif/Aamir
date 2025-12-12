using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using StudentManagementSystem.ViewModels; 
using StudentManagementSystem.Models;

namespace StudentManagementSystem.Models
{
    public class CourseRegistration : BaseEntity
    {
        public int StudentId { get; set; }
        public int CourseId { get; set; }
        public int SemesterId { get; set; }

        [Display(Name = "Registration Date")]
        public DateTime RegistrationDate { get; set; } = DateTime.Now;

        [Display(Name = "Registration Status")]
        public RegistrationStatus Status { get; set; } = RegistrationStatus.Pending;

        [Display(Name = "Approved By")]
        [StringLength(100)]
        public string? ApprovedBy { get; set; }

        [Display(Name = "Approval Date")]
        public DateTime? ApprovalDate { get; set; }

        [Display(Name = "Registration Type")]
        public RegistrationType RegistrationType { get; set; } = RegistrationType.Regular;

        [StringLength(500)]
        [Display(Name = "Remarks")]
        public string? Remarks { get; set; }

        [Display(Name = "Priority")]
        public int Priority { get; set; } = 1;

        // Navigation properties
        [ForeignKey("StudentId")]
        public virtual Student? Student { get; set; }

        [ForeignKey("CourseId")]
        public virtual Course? Course { get; set; }

        [ForeignKey("SemesterId")]
        public virtual Semester? Semester { get; set; }
    }

    public class RegistrationRule : BaseEntity
    {
        [Required]
        [StringLength(100)]
        [Display(Name = "Rule Name")]
        public string RuleName { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Rule Type")]
        public RuleType RuleType { get; set; }

        [Display(Name = "Minimum GPA")]
        [Column(TypeName = "decimal(4,2)")]
        public decimal? MinimumGPA { get; set; }

        [Display(Name = "Minimum Passed Hours")]
        public int? MinimumPassedHours { get; set; }

        [Display(Name = "Maximum Credit Hours")]
        public int? MaximumCreditHours { get; set; }

        [Display(Name = "Minimum Credit Hours")]
        public int? MinimumCreditHours { get; set; }

        [Display(Name = "Department ID")]
        public int? DepartmentId { get; set; }

        [Display(Name = "Grade Level")]
        public int? GradeLevel { get; set; }

        //[Display(Name = "Is Active")]
        //public bool IsActive { get; set; } = true;

        [Display(Name = "Enforcement Level")]
        public EnforcementLevel EnforcementLevel { get; set; } = EnforcementLevel.Warning;

        // Navigation properties
        [ForeignKey("DepartmentId")]
        public virtual Department? Department { get; set; }

        [Display(Name = "Rule Category")]
        public RuleCategory RuleCategory { get; set; } = RuleCategory.General;

        // Add ability to link to specific course
        public int? CourseId { get; set; }

        [ForeignKey("CourseId")]
        public virtual Course? Course { get; set; }
               

        [NotMapped]
        public bool IsCourseBased => CourseId.HasValue;

        [NotMapped]
        public string? SourceCourseCode => Course?.CourseCode;
    }

    public class RegistrationPeriod : BaseEntity
    {
        [Required]
        [StringLength(100)]
        [Display(Name = "Period Name")]
        public string PeriodName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }

        [Required]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; }

        [Display(Name = "Semester ID")]
        public int SemesterId { get; set; }

        [Display(Name = "Registration Type")]
        public RegistrationType RegistrationType { get; set; }

        //[Display(Name = "Is Active")]
       // public bool IsActive { get; set; } = true;

        [Display(Name = "Max Courses Per Student")]
        public int? MaxCoursesPerStudent { get; set; }

        [Display(Name = "Max Credit Hours")]
        public int? MaxCreditHours { get; set; }

        // Navigation properties
        [ForeignKey("SemesterId")]
        public virtual Semester? Semester { get; set; }

        [NotMapped]
        public bool IsOpen => DateTime.Now >= StartDate && DateTime.Now <= EndDate && IsActive;
    }

    public enum RegistrationStatus
    {
        Pending,
        Approved,
        Rejected,
        Waitlisted,
        Dropped,
        Completed
    }

    public enum RuleCategory
    {
        CoursePrerequisite,
        CreditLimit,
        GPARequirement,
        TimeConflict,
        Departmental,
        ProgramSpecific,
        General
    }

   

}