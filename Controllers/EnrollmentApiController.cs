using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;
using StudentManagementSystem.Services;

namespace StudentManagementSystem.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class EnrollmentApiController : ControllerBase
    {
        private readonly IEnrollmentService _enrollmentService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EnrollmentApiController> _logger;

        public EnrollmentApiController(IEnrollmentService enrollmentService,
                                     ApplicationDbContext context,
                                     ILogger<EnrollmentApiController> logger)
        {
            _enrollmentService = enrollmentService;
            _context = context;
            _logger = logger;
        }

        // ========== API STATUS & HEALTH ========== //

        [HttpGet]
        public IActionResult Get()
        {
            var apiInfo = new
            {
                message = "Enrollment API is running successfully!",
                version = "1.0",
                timestamp = DateTime.Now,
                availableEndpoints = new[]
                {
                    "GET    /api/EnrollmentApi",
                    "GET    /api/EnrollmentApi/test",
                    "GET    /api/EnrollmentApi/health",
                    "GET    /api/EnrollmentApi/status",
                    "POST   /api/EnrollmentApi/enroll",
                    "GET    /api/EnrollmentApi/eligibility/{studentId}/{courseId}/{semesterId}",
                    "POST   /api/EnrollmentApi/bulk-enroll",
                    "POST   /api/EnrollmentApi/waitlist",
                    "GET    /api/EnrollmentApi/waitlist/{courseId}/{semesterId}",
                    "POST   /api/EnrollmentApi/process-waitlist/{courseId}/{semesterId}",
                    "GET    /api/EnrollmentApi/recent-activity"
                }
            };

            return Ok(apiInfo);
        }

        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new
            {
                message = "Enrollment API is working perfectly!",
                status = "OK",
                timestamp = DateTime.Now
            });
        }

        [HttpGet("health")]
        public async Task<IActionResult> HealthCheck()
        {
            try
            {
                var canConnect = await _context.Database.CanConnectAsync();
                var totalEnrollments = await _context.CourseEnrollments.CountAsync();
                var activeEnrollments = await _context.CourseEnrollments.CountAsync(e => e.IsActive);
                var waitlistCount = await _context.WaitlistEntries.CountAsync(w => w.IsActive);

                return Ok(new
                {
                    status = "Healthy",
                    database = canConnect ? "Connected" : "Disconnected",
                    statistics = new
                    {
                        totalEnrollments,
                        activeEnrollments,
                        waitlistCount
                    },
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = "Unhealthy",
                    error = ex.Message,
                    timestamp = DateTime.Now
                });
            }
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetStatus()
        {
            try
            {
                var totalEnrollments = await _context.CourseEnrollments.CountAsync();
                var activeEnrollments = await _context.CourseEnrollments.CountAsync(e => e.IsActive);
                var waitlistCount = await _context.WaitlistEntries.CountAsync(w => w.IsActive);
                var totalStudents = await _context.Students.CountAsync(s => s.IsActive);
                var totalCourses = await _context.Courses.CountAsync(c => c.IsActive);

                return Ok(new
                {
                    status = "Operational",
                    summary = new
                    {
                        totalStudents,
                        totalCourses,
                        totalEnrollments,
                        activeEnrollments,
                        waitlistCount
                    },
                    serverTime = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting enrollment status");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // ========== CORE ENROLLMENT API ========== //

        [HttpPost("enroll")]
        public async Task<IActionResult> EnrollStudent([FromBody] EnrollmentRequest request)
        {
            try
            {
                // Add null check for request
                if (request == null)
                {
                    return BadRequest(new { error = "Request cannot be null" });
                }

                request.RequestedBy = User.Identity?.Name ?? "API User";
                var result = await _enrollmentService.EnrollStudentAsync(request);

                if (result.Success)
                {
                    return Ok(new
                    {
                        success = true,
                        message = result.Message,
                        enrollment = result.Enrollment
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        errors = result.Errors,
                        warnings = result.Warnings
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in enrollment API");
                return StatusCode(500, new { error = "An internal error occurred." });
            }
        }

        [HttpGet("eligibility/{studentId}/{courseId}/{semesterId}")]
        public async Task<IActionResult> CheckEligibility(int studentId, int courseId, int semesterId)
        {
            try
            {
                var eligibility = await _enrollmentService.CheckEligibilityAsync(studentId, courseId, semesterId);
                return Ok(eligibility);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking eligibility");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("bulk-enroll")]
        public async Task<IActionResult> BulkEnroll([FromBody] BulkEnrollmentRequest request)
        {
            try
            {
                // Add null check for request
                if (request == null)
                {
                    return BadRequest(new { error = "Request cannot be null" });
                }

                request.RequestedBy = User.Identity?.Name ?? "API User";
                var result = await _enrollmentService.ProcessBulkEnrollmentAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk enrollment API");
                return StatusCode(500, new { error = "An internal error occurred." });
            }
        }

        // ========== WAITLIST API ========== //

        [HttpPost("waitlist")]
        public async Task<IActionResult> AddToWaitlist([FromBody] WaitlistRequest request)
        {
            try
            {
                request.RequestedBy = User.Identity?.Name ?? "API User";
                var result = await _enrollmentService.AddToWaitlistAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding to waitlist via API");
                return StatusCode(500, new { error = "An internal error occurred." });
            }
        }

        [HttpGet("waitlist/{courseId}/{semesterId}")]
        public async Task<IActionResult> GetWaitlist(int courseId, int semesterId)
        {
            try
            {
                var waitlist = await _enrollmentService.GetWaitlistAsync(courseId, semesterId);
                return Ok(waitlist);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting waitlist");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("process-waitlist/{courseId}/{semesterId}")]
        public async Task<IActionResult> ProcessWaitlist(int courseId, int semesterId)
        {
            try
            {
                var result = await _enrollmentService.ProcessWaitlistAsync(courseId, semesterId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing waitlist");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("process-all-waitlists")]
        public async Task<IActionResult> ProcessAllWaitlists()
        {
            try
            {
                var activeSemesters = await _context.Semesters
                    .Where(s => s.IsActive && s.IsRegistrationOpen)
                    .ToListAsync();

                int processedCount = 0;
                var results = new List<object>();

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
                            results.Add(new
                            {
                                course = course.CourseCode,
                                semester = semester.Name,
                                success = true
                            });
                        }
                    }
                }

                return Ok(new
                {
                    success = true,
                    message = $"Processed waitlists for {processedCount} courses across {activeSemesters.Count} semesters.",
                    details = results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing all waitlists");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error processing waitlists."
                });
            }
        }

        // ========== DATA API ENDPOINTS ========== //

        [HttpGet("active-students")]
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
                        department = s.Department ?? "No Department",
                        gpa = s.GPA.ToString("0.00"),
                        passedHours = s.PassedHours
                    })
                    .ToListAsync();

                return Ok(students);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading active students in API");
                return StatusCode(500, new { error = "Failed to load students", details = ex.Message });
            }
        }

        [HttpGet("students-for-semester/{semesterId}")]
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
                        department = s.Department ?? "No Department",
                        gpa = s.GPA.ToString("0.00"),
                        passedHours = s.PassedHours
                    })
                    .ToListAsync();

                return Ok(students);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading students for semester {SemesterId} in API", semesterId);
                return StatusCode(500, new { error = "Failed to load students", details = ex.Message });
            }
        }

        [HttpGet("active-courses")]
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

                return Ok(courses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading active courses");
                return StatusCode(500, new { error = "Failed to load courses" });
            }
        }

        [HttpGet("courses-for-semester/{semesterId}")]
        public async Task<IActionResult> GetCoursesForSemester(int semesterId)
        {
            try
            {
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

                return Ok(courses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading courses for semester {SemesterId}", semesterId);
                return StatusCode(500, new { error = "Failed to load courses" });
            }
        }

        [HttpGet("active-semesters")]
        public async Task<IActionResult> GetActiveSemesters()
        {
            try
            {
                var semesters = await _context.Semesters
                    .Where(s => s.IsActive)
                    .Select(s => new { id = s.Id, name = s.Name })
                    .ToListAsync();
                return Ok(semesters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading active semesters");
                return StatusCode(500, new { error = "Failed to load semesters" });
            }
        }

        [HttpGet("recent-activity")]
        public async Task<IActionResult> GetRecentActivity()
        {
            try
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

                return Ok(recentEnrollments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent activity");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /*
         [HttpGet("recent-activity")]
        public async Task<IActionResult> GetRecentActivity()
        {
            try
            {
                var recentEnrollments = await _context.CourseEnrollments
                    .Include(ce => ce.Student)
                    .Include(ce => ce.Course)
                    .OrderByDescending(ce => ce.EnrollmentDate)
                    .Take(5)
                    .ToListAsync();

                var html = "";
                if (recentEnrollments.Any())
                {
                    html = "<div class='list-group'>";
                    foreach (var enrollment in recentEnrollments)
                    {
                        html += $@"
                            <div class='list-group-item'>
                                <div class='d-flex w-100 justify-content-between'>
                                    <h6 class='mb-1'>{enrollment.Student?.Name}</h6>
                                    <small>{enrollment.EnrollmentDate:MMM dd}</small>
                                </div>
                                <p class='mb-1'>{enrollment.Course?.CourseCode} - {enrollment.Course?.CourseName}</p>
                                <small class='text-muted'>Status: {enrollment.EnrollmentStatus}</small>
                            </div>";
                    }
                    html += "</div>";
                }
                else
                {
                    html = "<div class='text-center text-muted py-3'><p>No recent enrollment activity</p></div>";
                }

                return Content(html, "text/html");
            }
            catch (Exception ex)
            {
                return Content("<div class='text-center text-danger py-3'><p>Error loading activity</p></div>", "text/html");
            }
        } 
         */
    }
}