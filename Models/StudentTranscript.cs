using System.ComponentModel.DataAnnotations;


namespace StudentManagementSystem.Models
{
    public class StudentTranscript : BaseEntity
    {
        public List<CourseEnrollment> Enrollments { get; set; } = new List<CourseEnrollment>();
        public decimal GPA { get; set; }
        public DateTime GeneratedDate { get; set; }
        public Student? Student { get; set; }
        [Display(Name = "Total Credits")]
        public int TotalCredits => Enrollments.Where(e => e.GradeStatus == GradeStatus.Completed).Sum(e => e.Course?.Credits ?? 0);

        [Display(Name = "Completed Courses")]
        public int CompletedCourses => Enrollments.Count(e => e.GradeStatus == GradeStatus.Completed);

        [Display(Name = "Total Grade Points")]
        public decimal TotalGradePoints => Enrollments.Where(e => e.GradePoints.HasValue).Sum(e => e.GradePoints!.Value * (e.Course?.Credits ?? 0));

        [Display(Name = "Academic Standing")]
        public string AcademicStanding => GPA >= 3.5m ? "Honors" : GPA >= 2.0m ? "Good Standing" : "Academic Probation";





    }
}