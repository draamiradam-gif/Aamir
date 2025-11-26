using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;
using StudentManagementSystem.Services;
using StudentManagementSystem.ViewModels;
using System.Text;

namespace StudentManagementSystem.Controllers
{
    public class GradesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IComprehensiveGradeService _comprehensiveGradeService;
        private readonly IGradeService _gradeService;
        private readonly ILogger<GradeService> _logger;

        public GradesController(ApplicationDbContext context, IComprehensiveGradeService comprehensiveGradeService, IGradeService gradeService, ILogger<GradeService> logger)
        {
            _context = context;
            _comprehensiveGradeService = comprehensiveGradeService;
            _gradeService = gradeService;
            _logger = logger;
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
            try
            {
                var enrollment = await _context.CourseEnrollments
                    .Include(e => e.Student)
                    .Include(e => e.Course)
                    .FirstOrDefaultAsync(e => e.Id == enrollmentId);

                if (enrollment == null)
                {
                    TempData["Error"] = "Enrollment not found";
                    return RedirectToAction(nameof(Manage));
                }

                if (mark < 0 || mark > 100)
                {
                    TempData["Error"] = "Mark must be between 0 and 100";
                    return RedirectToAction(nameof(Manage));
                }

                // Update the grade
                enrollment.Grade = mark;

                // Calculate grade letter and points
                enrollment.CalculateGrade(); // Make sure this method exists!

                enrollment.GradeStatus = GradeStatus.Completed;
                enrollment.LastActivityDate = DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["Success"] = $"Grade assigned to {enrollment.Student?.Name} for {enrollment.Course?.CourseName}";
                return RedirectToAction(nameof(Manage));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning grade for enrollment {EnrollmentId}", enrollmentId);
                TempData["Error"] = $"Error assigning grade: {ex.Message}";
                return RedirectToAction(nameof(Manage));
            }
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

        ///////////
        ///
        // GET: Grades/Statistics/5
        public async Task<IActionResult> Statistics(int courseId)
        {
            var course = await _context.Courses.FindAsync(courseId);
            if (course == null)
            {
                TempData["Error"] = "Course not found";
                return RedirectToAction(nameof(Manage));
            }

            var statistics = await _gradeService.GetCourseGradeStatisticsAsync(courseId);
            ViewBag.CourseName = course.CourseName;

            return View(statistics);
        }

        // GET: Grades/AcademicWarnings
        public async Task<IActionResult> AcademicWarnings()
        {
            var warnings = await _gradeService.GetAcademicWarningsAsync();
            return View(warnings);
        }

        // POST: Grades/BulkAssign
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkAssign(List<BulkGradeModel> grades)
        {
            if (grades == null || !grades.Any())
            {
                TempData["Error"] = "No grades provided";
                return RedirectToAction(nameof(Manage));
            }

            var gradeDict = grades.ToDictionary(g => g.EnrollmentId, g => g.Mark);
            var success = await _gradeService.BulkAssignGradesAsync(gradeDict);

            if (success)
                TempData["Success"] = $"Bulk grades assigned for {grades.Count} enrollments";
            else
                TempData["Error"] = "Failed to assign bulk grades";

            return RedirectToAction(nameof(Manage));
        }

        // GET: Grades/Export/5
        public async Task<IActionResult> Export(int courseId)
        {
            var enrollments = await _gradeService.GetCourseEnrollmentsAsync(courseId);
            var course = await _context.Courses.FindAsync(courseId);

            // Generate CSV
            var csv = new StringBuilder();
            csv.AppendLine("StudentID,StudentName,Grade,GradeLetter,GradePoints,Status");

            foreach (var enrollment in enrollments)
            {
                csv.AppendLine($"\"{enrollment.Student?.StudentId}\",\"{enrollment.Student?.Name}\",{enrollment.Grade},{enrollment.GradeLetter},{enrollment.GradePoints},{enrollment.GradeStatus}");
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"{course?.CourseCode}_grades_{DateTime.Now:yyyyMMdd}.csv");
        }
    }
}