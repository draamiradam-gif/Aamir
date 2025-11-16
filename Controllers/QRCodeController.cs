using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;
using StudentManagementSystem.Services;
using StudentManagementSystem.ViewModels;

namespace StudentManagementSystem.Controllers
{
    [Authorize]
    public class QRCodeController : Controller
    {
        private readonly IQRCodeService _qrCodeService;
        private readonly ICourseService _courseService;
        private readonly ILogger<QRCodeController> _logger;
        private readonly ApplicationDbContext _context;

        public QRCodeController(IQRCodeService qrCodeService, ICourseService courseService,
            ILogger<QRCodeController> logger, ApplicationDbContext context)
        {
            _qrCodeService = qrCodeService;
            _courseService = courseService;
            _logger = logger;
            _context = context;

            ExcelPackage.License.SetNonCommercialOrganization("Student Management System");
        }

        // ========== BASIC QR CODE ACTIONS ==========

        public async Task<IActionResult> Index()
        {
            var activeSessions = await _qrCodeService.GetActiveSessionsAsync();
            return View(activeSessions);
        }

        public async Task<IActionResult> Create()
        {
            var courses = await _courseService.GetAllCoursesAsync();
            ViewBag.Courses = new SelectList(courses, "Id", "CourseCodeName");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(QRCodeSession session)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    session.CreatedBy = User.Identity?.Name ?? "System";
                    session.CreatedAt = DateTime.Now;
                    session.IsActive = true;
                    session.Token = Guid.NewGuid().ToString();

                    var createdSession = await _qrCodeService.CreateSessionAsync(session);

                    TempData["Success"] = $"QR session '{session.SessionTitle}' created successfully!";
                    return RedirectToAction("SessionCreated", new { id = createdSession.Id });
                }

                var courses = await _courseService.GetAllCoursesAsync();
                ViewBag.Courses = new SelectList(courses, "Id", "CourseCodeName", session.CourseId);
                return View(session);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating QR session");
                var errorMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    errorMessage += $" | Inner: {ex.InnerException.Message}";
                }

                TempData["Error"] = $"Error creating session: {errorMessage}";

