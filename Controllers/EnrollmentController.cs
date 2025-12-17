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

        //[HttpPost]
        //public async Task<IActionResult> ProcessBulkEnrollment([FromBody] BulkEnrollmentRequest request)
        //{
        //    try
        //    {
        //        _logger.LogInformation("Processing bulk enrollment request - SemesterId: {SemesterId}, Students: {StudentCount}, Courses: {CourseCount}",
        //            request.SemesterId, request.StudentIds?.Count ?? 0, request.CourseIds?.Count ?? 0);

        //        // NULL CHECK: Ensure request is not null
        //        if (request == null)
        //        {
        //            _logger.LogWarning("Bulk enrollment request is null");
        //            return Json(new
        //            {
        //                success = false,
        //                message = "Request cannot be null"
        //            });
        //        }

        //        // NULL CHECK: Ensure lists are not null
        //        if (request.StudentIds == null || !request.StudentIds.Any())
        //        {
        //            _logger.LogWarning("No students selected in bulk enrollment");
        //            return Json(new
        //            {
        //                success = false,
        //                message = "No students selected"
        //            });
        //        }

        //        if (request.CourseIds == null || !request.CourseIds.Any())
        //        {
        //            _logger.LogWarning("No courses selected in bulk enrollment");
        //            return Json(new
        //            {
        //                success = false,
        //                message = "No courses selected"
        //            });
        //        }

        //        _logger.LogInformation("Starting bulk enrollment for {StudentCount} students and {CourseCount} courses",
        //            request.StudentIds.Count, request.CourseIds.Count);

        //        request.RequestedBy = User.Identity?.Name ?? "Bulk Enrollment System";

        //        // Process in smaller chunks to prevent timeout
        //        var chunkSize = 50;
        //        var semester = await _context.Semesters.FindAsync(request.SemesterId);
        //        var result = new BulkEnrollmentResult
        //        {
        //            TotalStudents = request.StudentIds.Count,
        //            SuccessfullyEnrolled = 0,
        //            FailedEnrollments = 0,
        //            SemesterName = semester?.Name ?? "Unknown Semester",
        //            ProcessedAt = DateTime.Now
        //        };

        //        // Process students in chunks
        //        for (int i = 0; i < request.StudentIds.Count; i += chunkSize)
        //        {
        //            var studentChunk = request.StudentIds.Skip(i).Take(chunkSize).ToList();
        //            var chunkRequest = new BulkEnrollmentRequest
        //            {
        //                SemesterId = request.SemesterId,
        //                StudentIds = studentChunk,
        //                CourseIds = request.CourseIds,
        //                Type = request.Type,
        //                RequestedBy = request.RequestedBy,
        //                SelectionType = request.SelectionType,
        //                Notes = request.Notes
        //            };

        //            _logger.LogInformation("Processing chunk {ChunkNumber} with {ChunkSize} students",
        //                i / chunkSize + 1, studentChunk.Count);

        //            var chunkResult = await _enrollmentService.ProcessBulkEnrollmentAsync(chunkRequest);
        //            result.SuccessfullyEnrolled += chunkResult.SuccessfullyEnrolled;
        //            result.FailedEnrollments += chunkResult.FailedEnrollments;
        //            result.Results.AddRange(chunkResult.Results);
        //        }

        //        result.Message = $"Bulk enrollment completed: {result.SuccessfullyEnrolled} students successfully enrolled, {result.FailedEnrollments} failed";

        //        _logger.LogInformation("Bulk enrollment completed: {SuccessCount} successful, {FailedCount} failed",
        //            result.SuccessfullyEnrolled, result.FailedEnrollments);

        //        return Json(new
        //        {
        //            success = true,
        //            totalStudents = result.TotalStudents,
        //            successfullyEnrolled = result.SuccessfullyEnrolled,
        //            failedEnrollments = result.FailedEnrollments,
        //            message = result.Message
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error processing bulk enrollment");
        //        return Json(new
        //        {
        //            success = false,
        //            message = "Error processing bulk enrollment: " + ex.Message
        //        });
        //    }
        //}
        [HttpPost]
        public async Task<IActionResult> ProcessBulkEnrollment([FromBody] BulkEnrollmentRequest request)
        {
            try
            {
                // NULL CHECK: Ensure request is not null
                if (request == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Request cannot be null"
                    });
                }

                // NULL CHECK: Ensure lists are not null
                if (request.StudentIds == null || !request.StudentIds.Any())
                {
                    return Json(new
                    {
                        success = false,
                        message = "No students selected"
                    });
                }

                if (request.CourseIds == null || !request.CourseIds.Any())
                {
                    return Json(new
                    {
                        success = false,
                        message = "No courses selected"
                    });
                }

                _logger.LogInformation("Starting bulk enrollment for {StudentCount} students and {CourseCount} courses",
                    request.StudentIds.Count, request.CourseIds.Count);

                request.RequestedBy = User.Identity?.Name ?? "Bulk Enrollment System";

                // Process in smaller chunks to prevent timeout
                var chunkSize = 50;
                var semester = await _context.Semesters.FindAsync(request.SemesterId);
                var result = new BulkEnrollmentResult
                {
                    TotalStudents = request.StudentIds.Count,
                    SuccessfullyEnrolled = 0,
                    FailedEnrollments = 0,
                    SemesterName = semester?.Name ?? "Unknown Semester",
                    ProcessedAt = DateTime.Now
                };

                // Process students in chunks
                for (int i = 0; i < request.StudentIds.Count; i += chunkSize)
                {
                    var studentChunk = request.StudentIds.Skip(i).Take(chunkSize).ToList();
                    var chunkRequest = new BulkEnrollmentRequest
                    {
                        SemesterId = request.SemesterId,
                        StudentIds = studentChunk,
                        CourseIds = request.CourseIds,
                        Type = request.Type,
                        RequestedBy = request.RequestedBy,
                        SelectionType = request.SelectionType,
                        Notes = request.Notes
                    };

                    var chunkResult = await _enrollmentService.ProcessBulkEnrollmentAsync(chunkRequest);
                    result.SuccessfullyEnrolled += chunkResult.SuccessfullyEnrolled;
                    result.FailedEnrollments += chunkResult.FailedEnrollments;
                    result.Results.AddRange(chunkResult.Results);
                }

                result.Message = $"Bulk enrollment completed: {result.SuccessfullyEnrolled} students successfully enrolled, {result.FailedEnrollments} failed";

                _logger.LogInformation("Bulk enrollment completed: {SuccessCount} successful, {FailedCount} failed",
                    result.SuccessfullyEnrolled, result.FailedEnrollments);

                return Json(new
                {
                    success = true,
                    totalStudents = result.TotalStudents,
                    successfullyEnrolled = result.SuccessfullyEnrolled,
                    failedEnrollments = result.FailedEnrollments,
                    message = result.Message
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

        //[HttpPost]
        //[Consumes("application/json")]
        //public async Task<IActionResult> CheckBulkEligibility([FromBody] BulkEligibilityRequest request)
        //{
        //    try
        //    {
        //        if (request == null)
        //        {
        //            return BadRequest(new { error = "Request cannot be null" });
        //        }

        //        if (request.StudentIds == null || !request.StudentIds.Any())
        //        {
        //            return BadRequest(new { error = "No students selected" });
        //        }

        //        if (request.CourseIds == null || !request.CourseIds.Any())
        //        {
        //            return BadRequest(new { error = "No courses selected" });
        //        }

        //        var results = new BulkEligibilityResult();
        //        var ineligibleStudents = new List<IneligibleStudent>();

        //        foreach (var studentId in request.StudentIds)
        //        {
        //            foreach (var courseId in request.CourseIds)
        //            {
        //                var eligibility = await _enrollmentService.CheckEligibilityAsync(studentId, courseId, request.SemesterId);

        //                if (!eligibility.IsEligible)
        //                {
        //                    var student = await _context.Students.FindAsync(studentId);
        //                    var course = await _context.Courses.FindAsync(courseId);

        //                    ineligibleStudents.Add(new IneligibleStudent
        //                    {
        //                        StudentId = studentId,
        //                        StudentName = student?.Name ?? "Unknown",
        //                        CourseCode = course?.CourseCode ?? "Unknown",
        //                        Reason = string.Join(", ", eligibility.MissingRequirements.Concat(eligibility.MissingPrerequisites))
        //                    });
        //                }
        //                else
        //                {
        //                    results.EstimatedSuccess++;
        //                }
        //            }
        //        }

        //        var conflicts = await _enrollmentService.CheckConflictsAsync(request.StudentIds.First(), request.SemesterId, request.CourseIds);
        //        results.Conflicts = conflicts;
        //        results.IneligibleStudents = ineligibleStudents.DistinctBy(s => s.StudentId).ToList();

        //        return Json(results);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error checking bulk eligibility");
        //        return Json(new BulkEligibilityResult { HasErrors = true });
        //    }
        //}

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

        // Drop a single course
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Drop(int enrollmentId)
        {
            var enrollment = await _context.CourseEnrollments
                .Include(e => e.Semester)
                .Include(e => e.Course)
                .FirstOrDefaultAsync(e => e.Id == enrollmentId);

            if (enrollment == null)
            {
                TempData["ErrorMessage"] = "Enrollment not found.";
                return RedirectToAction("MyEnrollments");
            }

            // Get current user ID as string
            var userId = _userManager?.GetUserId(User) ?? "";

            // Check Drop rules
            var settings = await _context.SystemSettings.FirstOrDefaultAsync();
            if (!enrollment.CanBeDropped)
            {
                TempData["ErrorMessage"] = "This course cannot be dropped at this time.";
                return RedirectToAction("MyEnrollments");
            }

            if (enrollment.Grade.HasValue && !(settings?.AllowDropWithGrade ?? false))
            {
                TempData["ErrorMessage"] = "Cannot drop a course that has a grade.";
                return RedirectToAction("MyEnrollments");
            }

            if (enrollment.Semester?.DropDeadline.HasValue == true &&
                DateTime.Now > enrollment.Semester.DropDeadline.Value &&
                !(settings?.AllowDropAfterDeadline ?? false))
            {
                TempData["ErrorMessage"] = "Drop deadline has passed.";
                return RedirectToAction("MyEnrollments");
            }

            // Drop the course
            enrollment.EnrollmentStatus = EnrollmentStatus.Withdrawn;
            enrollment.DropDate = DateTime.Now;
            enrollment.AddAuditEntry("Dropped course", userId);

            // Log system action
            _context.SystemLogs.Add(new SystemLog
            {
                UserId = userId,
                Action = $"Dropped course {enrollment.CourseId} ({enrollment.Course?.CourseCode}) in semester {enrollment.SemesterId}",
                Timestamp = DateTime.Now
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Course dropped successfully.";
            return RedirectToAction("MyEnrollments");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DropSelected(int[] enrollmentIds)
        {
            var enrollments = await _context.CourseEnrollments
                .Where(e => enrollmentIds.Contains(e.Id) && e.IsActive)
                .ToListAsync();

            foreach (var e in enrollments)
            {
                e.IsActive = false;
                e.GradeStatus = GradeStatus.Withdrawn;
                e.DropDate = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Successfully dropped {enrollments.Count} course(s).";

            // Redirect to semester of the first dropped enrollment
            return RedirectToAction("MyEnrollments", new { semesterId = enrollments.FirstOrDefault()?.SemesterId });
        }

        // Drop all courses in a semester
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DropAll(int semesterId)
        {
            var enrollmentIds = await _context.CourseEnrollments
                .Where(e => e.SemesterId == semesterId && e.IsActive)
                .Select(e => e.Id)
                .ToArrayAsync();

            if (!enrollmentIds.Any())
            {
                TempData["InfoMessage"] = "No active courses to drop.";
                return RedirectToAction("MyEnrollments", new { semesterId });
            }

            return await DropSelected(enrollmentIds);
        }



        // Check if a course can be dropped
        private bool CanDrop(CourseEnrollment enrollment)
        {
            // SuperAdmin override (hardcoded for now)
            bool isSuperAdmin = User.IsInRole("SuperAdmin");
            if (isSuperAdmin) return true;

            // Use system settings if exist
            var settings = _context.SystemSettings.FirstOrDefault() ?? new SystemSettings();

            // Restrict if grade exists
            if (!settings.AllowDropWithGrade && enrollment.Grade.HasValue) return false;

            // Restrict if past semester drop deadline
            if (!settings.AllowDropAfterDeadline && enrollment.Semester?.DropDeadline.HasValue == true)
            {
                if (DateTime.Now > enrollment.Semester.DropDeadline.Value) return false;
            }

            return true;
        }        

        private async Task<BulkEligibilityResult> ProcessEligibilityCheckAsync(BulkEligibilityRequest request)
        {
            var results = new BulkEligibilityResult();
            var ineligibleStudents = new List<IneligibleStudent>();

            // Get course details for display
            var courses = await _context.Courses
                .Where(c => request.CourseIds != null && request.CourseIds.Contains(c.Id))
                .ToListAsync();

            if (courses == null || courses.Count == 0)
            {
                results.HasErrors = true;
                return results;
            }

            // Process each student
            foreach (var studentId in request.StudentIds ?? new List<int>())
            {
                var student = await _context.Students.FindAsync(studentId);
                if (student == null) continue;

                bool allCoursesEligible = true;
                bool someCoursesEligible = false;
                List<string> studentReasons = new List<string>();

                foreach (var course in courses)
                {
                    try
                    {
                        var eligibility = await _enrollmentService.CheckEligibilityAsync(studentId, course.Id, request.SemesterId);

                        if (!eligibility.IsEligible)
                        {
                            allCoursesEligible = false;

                            // Add to ineligible students list
                            var reason = GetEligibilityReason(eligibility);
                            studentReasons.Add(reason);

                            ineligibleStudents.Add(new IneligibleStudent
                            {
                                StudentId = studentId,
                                StudentName = student.Name ?? "Unknown",
                                CourseCode = course.CourseCode ?? "Unknown",
                                Reason = reason
                            });
                        }
                        else
                        {
                            someCoursesEligible = true;
                            results.EstimatedSuccess++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error checking eligibility for student {StudentId} in course {CourseId}",
                            studentId, course.Id);

                        ineligibleStudents.Add(new IneligibleStudent
                        {
                            StudentId = studentId,
                            StudentName = student.Name ?? "Unknown",
                            CourseCode = course.CourseCode ?? "Unknown",
                            Reason = "Error checking eligibility: " + ex.Message
                        });
                    }
                }

                // Track student eligibility status
                if (allCoursesEligible)
                {
                    results.EstimatedSuccess += courses.Count;
                }
                else if (someCoursesEligible)
                {
                    results.EstimatedSuccess += courses.Count / 2; // Rough estimate
                }
            }

            // Check for conflicts (for the first student as sample)
            if (request.StudentIds != null && request.StudentIds.Any())
            {
                try
                {
                    var conflicts = await _enrollmentService.CheckConflictsAsync(
                        request.StudentIds.First(),
                        request.SemesterId,
                        request.CourseIds ?? new List<int>());
                    results.Conflicts = conflicts ?? new List<EnrollmentConflict>();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking conflicts");
                    results.Conflicts = new List<EnrollmentConflict>();
                }
            }

            results.IneligibleStudents = ineligibleStudents.DistinctBy(s => s.StudentId).ToList() ?? new List<IneligibleStudent>();

            return results;
        }

        //[HttpGet]
        //public async Task<IActionResult> CheckEligibilityResults(int semesterId)
        //{
        //    try
        //    {
        //        // Get stored request data
        //        if (TempData["EligibilityRequest"] is not string requestJson)
        //        {
        //            TempData["ErrorMessage"] = "No eligibility request data found.";
        //            return RedirectToAction("BulkEnrollment");
        //        }

        //        var request = JsonSerializer.Deserialize<BulkEligibilityRequest>(requestJson);

        //        if (request == null)
        //        {
        //            TempData["ErrorMessage"] = "Invalid request data.";
        //            return RedirectToAction("BulkEnrollment");
        //        }

        //        // Process eligibility check
        //        var results = await ProcessEligibilityCheckAsync(request);

        //        ViewBag.EligibilityRequest = request;
        //        ViewBag.StudentIds = request.StudentIds ?? new List<int>();
        //        ViewBag.CourseIds = request.CourseIds ?? new List<int>();
        //        ViewBag.SemesterId = semesterId;

        //        return View("CheckEligibility", results);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error loading eligibility results");
        //        TempData["ErrorMessage"] = "Error loading eligibility results: " + ex.Message;
        //        return RedirectToAction("BulkEnrollment");
        //    }
        //}

        private string GetEligibilityReason(EnrollmentEligibility eligibility)
        {
            if (eligibility == null) return "Unknown eligibility status";

            var reasons = new List<string>();

            if (eligibility.MissingPrerequisites != null && eligibility.MissingPrerequisites.Any())
                reasons.Add("Missing prerequisites: " + string.Join(", ", eligibility.MissingPrerequisites));

            if (eligibility.MissingRequirements != null && eligibility.MissingRequirements.Any())
                reasons.AddRange(eligibility.MissingRequirements);

            if (!eligibility.HasAvailableSeats)
                reasons.Add("Course is full");

            if (eligibility.Conflicts != null && eligibility.Conflicts.Any())
                reasons.Add("Schedule conflicts");

            return reasons.Count > 0 ? string.Join("; ", reasons) : "Not eligible";
        }

        [HttpPost]
        public IActionResult CheckBulkEligibilityDirect(int semesterId, List<int> studentIds, List<int> courseIds)
        {
            try
            {
                // Store the request data
                var request = new BulkEligibilityRequest
                {
                    SemesterId = semesterId,
                    StudentIds = studentIds,
                    CourseIds = courseIds
                };

                TempData["EligibilityRequest"] = JsonSerializer.Serialize(request);

                // Redirect to the results page
                return RedirectToAction("CheckEligibilityResults", new { semesterId = semesterId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in direct eligibility check");
                TempData["ErrorMessage"] = "Error: " + ex.Message;
                return RedirectToAction("BulkEnrollment");
            }
        }

        [HttpPost]
        public async Task<IActionResult> ProcessBulkEnrollmentDetailed([FromBody] BulkEnrollmentRequest request)
        {
            try
            {
                if (request == null)
                {
                    return Json(new { success = false, message = "Request cannot be null" });
                }

                if (request.StudentIds == null || !request.StudentIds.Any())
                {
                    return Json(new { success = false, message = "No students selected" });
                }

                if (request.CourseIds == null || !request.CourseIds.Any())
                {
                    return Json(new { success = false, message = "No courses selected" });
                }

                // Process with detailed tracking using the service
                var detailedResult = await _enrollmentService.ProcessBulkEnrollmentWithDetailsAsync(request);

                // Store in TempData for the results page
                TempData["BulkEnrollmentDetailedResult"] = JsonSerializer.Serialize(detailedResult);
                TempData["BulkEnrollmentRequest"] = JsonSerializer.Serialize(request);

                return Json(new
                {
                    success = true,
                    message = "Bulk enrollment processing completed",
                    result = detailedResult,
                    redirectUrl = Url.Action("BulkEnrollmentResultsDetailed", "Enrollment")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in detailed bulk enrollment");
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        public IActionResult CheckBulkEligibilityDetailed(int semesterId, string studentIds, string courseIds, int type = 0)
        {
            try
            {
                var request = new BulkEnrollmentRequest
                {
                    SemesterId = semesterId,
                    StudentIds = studentIds.Split(',').Select(int.Parse).ToList(),
                    CourseIds = courseIds.Split(',').Select(int.Parse).ToList(),
                    Type = (EnrollmentType)type
                };

                // Store in Session to avoid header size issues
                HttpContext.Session.SetString("BulkEnrollmentRequest", JsonSerializer.Serialize(request));

                // Redirect to the detailed results page for eligibility check
                return RedirectToAction("BulkEnrollmentResultsDetailed", new { checkOnly = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in detailed eligibility check");
                TempData["ErrorMessage"] = "Error: " + ex.Message;
                return RedirectToAction("BulkEnrollment");
            }
        }

        [HttpGet]
public async Task<IActionResult> BulkEnrollmentResultsDetailed(bool checkOnly = false)
{
    try
    {
        // Get request data from Session
        var requestJson = HttpContext.Session.GetString("BulkEnrollmentRequest");
        if (string.IsNullOrEmpty(requestJson))
        {
            // Fallback to TempData if Session not available
            if (TempData["BulkEnrollmentRequest"] is not string tempDataJson)
            {
                TempData["ErrorMessage"] = "No enrollment request data found.";
                return RedirectToAction("BulkEnrollment");
            }
            requestJson = tempDataJson;
        }

        var request = JsonSerializer.Deserialize<BulkEnrollmentRequest>(requestJson);
        if (request == null)
        {
            TempData["ErrorMessage"] = "Invalid request data.";
            return RedirectToAction("BulkEnrollment");
        }

        BulkEnrollmentDetailedResult? result;

        if (checkOnly)
        {
            // Only check eligibility
            result = await _enrollmentService.CheckBulkEligibilityWithDetailsAsync(request);
        }
        else
        {
            // Get results from processing
            if (TempData["BulkEnrollmentDetailedResult"] is string resultJson)
            {
                result = JsonSerializer.Deserialize<BulkEnrollmentDetailedResult>(resultJson);
                if (result == null)
                {
                    result = await _enrollmentService.ProcessBulkEnrollmentWithDetailsAsync(request);
                }
            }
            else
            {
                result = await _enrollmentService.ProcessBulkEnrollmentWithDetailsAsync(request);
            }
        }

        // Clear session data
        HttpContext.Session.Remove("BulkEnrollmentRequest");

        // Get additional data for display
        var semester = await _context.Semesters.FindAsync(request.SemesterId);
        var students = await _context.Students
            .Where(s => request.StudentIds.Contains(s.Id))
            .ToListAsync();
        var courses = await _context.Courses
            .Where(c => request.CourseIds.Contains(c.Id))
            .ToListAsync();

        ViewBag.Request = request;
        ViewBag.Semester = semester;
        ViewBag.Students = students;
        ViewBag.Courses = courses;
        ViewBag.CheckOnly = checkOnly;
        ViewBag.EnrollmentType = request.Type;

        return View(result);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error loading detailed results");
        TempData["ErrorMessage"] = "Error loading results: " + ex.Message;
        return RedirectToAction("BulkEnrollment");
    }
}



        [HttpPost]
        public async Task<IActionResult> ProcessBulkEnrollmentFromCheck(int semesterId, string studentIds, string courseIds, int type)
        {
            try
            {
                var request = new BulkEnrollmentRequest
                {
                    SemesterId = semesterId,
                    StudentIds = studentIds.Split(',').Select(int.Parse).ToList(),
                    CourseIds = courseIds.Split(',').Select(int.Parse).ToList(),
                    Type = (EnrollmentType)type,
                    RequestedBy = User.Identity?.Name ?? "From Eligibility Check"
                };

                // Process the enrollment using the service
                var result = await _enrollmentService.ProcessBulkEnrollmentWithDetailsAsync(request);

                TempData["BulkEnrollmentDetailedResult"] = JsonSerializer.Serialize(result);
                TempData["BulkEnrollmentRequest"] = JsonSerializer.Serialize(request);

                return RedirectToAction("BulkEnrollmentResultsDetailed");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error processing enrollment: " + ex.Message;
                return RedirectToAction("BulkEnrollment");
            }
        }

        //[HttpPost]
        //public async Task<IActionResult> CheckBulkEligibility([FromBody] BulkEligibilityRequest request)
        //{
        //    try
        //    {
        //        // Validate request
        //        if (request == null)
        //        {
        //            return BadRequest(new { error = "Request cannot be null" });
        //        }

        //        if (request.StudentIds == null || request.StudentIds.Count == 0)
        //        {
        //            return BadRequest(new { error = "No students selected" });
        //        }

        //        if (request.CourseIds == null || request.CourseIds.Count == 0)
        //        {
        //            return BadRequest(new { error = "No courses selected" });
        //        }

        //        if (request.SemesterId <= 0)
        //        {
        //            return BadRequest(new { error = "Invalid semester ID" });
        //        }

        //        // Store the request data in TempData for the results page
        //        TempData["EligibilityRequest"] = JsonSerializer.Serialize(request);

        //        // Process eligibility check
        //        var results = await ProcessEligibilityCheckAsync(request);

        //        // Return JSON with redirect URL
        //        return Json(new
        //        {
        //            redirectUrl = Url.Action("CheckEligibilityResults", "Enrollment", new
        //            {
        //                semesterId = request.SemesterId
        //            }),
        //            success = true,
        //            results = results
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error checking bulk eligibility");
        //        return Json(new
        //        {
        //            success = false,
        //            error = "Error checking eligibility: " + ex.Message
        //        });
        //    }
        //}

        //[HttpPost]
        //public async Task<IActionResult> CheckBulkEligibility([FromBody] BulkEligibilityRequest request)
        //{
        //    try
        //    {
        //        // Validate request
        //        if (request == null)
        //        {
        //            return Json(new { success = false, error = "Request cannot be null" });
        //        }

        //        if (request.StudentIds == null || request.StudentIds.Count == 0)
        //        {
        //            return Json(new { success = false, error = "No students selected" });
        //        }

        //        if (request.CourseIds == null || request.CourseIds.Count == 0)
        //        {
        //            return Json(new { success = false, error = "No courses selected" });
        //        }

        //        if (request.SemesterId <= 0)
        //        {
        //            return Json(new { success = false, error = "Invalid semester ID" });
        //        }

        //        // Generate a unique cache key for this request
        //        var cacheKey = Guid.NewGuid().ToString();

        //        // Store the FULL request in Session (not TempData to avoid header size issues)
        //        HttpContext.Session.SetString($"eligibility_{cacheKey}", JsonSerializer.Serialize(request));

        //        // Set a short expiration for the cache
        //        HttpContext.Session.SetString($"eligibility_exp_{cacheKey}",
        //            DateTime.Now.AddMinutes(10).ToString("o"));

        //        // Return minimal data - just the cache key
        //        return Json(new
        //        {
        //            success = true,
        //            cacheKey = cacheKey,
        //            semesterId = request.SemesterId,
        //            redirectUrl = Url.Action("CheckEligibilityResults", "Enrollment", new
        //            {
        //                cacheKey = cacheKey,
        //                semesterId = request.SemesterId
        //            })
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error checking bulk eligibility");
        //        return Json(new
        //        {
        //            success = false,
        //            error = "Error checking eligibility: " + ex.Message
        //        });
        //    }
        //}

        [HttpGet]
        public async Task<IActionResult> CheckEligibilityResults(string cacheKey, int semesterId)
        {
            try
            {
                // Validate cache key
                if (string.IsNullOrEmpty(cacheKey))
                {
                    TempData["ErrorMessage"] = "No cache key provided.";
                    return RedirectToAction("BulkEnrollment");
                }

                // Get stored request data from Session
                var requestJson = HttpContext.Session.GetString($"eligibility_{cacheKey}");
                if (string.IsNullOrEmpty(requestJson))
                {
                    TempData["ErrorMessage"] = "Eligibility request data has expired or not found.";
                    return RedirectToAction("BulkEnrollment");
                }

                // Check expiration
                var expJson = HttpContext.Session.GetString($"eligibility_exp_{cacheKey}");
                if (!string.IsNullOrEmpty(expJson) && DateTime.TryParse(expJson, out var expiration))
                {
                    if (DateTime.Now > expiration)
                    {
                        // Clear expired data
                        HttpContext.Session.Remove($"eligibility_{cacheKey}");
                        HttpContext.Session.Remove($"eligibility_exp_{cacheKey}");
                        TempData["ErrorMessage"] = "Eligibility request has expired. Please try again.";
                        return RedirectToAction("BulkEnrollment");
                    }
                }

                var request = JsonSerializer.Deserialize<BulkEligibilityRequest>(requestJson);

                if (request == null)
                {
                    TempData["ErrorMessage"] = "Invalid request data.";
                    return RedirectToAction("BulkEnrollment");
                }

                // Clear session data after retrieval
                HttpContext.Session.Remove($"eligibility_{cacheKey}");
                HttpContext.Session.Remove($"eligibility_exp_{cacheKey}");

                // Process eligibility check using the new detailed method
                var results = await _enrollmentService.CheckBulkEligibilityWithDetailsAsync(new BulkEnrollmentRequest
                {
                    SemesterId = request.SemesterId,
                    StudentIds = request.StudentIds,
                    CourseIds = request.CourseIds,
                    Type = EnrollmentType.Regular,
                    RequestedBy = User.Identity?.Name ?? "System"
                });

                // Return the detailed results view instead of CheckEligibility view
                ViewBag.Request = new BulkEnrollmentRequest
                {
                    SemesterId = request.SemesterId,
                    StudentIds = request.StudentIds,
                    CourseIds = request.CourseIds,
                    Type = EnrollmentType.Regular
                };
                ViewBag.Semester = await _context.Semesters.FindAsync(request.SemesterId);
                ViewBag.Students = await _context.Students
                    .Where(s => request.StudentIds.Contains(s.Id))
                    .ToListAsync();
                ViewBag.Courses = await _context.Courses
                    .Where(c => request.CourseIds.Contains(c.Id))
                    .ToListAsync();
                ViewBag.CheckOnly = true; // This is an eligibility check only
                ViewBag.EnrollmentType = EnrollmentType.Regular;

                return View("BulkEnrollmentResultsDetailed", results); // Changed to use detailed results view
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading eligibility results");
                TempData["ErrorMessage"] = "Error loading eligibility results: " + ex.Message;
                return RedirectToAction("BulkEnrollment");
            }
        }

        [HttpPost]
        public IActionResult CheckBulkEligibility([FromBody] BulkEligibilityRequest request)
        {
            try
            {
                // Validate request
                if (request == null)
                {
                    return Json(new { success = false, error = "Request cannot be null" });
                }

                if (request.StudentIds == null || request.StudentIds.Count == 0)
                {
                    return Json(new { success = false, error = "No students selected" });
                }

                if (request.CourseIds == null || request.CourseIds.Count == 0)
                {
                    return Json(new { success = false, error = "No courses selected" });
                }

                if (request.SemesterId <= 0)
                {
                    return Json(new { success = false, error = "Invalid semester ID" });
                }

                // Generate a unique cache key for this request
                var cacheKey = Guid.NewGuid().ToString();

                // Store the FULL request in Session (not TempData to avoid header size issues)
                HttpContext.Session.SetString($"eligibility_{cacheKey}", JsonSerializer.Serialize(request));

                // Set a short expiration for the cache
                HttpContext.Session.SetString($"eligibility_exp_{cacheKey}",
                    DateTime.Now.AddMinutes(10).ToString("o"));

                // Simple logging using ILogger only
                _logger.LogInformation("Eligibility check: {StudentCount} students, {CourseCount} courses, Semester {SemesterId}, CacheKey: {CacheKey}",
                    request.StudentIds?.Count ?? 0,
                    request.CourseIds?.Count ?? 0,
                    request.SemesterId,
                    cacheKey);

                // Return minimal data - just the cache key
                return Json(new
                {
                    success = true,
                    cacheKey = cacheKey,
                    semesterId = request.SemesterId,
                    redirectUrl = Url.Action("CheckEligibilityResults", "Enrollment", new
                    {
                        cacheKey = cacheKey,
                        semesterId = request.SemesterId
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking bulk eligibility");
                return Json(new
                {
                    success = false,
                    error = "Error checking eligibility: " + ex.Message
                });
            }
        }

        private void LogToFileOrLogger(BulkEligibilityRequest request)
        {
            try
            {
                // Log using ILogger (already configured)
                _logger.LogInformation("Eligibility check by {User}: {StudentCount} students, {CourseCount} courses, Semester {SemesterId}",
                    User.Identity?.Name ?? "Anonymous",
                    request.StudentIds?.Count ?? 0,
                    request.CourseIds?.Count ?? 0,
                    request.SemesterId);

                // Optional: Also log to a text file using System.IO.File
                var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] " +
                                $"User: {User.Identity?.Name ?? "Anonymous"}, " +
                                $"Action: Eligibility Check, " +
                                $"Students: {request.StudentIds?.Count ?? 0}, " +
                                $"Courses: {request.CourseIds?.Count ?? 0}, " +
                                $"Semester: {request.SemesterId}";

                // Write to a log file (optional) - fully qualify System.IO.File
                var logPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Logs", "eligibility-checks.log");
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(logPath)!);
                System.IO.File.AppendAllText(logPath, logMessage + Environment.NewLine);
            }
            catch (Exception ex)
            {
                // Don't crash if logging fails
                _logger.LogWarning(ex, "Failed to write eligibility check log");
            }
        }

        // Optional helper methods for async operations
        private async Task LogEligibilityCheckRequestAsync(BulkEligibilityRequest request)
        {
            try
            {
                var log = new SystemLog
                {
                    UserId = User.Identity?.Name ?? "Anonymous",
                    Action = $"Eligibility check requested: {request.StudentIds?.Count ?? 0} students, {request.CourseIds?.Count ?? 0} courses",
                    Details = JsonSerializer.Serialize(new
                    {
                        SemesterId = request.SemesterId,
                        StudentCount = request.StudentIds?.Count ?? 0,
                        CourseCount = request.CourseIds?.Count ?? 0
                    }),
                    Timestamp = DateTime.Now
                };

                _context.SystemLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to log eligibility check request");
                // Don't throw - this is just logging
            }
        }

        private async Task UpdateEligibilityCheckStatsAsync(int studentCount, int courseCount)
        {
            try
            {
                // Check if we already have stats
                var stats = await _context.SystemStats.FirstOrDefaultAsync();
                if (stats == null)
                {
                    // Create initial stats
                    stats = new SystemStats
                    {
                        TotalEligibilityChecks = 1,
                        TodayEligibilityChecks = 1,
                        LastEligibilityCheck = DateTime.Now,
                        LastResetDate = DateTime.Today
                    };
                    _context.SystemStats.Add(stats);
                }
                else
                {
                    // Update existing stats
                    stats.IncrementEligibilityChecks();

                    // Update average rates if needed
                    // You can implement more sophisticated calculations here
                }

                await _context.SaveChangesAsync();

                // Also log to regular logger
                _logger.LogInformation("Eligibility check: {StudentCount} students, {CourseCount} courses",
                    studentCount, courseCount);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update eligibility stats - continuing anyway");
                // Don't throw - this is just statistics tracking
            }
        }
    }


}