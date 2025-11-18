using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;

namespace StudentManagementSystem.Services
{
    public interface IEnrollmentService
    {
        Task<List<Course>> GetAvailableCoursesAsync(int studentId, int semesterId);
        Task<List<CourseEnrollment>> GetStudentEnrollmentsAsync(int studentId, int semesterId);
        Task<bool> EnrollStudentInCourseAsync(int studentId, int courseId, int semesterId);
        Task<bool> CanStudentEnrollInCourseAsync(int studentId, int courseId, int semesterId);
    }

    public class EnrollmentService : IEnrollmentService
    {
        private readonly ApplicationDbContext _context;

        public EnrollmentService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Get available courses for a student based on grade level
        public async Task<List<Course>> GetAvailableCoursesAsync(int studentId, int semesterId)
        {
            var student = await _context.Students
                .Include(s => s.CourseEnrollments)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null)
                return new List<Course>();

            return await _context.Courses
                .Where(c => c.SemesterId == semesterId &&
                           c.GradeLevel == student.GradeLevel &&
                           c.IsActive &&
                           c.HasAvailableSeats &&
                           !student.CourseEnrollments.Any(ce => ce.CourseId == c.Id && ce.IsActive))
                .Include(c => c.CourseDepartment)
                .Include(c => c.CourseSemester)
                .ToListAsync();
        }

        // Get student's current enrollments
        public async Task<List<CourseEnrollment>> GetStudentEnrollmentsAsync(int studentId, int semesterId)
        {
            return await _context.CourseEnrollments
                .Include(ce => ce.Course)
                .Include(ce => ce.Semester)
                .Where(ce => ce.StudentId == studentId &&
                            ce.SemesterId == semesterId &&
                            ce.IsActive)
                .OrderBy(ce => ce.Course!.CourseName)
                .ToListAsync();
        }

        // Enroll student in a course
        public async Task<bool> EnrollStudentInCourseAsync(int studentId, int courseId, int semesterId)
        {
            try
            {
                // Check if enrollment is possible
                if (!await CanStudentEnrollInCourseAsync(studentId, courseId, semesterId))
                    return false;

                var enrollment = new CourseEnrollment
                {
                    StudentId = studentId,
                    CourseId = courseId,
                    SemesterId = semesterId,
                    EnrollmentDate = DateTime.Now,
                    IsActive = true,
                    GradeStatus = GradeStatus.InProgress
                };

                _context.CourseEnrollments.Add(enrollment);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // Check if student can enroll in course
        public async Task<bool> CanStudentEnrollInCourseAsync(int studentId, int courseId, int semesterId)
        {
            var student = await _context.Students
                .Include(s => s.CourseEnrollments)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (student == null || course == null)
                return false;

            // Check grade level match
            if (student.GradeLevel != course.GradeLevel)
                return false;

            // Check if already enrolled
            if (student.CourseEnrollments.Any(ce => ce.CourseId == courseId && ce.SemesterId == semesterId && ce.IsActive))
                return false;

            // Check course capacity
            var currentEnrollment = await _context.CourseEnrollments
                .CountAsync(ce => ce.CourseId == courseId && ce.SemesterId == semesterId && ce.IsActive);

            if (currentEnrollment >= course.MaxStudents)
                return false;

            // Check student GPA requirement
            if (student.GPA < course.MinGPA)
                return false;

            // Check passed hours requirement
            if (student.PassedHours < course.MinPassedHours)
                return false;

            return true;
        }
    }
}