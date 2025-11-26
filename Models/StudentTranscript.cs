using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementSystem.Models
{
    public class StudentTranscript
    {
        public Student? Student { get; set; }
        public List<CourseEnrollment> Enrollments { get; set; } = new List<CourseEnrollment>();
        public decimal GPA { get; set; }
        public DateTime GeneratedDate { get; set; } = DateTime.Now;

        // Academic Summary
        [Display(Name = "Total Credits")]
        public int TotalCredits => Enrollments
            .Where(e => e.GradeStatus == GradeStatus.Completed && e.Course != null)
            .Sum(e => e.Course!.Credits);

        [Display(Name = "Completed Courses")]
        public int CompletedCourses => Enrollments.Count(e => e.GradeStatus == GradeStatus.Completed);

        [Display(Name = "Total Grade Points")]
        public decimal TotalGradePoints => Enrollments
            .Where(e => e.GradePoints.HasValue && e.Course != null)
            .Sum(e => e.GradePoints!.Value * e.Course!.Credits);

        [NotMapped]
        [Display(Name = "Academic Standing")]
        public string AcademicStanding => CalculateAcademicStanding();

        [Display(Name = "Cumulative Percentage")]
        public decimal CumulativePercentage => CalculateCumulativePercentage();

        [Display(Name = "Honors Status")]
        public string HonorsStatus => CalculateHonorsStatus();

        // NEW: Enhanced transcript features
        [Display(Name = "Major GPA")]
        public decimal MajorGPA { get; set; }

        [Display(Name = "Minor GPA")]
        public decimal? MinorGPA { get; set; }

        [Display(Name = "Transfer Credits")]
        public int TransferCredits { get; set; }

        [Display(Name = "Institution Credits")]
        public int InstitutionCredits => TotalCredits - TransferCredits;

        [Display(Name = "Academic Level")]
        public string AcademicLevel => CalculateAcademicLevel();

        [Display(Name = "Expected Graduation Date")]
        public DateTime? ExpectedGraduationDate { get; set; }

        [Display(Name = "Program")]
        public string? Program { get; set; }

        [Display(Name = "Major")]
        public string? Major { get; set; }

        [Display(Name = "Minor")]
        public string? Minor { get; set; }

        [Display(Name = "Advisor")]
        public string? Advisor { get; set; }

        // Methods
        public Dictionary<string, int> GetGradeDistribution()
        {
            return Enrollments
                .Where(e => !string.IsNullOrEmpty(e.GradeLetter))
                .GroupBy(e => e.GradeLetter!)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        public List<CourseEnrollment> GetCoursesBySemester(int semesterId)
        {
            return Enrollments
                .Where(e => e.SemesterId == semesterId)
                .OrderBy(e => e.Course?.CourseCode)
                .ToList();
        }

        public decimal CalculateSemesterGPA(int semesterId)
        {
            var semesterEnrollments = Enrollments
                .Where(e => e.SemesterId == semesterId && e.GradePoints.HasValue && e.Course != null)
                .ToList();

            if (!semesterEnrollments.Any()) return 0.0m;

            decimal totalPoints = semesterEnrollments.Sum(e => e.GradePoints!.Value * e.Course!.Credits);
            int totalCredits = semesterEnrollments.Sum(e => e.Course!.Credits);

            return totalCredits > 0 ? Math.Round(totalPoints / totalCredits, 2) : 0.0m;
        }

        public List<AcademicTerm> GetAcademicTerms()
        {
            return Enrollments
                .GroupBy(e => e.SemesterId)
                .Select(g => new AcademicTerm
                {
                    SemesterId = g.Key,
                    SemesterName = g.First().Semester?.Name ?? $"Semester {g.Key}",
                    GPA = CalculateSemesterGPA(g.Key),
                    Credits = g.Where(e => e.Course != null).Sum(e => e.Course!.Credits),
                    Courses = g.ToList()
                })
                .OrderBy(t => t.SemesterId)
                .ToList();
        }

        private string CalculateAcademicStanding()
        {
            if (GPA >= 3.8m) return "President's List";
            if (GPA >= 3.5m) return "Dean's List";
            if (GPA >= 2.0m) return "Good Standing";
            if (GPA >= 1.5m) return "Academic Warning";
            return "Academic Probation";
        }

        private decimal CalculateCumulativePercentage()
        {
            var gradedEnrollments = Enrollments.Where(e => e.Grade.HasValue).ToList();
            return gradedEnrollments.Any() ?
                Math.Round(gradedEnrollments.Average(e => e.Grade!.Value), 2) : 0.0m;
        }

        private string CalculateHonorsStatus()
        {
            return GPA switch
            {
                >= 3.9m => "Summa Cum Laude",
                >= 3.7m => "Magna Cum Laude",
                >= 3.5m => "Cum Laude",
                _ => "No Honors"
            };
        }

        private string CalculateAcademicLevel()
        {
            var totalCompletedCredits = TotalCredits + TransferCredits;
            return totalCompletedCredits switch
            {
                < 30 => "Freshman",
                < 60 => "Sophomore",
                < 90 => "Junior",
                _ => "Senior"
            };
        }

        // NEW: Transcript analysis methods
        public TranscriptAnalysis AnalyzeTranscript()
        {
            var analysis = new TranscriptAnalysis
            {
                TotalCourses = Enrollments.Count,
                CompletedCourses = CompletedCourses,
                FailedCourses = Enrollments.Count(e => e.GradeStatus == GradeStatus.Failed),
                WithdrawnCourses = Enrollments.Count(e => e.GradeStatus == GradeStatus.Withdrawn),
                AverageGrade = Enrollments.Where(e => e.Grade.HasValue).Average(e => e.Grade!.Value),
                StrongestArea = FindStrongestArea(),
                ImprovementAreas = FindImprovementAreas()
            };

            return analysis;
        }

        private string FindStrongestArea()
        {
            var areaGrades = Enrollments
                .Where(e => e.Grade.HasValue && e.Course != null)
                .GroupBy(e => e.Course!.Department)
                .Select(g => new { Department = g.Key, Average = g.Average(e => e.Grade!.Value) })
                .OrderByDescending(x => x.Average)
                .FirstOrDefault();

            return areaGrades?.Department ?? "N/A";
        }

        private List<string> FindImprovementAreas()
        {
            return Enrollments
                .Where(e => e.Grade.HasValue && e.Grade < 70 && e.Course != null)
                .Select(e => e.Course!.Department)
                .Distinct()
                .ToList();
        }
    }

    // Supporting classes
    public class AcademicTerm
    {
        public int SemesterId { get; set; }
        public string SemesterName { get; set; } = string.Empty;
        public decimal GPA { get; set; }
        public int Credits { get; set; }
        public List<CourseEnrollment> Courses { get; set; } = new List<CourseEnrollment>();
    }

    public class TranscriptAnalysis
    {
        public int TotalCourses { get; set; }
        public int CompletedCourses { get; set; }
        public int FailedCourses { get; set; }
        public int WithdrawnCourses { get; set; }
        public decimal AverageGrade { get; set; }
        public string StrongestArea { get; set; } = string.Empty;
        public List<string> ImprovementAreas { get; set; } = new List<string>();
    }
}