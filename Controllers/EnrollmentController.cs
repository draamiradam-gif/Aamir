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
    public class EnrollmentController : BaseController
    {
        private readonly IEnrollmentService _enrollmentService;
        private readonly ILogger<EnrollmentController> _logger;
        private readonly ApplicationDbContext _context;

        public EnrollmentController(ApplicationDbContext context, IEnrollmentService enrollmentService, ILogger<EnrollmentController> logger)
        {
            _enrollmentService = enrollmentService;
            _logger = logger;
            _context = context;
        }

        // ========== MVC VIEW ACTIONS WITH DATA ========== //

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard(int semesterId = 0)
        {
            // Get current semester if not specified
            if (semesterId == 0)
            {
                var currentSemester = await _context.Semesters
                    .FirstOrDefaultAsync(s => s.IsCurrent && s.IsActive);
                semesterId = currentSemester?.Id ?? 0;
            }

            var report = await _enrollmentService.GenerateEnrollmentReportAsync(semesterId);
            return View(report);
        }

        [HttpGet]
        public IActionResult ConflictResolution()
        {
            return View();
        }

        [HttpGet]
        public IActionResult BulkEnrollment()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> WaitlistManagement()
        {
            var waitlistEntries = await _enrollmentService.GetAllWaitlistEntriesAsync();
            return View(waitlistEntries);
        }

        [HttpGet]
        public async Task<IActionResult> Available(int semesterId)
        {
            var studentId = GetCurrentStudentId();
            if (studentId == 0) return RedirectToAction("Login", "Account");

            var courses = await _enrollmentService.GetAvailableCoursesAsync(studentId, semesterId);
            var semester = await _context.Semesters.FindAsync(semesterId);

            ViewBag.Semester = semester;
            return View(courses);
        }

        [HttpGet]
        public async Task<IActionResult> MyEnrollments(int semesterId)
        {
            // Get current user with null checks
            if (User?.Identity?.IsAuthenticated != true)
            {
                return RedirectToAction("Login", "Account");
            }

            // Try multiple ways to find student with proper null handling
            var userEmail = User.Identity.Name ?? string.Empty;
            var userName = User.Identity.Name ?? string.Empty;

            // Method 1: Find by email (only if email is not empty)
            var student = !string.IsNullOrEmpty(userEmail)
                ? await _context.Students.FirstOrDefaultAsync(s => s.Email == userEmail)
                : null;

            // Method 2: If not found by email, try by name (only if name is not empty)
            if (student == null && !string.IsNullOrEmpty(userName))
            {
                student = await _context.Students.FirstOrDefaultAsync(s =>
                    s.Name != null && s.Name.Contains(userName));
            }

            // Method 3: If still not found, get first active student (for testing/demo)
            if (student == null)
            {
                student = await _context.Students.FirstOrDefaultAsync(s => s.IsActive);
                if (student != null)
                {
                    _logger.LogWarning("Using first active student for demo: {StudentId}", student.Id);
                }
            }

            if (student == null)
            {
                TempData["ErrorMessage"] = "No active student profile found. Please contact administration.";
                return RedirectToAction("Dashboard");
            }

            // If no semester specified, try to find current semester
            if (semesterId == 0)
            {
                var currentSemester = await _context.Semesters
                    .FirstOrDefaultAsync(s => s.IsCurrent && s.IsActive);
                semesterId = currentSemester?.Id ?? 0;
            }

            var enrollments = await _enrollmentService.GetStudentEnrollmentsAsync(student.Id, semesterId);
            var semester = await _context.Semesters.FindAsync(semesterId);

            ViewBag.Semester = semester;
            ViewBag.Student = student;
            return View(enrollments);
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

        // ========== DATA METHODS FOR MVC VIEWS & OTHER CONTROLLERS ========== //

        [HttpGet]
        public async Task<IActionResult> GetActiveSemesters()
        {
            var semesters = await _context.Semesters
                .Where(s => s.IsActive)
                .Select(s => new { id = s.Id, name = s.Name })
                .ToListAsync();
            return Json(semesters);
        }

        [HttpGet]
        public async Task<IActionResult> GetCoursesWithWaitlists()
        {
            var courses = await _context.Courses
                .Where(c => c.IsActive && _context.WaitlistEntries.Any(w => w.CourseId == c.Id && w.IsActive))
                .Select(c => new { id = c.Id, courseCode = c.CourseCode, courseName = c.CourseName })
                .ToListAsync();
            return Json(courses);
        }

        // KEEP THIS - Used by other parts of the app
        [HttpGet]
        public async Task<IActionResult> GetActiveStudents()
        {
            try
            {
                var students = await _context.Students
                    .Where(s => s.IsActive)
                    .Select(s => new
                    {
                        id = s.Id,
                        studentId = s.StudentId,
                        name = s.Name,
                        department = s.Department,
                        gpa = s.GPA.ToString("0.00"),
                        passedHours = s.PassedHours
                    })
                    .ToListAsync();

                return Json(students);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active students");
                return Json(new { error = "Error loading students" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetStudentsForSemester(int semesterId)
        {
            try
            {
                // Always return all active students regardless of semester
                var students = await _context.Students
                    .Where(s => s.IsActive)
                    .Select(s => new
                    {
                        id = s.Id,
                        studentId = s.StudentId,
                        name = s.Name,
                        department = s.Department,
                        gpa = s.GPA.ToString("0.00"),
                        passedHours = s.PassedHours
                    })
                    .ToListAsync();

                return Json(students);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting students for semester {SemesterId}", semesterId);
                return Json(new { error = "Error loading students" });
            }
        }

        // KEEP THIS - Used by other parts of the app
        [HttpGet]
        public async Task<IActionResult> GetActiveCourses()
        {
            try
            {
                var courses = await _context.Courses
                    .Where(c => c.IsActive)
                    .Select(c => new
                    {
                        id = c.Id,
                        courseCode = c.CourseCode,
                        courseName = c.CourseName,
                        credits = c.Credits,
                        department = c.Department,
                        maxStudents = c.MaxStudents,
                        currentEnrollment = c.CourseEnrollments.Count(ce => ce.IsActive && ce.EnrollmentStatus == EnrollmentStatus.Active),
                        prerequisites = c.PrerequisitesString
                    })
                    .ToListAsync();

                return Json(courses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active courses");
                return Json(new { error = "Error loading courses" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCoursesForSemester(int semesterId)
        {
            try
            {
                // If no semester selected, return all active courses
                if (semesterId == 0)
                {
                    var allCourses = await _context.Courses
                        .Where(c => c.IsActive)
                        .Select(c => new
                        {
                            id = c.Id,
                            courseCode = c.CourseCode,
                            courseName = c.CourseName,
                            credits = c.Credits,
                            department = c.Department,
                            maxStudents = c.MaxStudents,
                            currentEnrollment = c.CourseEnrollments.Count(ce => ce.IsActive && ce.EnrollmentStatus == EnrollmentStatus.Active),
                            prerequisites = c.PrerequisitesString
                        })
                        .ToListAsync();
                    return Json(allCourses);
                }

                // If semester selected, return courses for that semester
                var courses = await _context.Courses
                    .Where(c => c.IsActive && (semesterId == 0 || c.SemesterId == semesterId))
                    .Select(c => new
                    {
                        id = c.Id,
                        courseCode = c.CourseCode,
                        courseName = c.CourseName,
                        credits = c.Credits,
                        department = c.Department,
                        maxStudents = c.MaxStudents,
                        currentEnrollment = c.CourseEnrollments.Count(ce => ce.IsActive && ce.EnrollmentStatus == EnrollmentStatus.Active),
                        prerequisites = c.PrerequisitesString
                    })
                    .ToListAsync();

                return Json(courses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading courses for semester {SemesterId}", semesterId);
                return Json(new { error = "Error loading courses" });
            }
        }

        // ========== FORM PROCESSING ACTIONS ========== //

        [HttpPost]
        public async Task<IActionResult> ProcessBulkEnrollment([FromBody] BulkEnrollmentRequest request)
        {
            try
            {
                _logger.LogInformation("Starting bulk enrollment for {StudentCount} students and {CourseCount} courses",
                    request.StudentIds?.Count ?? 0, request.CourseIds?.Count ?? 0);

                request.RequestedBy = User.Identity?.Name ?? "Bulk Enrollment System";
                var result = await _enrollmentService.ProcessBulkEnrollmentAsync(request);

                _logger.LogInformation("Bulk enrollment completed: {SuccessCount} successful, {FailedCount} failed",
                    result.SuccessfullyEnrolled, result.FailedEnrollments);

                return Json(new
                {
                    success = true,
                    totalStudents = result.TotalStudents,
                    successfullyEnrolled = result.SuccessfullyEnrolled,
                    failedEnrollments = result.FailedEnrollments,
                    message = result.Message // Use the message from the service
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing bulk enrollment");
                return Json(new
                {
                    success = false,
                    message = "Error processing bulk enrollment: " + ex.Message
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CheckBulkEligibility([FromBody] BulkEligibilityRequest request)
        {
            try
            {
                var results = new BulkEligibilityResult();
                var ineligibleStudents = new List<IneligibleStudent>();

                foreach (var studentId in request.StudentIds)
                {
                    foreach (var courseId in request.CourseIds)
                    {
                        var eligibility = await _enrollmentService.CheckEligibilityAsync(studentId, courseId, request.SemesterId);

                        if (!eligibility.IsEligible)
                        {
                            var student = await _context.Students.FindAsync(studentId);
                            var course = await _context.Courses.FindAsync(courseId);

                            ineligibleStudents.Add(new IneligibleStudent
                            {
                                StudentId = studentId,
                                StudentName = student?.Name ?? "Unknown",
                                CourseCode = course?.CourseCode ?? "Unknown",
                                Reason = string.Join(", ", eligibility.MissingRequirements.Concat(eligibility.MissingPrerequisites))
                            });
                        }
                        else
                        {
                            results.EstimatedSuccess++;
                        }
                    }
                }

                var conflicts = await _enrollmentService.CheckConflictsAsync(request.StudentIds.First(), request.SemesterId, request.CourseIds);
                results.Conflicts = conflicts;
                results.IneligibleStudents = ineligibleStudents.DistinctBy(s => s.StudentId).ToList();

                return Json(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking bulk eligibility");
                return Json(new BulkEligibilityResult { HasErrors = true });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFromWaitlist(int entryId)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            try
            {
                var entry = await _context.WaitlistEntries
                    .Include(w => w.Student)
                    .Include(w => w.Course)
                    .Include(w => w.Semester)
                    .FirstOrDefaultAsync(w => w.Id == entryId);

                if (entry != null)
                {
                    entry.IsActive = false;
                    await _context.SaveChangesAsync();

                    await ReorderWaitlistPositions(entry.CourseId, entry.SemesterId);

                    return Json(new { success = true, message = "Student removed from waitlist." });
                }
                return Json(new { success = false, message = "Waitlist entry not found." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing waitlist entry {EntryId}", entryId);
                return Json(new { success = false, message = "Error removing from waitlist." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ProcessAllWaitlists()
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            try
            {
                var activeSemesters = await _context.Semesters
                    .Where(s => s.IsActive && s.IsRegistrationOpen)
                    .ToListAsync();

                int processedCount = 0;
                foreach (var semester in activeSemesters)
                {
                    var courses = await _context.Courses
                        .Where(c => c.SemesterId == semester.Id && c.IsActive)
                        .ToListAsync();

                    foreach (var course in courses)
                    {
                        var result = await _enrollmentService.ProcessWaitlistAsync(course.Id, semester.Id);
                        if (result.Success)
                        {
                            processedCount++;
                        }
                    }
                }

                return Json(new
                {
                    success = true,
                    message = $"Processed waitlists for {processedCount} courses across {activeSemesters.Count} semesters."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing all waitlists");
                return Json(new { success = false, message = "Error processing waitlists." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRecentActivity()
        {
            var recentEnrollments = await _context.CourseEnrollments
                .Include(ce => ce.Student)
                .Include(ce => ce.Course)
                .Include(ce => ce.Semester)
                .Where(ce => ce.CreatedDate >= DateTime.Now.AddDays(-7))
                .OrderByDescending(ce => ce.CreatedDate)
                .Take(10)
                .Select(ce => new
                {
                    StudentName = ce.Student!.Name,
                    CourseCode = ce.Course!.CourseCode,
                    SemesterName = ce.Semester!.Name,
                    EnrollmentDate = ce.CreatedDate,
                    Status = ce.EnrollmentStatus.ToString(),
                    Type = ce.EnrollmentType.ToString()
                })
                .ToListAsync();

            return PartialView("_RecentActivity", recentEnrollments);
        }

        // ========== STUDENT ENROLLMENT ACTIONS ========== //

        [HttpPost]
        public async Task<IActionResult> Enroll(int courseId, int semesterId)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

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

        [HttpPost]
        public async Task<IActionResult> QuickEnrollCourse(int courseId, int semesterId)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

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

        [HttpPost]
        public async Task<IActionResult> Withdraw(int enrollmentId)
        {
            try
            {
                var enrollment = await _context.CourseEnrollments.FindAsync(enrollmentId);
                if (enrollment != null)
                {
                    enrollment.IsActive = false;
                    enrollment.GradeStatus = GradeStatus.Withdrawn;
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Successfully withdrawn from course.";
                    return RedirectToAction("MyEnrollments", new { semesterId = enrollment.SemesterId });
                }

                TempData["ErrorMessage"] = "Enrollment not found.";
                return RedirectToAction("MyEnrollments");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error withdrawing from enrollment {EnrollmentId}", enrollmentId);
                TempData["ErrorMessage"] = "Error withdrawing from course.";
                return RedirectToAction("MyEnrollments");
            }
        }

        [HttpPost]
        public async Task<IActionResult> BulkEnrollSemester(int semesterId, string selectedStudents)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

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

                TempData["BulkEnrollmentResult"] = JsonSerializer.Serialize(result);
                return RedirectToAction("BulkEnrollmentResults");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Bulk enrollment failed: {ex.Message}";
                return RedirectToAction("Index", "Students");
            }
        }

        // ========== PRIVATE METHODS ========== //

        private int GetCurrentStudentId()
        {
            if (User?.Identity?.IsAuthenticated != true)
                return 0;

            var userEmail = User.Identity.Name ?? string.Empty;
            if (!string.IsNullOrEmpty(userEmail))
            {
                // Try multiple methods to find student with null checks
                var student = _context.Students.FirstOrDefault(s => s.Email == userEmail) ??
                             _context.Students.FirstOrDefault(s =>
                                 s.Name != null && s.Name.Contains(userEmail)) ??
                             _context.Students.FirstOrDefault(s => s.IsActive);

                return student?.Id ?? 0;
            }
            return 0;
        }

        private async Task ReorderWaitlistPositions(int courseId, int semesterId)
        {
            var activeWaitlist = await _context.WaitlistEntries
                .Where(w => w.CourseId == courseId && w.SemesterId == semesterId && w.IsActive)
                .OrderBy(w => w.Position)
                .ToListAsync();

            for (int i = 0; i < activeWaitlist.Count; i++)
            {
                activeWaitlist[i].Position = i + 1;
            }

            await _context.SaveChangesAsync();
        }
    }
}