using System.ComponentModel.DataAnnotations;

namespace StudentManagementSystem.Models
{
    // Request/Response Models
    public class EnrollmentRequest : BaseEntity
    {
        public int StudentId { get; set; }
        public int CourseId { get; set; }
        public int SemesterId { get; set; }
        public EnrollmentType Type { get; set; } = EnrollmentType.Regular;
        public DateTime RequestedDate { get; set; } = DateTime.Now;
        public string? RequestedBy { get; set; }
        public string? Notes { get; set; }
    }

    public class EnrollmentResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public CourseEnrollment? Enrollment { get; set; }
        public List<EnrollmentWarning> Warnings { get; set; } = new List<EnrollmentWarning>();
        public List<string> Errors { get; set; } = new List<string>();
    }

    public class EnrollmentWarning
    {
        public WarningType Type { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class EnrollmentEligibility
    {
        public bool IsEligible { get; set; }
        public List<string> MissingPrerequisites { get; set; } = new List<string>();
        public List<string> MissingRequirements { get; set; } = new List<string>();
        public List<EnrollmentConflict> Conflicts { get; set; } = new List<EnrollmentConflict>();
        public decimal? RequiredGPA { get; set; }
        public int? RequiredPassedHours { get; set; }
        public bool HasAvailableSeats { get; set; }
    }

    public class EnrollmentConflict
    {
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
    }

    public class EnrollmentValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }

    // Waitlist Models
    public class WaitlistRequest
    {
        public int StudentId { get; set; }
        public int CourseId { get; set; }
        public int SemesterId { get; set; }
        public string? RequestedBy { get; set; }
    }

    public class WaitlistResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int Position { get; set; }
        public WaitlistEntry? WaitlistEntry { get; set; }
    }

    // Bulk Operation Models
    public class BulkEnrollmentRequest
    {
        public int SemesterId { get; set; }
        public List<int> StudentIds { get; set; } = new List<int>();
        public List<int> CourseIds { get; set; } = new List<int>();
        public string? RequestedBy { get; set; }        
        public string SelectionType { get; set; } = "specific"; // all, eligible, specific
        public EnrollmentType Type { get; set; } = EnrollmentType.Regular;

    }

    public class BulkDropRequest
    {
        public int SemesterId { get; set; }
        public List<int> EnrollmentIds { get; set; } = new List<int>();
        public string Reason { get; set; } = string.Empty;
        public string? RequestedBy { get; set; }
    }

    // Report Models
    public class EnrollmentReport
    {
        public int SemesterId { get; set; }
        public string SemesterName { get; set; } = string.Empty;
        public int TotalEnrollments { get; set; }
        public int ActiveEnrollments { get; set; }
        public int WaitlistedEnrollments { get; set; }
        public List<CourseEnrollmentStats> CourseStats { get; set; } = new List<CourseEnrollmentStats>();
    }

    public class CourseEnrollmentStats
    {
        public int CourseId { get; set; }
        public string CourseCode { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public int CurrentEnrollment { get; set; }
        public int MaxCapacity { get; set; }
        public int WaitlistCount { get; set; }
    }

    public class CourseDemandAnalysis
    {
        public int CourseId { get; set; }
        public string CourseCode { get; set; } = string.Empty;
        public int CurrentEnrollment { get; set; }
        public int WaitlistCount { get; set; }
        public decimal EnrollmentRate { get; set; }
        public List<string> Recommendations { get; set; } = new List<string>();
    }

    // Additional Enums
    public enum WarningType
    {
        Waitlisted,
        PrerequisiteWarning,
        CapacityWarning,
        ConflictWarning
    }

    public class BulkEligibilityRequest
    {
        public int SemesterId { get; set; }
        public List<int> StudentIds { get; set; } = new List<int>();
        public List<int> CourseIds { get; set; } = new List<int>();
    }

    public class BulkEligibilityResult
    {
        public List<IneligibleStudent> IneligibleStudents { get; set; } = new List<IneligibleStudent>();
        public List<EnrollmentConflict> Conflicts { get; set; } = new List<EnrollmentConflict>();
        public int EstimatedSuccess { get; set; }
        public bool HasErrors { get; set; }

    }

    public class IneligibleStudent
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string CourseCode { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }
}