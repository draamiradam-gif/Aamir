using StudentManagementSystem.Models;
using StudentManagementSystem.Services;

namespace StudentManagementSystem.ViewModels
{
    public class GradingDashboardViewModel
    {
        public List<CourseEvaluation> RecentEvaluations { get; set; } = new();
        public List<CourseEvaluation> UpcomingEvaluations { get; set; } = new();
        public List<GradingTemplate> GradingTemplates { get; set; } = new();
    }

    public class GradeEvaluationViewModel
    {
        public CourseEvaluation Evaluation { get; set; } = null!;
        public List<StudentGradeEntry> Students { get; set; } = new();
    }

    public class StudentGradeEntry
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentNumber { get; set; } = string.Empty;
        public decimal Score { get; set; }
        public decimal MaxScore { get; set; } = 100;
        public string? Comments { get; set; }
        public bool IsAbsent { get; set; }
        public bool IsExcused { get; set; }
        public string? ExcuseReason { get; set; }
        public StudentGrade? ExistingGrade { get; set; }
    }

    public class ImportGradesViewModel
    {
        public int EvaluationId { get; set; }
        public string EvaluationName { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public IFormFile? ExcelFile { get; set; }
    }

    // ✅ ADD THESE MISSING VIEWMODELS:
    public class CourseFinalGradesViewModel
    {
        public CourseGradeSummary Summary { get; set; } = null!;
        public int CourseId { get; set; }
    }

    public class EvaluationStatisticsViewModel
    {
        public EvaluationStatistics Statistics { get; set; } = null!;
        public CourseEvaluation Evaluation { get; set; } = null!;
    }

    public class GradeDistributionViewModel
    {
        public GradeDistribution Distribution { get; set; } = null!;
        public Course Course { get; set; } = null!;
    }

    // ✅ ADD THESE SIMPLE VIEWMODELS FOR BASIC PAGES:
    public class CreateEvaluationViewModel
    {
        public int CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public List<EvaluationType> EvaluationTypes { get; set; } = new();
    }

    public class GradingTemplateViewModel
    {
        public GradingTemplate Template { get; set; } = null!;
        public List<EvaluationType> AvailableEvaluationTypes { get; set; } = new();
    }


    public class ExportReportsViewModel
    {
        public List<Course> Courses { get; set; } = new();
        public List<Student> Students { get; set; } = new();
        public List<ReportType> ReportTypes { get; set; } = new();
    }

    public class ReportType
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class ExportRequest
    {
        public int ReportTypeId { get; set; }
        public int CourseId { get; set; }
        public int StudentId { get; set; }
        public string Format { get; set; } = "Excel";
    }
}