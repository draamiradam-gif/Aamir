using System.Text;

namespace StudentManagementSystem.Models
{
    public static class BulkEnrollmentHelpers
    {
        public static string GenerateDetailedSummary(BulkEnrollmentDetailedResult result)
        {
            var summary = new StringBuilder();

            summary.AppendLine($"<strong>Bulk Enrollment Results</strong>");
            summary.AppendLine($"<div class='mt-2'>");
            summary.AppendLine($"<p><strong>Processed:</strong> {result.ProcessedAt:yyyy-MM-dd HH:mm:ss}</p>");
            summary.AppendLine($"<p><strong>Total Students:</strong> {result.TotalStudents}</p>");
            summary.AppendLine($"<p><strong>Total Courses:</strong> {result.TotalCourses}</p>");
            summary.AppendLine($"<p><strong>Total Enrollment Attempts:</strong> {result.TotalEnrollmentsAttempted}</p>");
            summary.AppendLine($"<p><strong>Successful Enrollments:</strong> {result.SuccessfulEnrollments}</p>");
            summary.AppendLine($"<p><strong>Failed Enrollments:</strong> {result.FailedEnrollments}</p>");
            summary.AppendLine($"<p><strong>Overall Success Rate:</strong> {result.SuccessRate:F1}%</p>");
            summary.AppendLine($"</div>");

            return summary.ToString();
        }

        public static string GenerateEligibilitySummary(BulkEnrollmentDetailedResult result)
        {
            var summary = new StringBuilder();

            summary.AppendLine($"<strong>Eligibility Check Results</strong>");
            summary.AppendLine($"<div class='mt-2'>");
            summary.AppendLine($"<p><strong>Checked:</strong> {result.ProcessedAt:yyyy-MM-dd HH:mm:ss}</p>");
            summary.AppendLine($"<p><strong>Total Students:</strong> {result.TotalStudents}</p>");
            summary.AppendLine($"<p><strong>Total Courses:</strong> {result.TotalCourses}</p>");
            summary.AppendLine($"<p><strong>Potential Enrollments:</strong> {result.TotalEnrollmentsAttempted}</p>");
            summary.AppendLine($"<p><strong>Eligible Enrollments:</strong> {result.SuccessfulEnrollments}</p>");
            summary.AppendLine($"<p><strong>Ineligible Enrollments:</strong> {result.FailedEnrollments}</p>");

            var eligibleStudents = result.StudentDetails.Count(s => s.Status.Contains("Eligible"));
            summary.AppendLine($"<p><strong>Eligible Students:</strong> {eligibleStudents} ({result.StudentSuccessRate:F1}%)</p>");
            summary.AppendLine($"</div>");

            return summary.ToString();
        }
    }
}