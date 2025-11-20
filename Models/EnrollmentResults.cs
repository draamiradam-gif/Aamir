// Models/EnrollmentResults.cs
namespace StudentManagementSystem.Models
{
    public class BulkEnrollmentResult
    {
        public int TotalStudents { get; set; }
        public int SuccessfullyEnrolled { get; set; }
        public int FailedEnrollments { get; set; }
        public List<StudentEnrollmentResult> Results { get; set; } = new List<StudentEnrollmentResult>();
        public string SemesterName { get; set; } = string.Empty;
        public DateTime ProcessedAt { get; set; } = DateTime.Now;
    }

    public class StudentEnrollmentResult
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending"; // Success, Partial, Failed
        public List<CourseEnrollmentResult> CourseResults { get; set; } = new List<CourseEnrollmentResult>();
        public string Summary => $"{CourseResults.Count(r => r.Success)}/{CourseResults.Count} courses enrolled";
    }

    public class CourseEnrollmentResult
    {
        public int CourseId { get; set; }
        public string CourseCode { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}