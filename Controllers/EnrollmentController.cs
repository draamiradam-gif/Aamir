using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;
using StudentManagementSystem.Services;
using System.Text.Json;

namespace StudentManagementSystem.Controllers
{
    [Authorize]
    public class EnrollmentController : Controller
    {
        private readonly IEnrollmentService _enrollmentService;
        private readonly ApplicationDbContext _context;

        public EnrollmentController(IEnrollmentService enrollmentService, ApplicationDbContext context)
        {
            _enrollmentService = enrollmentService;
            _context = context;
        }

        // GET: Enrollment/Available/{semesterId}
        public async Task<IActionResult> Available(int semesterId)
        {
            var studentId = GetCurrentStudentId();
            if (studentId == 0) return RedirectToAction("Login", "Account");

            var courses = await _enrollmentService.GetAvailableCoursesAsync(studentId, semesterId);
            var semester = await _context.Semesters.FindAsync(semesterId);

            ViewBag.Semester = semester;
            return View(courses);
        }

        // GET: Enrollment/MyEnrollments/{semesterId}
        public async Task<IActionResult> MyEnrollments(int semesterId)
        {
            var studentId = GetCurrentStudentId();
            if (studentId == 0) return RedirectToAction("Login", "Account");

            var enrollments = await _enrollmentService.GetStudentEnrollmentsAsync(studentId, semesterId);
            var semester = await _context.Semesters.FindAsync(semesterId);

            ViewBag.Semester = semester;
            return View(enrollments);
        }

        // POST: Enrollment/Enroll
        [HttpPost]
        public async Task<IActionResult> Enroll(int courseId, int semesterId)
        {
            var studentId = GetCurrentStudentId();
            if (studentId == 0) return RedirectToAction("Login", "Account");

            var result = await _enrollmentService.EnrollStudentInCourseAsync(studentId, courseId, semesterId);

            if (result)
            {
                TempData["SuccessMessage"] = "Successfully enrolled in course!";
            }
            else
            {
                TempData["ErrorMessage"] = "Could not enroll in course. Please check requirements.";
            }

            return RedirectToAction("Available", new { semesterId });
        }

        // POST: Enrollment/Withdraw
        [HttpPost]
        public async Task<IActionResult> Withdraw(int enrollmentId)
        {
            var enrollment = await _context.CourseEnrollments.FindAsync(enrollmentId);
            if (enrollment != null)
            {
                enrollment.IsActive = false;
                enrollment.GradeStatus = GradeStatus.Withdrawn;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Successfully withdrawn from course.";

                // FIX: Use safe navigation operator
                return RedirectToAction("MyEnrollments", new { semesterId = enrollment.SemesterId });
            }

            TempData["ErrorMessage"] = "Enrollment not found.";
            return RedirectToAction("Index", "Home");
        }

        private int GetCurrentStudentId()
        {
            // FIX: Add null check for User and User.Identity
            if (User?.Identity?.IsAuthenticated != true)
                return 0;

            var userEmail = User.Identity.Name;
            if (!string.IsNullOrEmpty(userEmail))
            {
                var student = _context.Students.FirstOrDefault(s => s.Email == userEmail);
                return student?.Id ?? 0;
            }
            return 0;
        }
        ///////////
        ///
        // Add to Controllers/EnrollmentController.cs

        [HttpPost]
        public async Task<IActionResult> BulkEnrollSemester(int semesterId, string selectedStudents)
        {
            try
            {
                if (string.IsNullOrEmpty(selectedStudents))
                {
                    TempData["ErrorMessage"] = "No students selected for enrollment.";
                    return RedirectToAction("Index", "Students");
                }

                var studentIds = selectedStudents.Split(',')
                    .Select(id => int.TryParse(id, out var result) ? result : 0)
                    .Where(id => id > 0)
                    .ToList();

                if (!studentIds.Any())
                {
                    TempData["ErrorMessage"] = "Invalid student selection.";
                    return RedirectToAction("Index", "Students");
                }

                var result = await _enrollmentService.BulkEnrollInSemesterAsync(semesterId, studentIds);

                // Store result in TempData for display
                TempData["BulkEnrollmentResult"] = JsonSerializer.Serialize(result);

                return RedirectToAction("BulkEnrollmentResults");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Bulk enrollment failed: {ex.Message}";
                return RedirectToAction("Index", "Students");
            }
        }

        [HttpPost]
        public async Task<IActionResult> QuickEnrollCourse(int courseId, int semesterId)
        {
            var studentId = GetCurrentStudentId();
            if (studentId == 0) return RedirectToAction("Login", "Account");

            var result = await _enrollmentService.QuickEnrollInCourseAsync(studentId, courseId, semesterId);

            if (result.Success)
            {
                TempData["SuccessMessage"] = $"Successfully enrolled in {result.CourseCode}!";
            }
            else
            {
                TempData["ErrorMessage"] = $"Enrollment failed: {result.Message}";
            }

            return RedirectToAction("Available", new { semesterId });
        }

        [HttpGet]
        public IActionResult BulkEnrollmentResults()
        {
            if (TempData["BulkEnrollmentResult"] is not string resultJson)
            {
                TempData["ErrorMessage"] = "No enrollment results found.";
                return RedirectToAction("Index", "Students");
            }

            var result = JsonSerializer.Deserialize<BulkEnrollmentResult>(resultJson);
            return View(result);
        }

        // Add this method to get available semesters for bulk enrollment
        [HttpGet]
        public async Task<IActionResult> GetAvailableSemesters()
        {
            var semesters = await _context.Semesters
                .Where(s => s.IsActive && s.IsRegistrationOpen && s.IsRegistrationPeriod)
                .OrderByDescending(s => s.AcademicYear)
                .ThenByDescending(s => s.StartDate)
                .ToListAsync();

            return Json(semesters);
        }

        // Update Services/IEnrollmentService.cs

        public interface IEnrollmentService
        {
            Task<List<Course>> GetAvailableCoursesAsync(int studentId, int semesterId);
            Task<List<CourseEnrollment>> GetStudentEnrollmentsAsync(int studentId, int semesterId);
            Task<bool> EnrollStudentInCourseAsync(int studentId, int courseId, int semesterId);
            Task<bool> CanStudentEnrollInCourseAsync(int studentId, int courseId, int semesterId);

            // ADD THESE NEW METHODS:
            Task<BulkEnrollmentResult> BulkEnrollInSemesterAsync(int semesterId, List<int> studentIds);
            Task<CourseEnrollmentResult> QuickEnrollInCourseAsync(int studentId, int courseId, int semesterId);
        }


    }
}