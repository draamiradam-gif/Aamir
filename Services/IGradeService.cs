using StudentManagementSystem.Models;

namespace StudentManagementSystem.Services
{
    public interface IGradeService
    {
        Task<bool> AssignGradeAsync(int enrollmentId, decimal mark);
        Task<bool> UpdateGradeAsync(int enrollmentId, decimal mark);
        Task<decimal> CalculateStudentGPAAsync(int studentId);
        Task<decimal> CalculateSemesterGPAAsync(int studentId, int semesterId);
        Task<StudentTranscript> GenerateTranscriptAsync(int studentId);
        Task<List<GradeScale>> GetActiveGradeScalesAsync();
        Task<bool> InitializeDefaultGradeScalesAsync();
        Task<string> CalculateGradeFromMark(decimal mark);
        Task<decimal> CalculatePointsFromMark(decimal mark);

        // New methods for CourseEnrollment
        Task<List<CourseEnrollment>> GetStudentEnrollmentsAsync(int studentId);
        Task<List<CourseEnrollment>> GetCourseEnrollmentsAsync(int courseId);
        Task<bool> EnrollStudentInCourseAsync(int studentId, int courseId);
    }
}