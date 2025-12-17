using StudentManagementSystem.Models;

namespace StudentManagementSystem.Services
{
    public interface IEnrollmentService
    {
        // Core enrollment (NEW PROFESSIONAL METHODS)
        Task<EnrollmentResult> EnrollStudentAsync(EnrollmentRequest request);
        Task<EnrollmentResult> DropStudentAsync(int enrollmentId, string? reason = null); // Add nullable
        Task<EnrollmentResult> WithdrawStudentAsync(int enrollmentId, string? reason = null); // Add nullable

        // Bulk operations
        Task<BulkEnrollmentResult> BulkEnrollStudentsAsync(BulkEnrollmentRequest request);
        Task<BulkEnrollmentResult> BulkDropStudentsAsync(BulkDropRequest request); // Changed return type

        // Validation & Queries
        Task<EnrollmentEligibility> CheckEligibilityAsync(int studentId, int courseId, int semesterId);
        Task<List<EnrollmentConflict>> CheckConflictsAsync(int studentId, int semesterId, List<int> courseIds);
        Task<List<Course>> GetRecommendedCoursesAsync(int studentId, int semesterId);

        // Waitlist management
        Task<WaitlistResult> AddToWaitlistAsync(WaitlistRequest request);
        Task<WaitlistResult> ProcessWaitlistAsync(int courseId, int semesterId);
        Task<List<WaitlistEntry>> GetWaitlistAsync(int courseId, int semesterId);

        // Reports & Analytics
        Task<EnrollmentReport> GenerateEnrollmentReportAsync(int semesterId);
        Task<CourseDemandAnalysis> AnalyzeCourseDemandAsync(int courseId, int semesterId);

        // KEEP EXISTING METHODS FOR BACKWARD COMPATIBILITY
        Task<List<Course>> GetAvailableCoursesAsync(int studentId, int semesterId);
        Task<List<CourseEnrollment>> GetStudentEnrollmentsAsync(int studentId, int semesterId);
        Task<bool> EnrollStudentInCourseAsync(int studentId, int courseId, int semesterId);
        Task<bool> CanStudentEnrollInCourseAsync(int studentId, int courseId, int semesterId);
        Task<BulkEnrollmentResult> BulkEnrollInSemesterAsync(int semesterId, List<int> studentIds);
        Task<CourseEnrollmentResult> QuickEnrollInCourseAsync(int studentId, int courseId, int semesterId);
        Task<BulkEnrollmentResult> BulkEnrollInCoursesAsync(int semesterId, List<int> courseIds, List<int> studentIds);
        Task<List<WaitlistEntry>> GetAllWaitlistEntriesAsync();
        Task<BulkEnrollmentResult> ProcessBulkEnrollmentAsync(BulkEnrollmentRequest request);
        
        Task<BulkEnrollmentDetailedResult> ProcessBulkEnrollmentWithDetailsAsync(BulkEnrollmentRequest request);
        Task<BulkEnrollmentDetailedResult> CheckBulkEligibilityWithDetailsAsync(BulkEnrollmentRequest request);


    }
}