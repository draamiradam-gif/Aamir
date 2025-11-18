using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;
using StudentManagementSystem.Services;

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
    }
}