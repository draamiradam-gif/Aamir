using StudentManagementSystem.Models;

namespace StudentManagementSystem.ViewModels
{
    public class StudentTranscript
    {
        public Student? Student { get; set; }
        public List<CourseEnrollment> Enrollments { get; set; } = new List<CourseEnrollment>();
        public decimal GPA { get; set; }
        public DateTime GeneratedDate { get; set; }
        public int TotalCredits => Enrollments.Where(e => e.GradeStatus == GradeStatus.Completed).Sum(e => e.Course?.Credits ?? 0);
        public int CompletedCourses => Enrollments.Count(e => e.GradeStatus == GradeStatus.Completed);
    }
}