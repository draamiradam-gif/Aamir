using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementSystem.Models
{
    public class BulkEnrollmentDetailedResult
    {
        public int SemesterId { get; set; }
        public DateTime ProcessedAt { get; set; }
        public string RequestType { get; set; } = string.Empty;
        public string? RequestedBy { get; set; }
        public bool IsEligibilityCheck { get; set; }

        // Statistics
        public int TotalStudents { get; set; }
        public int TotalCourses { get; set; }
        public int TotalEnrollmentsAttempted { get; set; }
        public int SuccessfulEnrollments { get; set; }
        public int FailedEnrollments { get; set; }
        public int ProcessedStudents { get; set; }
        public decimal SuccessRate { get; set; }
        public decimal StudentSuccessRate { get; set; }

        // Details
        public List<StudentEnrollmentDetail> StudentDetails { get; set; } = new List<StudentEnrollmentDetail>();

        // Error handling
        public bool HasErrors { get; set; }
        public string? ErrorMessage { get; set; }

        // Summary
        public string? Summary { get; set; }
    }

    public class StudentEnrollmentDetail
    {
        public int StudentId { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public decimal GPA { get; set; }
        public int PassedHours { get; set; }
        public string GradeLevel { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";

        // Statistics
        public int TotalCoursesAttempted { get; set; }
        public int SuccessfulEnrollments { get; set; }
        public int EligibleCourses { get; set; }

        // Details
        public List<EnrollmentProcessDetail> EnrollmentDetails { get; set; } = new List<EnrollmentProcessDetail>();
    }

    public class EnrollmentProcessDetail
    {
        public int CourseId { get; set; }
        public string CourseCode { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;

        // Requirements
        public List<RequirementCheck> EligibilityChecks { get; set; } = new List<RequirementCheck>();
        public RequirementCheck? CapacityCheck { get; set; }
        public RequirementCheck? DuplicateCheck { get; set; }

        // Status
        public EnrollmentProcessStatus Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsEligible { get; set; }

        // Result
        public int? EnrollmentId { get; set; }
        public DateTime? EnrollmentDate { get; set; }

        [NotMapped]
        public string StatusDisplay
        {
            get
            {
                switch (Status)
                {
                    case EnrollmentProcessStatus.Enrolled:
                        return "<span class='badge bg-success'>Enrolled</span>";
                    case EnrollmentProcessStatus.Eligible:
                        return "<span class='badge bg-success'>Eligible</span>";
                    case EnrollmentProcessStatus.NotEligible:
                        return "<span class='badge bg-danger'>Not Eligible</span>";
                    case EnrollmentProcessStatus.Failed:
                        return "<span class='badge bg-danger'>Failed</span>";
                    case EnrollmentProcessStatus.CapacityFull:
                        return "<span class='badge bg-warning'>Capacity Full</span>";
                    case EnrollmentProcessStatus.Duplicate:
                        return "<span class='badge bg-warning'>Duplicate</span>";
                    case EnrollmentProcessStatus.Error:
                        return "<span class='badge bg-danger'>Error</span>";
                    case EnrollmentProcessStatus.Pending:
                    case EnrollmentProcessStatus.Checking:
                    default:
                        return IsEligible
                            ? "<span class='badge bg-success'>Eligible</span>"
                            : "<span class='badge bg-secondary'>Pending</span>";
                }
            }
        }
    }

    public class RequirementCheck
    {
        public string Name { get; set; } = string.Empty;
        public bool IsMet { get; set; }
        public string Details { get; set; } = string.Empty;
        public string? RequiredValue { get; set; }
        public string? ActualValue { get; set; }
    }

    public enum EnrollmentProcessStatus
    {
        Pending,
        Checking,
        Eligible,
        NotEligible,
        Enrolled,
        Failed,
        CapacityFull,
        Duplicate,
        Error
    }
}