                var courses = await _courseService.GetAllCoursesAsync();
                ViewBag.Courses = new SelectList(courses, "Id", "CourseCodeName", session.CourseId);
                return View(session);
            }
        }

        
        public async Task<IActionResult> ActiveSessions()
        {
            var sessions = await _qrCodeService.GetActiveSessionsAsync();
            return View(sessions);
        }

        [AllowAnonymous]
        [HttpGet] // ✅ EXPLICITLY MARK AS GET
        public IActionResult Scan(string token)
        {
            Console.WriteLine($"DEBUG: Scan action called with token: '{token}'");
            Console.WriteLine($"DEBUG: Full URL: {Request.GetDisplayUrl()}");

            // ✅ SET THE TOKEN IN VIEWBAG
            ViewBag.Token = token;

            // ✅ DEBUG: Check what's actually being received
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("DEBUG: Token is NULL or EMPTY!");
                Console.WriteLine($"DEBUG: Query string: {Request.QueryString}");
            }

            return View();
        }
        public async Task<IActionResult> Scanner()
        {
            var activeSessions = await _qrCodeService.GetActiveSessionsAsync();

            if (activeSessions.Count == 1)
            {
                // Auto-redirect if only one active session
                return RedirectToAction("Scan", new { token = activeSessions[0].Token });
            }
            else if (activeSessions.Count > 1)
            {
                // Show session selection
                return View(activeSessions);
            }
            else
            {
                TempData["Error"] = "No active QR sessions found. Please create a session first.";
                return RedirectToAction("Create");
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<JsonResult> Scan(string token, int studentId)
        {
            try
            {
                Console.WriteLine($"=== SCAN DEBUG START ===");
                Console.WriteLine($"Token: {token}");
                Console.WriteLine($"StudentId: {studentId}");
                Console.WriteLine($"Current Time: {DateTime.Now}");

                // ✅ USE DIRECT DATABASE QUERY (same as validation logic)
                var session = await _context.QRCodeSessions
                    .FirstOrDefaultAsync(s => s.Token == token && s.IsActive);

                Console.WriteLine($"Direct DB Query - Found: {session != null}");
                Console.WriteLine($"Session ID: {session?.Id}");
                Console.WriteLine($"Session IsActive: {session?.IsActive}");

                if (session == null)
                {
                    return Json(new { success = false, message = "Session not found or inactive" });
                }

                // ✅ CALCULATE EXPIRATION (same as validation logic)
                var expiresAt = session.CreatedAt.AddMinutes(session.DurationMinutes);
                Console.WriteLine($"Session CreatedAt: {session.CreatedAt}");
                Console.WriteLine($"Session Duration: {session.DurationMinutes} minutes");
                Console.WriteLine($"Calculated ExpiresAt: {expiresAt}");
                Console.WriteLine($"Is Expired: {expiresAt < DateTime.Now}");

                if (expiresAt < DateTime.Now)
                {
                    // Auto-deactivate expired session
                    session.IsActive = false;
                    await _context.SaveChangesAsync();
                    return Json(new { success = false, message = "Session has expired" });
                }

                // ✅ Check if student exists
                var student = await _context.Students.FindAsync(studentId);
                if (student == null)
                {
                    return Json(new { success = false, message = "Student ID not found" });
                }

                // ✅ Create attendance
                var attendance = new QRAttendance
                {
                    QRCodeSessionId = session.Id,
                    StudentId = studentId,
                    DeviceInfo = Request.Headers["User-Agent"],
                    IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    ScannedAt = DateTime.Now
                };

                _context.QRAttendances.Add(attendance);
                await _context.SaveChangesAsync();

                Console.WriteLine($"=== ATTENDANCE SAVED SUCCESSFULLY ===");
                return Json(new { success = true, message = "Attendance marked successfully!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== SCAN ERROR: {ex.Message} ===");
                Console.WriteLine($"Stack: {ex.StackTrace}");
                return Json(new { success = false, message = ex.Message });
            }
        }
        public async Task<IActionResult> Display(int id)
        {
            var session = await _qrCodeService.GetSessionByIdAsync(id);
            if (session == null)
            {
                return NotFound();
            }

            // ✅ FIX: Get current token and set it in ViewBag
            ViewBag.CurrentToken = await _qrCodeService.GetCurrentTokenAsync(id);

            return View(session);
        }

        public async Task<IActionResult> SessionCreated(int id)
        {
            var session = await _qrCodeService.GetSessionByIdAsync(id);
            if (session == null)
            {
                return NotFound();
            }

            // ✅ FIX: Get current token and set it in ViewBag
            ViewBag.CurrentToken = await _qrCodeService.GetCurrentTokenAsync(id);

            return View(session);
        }

        public IActionResult TestQR()
        {
            return View();
        }

        public async Task<IActionResult> Dashboard(int id)
        {
            var session = await _qrCodeService.GetSessionByIdAsync(id);
            if (session == null)
            {
                return NotFound();
            }

            var attendances = await _qrCodeService.GetSessionAttendancesAsync(id);
            var viewModel = new QRDashboardViewModel
            {
                Session = session,
                Attendances = attendances,
                TotalStudents = attendances.Count,
                UniqueScans = attendances.GroupBy(a => a.StudentId).Count()
            };

            return View(viewModel);
        }

        // ========== EXPORT METHODS ==========

        [HttpPost]
        public async Task<IActionResult> ExportSessionExcel(int sessionId)
        {
            try
            {
                var fileBytes = await _qrCodeService.ExportAttendanceToExcelAsync(sessionId);
                var session = await _qrCodeService.GetSessionByIdAsync(sessionId);

                var fileName = $"Attendance_{session?.SessionTitle?.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                return File(fileBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting session to Excel");
                TempData["Error"] = $"Export failed: {ex.Message}";
                return RedirectToAction("Dashboard", new { id = sessionId });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ExportSessionPdf(int sessionId)
        {
            try
            {
                var fileBytes = await _qrCodeService.ExportAttendanceToPdfAsync(sessionId);
                var session = await _qrCodeService.GetSessionByIdAsync(sessionId);

                var fileName = $"Attendance_{session?.SessionTitle?.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                return File(fileBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting session to PDF");
                TempData["Error"] = $"PDF export failed: {ex.Message}";
                return RedirectToAction("Dashboard", new { id = sessionId });
            }
        }

        public async Task<IActionResult> ExportAllSessions(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var fileBytes = await _qrCodeService.ExportAllSessionsToExcelAsync(startDate, endDate);

                var dateRange = "";
                if (startDate.HasValue || endDate.HasValue)
                {
                    dateRange = $"{startDate?.ToString("yyyyMMdd") ?? "All"}_to_{endDate?.ToString("yyyyMMdd") ?? "Now"}";
                }

                var fileName = $"All_Sessions_Report_{dateRange}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                return File(fileBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting all sessions");
                TempData["Error"] = $"Export failed: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // ========== IMPORT METHODS ==========

        public IActionResult ImportAttendance()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportAttendance(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    TempData["Error"] = "Please select a file.";
                    return View();
                }

                var extension = Path.GetExtension(file.FileName).ToLower();
                if (extension != ".xlsx" && extension != ".xls")
                {
                    TempData["Error"] = "Please select an Excel file (.xlsx or .xls).";
                    return View();
                }

                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                stream.Position = 0;

                var result = await _qrCodeService.ImportAttendanceFromExcelAsync(stream);

                if (result.Success)
                {
                    TempData["Success"] = result.Message;
                    if (result.Errors != null && result.Errors.Any())
                    {
                        TempData["ImportWarnings"] = string.Join("; ", result.Errors.Take(5));
                    }
                }
                else
                {
                    TempData["Error"] = result.Message;
                    if (result.Errors != null && result.Errors.Any())
                    {
                        TempData["ImportErrors"] = string.Join("; ", result.Errors.Take(5));
                    }
                }

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing attendance");
                TempData["Error"] = $"Import failed: {ex.Message}";
                return View();
            }
        }

        public IActionResult DownloadImportTemplate()
        {
            try
            {
                var fileBytes = _qrCodeService.GenerateAttendanceImportTemplate();
                return File(fileBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "Attendance_Import_Template.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating import template");
                TempData["Error"] = $"Error generating template: {ex.Message}";
                return RedirectToAction("ImportAttendance");
            }
        }

        public IActionResult BulkExport()
        {
            return View();
        }

        // ========== SESSION MANAGEMENT ==========

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EndSession(int id)
        {
            try
            {
                var session = await _qrCodeService.GetSessionByIdAsync(id);
                if (session != null)
                {
                    session.IsActive = false;
                    _context.QRCodeSessions.Update(session);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Session ended successfully!";
                }
                return RedirectToAction("ActiveSessions");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ending QR session");
                TempData["Error"] = $"Error ending session: {ex.Message}";
                return RedirectToAction("ActiveSessions");
            }
        }
        /*
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSession(int id)
        {
            try
            {
                var session = await _qrCodeService.GetSessionByIdAsync(id);
                if (session != null)
                {
                    _context.QRCodeSessions.Remove(session);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Session deleted successfully!";
                }
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting QR session");
                TempData["Error"] = $"Error deleting session: {ex.Message}";
                return RedirectToAction("ActiveSessions");
            }
        }
        */
        // ========== API METHODS ==========

        public async Task<IActionResult> DashboardData(int id)
        {
            var session = await _qrCodeService.GetSessionByIdAsync(id);
            if (session == null)
            {
                return NotFound();
            }

            var attendances = await _qrCodeService.GetSessionAttendancesAsync(id);
            var totalStudents = attendances.Count;
            var uniqueScans = attendances.GroupBy(a => a.StudentId).Count();
            var attendancePercentage = totalStudents > 0 ? (uniqueScans * 100.0) / totalStudents : 0;

            var data = new
            {
                totalStudents,
                uniqueScans,
                attendancePercentage,
                attendances = attendances.OrderByDescending(a => a.ScannedAt).Select(a => new
                {
                    studentId = a.Student?.StudentId,
                    studentName = a.Student?.Name,
                    scannedAt = a.ScannedAt,
                    deviceInfo = a.DeviceInfo
                })
            };

            return Json(data);
        }

        // ADD THIS METHOD TO YOUR CONTROLLER:
        [AllowAnonymous]
        public async Task<JsonResult> ValidateSession(string token)
        {
            try
            {
                _logger.LogInformation("Validating session with token: {Token}", token);

                var isValid = await _qrCodeService.ValidateSessionAsync(token);

                _logger.LogInformation("Session validation result: {IsValid} for token: {Token}", isValid, token);

                return Json(new { success = true, isValid = isValid });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating session with token: {Token}", token);
                return Json(new { success = false, error = ex.Message });
            }
        }

        // ADD THIS METHOD TO YOUR QRCodeController:
        public async Task<IActionResult> Reports()
        {
            try
            {
                var sessions = await _qrCodeService.GetActiveSessionsAsync();
                var allSessions = await _context.QRCodeSessions
                    .Include(s => s.Course)
                    .Include(s => s.Attendances)
                    .OrderByDescending(s => s.CreatedAt)
                    .Take(50) // Limit to recent sessions
                    .ToListAsync();

                // Calculate some statistics
                var totalSessions = allSessions.Count;
                var totalAttendances = allSessions.Sum(s => s.Attendances.Count);
                var activeSessions = sessions.Count;

                ViewBag.TotalSessions = totalSessions;
                ViewBag.TotalAttendances = totalAttendances;
                ViewBag.ActiveSessions = activeSessions;
                ViewBag.RecentSessions = allSessions.Take(10).ToList();

                return View(allSessions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading reports");
                TempData["Error"] = "Error loading reports: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> ReopenSession(int id, int additionalMinutes = 15)
        {
            try
            {
                var success = await _qrCodeService.ReopenSessionAsync(id, additionalMinutes);
                if (success)
                {
                    TempData["Success"] = $"Session reopened for {additionalMinutes} additional minutes!";
                }
                else
                {
                    TempData["Error"] = "Failed to reopen session. Session may not exist or new duration is not longer.";
                }
                return RedirectToAction("ActiveSessions");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reopening session {SessionId}", id);
                TempData["Error"] = $"Error reopening session: {ex.Message}";
                return RedirectToAction("ActiveSessions");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSession(int id)
        {
            try
            {
                var success = await _qrCodeService.DeleteSessionWithAttendanceAsync(id);
                if (success)
                {
                    TempData["Success"] = "Session and all attendance records deleted successfully!";
                }
                else
                {
                    TempData["Error"] = "Session not found or could not be deleted.";
                }
                return RedirectToAction("ActiveSessions");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting session {SessionId}", id);
                TempData["Error"] = $"Error deleting session: {ex.Message}";
                return RedirectToAction("ActiveSessions");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCurrentToken(int id)
        {
            try
            {
                var token = await _qrCodeService.GetCurrentTokenAsync(id);
                return Json(new { token });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current token for session {SessionId}", id);
                return Json(new { token = "" });
            }
        }

        // In QRCodeController.cs
        public async Task<IActionResult> Edit(int id)
        {
            var session = await _qrCodeService.GetSessionByIdAsync(id);
            if (session == null)
            {
                return NotFound();
            }

            var courses = await _courseService.GetAllCoursesAsync();
            ViewBag.Courses = new SelectList(courses, "Id", "CourseCodeName", session.CourseId);

            return View(session);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, QRCodeSession session)
        {
            if (id != session.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Update session
                    var existingSession = await _qrCodeService.GetSessionByIdAsync(id);
                    if (existingSession == null)
                    {
                        return NotFound();
                    }

                    // Update properties
                    existingSession.SessionTitle = session.SessionTitle;
                    existingSession.Description = session.Description;
                    existingSession.CourseId = session.CourseId;
                    existingSession.DurationMinutes = session.DurationMinutes;
                    existingSession.MaxScans = session.MaxScans;
                    existingSession.AllowMultipleScans = session.AllowMultipleScans;
                    existingSession.EnableDynamicQR = session.EnableDynamicQR;
                    existingSession.TokenUpdateIntervalSeconds = session.TokenUpdateIntervalSeconds;

                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Session updated successfully!";
                    return RedirectToAction("ActiveSessions");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating session");
                    TempData["Error"] = $"Error updating session: {ex.Message}";
                }
            }

            var courses = await _courseService.GetAllCoursesAsync();
            ViewBag.Courses = new SelectList(courses, "Id", "CourseCodeName", session.CourseId);
            return View(session);
        }

        // ADD THIS DEBUG METHOD TO YOUR CONTROLLER
        [HttpGet]
        public async Task<JsonResult> DebugSession(int id)
        {
            try
            {
                var session = await _qrCodeService.GetSessionByIdAsync(id);
                var currentToken = await _qrCodeService.GetCurrentTokenAsync(id);

                return Json(new
                {
                    success = true,
                    sessionExists = session != null,
                    sessionTitle = session?.SessionTitle,
                    sessionToken = session?.Token,
                    currentToken = currentToken,
                    enableDynamicQR = session?.EnableDynamicQR,
                    viewBagToken = ViewBag.CurrentToken
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // ADD THIS METHOD TO YOUR QRCodeController
        [HttpGet]
        public async Task<IActionResult> ExportAttendance(int sessionId)
        {
            try
            {
                var fileBytes = await _qrCodeService.ExportAttendanceToExcelAsync(sessionId);
                var session = await _qrCodeService.GetSessionByIdAsync(sessionId);

                var fileName = $"Attendance_{session?.SessionTitle?.Replace(" ", "_") ?? "Session"}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                return File(fileBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting attendance for session {SessionId}", sessionId);
                TempData["Error"] = $"Export failed: {ex.Message}";
                return RedirectToAction("Dashboard", new { id = sessionId });
            }
        }

        [HttpGet]
        public async Task<JsonResult> DebugToken(string token)
        {
            try
            {
                var session = await _context.QRCodeSessions
                    .FirstOrDefaultAsync(s => s.Token == token);

                return Json(new
                {
                    tokenProvided = token,
                    sessionExists = session != null,
                    sessionId = session?.Id,
                    sessionTitle = session?.SessionTitle,
                    isActive = session?.IsActive,
                    expiresAt = session?.ExpiresAt,
                    currentTime = DateTime.Now,
                    isExpired = session?.ExpiresAt < DateTime.Now
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<JsonResult> DebugSessionByToken(string token)
        {
            var session = await _context.QRCodeSessions
                .Include(s => s.Course)
                .FirstOrDefaultAsync(s => s.Token == token);

            if (session == null)
            {
                return Json(new
                {
                    found = false,
                    message = "No session found with this token",
                    token = token
                });
            }

            return Json(new
            {
                found = true,
                sessionId = session.Id,
                sessionTitle = session.SessionTitle,
                course = session.Course?.CourseCode + " - " + session.Course?.CourseName,
                isActive = session.IsActive,
                expiresAt = session.ExpiresAt,
                currentTime = DateTime.Now,
                isExpired = session.ExpiresAt < DateTime.Now,
                durationMinutes = session.DurationMinutes,
                enableDynamicQR = session.EnableDynamicQR
            });
        }

        [HttpGet]
        public async Task<JsonResult> ListAllSessions()
        {
            var sessions = await _context.QRCodeSessions
                .Include(s => s.Course)
                .OrderByDescending(s => s.CreatedAt)
                .Take(10)
                .Select(s => new {
                    id = s.Id,
                    title = s.SessionTitle,
                    token = s.Token,
                    course = s.Course != null ? s.Course.CourseCode + " - " + s.Course.CourseName : "No Course",
                    isActive = s.IsActive,
                    createdAt = s.CreatedAt,
                    expiresAt = s.ExpiresAt
                })
                .ToListAsync();

            return Json(sessions);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<JsonResult> Scan([FromBody] ScanRequest request)
        {
            try
            {
                Console.WriteLine($"=== SCAN DEBUG START ===");
                Console.WriteLine($"Token: {request.Token}");
                Console.WriteLine($"StudentId: {request.StudentId}");
                Console.WriteLine($"Request received at: {DateTime.Now}");

                // ✅ DEBUG: Check if session exists at all
                var allSessions = await _context.QRCodeSessions
                    .Where(s => s.Token == request.Token)
                    .ToListAsync();

                Console.WriteLine($"Total sessions with token: {allSessions.Count}");
                foreach (var s in allSessions)
                {
                    Console.WriteLine($"Session {s.Id}: IsActive={s.IsActive}, CreatedAt={s.CreatedAt}, Duration={s.DurationMinutes}min");
                }

                // ✅ Find active session
                var session = allSessions.FirstOrDefault(s => s.IsActive);
                Console.WriteLine($"Active session found: {session != null}");

                if (session == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"No active session found. Total sessions with token: {allSessions.Count}"
                    });
                }

                // ✅ Check expiration
                var expiresAt = session.CreatedAt.AddMinutes(session.DurationMinutes);
                Console.WriteLine($"Session CreatedAt: {session.CreatedAt}");
                Console.WriteLine($"Duration: {session.DurationMinutes} minutes");
                Console.WriteLine($"ExpiresAt: {expiresAt}");
                Console.WriteLine($"Current Time: {DateTime.Now}");
                Console.WriteLine($"Is Expired: {expiresAt < DateTime.Now}");

                if (expiresAt < DateTime.Now)
                {
                    session.IsActive = false;
                    await _context.SaveChangesAsync();
                    return Json(new { success = false, message = "Session expired" });
                }

                // ✅ Check student - IMPORTANT: StudentId might be string in database
                Console.WriteLine($"Looking for student with ID: {request.StudentId}");
                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.StudentId == request.StudentId.ToString());

                Console.WriteLine($"Student found: {student != null}");
                if (student == null)
                {
                    // Try alternative lookup
                    var studentById = await _context.Students.FindAsync(request.StudentId);
                    Console.WriteLine($"Student by FindAsync: {studentById != null}");

                    return Json(new { success = false, message = $"Student ID {request.StudentId} not found" });
                }

                // ✅ Check for existing attendance
                var existingAttendance = await _context.QRAttendances
                    .FirstOrDefaultAsync(a => a.QRCodeSessionId == session.Id && a.StudentId == request.StudentId);

                Console.WriteLine($"Existing attendance found: {existingAttendance != null}");

                if (existingAttendance != null && !session.AllowMultipleScans)
                {
                    return Json(new { success = false, message = "Already scanned this session" });
                }

                // ✅ Create attendance
                var attendance = new QRAttendance
                {
                    QRCodeSessionId = session.Id,
                    StudentId = request.StudentId,
                    DeviceInfo = request.DeviceInfo ?? Request.Headers["User-Agent"],
                    IPAddress = request.IPAddress ?? HttpContext.Connection.RemoteIpAddress?.ToString(),
                    ScannedAt = DateTime.Now
                };

                Console.WriteLine($"Creating attendance record...");
                _context.QRAttendances.Add(attendance);
                await _context.SaveChangesAsync();

                Console.WriteLine($"=== ATTENDANCE SAVED SUCCESSFULLY ===");
                Console.WriteLine($"Attendance ID: {attendance.Id}");

                return Json(new { success = true, message = "Attendance marked successfully!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== SCAN ERROR ===");
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    Console.WriteLine($"Inner Stack: {ex.InnerException.StackTrace}");
                }

                return Json(new
                {
                    success = false,
                    message = $"Server error: {ex.Message}"
                });
            }
        }


        public class ScanRequest
        {
            public string Token { get; set; } = string.Empty;
            public int StudentId { get; set; }
            public string? DeviceInfo { get; set; }
            public string? IPAddress { get; set; }
        }
    }
}