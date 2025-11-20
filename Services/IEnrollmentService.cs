// Services/IEnrollmentService.cs
using StudentManagementSystem.Models;

namespace StudentManagementSystem.Services
{
    public interface IEnrollmentService
    {
        Task<List<Course>> GetAvailableCoursesAsync(int studentId, int semesterId);
        Task<List<CourseEnrollment>> GetStudentEnrollmentsAsync(int studentId, int semesterId);
        Task<bool> EnrollStudentInCourseAsync(int studentId, int courseId, int semesterId);
        Task<bool> CanStudentEnrollInCourseAsync(int studentId, int courseId, int semesterId);

        // NEW METHODS FOR BULK ENROLLMENT
        Task<BulkEnrollmentResult> BulkEnrollInSemesterAsync(int semesterId, List<int> studentIds);
        Task<CourseEnrollmentResult> QuickEnrollInCourseAsync(int studentId, int courseId, int semesterId);

        Task<BulkEnrollmentResult> BulkEnrollInCoursesAsync(int semesterId, List<int> courseIds, List<int> studentIds);


    }





}