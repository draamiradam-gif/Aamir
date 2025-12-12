using StudentManagementSystem.Models;
using StudentManagementSystem.ViewModels;

namespace StudentManagementSystem.ViewModels
{
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

    public class RegistrationReportViewModel
    {
        public List<Semester> Semesters { get; set; } = new List<Semester>();
        public Semester? SelectedSemester { get; set; }
        public RegistrationReportData ReportData { get; set; } = new RegistrationReportData();
    }

    public class StudentRegistrationHistoryViewModel
    {
        public Student Student { get; set; } = new Student();
        public List<CourseRegistration> RegistrationHistory { get; set; } = new List<CourseRegistration>();
    }

    /////////
    ///
    
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

}