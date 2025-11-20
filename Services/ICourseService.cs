using Microsoft.AspNetCore.Mvc;
using StudentManagementSystem.Models;

namespace StudentManagementSystem.Services
{
    public interface ICourseService
    {
        // Basic Course CRUD
        Task<List<Course>> GetAllCoursesAsync();
        Task<Course?> GetCourseByIdAsync(int id);
        Task AddCourseAsync(Course course);
        Task UpdateCourseAsync(Course course);
        Task DeleteCourseAsync(int id);

        // Enrollment Management
        Task<List<CourseEnrollment>> GetCourseEnrollmentsAsync(int courseId);
        Task EnrollStudentAsync(int courseId, int studentId);
        Task UnenrollStudentAsync(int enrollmentId);
        Task UpdateGradeAsync(int enrollmentId, decimal grade, string gradeLetter);

        // Import/Export
        Task<ImportResult> ImportCoursesFromExcelAsync(Stream stream);
        Task<byte[]> ExportCoursesToExcelAsync();
        Task<byte[]> ExportCoursesToPdfAsync();
        Task<byte[]> ExportCourseDetailsToPdfAsync(int courseId);
        Task<byte[]> ExportCourseEnrollmentsToExcelAsync(int courseId);

        // Prerequisite Management
        Task AddPrerequisiteAsync(int courseId, int prerequisiteCourseId, decimal? minGrade);
        Task<List<CoursePrerequisite>> GetCoursePrerequisitesAsync(int courseId);
        Task RemovePrerequisiteAsync(int prerequisiteId);
        Task<bool> CanStudentEnrollAsync(int studentId, int courseId);
        Task<List<string>> GetMissingPrerequisitesAsync(int studentId, int courseId);

        // Additional methods (if they exist in your current interface)
        Task<Course?> GetCourseByCodeAsync(string courseCode);
        Task<List<CourseEnrollment>> GetStudentEnrollmentsAsync(int studentId);
        Task<int> GetTotalCoursesAsync();
        Task<int> GetActiveEnrollmentsCountAsync();
        Task<List<Course>> GetCoursesByDepartmentAsync(string department);

        // Grade Management
        Task UpdateGradeWithCalculationAsync(int enrollmentId, decimal grade, string gradeLetter);
        Task<List<GradeScale>> GetGradeScalesAsync();
        Task<decimal> CalculateStudentGPAAsync(int studentId);

        // Grade Scale Management
        Task AddGradeScaleAsync(GradeScale gradeScale);
        Task UpdateGradeScaleAsync(GradeScale gradeScale);
        Task DeleteGradeScaleAsync(int id);
        Task<GradeScale?> GetGradeScaleByIdAsync(int id);
        Task<List<GradeScale>> GetAllGradeScalesAsync();

        // Student Grade Reports
        Task<List<CourseEnrollment>> GetStudentGradesAsync(int studentId);
        Task<StudentTranscript> GenerateStudentTranscriptAsync(int studentId);

        Task<byte[]> ExportSelectedCoursesToExcelAsync(int[] courseIds);
        Task<(bool CanDelete, string Message)> CanDeleteCourseAsync(int courseId);

        //Task<List<Course>> GetAllCoursesAsync();
        //Task<Course?> GetCourseByIdAsync(int id);
        //Task<Course?> GetCourseByCodeAsync(string courseCode);
        //Task AddCourseAsync(Course course);
        //Task UpdateCourseAsync(Course course);
        //Task DeleteCourseAsync(int id);
        //Task<byte[]> ExportCoursesToExcelAsync();
        Task<bool> CourseExistsAsync(string courseCode);

        // Import/Export methods
        Task<ImportResult> AnalyzeExcelImportAsync(Stream stream, ImportSettings? settings = null);
        Task<ImportResult> ExecuteImportAsync(ImportResult analysisResult, ImportSettings settings);
        
        Task<byte[]> ExportSelectedCoursesAsync(int[] courseIds);
        Task<byte[]> ExportCourseToPdfAsync(int courseId);
        Task<byte[]> ExportAllCoursesToPdfAsync();
        Task<string> AddTestCourses();

        Task DeleteMultipleCoursesAsync(int[] courseIds);
        Task DeleteAllCoursesAsync();
        Task<List<Course>> GetAllCoursesWithPrerequisitesAsync();

    }
}