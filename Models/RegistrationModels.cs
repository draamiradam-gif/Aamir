using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

    public enum RegistrationType
    {
        Regular,
        Early,
        Late,
        AddDrop,
        Special
    }

    public enum RuleType
    {
        Prerequisite,
        CreditLimit,
        GPARequirement,
        Departmental,
        GradeLevel,
        TimeConflict,
        MaximumCourses
    }

    public enum EnforcementLevel
    {
        Warning,
        Block,
        Recommendation
    }

    // Request/Response Models for Registration
    public class RegistrationRequest
    {
        public int StudentId { get; set; }
        public List<int> CourseIds { get; set; } = new List<int>();
        public int SemesterId { get; set; }
        public RegistrationType RegistrationType { get; set; } = RegistrationType.Regular;
        public string? RequestedBy { get; set; }
        public string? Notes { get; set; }
    }

    public class RegistrationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<CourseRegistration> Registrations { get; set; } = new List<CourseRegistration>();
        public List<RegistrationError> Errors { get; set; } = new List<RegistrationError>();
        public List<RegistrationWarning> Warnings { get; set; } = new List<RegistrationWarning>();
        public int TotalCredits { get; set; }
        public decimal CurrentGPA { get; set; }
        public int PassedHours { get; set; }
    }

    public class RegistrationError
    {
        public string CourseCode { get; set; } = string.Empty;
        public string ErrorType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public RuleType RuleType { get; set; }
    }

    public class RegistrationWarning
    {
        public string CourseCode { get; set; } = string.Empty;
        public string WarningType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
    }

    public class StudentEligibility
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public decimal GPA { get; set; }
        public int PassedHours { get; set; }
        public int CurrentCredits { get; set; }
        public int MaxAllowedCredits { get; set; }
        public bool IsEligible { get; set; }
        public List<string> EligibilityReasons { get; set; } = new List<string>();
        public List<CourseEligibility> EligibleCourses { get; set; } = new List<CourseEligibility>();
    }

    public class CourseEligibility
    {
        public int CourseId { get; set; }
        public string CourseCode { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public int Credits { get; set; }
        public bool IsEligible { get; set; }
        public List<string> Requirements { get; set; } = new List<string>();
        public List<string> MissingPrerequisites { get; set; } = new List<string>();
        public bool HasSeatsAvailable { get; set; }
        public bool HasTimeConflict { get; set; }
    }

    public class RegistrationAnalytics
    {
        public int TotalRegistrations { get; set; }
        public int SuccessfulRegistrations { get; set; }
        public int FailedRegistrations { get; set; }
        public decimal SuccessRate { get; set; }
        public Dictionary<string, int> RegistrationByDepartment { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> TopCourses { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> CommonErrors { get; set; } = new Dictionary<string, int>();
        public List<RegistrationTrend> WeeklyTrends { get; set; } = new List<RegistrationTrend>();


    }

    public class RegistrationTrend
    {
        public DateTime Week { get; set; }
        public int Registrations { get; set; }
        public int SuccessCount { get; set; }
    }

    public class RegistrationReportData
    {
        public int TotalStudents { get; set; }
        public int TotalCourses { get; set; }
        public decimal AverageCoursesPerStudent { get; set; }
        public Dictionary<string, int> StatusDistribution { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> DepartmentEnrollment { get; set; } = new Dictionary<string, int>();
        public List<CourseEnrollmentStats> CourseStats { get; set; } = new List<CourseEnrollmentStats>();

    }


    public class StudentPortalViewModel
    {
        public Student? Student { get; set; }
        public Semester? CurrentSemester { get; set; }
        public StudentEligibility Eligibility { get; set; } = new StudentEligibility();
        public List<CourseRegistration> CurrentRegistrations { get; set; } = new List<CourseRegistration>();
        public List<CourseEligibility> EligibleCourses { get; set; } = new List<CourseEligibility>();
        public List<RegistrationPeriod> ActivePeriods { get; set; } = new List<RegistrationPeriod>();
    }

    public class RegistrationManagementViewModel
    {
        public List<Semester> Semesters { get; set; } = new List<Semester>();
        public Semester? SelectedSemester { get; set; }
        public List<CourseRegistration> Registrations { get; set; } = new List<CourseRegistration>();
        public int PendingCount { get; set; }
        public int ApprovedCount { get; set; }
        public int TotalCount { get; set; }
        public string CurrentFilter { get; set; } = "all";
        public string CurrentSort { get; set; } = "date";
        public string CurrentSortOrder { get; set; } = "desc";
    }


    public class RulesManagementViewModel
    {
        public List<RegistrationRule> Rules { get; set; } = new List<RegistrationRule>();
        public List<Department> Departments { get; set; } = new List<Department>();
    }

    public class PeriodsManagementViewModel
    {
        public List<RegistrationPeriod> Periods { get; set; } = new List<RegistrationPeriod>();
        public List<Semester> Semesters { get; set; } = new List<Semester>();
    }

    
}