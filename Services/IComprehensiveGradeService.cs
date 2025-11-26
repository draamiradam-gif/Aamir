using StudentManagementSystem.Models;

namespace StudentManagementSystem.Services
{
    public interface IComprehensiveGradeService
    {
        // Evaluation Management
        Task<CourseEvaluation> CreateEvaluationAsync(CourseEvaluation evaluation);
        Task<bool> UpdateEvaluationAsync(CourseEvaluation evaluation);
        Task<List<CourseEvaluation>> GetCourseEvaluationsAsync(int courseId);

        // Grade Management
        Task<bool> AssignGradeAsync(StudentGrade grade);
        Task<bool> BulkAssignGradesAsync(List<StudentGrade> grades);
        Task<bool> ImportGradesFromExcelAsync(Stream fileStream, int evaluationId);
        Task<StudentGrade?> GetStudentGradeAsync(int studentId, int evaluationId);
        Task<List<StudentGrade>> GetEvaluationGradesAsync(int evaluationId);

        // Final Grade Calculation
        Task<decimal> CalculateStudentCourseGradeAsync(int studentId, int courseId);
        Task<bool> CalculateAllFinalGradesAsync(int courseId);
        Task<CourseGradeSummary> GetCourseGradeSummaryAsync(int courseId);

        // Template Management
        Task<GradingTemplate> CreateGradingTemplateAsync(GradingTemplate template);
        Task<bool> ApplyTemplateToCourseAsync(int templateId, int courseId);
        Task<List<GradingTemplate>> GetGradingTemplatesAsync();

        // Analytics
        Task<EvaluationStatistics> GetEvaluationStatisticsAsync(int evaluationId);
        Task<GradeDistribution> GetGradeDistributionAsync(int courseId);
        Task<StudentTranscript> GenerateTranscriptAsync(int studentId);


    }
}

    