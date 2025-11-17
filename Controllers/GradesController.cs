using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;
using StudentManagementSystem.Services;
using StudentManagementSystem.ViewModels;

namespace StudentManagementSystem.Controllers
{
    public class GradesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IGradeService _gradeService;

        public GradesController(ApplicationDbContext context, IGradeService gradeService)
        {
            _context = context;
            _gradeService = gradeService;
        }

        // GET: Grades/Manage
        public async Task<IActionResult> Manage(int? courseId)
        {
            var enrollmentsQuery = _context.CourseEnrollments
                .Include(e => e.Student)
                .Include(e => e.Course)
                .Where(e => e.IsActive);

            if (courseId.HasValue)
            {
                enrollmentsQuery = enrollmentsQuery.Where(e => e.CourseId == courseId.Value);
            }

            var enrollments = await enrollmentsQuery.ToListAsync();
            return View(enrollments);
        }

        // POST: Grades/AssignGrade
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignGrade(int enrollmentId, decimal mark)
        {
            if (mark < 0 || mark > 100)
            {
                TempData["Error"] = "Mark must be between 0 and 100";
                return RedirectToAction(nameof(Manage));
            }

            var success = await _gradeService.AssignGradeAsync(enrollmentId, mark);

            if (success)
                TempData["Success"] = "Grade assigned successfully";
            else
                TempData["Error"] = "Failed to assign grade";

            return RedirectToAction(nameof(Manage));
        }

        // GET: Grades/Transcript/5
        public async Task<IActionResult> Transcript(int studentId)
        {
            var transcript = await _gradeService.GenerateTranscriptAsync(studentId);

            if (transcript?.Student == null)
            {
                TempData["Error"] = "Student not found";
                return RedirectToAction("Index", "Students");
            }

            return View(transcript);
        }

        // GET: Grades/GPA/5
        public async Task<IActionResult> GPA(int studentId)
        {
            var gpa = await _gradeService.CalculateStudentGPAAsync(studentId);
            var student = await _context.Students.FindAsync(studentId);

            ViewBag.StudentName = student?.Name ?? "Unknown Student";
            ViewBag.StudentId = studentId;
            ViewBag.GPA = gpa;

            return View();
        }

        // GET: Grades/Scale
        public async Task<IActionResult> Scale()
        {
            var gradeScales = await _gradeService.GetActiveGradeScalesAsync();
            return View(gradeScales);
        }

        // GET: Grades/StudentEnrollments/5
        public async Task<IActionResult> StudentEnrollments(int studentId)
        {
            var enrollments = await _gradeService.GetStudentEnrollmentsAsync(studentId);
            var student = await _context.Students.FindAsync(studentId);

            ViewBag.StudentName = student?.Name;
            return View(enrollments);
        }

        // GET: Grades/CourseEnrollments/5
        public async Task<IActionResult> CourseEnrollments(int courseId)
        {
            var enrollments = await _gradeService.GetCourseEnrollmentsAsync(courseId);
            var course = await _context.Courses.FindAsync(courseId);

            ViewBag.CourseName = course?.CourseName;
            return View(enrollments);
        }

        // POST: Grades/EnrollStudent
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnrollStudent(int studentId, int courseId)
        {
            var success = await _gradeService.EnrollStudentInCourseAsync(studentId, courseId);

            if (success)
                TempData["Success"] = "Student enrolled successfully";
            else
                TempData["Error"] = "Failed to enroll student or student already enrolled";

            return RedirectToAction(nameof(Manage));
        }

        // GET: Grades/CreateTestData
        public async Task<IActionResult> CreateTestData()
        {
            try
            {
                var context = _context;

                // Get some students and courses
                var students = await context.Students.Take(5).ToListAsync();
                var courses = await context.Courses.Take(3).ToListAsync();

                if (!students.Any() || !courses.Any())
                {
                    TempData["Error"] = "Need at least 5 students and 3 courses in the database";
                    return RedirectToAction(nameof(Manage));
                }

                int enrollmentsCreated = 0;

                foreach (var student in students)
                {
                    foreach (var course in courses)
                    {
                        // Check if enrollment already exists
                        var existingEnrollment = await context.CourseEnrollments
                            .FirstOrDefaultAsync(e => e.StudentId == student.Id && e.CourseId == course.Id);

                        if (existingEnrollment == null)
                        {
                            var enrollment = new CourseEnrollment
                            {
                                StudentId = student.Id,
                                CourseId = course.Id,
                                EnrollmentDate = DateTime.Now.AddDays(-30),
                                GradeStatus = GradeStatus.InProgress,
                                IsActive = true
                            };

                            context.CourseEnrollments.Add(enrollment);
                            enrollmentsCreated++;
                        }
                    }
                }

                await context.SaveChangesAsync();
                TempData["Success"] = $"Created {enrollmentsCreated} test enrollments for grading";
                return RedirectToAction(nameof(Manage));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error creating test data: {ex.Message}";
                return RedirectToAction(nameof(Manage));
            }
        }


        // GET: Grades/Debug
        public async Task<IActionResult> Debug()
        {
            var enrollments = await _context.CourseEnrollments
                .Include(e => e.Student)
                .Include(e => e.Course)
                .ToListAsync();

            ViewBag.TotalEnrollments = enrollments.Count;
            ViewBag.TotalStudents = await _context.Students.CountAsync();
            ViewBag.TotalCourses = await _context.Courses.CountAsync();

            return View(enrollments);
        }
    }
}