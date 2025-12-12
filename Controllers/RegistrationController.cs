using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;
using StudentManagementSystem.Models.ViewModels;
using StudentManagementSystem.Services;
using StudentManagementSystem.ViewModels;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QColors = QuestPDF.Helpers.Colors;

namespace StudentManagementSystem.Controllers
{
    public class RegistrationController : BaseController
    {
        private readonly IRegistrationService _registrationService;
        private readonly ApplicationDbContext _context;
        private readonly IExportService _exportService;
        private readonly IImportService _importService;

        public RegistrationController(
            IRegistrationService registrationService,
            ApplicationDbContext context,
            IExportService exportService,
            IImportService importService)
        {
            _registrationService = registrationService;
            _context = context;
            _exportService = exportService;
            _importService = importService;
        }

        // HELPER METHOD: Get current courses count (FIXED VERSION)
        private async Task<int> GetCurrentCoursesCount(int studentId)
        {
            // Use GradeStatus instead of IsCompleted property
            return await _context.CourseEnrollments
                .CountAsync(ce => ce.StudentId == studentId &&
                                 ce.IsActive &&
                                 ce.GradeStatus == GradeStatus.InProgress);
        }

        // Student Portal Entry Point
        public async Task<IActionResult> StudentPortalEntry()
        {
            // Clear any existing student session
            HttpContext.Session.Remove("CurrentStudentId");
            HttpContext.Session.Remove("StudentName");

            ViewData["Layout"] = "_Layout"; // Force main layout for entry page

            var demoStudents = await _context.Students.Take(5).ToListAsync();
            ViewBag.DemoStudents = demoStudents;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> StudentLogin(string studentId, string email)
        {
            // Validate student credentials
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == studentId &&
                                         (s.Email == email || s.StudentId == studentId));

            if (student == null)
            {
                SetErrorMessage("Invalid student ID or email. Please try again.");
                return RedirectToAction("StudentPortalEntry");
            }

            // Set student session
            HttpContext.Session.SetString("CurrentStudentId", student.Id.ToString());
            HttpContext.Session.SetString("StudentName", student.Name);
            HttpContext.Session.SetString("StudentGPA", student.GPA.ToString("F2"));

            SetSuccessMessage($"Welcome back, {student.Name}!");
            return RedirectToAction("StudentDashboard", new { studentId = student.Id });
        }

        // Student Dashboard - FIXED
        public async Task<IActionResult> StudentDashboard(int studentId)
        {
            // Fix: Parse string to int for comparison
            if (!IsStudentUser() || !int.TryParse(CurrentStudentId, out int currentId) || currentId != studentId)
            {
                SetErrorMessage("Access denied. Please log in again.");
                return RedirectToAction("StudentPortalEntry", "Registration");
            }

            var student = await _context.Students
                .Include(s => s.CourseEnrollments)
                .ThenInclude(ce => ce.Course)
                .Include(s => s.StudentDepartment)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null)
            {
                SetErrorMessage("Student not found.");
                return RedirectToAction("StudentPortalEntry", "Registration");
            }

            // Set view data for layout
            ViewBag.StudentName = HttpContext.Session.GetString("StudentName") ?? student.Name;
            ViewBag.CurrentGPA = student.GPA;
            ViewBag.PassedHours = student.PassedHours;
            ViewBag.CurrentCoursesCount = await GetCurrentCoursesCount(studentId);

            return View(student);
        }

        // Student Registration Portal - FIXED
        public async Task<IActionResult> StudentPortal(int studentId)
        {
            if (!IsCurrentStudent(studentId))
            {
                base.SetErrorMessage("Invalid student access.");
                return RedirectToAction("StudentPortalEntry");
            }

            // Simple student access check
            if (!IsCurrentStudent(studentId))
            {
                base.SetErrorMessage("Invalid student access.");
                return RedirectToAction("StudentPortalEntry");
            }

            var student = await _context.Students
                .Include(s => s.StudentSemester)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null)
            {
                return NotFound();
            }

            // Set view data for student portal layout - USING HELPER METHOD
            ViewBag.StudentName = student.Name;
            ViewBag.CurrentGPA = student.GPA;
            ViewBag.PassedHours = student.PassedHours;
            ViewBag.CurrentCoursesCount = await GetCurrentCoursesCount(studentId);

            var currentSemester = student.StudentSemester ?? await _context.Semesters
                .FirstOrDefaultAsync(s => s.IsCurrent);

            if (currentSemester == null)
            {
                SetErrorMessage("No active semester found.");
                return RedirectToAction("Index", "Home");
            }

            var eligibility = await _registrationService.CheckStudentEligibility(studentId, currentSemester.Id);
            var registrations = await _registrationService.GetStudentRegistrations(studentId, currentSemester.Id);
            var eligibleCourses = await _registrationService.GetEligibleCourses(studentId, currentSemester.Id);

            var viewModel = new ViewModels.StudentPortalViewModel
            {
                Student = student,
                CurrentSemester = currentSemester,
                Eligibility = eligibility,
                CurrentRegistrations = registrations,
                EligibleCourses = eligibleCourses,
                ActivePeriods = await _registrationService.GetActiveRegistrationPeriods(currentSemester.Id)
            };

            return View(viewModel);
        }

        // Course Registration
        [HttpPost]
        public async Task<IActionResult> RegisterCourses(ViewModels.RegistrationRequest request)
        {
            if (request.CourseIds == null || !request.CourseIds.Any())
            {
                SetErrorMessage("Please select at least one course to register.");
                return RedirectToAction(nameof(StudentPortal), new { studentId = request.StudentId });
            }

            var result = await _registrationService.RegisterCourses(request);

            if (result.Success)
            {
                SetSuccessMessage(result.Message);

                if (result.Warnings.Any())
                {
                    SetWarningMessage("Registration completed with warnings. Please review.");
                }
            }
            else
            {
                SetErrorMessage(result.Message);

                if (result.Errors.Any())
                {
                    ViewBag.Errors = result.Errors;
                }
            }

            return RedirectToAction(nameof(StudentPortal), new { studentId = request.StudentId });
        }

        // Drop Course
        [HttpPost]
        public async Task<IActionResult> DropCourse(int registrationId, int studentId, string reason)
        {
            var result = await _registrationService.DropCourse(registrationId, reason, User.Identity?.Name ?? "System");

            if (result.Success)
            {
                SetSuccessMessage(result.Message);
            }
            else
            {
                SetErrorMessage(result.Message);
            }

            return RedirectToAction(nameof(StudentPortal), new { studentId });
        }

        // My Grades - FIXED
        public async Task<IActionResult> MyGrades(int studentId, int? semesterId)
        {
            // Simple student access check
            if (!IsCurrentStudent(studentId))
            {
                base.SetErrorMessage("Invalid student access.");
                return RedirectToAction("StudentPortalEntry");
            }

            var student = await _context.Students.FindAsync(studentId);
            if (student == null) return NotFound();

            var query = _context.FinalGrades
                .Include(fg => fg.Course)
                .Include(fg => fg.Semester)
                .Where(fg => fg.StudentId == studentId);

            if (semesterId.HasValue)
            {
                query = query.Where(fg => fg.SemesterId == semesterId.Value);
            }

            var grades = await query.OrderByDescending(fg => fg.Semester!.StartDate).ToListAsync();
            var semesters = await _context.Semesters.Where(s => s.IsActive).ToListAsync();

            // Calculate semester GPA if filtered
            decimal? semesterGPA = null;
            if (semesterId.HasValue)
            {
                var semesterGrades = grades.Where(g => g.SemesterId == semesterId.Value && g.GradeStatus == GradeStatus.Completed).ToList();
                var totalPoints = semesterGrades.Sum(g => (g.Course?.Credits ?? 0) * (g.FinalGradePoints ?? 0));
                var totalCredits = semesterGrades.Sum(g => g.Course?.Credits ?? 0);
                semesterGPA = totalCredits > 0 ? totalPoints / totalCredits : 0;
            }

            // Set view data for student portal layout - USING HELPER METHOD
            ViewBag.StudentName = student.Name;
            ViewBag.CurrentGPA = student.GPA;
            ViewBag.PassedHours = student.PassedHours;
            ViewBag.CurrentCoursesCount = await GetCurrentCoursesCount(studentId);

            ViewBag.Student = student;
            ViewBag.Semesters = semesters;
            ViewBag.SelectedSemesterId = semesterId;
            ViewBag.SemesterGPA = semesterGPA;

            return View(grades);
        }

        // My Schedule - FIXED
        public async Task<IActionResult> MySchedule(int studentId)
        {
            // Simple student access check
            if (!IsCurrentStudent(studentId))
            {
                base.SetErrorMessage("Invalid student access.");
                return RedirectToAction("StudentPortalEntry");
            }

            var student = await _context.Students
                .Include(s => s.CourseEnrollments)
                    .ThenInclude(ce => ce.Course)
                .Include(s => s.CourseEnrollments)
                    .ThenInclude(ce => ce.Semester)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null) return NotFound();

            // Set view data for student portal layout
            ViewBag.StudentName = student.Name;
            ViewBag.CurrentGPA = student.GPA;
            ViewBag.PassedHours = student.PassedHours;
            ViewBag.CurrentCoursesCount = await GetCurrentCoursesCount(studentId);

            // Get CURRENT semester
            var currentSemester = await _context.Semesters
                .FirstOrDefaultAsync(s => s.IsCurrent) ??
                await _context.Semesters
                    .OrderByDescending(s => s.StartDate)
                    .FirstOrDefaultAsync();

            if (currentSemester == null)
            {
                base.SetErrorMessage("No active semester found.");
                return View(student);
            }

            // Get actual course enrollments for current semester
            var currentEnrollments = await _context.CourseEnrollments
                .Include(ce => ce.Course)
                .Include(ce => ce.Semester)
                .Where(ce => ce.StudentId == studentId &&
                            ce.SemesterId == currentSemester.Id &&
                            ce.IsActive &&
                            ce.GradeStatus == GradeStatus.InProgress)
                .ToListAsync();

            // Create actual schedule data
            var currentSchedule = new List<dynamic>();
            var weeklySchedule = new List<dynamic>();

            foreach (var enrollment in currentEnrollments)
            {
                if (enrollment.Course == null) continue;

                // Generate mock schedule data (in real system, this would come from CourseSchedule table)
                var courseCode = enrollment.Course.CourseCode;
                var courseName = enrollment.Course.CourseName;
                var credits = enrollment.Course.Credits;

                // Mock schedule data - in real system, this would come from database
                var schedule = GenerateMockSchedule(courseCode, courseName, credits);

                currentSchedule.Add(new
                {
                    CourseCode = courseCode,
                    CourseName = courseName,
                    Days = schedule.Days,
                    StartTime = schedule.StartTime,
                    EndTime = schedule.EndTime,
                    Room = schedule.Room,
                    Instructor = schedule.Instructor,
                    Credits = credits
                });

                // Add to weekly schedule for calendar view
                AddToWeeklySchedule(weeklySchedule, schedule, courseCode, courseName);
            }

            // If no enrollments, show empty schedule with helpful message
            if (!currentEnrollments.Any())
            {
                ViewBag.NoCoursesMessage = "You are not enrolled in any courses for the current semester.";
            }

            ViewBag.Student = student;
            ViewBag.CurrentSchedule = currentSchedule;
            ViewBag.WeeklySchedule = weeklySchedule;
            ViewBag.CurrentSemester = currentSemester.Name;

            return View(student);
        }

        // Helper method to generate mock schedule (replace with real data from CourseSchedule table)
        private dynamic GenerateMockSchedule(string courseCode, string courseName, int credits)
        {
            // This is mock data - in a real system, you would have a CourseSchedule table
            // with actual day, time, room, and instructor information

            // Simple algorithm to generate consistent mock schedule based on course code
            var hash = Math.Abs(courseCode.GetHashCode());

            var schedules = new[]
            {
        new { Days = "MWF", StartTime = "9:00 AM", EndTime = "9:50 AM", Room = "Science Bldg 101", Instructor = "Dr. Smith" },
        new { Days = "MWF", StartTime = "10:00 AM", EndTime = "10:50 AM", Room = "Science Bldg 102", Instructor = "Dr. Johnson" },
        new { Days = "MWF", StartTime = "11:00 AM", EndTime = "11:50 AM", Room = "Science Bldg 103", Instructor = "Dr. Williams" },
        new { Days = "TTH", StartTime = "9:00 AM", EndTime = "10:15 AM", Room = "Humanities 201", Instructor = "Dr. Brown" },
        new { Days = "TTH", StartTime = "10:30 AM", EndTime = "11:45 AM", Room = "Humanities 202", Instructor = "Dr. Davis" },
        new { Days = "TTH", StartTime = "1:00 PM", EndTime = "2:15 PM", Room = "CS Bldg 101", Instructor = "Dr. Wilson" },
        new { Days = "TTH", StartTime = "2:30 PM", EndTime = "3:45 PM", Room = "CS Bldg 102", Instructor = "Dr. Taylor" }
    };

            return schedules[hash % schedules.Length];
        }

        // Helper method to add course to weekly schedule
        private void AddToWeeklySchedule(List<dynamic> weeklySchedule, dynamic schedule, string courseCode, string courseName)
        {
            var days = schedule.Days.ToString();

            foreach (char dayChar in days)
            {
                string day = dayChar switch
                {
                    'M' => "Monday",
                    'T' => "Tuesday",
                    'W' => "Wednesday",
                    'H' => "Thursday",
                    'F' => "Friday",
                    'S' => "Saturday",
                    'U' => "Sunday",
                    _ => ""
                };

                if (!string.IsNullOrEmpty(day))
                {
                    // Parse time
                    var startTime = schedule.StartTime.ToString();
                    var endTime = schedule.EndTime.ToString();

                    // Convert to 24-hour for easier calculation
                    int startHour = ParseTimeToHour(startTime);
                    int endHour = ParseTimeToHour(endTime);

                    weeklySchedule.Add(new
                    {
                        Day = day,
                        CourseCode = courseCode,
                        CourseName = courseName,
                        StartHour = startHour,
                        EndHour = endHour,
                        Room = schedule.Room.ToString(),
                        Instructor = schedule.Instructor.ToString()
                    });
                }
            }
        }

        // Helper method to parse time string to hour
        private int ParseTimeToHour(string timeString)
        {
            try
            {
                timeString = timeString.ToUpper().Trim();

                // Remove AM/PM
                bool isPM = timeString.Contains("PM");
                timeString = timeString.Replace("AM", "").Replace("PM", "").Trim();

                // Parse hours and minutes
                var parts = timeString.Split(':');
                int hours = int.Parse(parts[0].Trim());
                //int minutes = parts.Length > 1 ? int.Parse(parts[1].Trim()) : 0;

                // Convert to 24-hour format
                if (isPM && hours < 12) hours += 12;
                if (!isPM && hours == 12) hours = 0;

                return hours;
            }
            catch
            {
                return 9; // Default to 9 AM if parsing fails
            }
        }

        // Unofficial Transcript - FIXED
        public async Task<IActionResult> Transcript(int studentId)
        {
            // Simple student access check
            if (!IsCurrentStudent(studentId))
            {
                base.SetErrorMessage("Invalid student access.");
                return RedirectToAction("StudentPortalEntry");
            }

            var student = await _context.Students.FindAsync(studentId);
            if (student == null) return NotFound();

            // Set view data for student portal layout - USING HELPER METHOD
            ViewBag.StudentName = student.Name;
            ViewBag.CurrentGPA = student.GPA;
            ViewBag.PassedHours = student.PassedHours;
            ViewBag.CurrentCoursesCount = await GetCurrentCoursesCount(studentId);

            var transcriptData = await _context.FinalGrades
                .Include(fg => fg.Course)
                .Include(fg => fg.Semester)
                .Where(fg => fg.StudentId == studentId)
                .OrderBy(fg => fg.Semester!.StartDate)
                .ToListAsync();

            // Calculate cumulative GPA
            var completedGrades = transcriptData.Where(g => g.GradeStatus == GradeStatus.Completed && g.FinalGradePoints.HasValue).ToList();
            var totalPoints = completedGrades.Sum(g => (g.Course?.Credits ?? 0) * (g.FinalGradePoints ?? 0));
            var totalCredits = completedGrades.Sum(g => g.Course?.Credits ?? 0);
            var cumulativeGPA = totalCredits > 0 ? totalPoints / totalCredits : 0;

            ViewBag.TranscriptData = transcriptData;
            ViewBag.CumulativeGPA = cumulativeGPA;
            ViewBag.TotalCredits = totalCredits;

            return View(student);
        }

        // Academic Progress - FIXED
        public async Task<IActionResult> AcademicProgress(int studentId)
        {
            // Simple student access check
            if (!IsCurrentStudent(studentId))
            {
                base.SetErrorMessage("Invalid student access.");
                return RedirectToAction("StudentPortalEntry");
            }

            var student = await _context.Students.FindAsync(studentId);
            if (student == null) return NotFound();

            // Set view data for student portal layout - USING HELPER METHOD
            ViewBag.StudentName = student.Name;
            ViewBag.CurrentGPA = student.GPA;
            ViewBag.PassedHours = student.PassedHours;
            ViewBag.CurrentCoursesCount = await GetCurrentCoursesCount(studentId);

            // Mock progress data
            var progressData = new List<dynamic>
            {
                new { Semester = "Fall 2023", GPA = 3.2m, Credits = 15 },
                new { Semester = "Spring 2024", GPA = 3.4m, Credits = 16 },
                new { Semester = "Fall 2024", GPA = 3.6m, Credits = 15 }
            };

            var programRequirements = new List<dynamic>
            {
                new { Category = "Core Courses", Completed = 45, Required = 60, Percentage = 75 },
                new { Category = "Electives", Completed = 15, Required = 30, Percentage = 50 },
                new { Category = "General Education", Completed = 30, Required = 30, Percentage = 100 }
            };

            var requirementsChecklist = new List<dynamic>
            {
                new { Description = "Complete 120 Credit Hours", Completed = student.PassedHours >= 120 },
                new { Description = "Maintain 2.0+ GPA", Completed = student.GPA >= 2.0m },
                new { Description = "Complete Core Requirements", Completed = false },
                new { Description = "Complete Elective Requirements", Completed = false },
                new { Description = "Senior Project/Thesis", Completed = student.PassedHours >= 90 }
            };

            ViewBag.ProgressData = progressData;
            ViewBag.ProgramRequirements = programRequirements;
            ViewBag.RequirementsChecklist = requirementsChecklist;
            ViewBag.CompletionPercentage = (student.PassedHours / 120.0) * 100;

            return View(student);
        }

        // Course Materials - FIXED
        public async Task<IActionResult> CourseMaterials(int studentId)
        {
            // Simple student access check
            if (!IsCurrentStudent(studentId))
            {
                base.SetErrorMessage("Invalid student access.");
                return RedirectToAction("StudentPortalEntry");
            }

            var student = await _context.Students.FindAsync(studentId);
            if (student == null) return NotFound();

            // Set view data for student portal layout - USING HELPER METHOD
            ViewBag.StudentName = student.Name;
            ViewBag.CurrentGPA = student.GPA;
            ViewBag.PassedHours = student.PassedHours;
            ViewBag.CurrentCoursesCount = await GetCurrentCoursesCount(studentId);

            // Mock course materials
            var courseMaterials = new List<dynamic>
            {
                new {
                    CourseCode = "CS101",
                    CourseName = "Introduction to Programming",
                    Materials = new List<dynamic>
                    {
                        new { Type = "Syllabus", Name = "Course Syllabus", UploadDate = DateTime.Now.AddDays(-30), Size = "250 KB" },
                        new { Type = "Lecture", Name = "Week 1 Slides", UploadDate = DateTime.Now.AddDays(-7), Size = "1.2 MB" },
                        new { Type = "Assignment", Name = "Project 1 Requirements", UploadDate = DateTime.Now.AddDays(-5), Size = "180 KB" }
                    }
                },
                new {
                    CourseCode = "MATH101",
                    CourseName = "Calculus I",
                    Materials = new List<dynamic>
                    {
                        new { Type = "Syllabus", Name = "Math 101 Syllabus", UploadDate = DateTime.Now.AddDays(-25), Size = "300 KB" },
                        new { Type = "Textbook", Name = "Calculus Textbook Chapter 1", UploadDate = DateTime.Now.AddDays(-10), Size = "5.4 MB" }
                    }
                }
            };

            ViewBag.CourseMaterials = courseMaterials;
            return View(student);
        }

        // Student Logout
        public IActionResult StudentLogout()
        {
            HttpContext.Session.Remove("CurrentStudentId");
            HttpContext.Session.Remove("StudentName");
            HttpContext.Session.Remove("StudentGPA");

            SetSuccessMessage("You have been logged out successfully.");
            return RedirectToAction("Index", "Home");
        }

        // Registration Management
        public async Task<IActionResult> Management(int semesterId = 0, string filter = "all", string sortBy = "date", string sortOrder = "desc")
        {
            if (!base.IsAdminUser())
            {
                base.SetErrorMessage("Admin access required.");
                return RedirectToAction("AccessDenied", "Home");
            }

            var semesters = await _context.Semesters.Where(s => s.IsActive).ToListAsync();
            var currentSemester = semesterId == 0
                ? await _context.Semesters.FirstOrDefaultAsync(s => s.IsCurrent)
                : await _context.Semesters.FindAsync(semesterId);

            // Handle case where no semester is found
            if (currentSemester == null)
            {
                SetErrorMessage("No active semester found.");
                return View(new RegistrationManagementViewModel
                {
                    Semesters = semesters,
                    Registrations = new List<CourseRegistration>()
                });
            }

            IQueryable<CourseRegistration> query = _context.CourseRegistrations
                .Include(r => r.Student)
                .Include(r => r.Course)
                .Include(r => r.Semester)
                .Where(r => r.SemesterId == currentSemester.Id);

            // Apply filters with null safety
            query = (filter?.ToLower() ?? "all") switch
            {
                "pending" => query.Where(r => r.Status == RegistrationStatus.Pending),
                "approved" => query.Where(r => r.Status == RegistrationStatus.Approved),
                "rejected" => query.Where(r => r.Status == RegistrationStatus.Rejected),
                "waitlisted" => query.Where(r => r.Status == RegistrationStatus.Waitlisted),
                "dropped" => query.Where(r => r.Status == RegistrationStatus.Dropped),
                _ => query
            };

            // Apply sorting with null safety
            query = ((sortBy?.ToLower() ?? "date"), (sortOrder?.ToLower() ?? "desc")) switch
            {
                ("student", "asc") => query.OrderBy(r => r.Student != null ? r.Student.Name : ""),
                ("student", "desc") => query.OrderByDescending(r => r.Student != null ? r.Student.Name : ""),
                ("course", "asc") => query.OrderBy(r => r.Course != null ? r.Course.CourseCode : ""),
                ("course", "desc") => query.OrderByDescending(r => r.Course != null ? r.Course.CourseCode : ""),
                ("date", "asc") => query.OrderBy(r => r.RegistrationDate),
                ("date", "desc") => query.OrderByDescending(r => r.RegistrationDate),
                ("status", "asc") => query.OrderBy(r => r.Status),
                ("status", "desc") => query.OrderByDescending(r => r.Status),
                _ => query.OrderByDescending(r => r.RegistrationDate)
            };

            var registrations = await query.ToListAsync();

            var viewModel = new ViewModels.RegistrationManagementViewModel
            {
                Semesters = semesters,
                SelectedSemester = currentSemester,
                Registrations = registrations,
                PendingCount = await _context.CourseRegistrations.CountAsync(r => r.SemesterId == currentSemester.Id && r.Status == RegistrationStatus.Pending),
                ApprovedCount = await _context.CourseRegistrations.CountAsync(r => r.SemesterId == currentSemester.Id && r.Status == RegistrationStatus.Approved),
                TotalCount = await _context.CourseRegistrations.CountAsync(r => r.SemesterId == currentSemester.Id),
                CurrentFilter = filter ?? "all",
                CurrentSort = sortBy ?? "date",
                CurrentSortOrder = sortOrder ?? "desc"
            };

            return View(viewModel);
        }

        // Registration Dashboard
        public async Task<IActionResult> RegistrationDashboard()
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            var currentSemester = await _context.Semesters.FirstOrDefaultAsync(s => s.IsCurrent);
            var today = DateTime.Now;

            var dashboardData = new
            {
                // Statistics
                TotalRegistrations = await _context.CourseRegistrations.CountAsync(),
                PendingRegistrations = await _context.CourseRegistrations.CountAsync(r => r.Status == RegistrationStatus.Pending),
                ApprovedRegistrations = await _context.CourseRegistrations.CountAsync(r => r.Status == RegistrationStatus.Approved),

                // Current semester stats
                CurrentSemesterRegistrations = currentSemester != null ?
                    await _context.CourseRegistrations.CountAsync(r => r.SemesterId == currentSemester.Id) : 0,

                // Active periods
                ActivePeriods = await _context.RegistrationPeriods
                    .Include(p => p.Semester)
                    .Where(p => p.IsActive && p.StartDate <= today && p.EndDate >= today)
                    .ToListAsync(),

                // Upcoming deadlines
                UpcomingDeadlines = await _context.RegistrationPeriods
                    .Where(p => p.StartDate > today)
                    .OrderBy(p => p.StartDate)
                    .Take(5)
                    .ToListAsync(),

                // Rule statistics
                ActiveRules = await _context.RegistrationRules.CountAsync(r => r.IsActive),
                BlockingRules = await _context.RegistrationRules.CountAsync(r => r.EnforcementLevel == EnforcementLevel.Block),

                // Recent activity
                RecentRegistrations = await _context.CourseRegistrations
                    .Include(r => r.Student)
                    .Include(r => r.Course)
                    .OrderByDescending(r => r.RegistrationDate)
                    .Take(10)
                    .ToListAsync()
            };

            return View(dashboardData);
        }

        // Registration Rules Management
        public async Task<IActionResult> Rules()
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            var rules = await _context.RegistrationRules
                .Include(r => r.Department)
                .Include(r => r.Course)
                .ToListAsync();

            var departments = await _context.Departments.ToListAsync();

            var viewModel = new ViewModels.RulesManagementViewModel
            {
                Rules = rules,
                Departments = departments
            };

            return View(viewModel);
        }

        // Save Rule
        [HttpPost]
        public async Task<IActionResult> SaveRule(Models.RegistrationRule rule)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (rule.Id == 0)
                    {
                        // New rule
                        rule.CreatedDate = DateTime.Now;
                        rule.ModifiedDate = DateTime.Now;
                        _context.RegistrationRules.Add(rule);
                    }
                    else
                    {
                        // Update existing rule
                        var existingRule = await _context.RegistrationRules.FindAsync(rule.Id);
                        if (existingRule != null)
                        {
                            // Update properties
                            existingRule.RuleName = rule.RuleName;
                            existingRule.Description = rule.Description;
                            existingRule.RuleType = rule.RuleType;
                            existingRule.DepartmentId = rule.DepartmentId;
                            existingRule.MinimumGPA = rule.MinimumGPA;
                            existingRule.MinimumPassedHours = rule.MinimumPassedHours;
                            existingRule.MaximumCreditHours = rule.MaximumCreditHours;
                            existingRule.MinimumCreditHours = rule.MinimumCreditHours;
                            existingRule.GradeLevel = rule.GradeLevel;
                            existingRule.EnforcementLevel = rule.EnforcementLevel;
                            existingRule.IsActive = rule.IsActive;
                            existingRule.CourseId = rule.CourseId;
                            existingRule.ModifiedDate = DateTime.Now;

                            _context.RegistrationRules.Update(existingRule);
                        }
                    }

                    await _context.SaveChangesAsync();
                    SetSuccessMessage("Registration rule saved successfully.");
                    return RedirectToAction(nameof(Rules));
                }
                catch (Exception ex)
                {
                    SetErrorMessage($"Error saving rule: {ex.Message}");
                    return View("Rules", await GetRulesViewModel());
                }
            }
            else
            {
                SetErrorMessage("Please correct the errors below.");
                return View("Rules", await GetRulesViewModel());
            }
        }

        // Registration Periods Management
        public async Task<IActionResult> Periods()
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            var periods = await _context.RegistrationPeriods
                .Include(p => p.Semester)
                .ToListAsync();

            var semesters = await _context.Semesters.Where(s => s.IsActive).ToListAsync();

            var viewModel = new ViewModels.PeriodsManagementViewModel
            {
                Periods = periods,
                Semesters = semesters
            };

            return View(viewModel);
        }

        // Save Period
        [HttpPost]
        public async Task<IActionResult> SavePeriod(Models.RegistrationPeriod period)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            if (ModelState.IsValid)
            {
                if (period.Id == 0)
                {
                    period.CreatedDate = DateTime.Now;
                    period.ModifiedDate = DateTime.Now;
                    _context.RegistrationPeriods.Add(period);
                }
                else
                {
                    var existingPeriod = await _context.RegistrationPeriods.FindAsync(period.Id);
                    if (existingPeriod != null)
                    {
                        existingPeriod.PeriodName = period.PeriodName;
                        existingPeriod.StartDate = period.StartDate;
                        existingPeriod.EndDate = period.EndDate;
                        existingPeriod.SemesterId = period.SemesterId;
                        existingPeriod.RegistrationType = period.RegistrationType;
                        existingPeriod.MaxCoursesPerStudent = period.MaxCoursesPerStudent;
                        existingPeriod.MaxCreditHours = period.MaxCreditHours;
                        existingPeriod.IsActive = period.IsActive;
                        existingPeriod.ModifiedDate = DateTime.Now;

                        _context.RegistrationPeriods.Update(existingPeriod);
                    }
                }

                await _context.SaveChangesAsync();
                SetSuccessMessage("Registration period saved successfully.");
            }
            else
            {
                SetErrorMessage("Please correct the errors below.");
            }

            return RedirectToAction(nameof(Periods));
        }

        // Toggle Rule
        [HttpPost]
        public async Task<IActionResult> ToggleRule(int id)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            var rule = await _context.RegistrationRules.FindAsync(id);
            if (rule != null)
            {
                rule.IsActive = !rule.IsActive;
                rule.ModifiedDate = DateTime.Now;
                await _context.SaveChangesAsync();
                return Json(new { success = true, isActive = rule.IsActive });
            }
            return Json(new { success = false });
        }

        // Toggle Period
        [HttpPost]
        public async Task<IActionResult> TogglePeriod(int id)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            var period = await _context.RegistrationPeriods.FindAsync(id);
            if (period != null)
            {
                period.IsActive = !period.IsActive;
                period.ModifiedDate = DateTime.Now;
                await _context.SaveChangesAsync();
                SetSuccessMessage($"Period {(period.IsActive ? "activated" : "deactivated")} successfully.");
            }
            return RedirectToAction(nameof(Periods));
        }

        // Approve/Reject Registration
        [HttpPost]
        public async Task<IActionResult> ApproveRegistration(int registrationId, int semesterId)
        {
            if (!base.IsAdminUser())
            {
                base.SetErrorMessage("Admin access required.");
                return RedirectToAction("AccessDenied", "Home");
            }

            var result = await _registrationService.ApproveRegistration(registrationId, User.Identity?.Name ?? "Admin");

            if (result)
            {
                base.SetSuccessMessage("Registration approved successfully.");
            }
            else
            {
                base.SetErrorMessage("Failed to approve registration.");
            }

            return RedirectToAction(nameof(Management), new { semesterId });
        }

        [HttpPost]
        public async Task<IActionResult> RejectRegistration(int registrationId, int semesterId, string reason)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            var result = await _registrationService.RejectRegistration(registrationId, reason, User.Identity?.Name ?? "Admin");

            if (result)
            {
                SetSuccessMessage("Registration rejected successfully.");
            }
            else
            {
                SetErrorMessage("Failed to reject registration.");
            }

            return RedirectToAction(nameof(Management), new { semesterId });
        }

        // Export Registrations
        public async Task<IActionResult> ExportRegistrations(int semesterId, string format = "excel", string filter = "all")
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            var semester = await _context.Semesters.FindAsync(semesterId);
            if (semester == null)
            {
                SetErrorMessage("Semester not found.");
                return RedirectToAction(nameof(Management));
            }

            var registrations = await _context.CourseRegistrations
                .Include(r => r.Student)
                .Include(r => r.Course)
                .Include(r => r.Semester)
                .Where(r => r.SemesterId == semesterId)
                .ToListAsync();

            // Apply filter
            if (!string.IsNullOrEmpty(filter) && filter != "all")
            {
                if (Enum.TryParse<RegistrationStatus>(filter, true, out var status))
                {
                    registrations = registrations.Where(r => r.Status == status).ToList();
                }
            }

            if (format.ToLower() == "excel")
            {
                // Use EPPlus for Excel export
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Registrations");

                    // Headers
                    worksheet.Cells[1, 1].Value = "Student ID";
                    worksheet.Cells[1, 2].Value = "Student Name";
                    worksheet.Cells[1, 3].Value = "Course Code";
                    worksheet.Cells[1, 4].Value = "Course Name";
                    worksheet.Cells[1, 5].Value = "Credits";
                    worksheet.Cells[1, 6].Value = "Registration Date";
                    worksheet.Cells[1, 7].Value = "Status";
                    worksheet.Cells[1, 8].Value = "Type";
                    worksheet.Cells[1, 9].Value = "Approved By";
                    worksheet.Cells[1, 10].Value = "Approval Date";

                    // Style headers
                    using (var range = worksheet.Cells[1, 1, 1, 10])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                    }

                    // Data
                    int row = 2;
                    foreach (var registration in registrations)
                    {
                        worksheet.Cells[row, 1].Value = registration.Student?.StudentId ?? "";
                        worksheet.Cells[row, 2].Value = registration.Student?.Name ?? "";
                        worksheet.Cells[row, 3].Value = registration.Course?.CourseCode ?? "";
                        worksheet.Cells[row, 4].Value = registration.Course?.CourseName ?? "";
                        worksheet.Cells[row, 5].Value = registration.Course?.Credits ?? 0;
                        worksheet.Cells[row, 6].Value = registration.RegistrationDate.ToString("yyyy-MM-dd HH:mm");
                        worksheet.Cells[row, 7].Value = registration.Status.ToString();
                        worksheet.Cells[row, 8].Value = registration.RegistrationType.ToString();
                        worksheet.Cells[row, 9].Value = registration.ApprovedBy ?? "";
                        worksheet.Cells[row, 10].Value = registration.ApprovalDate?.ToString("yyyy-MM-dd") ?? "";
                        row++;
                    }

                    // Auto-fit columns
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    var fileName = $"Registrations_{semester.Name}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    var bytes = package.GetAsByteArray();

                    return File(bytes,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        fileName);
                }
            }
            else if (format.ToLower() == "csv")
            {
                // CSV Export
                var csv = new StringBuilder();
                csv.AppendLine("StudentID,StudentName,CourseCode,CourseName,Credits,RegistrationDate,Status,Type,ApprovedBy,ApprovalDate");

                foreach (var registration in registrations)
                {
                    csv.AppendLine(
                        $"\"{registration.Student?.StudentId ?? ""}\"," +
                        $"\"{registration.Student?.Name ?? ""}\"," +
                        $"\"{registration.Course?.CourseCode ?? ""}\"," +
                        $"\"{registration.Course?.CourseName ?? ""}\"," +
                        $"{registration.Course?.Credits ?? 0}," +
                        $"\"{registration.RegistrationDate:yyyy-MM-dd HH:mm}\"," +
                        $"\"{registration.Status}\"," +
                        $"\"{registration.RegistrationType}\"," +
                        $"\"{registration.ApprovedBy ?? ""}\"," +
                        $"\"{registration.ApprovalDate?.ToString("yyyy-MM-dd") ?? ""}\""
                    );
                }

                var fileName = $"Registrations_{semester.Name}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                var bytes = Encoding.UTF8.GetBytes(csv.ToString());

                return File(bytes, "text/csv", fileName);
            }

            SetErrorMessage("Invalid export format.");
            return RedirectToAction(nameof(Management), new { semesterId });
        }

        // Import Registrations
        [HttpPost]
        public async Task<IActionResult> ImportRegistrations(IFormFile file, int semesterId, bool overwriteExisting = false)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            if (file == null || file.Length == 0)
            {
                SetErrorMessage("Please select a file to import.");
                return RedirectToAction(nameof(Management), new { semesterId });
            }

            var result = new ImportResult();
            var semester = await _context.Semesters.FindAsync(semesterId);

            if (semester == null)
            {
                SetErrorMessage("Semester not found.");
                return RedirectToAction(nameof(Management), new { semesterId });
            }

            try
            {
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    stream.Position = 0;

                    if (Path.GetExtension(file.FileName).ToLower() == ".xlsx" ||
                        Path.GetExtension(file.FileName).ToLower() == ".xls")
                    {
                        using (var package = new ExcelPackage(stream))
                        {
                            var worksheet = package.Workbook.Worksheets[0];
                            var rowCount = worksheet.Dimension?.Rows ?? 0;

                            for (int row = 2; row <= rowCount; row++)
                            {
                                try
                                {
                                    var studentId = worksheet.Cells[row, 1].Text?.Trim();
                                    var courseCode = worksheet.Cells[row, 2].Text?.Trim();

                                    if (string.IsNullOrEmpty(studentId) || string.IsNullOrEmpty(courseCode))
                                    {
                                        result.Errors.Add($"Row {row}: Missing Student ID or Course Code");
                                        result.SkippedCount++;
                                        continue;
                                    }

                                    // Find student
                                    var student = await _context.Students
                                        .FirstOrDefaultAsync(s => s.StudentId == studentId);

                                    if (student == null)
                                    {
                                        result.Errors.Add($"Row {row}: Student '{studentId}' not found");
                                        result.SkippedCount++;
                                        continue;
                                    }

                                    // Find course
                                    var course = await _context.Courses
                                        .FirstOrDefaultAsync(c => c.CourseCode == courseCode &&
                                                                  c.SemesterId == semesterId);

                                    if (course == null)
                                    {
                                        result.Errors.Add($"Row {row}: Course '{courseCode}' not found for semester {semester.Name}");
                                        result.SkippedCount++;
                                        continue;
                                    }

                                    // Check if registration already exists
                                    var existingRegistration = await _context.CourseRegistrations
                                        .FirstOrDefaultAsync(r => r.StudentId == student.Id &&
                                                                 r.CourseId == course.Id &&
                                                                 r.SemesterId == semesterId);

                                    if (existingRegistration != null)
                                    {
                                        if (overwriteExisting)
                                        {
                                            // Update existing
                                            existingRegistration.RegistrationDate = DateTime.Now;
                                            existingRegistration.Status = RegistrationStatus.Pending;
                                            _context.CourseRegistrations.Update(existingRegistration);
                                        }
                                        else
                                        {
                                            result.Errors.Add($"Row {row}: Registration already exists for {studentId} - {courseCode}");
                                            result.SkippedCount++;
                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        // Create new registration
                                        var registration = new CourseRegistration
                                        {
                                            StudentId = student.Id,
                                            CourseId = course.Id,
                                            SemesterId = semesterId,
                                            RegistrationDate = DateTime.Now,
                                            Status = RegistrationStatus.Pending,
                                            RegistrationType = RegistrationType.Regular,
                                            Remarks = "Imported from file"
                                        };

                                        _context.CourseRegistrations.Add(registration);
                                    }

                                    result.ImportedCount++;
                                }
                                catch (Exception ex)
                                {
                                    result.Errors.Add($"Row {row}: Error - {ex.Message}");
                                    result.SkippedCount++;
                                }
                            }

                            await _context.SaveChangesAsync();
                            result.Success = true;
                        }
                    }
                    else
                    {
                        SetErrorMessage("Unsupported file format. Please upload Excel (.xlsx/.xls) files.");
                        return RedirectToAction(nameof(Management), new { semesterId });
                    }
                }

                if (result.Success)
                {
                    SetSuccessMessage($"Successfully imported {result.ImportedCount} registrations. {result.SkippedCount} records skipped.");
                }
                else
                {
                    SetErrorMessage($"Import failed. {result.Errors.FirstOrDefault()}");
                }
            }
            catch (Exception ex)
            {
                SetErrorMessage($"Import error: {ex.Message}");
            }

            return RedirectToAction(nameof(Management), new { semesterId });
        }

        // Download Import Template
        public async Task<IActionResult> DownloadImportTemplate(string type = "registrations")
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            var templateBytes = await _exportService.GenerateImportTemplate(type);
            var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            var fileName = $"{type}_import_template_{DateTime.Now:yyyyMMdd}.xlsx";

            return File(templateBytes, contentType, fileName);
        }

        // Bulk Operations
        [HttpPost]
        public async Task<IActionResult> BulkApprove(int[] registrationIds, int semesterId)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            if (registrationIds == null || !registrationIds.Any())
            {
                SetErrorMessage("Please select at least one registration to approve.");
                return RedirectToAction(nameof(Management), new { semesterId });
            }

            int successCount = 0;
            foreach (var id in registrationIds)
            {
                var result = await _registrationService.ApproveRegistration(id, User.Identity?.Name ?? "Admin");
                if (result) successCount++;
            }

            SetSuccessMessage($"Successfully approved {successCount} out of {registrationIds.Length} registrations.");
            return RedirectToAction(nameof(Management), new { semesterId });
        }

        [HttpPost]
        public async Task<IActionResult> BulkReject(int[] registrationIds, int semesterId, string reason)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            if (registrationIds == null || !registrationIds.Any())
            {
                SetErrorMessage("Please select at least one registration to reject.");
                return RedirectToAction(nameof(Management), new { semesterId });
            }

            int successCount = 0;
            foreach (var id in registrationIds)
            {
                var result = await _registrationService.RejectRegistration(id, reason, User.Identity?.Name ?? "Admin");
                if (result) successCount++;
            }

            SetSuccessMessage($"Successfully rejected {successCount} out of {registrationIds.Length} registrations.");
            return RedirectToAction(nameof(Management), new { semesterId });
        }

        // Student History Selection
        public async Task<IActionResult> StudentHistorySelection()
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            var students = await _context.Students
                .Take(20)
                .Select(s => new
                {
                    Id = s.Id,
                    Name = s.Name,
                    StudentId = s.StudentId
                })
                .ToListAsync();

            // Convert to List<dynamic> for the view
            var studentList = new List<dynamic>();
            foreach (var student in students)
            {
                studentList.Add(new
                {
                    Id = student.Id,
                    Name = student.Name,
                    StudentId = student.StudentId
                });
            }

            ViewBag.Students = studentList;

            return View();
        }

        // Student Registration History
        public async Task<IActionResult> StudentHistory(int? studentId)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            if (!studentId.HasValue)
            {
                return RedirectToAction("StudentHistorySelection");
            }

            var student = await _context.Students
                .Include(s => s.CourseEnrollments)
                .ThenInclude(ce => ce.Course)
                .FirstOrDefaultAsync(s => s.Id == studentId.Value);

            if (student == null)
            {
                SetErrorMessage("Student not found.");
                return RedirectToAction("Index", "Students");
            }

            var registrationHistory = await _context.CourseRegistrations
                .Include(r => r.Course)
                .Include(r => r.Semester)
                .Where(r => r.StudentId == studentId.Value)
                .OrderByDescending(r => r.Semester != null ? r.Semester.StartDate : DateTime.MinValue)
                .ToListAsync();

            var viewModel = new ViewModels.StudentRegistrationHistoryViewModel
            {
                Student = student,
                RegistrationHistory = registrationHistory
            };

            return View(viewModel);
        }

        // Analytics
        public async Task<IActionResult> Analytics()
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            var analytics = await GenerateRegistrationAnalytics();
            return View(analytics);
        }

        // Reports
        public async Task<IActionResult> Reports(int semesterId = 0)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            var semesters = await _context.Semesters.Where(s => s.IsActive).ToListAsync();
            var currentSemester = semesterId == 0
                ? await _context.Semesters.FirstOrDefaultAsync(s => s.IsCurrent)
                : await _context.Semesters.FindAsync(semesterId);

            var reportData = await GenerateComprehensiveReport(currentSemester?.Id ?? 0);

            var viewModel = new ViewModels.RegistrationReportViewModel
            {
                Semesters = semesters,
                SelectedSemester = currentSemester,
                ReportData = reportData
            };

            return View(viewModel);
        }

        // Get Registration Details
        public async Task<IActionResult> GetRegistrationDetails(int id)
        {
            var registration = await _context.CourseRegistrations
                .Include(r => r.Student)
                .Include(r => r.Course)
                .Include(r => r.Semester)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (registration == null)
            {
                return NotFound();
            }

            return PartialView("_RegistrationDetails", registration);
        }

        // Helper Methods
        private async Task<RegistrationAnalytics> GenerateRegistrationAnalytics()
        {
            var currentSemester = await _context.Semesters.FirstOrDefaultAsync(s => s.IsCurrent);
            if (currentSemester == null)
                return new RegistrationAnalytics();

            var totalRegistrations = await _context.CourseRegistrations
                .Where(r => r.SemesterId == currentSemester.Id)
                .CountAsync();

            var successfulRegistrations = await _context.CourseRegistrations
                .Where(r => r.SemesterId == currentSemester.Id && r.Status == RegistrationStatus.Approved)
                .CountAsync();

            return new RegistrationAnalytics
            {
                TotalRegistrations = totalRegistrations,
                SuccessfulRegistrations = successfulRegistrations,
                FailedRegistrations = await _context.CourseRegistrations
                    .Where(r => r.SemesterId == currentSemester.Id && r.Status == RegistrationStatus.Rejected)
                    .CountAsync(),
                SuccessRate = totalRegistrations > 0 ? (decimal)successfulRegistrations / totalRegistrations * 100 : 0,
                RegistrationByDepartment = await GetRegistrationsByDepartment(currentSemester.Id),
                TopCourses = await GetTopCourses(currentSemester.Id),
                CommonErrors = new Dictionary<string, int>(),
                WeeklyTrends = new List<RegistrationTrend>()
            };
        }

        private async Task<RegistrationReportData> GenerateComprehensiveReport(int semesterId)
        {
            var reportData = new RegistrationReportData
            {
                TotalStudents = await _context.CourseRegistrations
                    .Where(r => r.SemesterId == semesterId)
                    .Select(r => r.StudentId)
                    .Distinct()
                    .CountAsync(),
                TotalCourses = await _context.CourseRegistrations
                    .Where(r => r.SemesterId == semesterId)
                    .Select(r => r.CourseId)
                    .Distinct()
                    .CountAsync()
            };

            // Calculate average courses per student
            var studentCourseCounts = await _context.CourseRegistrations
                .Where(r => r.SemesterId == semesterId)
                .GroupBy(r => r.StudentId)
                .Select(g => g.Count())
                .ToListAsync();

            reportData.AverageCoursesPerStudent = studentCourseCounts.Any() ?
                (decimal)studentCourseCounts.Average() : 0;

            // Status distribution
            var statusGroups = await _context.CourseRegistrations
                .Where(r => r.SemesterId == semesterId)
                .GroupBy(r => r.Status)
                .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                .ToListAsync();

            reportData.StatusDistribution = statusGroups.ToDictionary(g => g.Status, g => g.Count);

            return reportData;
        }

        private async Task<Dictionary<string, int>> GetRegistrationsByDepartment(int semesterId)
        {
            var departmentGroups = await _context.CourseRegistrations
                .Include(r => r.Course)
                .Where(r => r.SemesterId == semesterId)
                .Select(r => new
                {
                    Department = r.Course != null ? r.Course.Department : "Unknown"
                })
                .GroupBy(x => string.IsNullOrEmpty(x.Department) ? "Unknown" : x.Department)
                .Select(g => new { Department = g.Key, Count = g.Count() })
                .ToListAsync();

            return departmentGroups.ToDictionary(x => x.Department, x => x.Count);
        }

        private async Task<Dictionary<string, int>> GetTopCourses(int semesterId)
        {
            var courseGroups = await _context.CourseRegistrations
                .Include(r => r.Course)
                .Where(r => r.SemesterId == semesterId)
                .Select(r => new
                {
                    CourseCode = r.Course != null ? r.Course.CourseCode : "Unknown"
                })
                .GroupBy(x => string.IsNullOrEmpty(x.CourseCode) ? "Unknown" : x.CourseCode)
                .Select(g => new { Course = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToListAsync();

            return courseGroups.ToDictionary(x => x.Course, x => x.Count);
        }

        // Import Rules from Courses
        [HttpPost]
        public async Task<IActionResult> ImportRulesFromCourses(bool importGPA, bool importPrerequisites,
            bool importPassedHours, bool importCreditLimits, bool autoActivate = true)
        {
            if (!IsAdminUser())
            {
                return Json(new { success = false, message = "Admin access required." });
            }

            try
            {
                var courses = await _context.Courses
                    .Include(c => c.Prerequisites)
                    .ThenInclude(p => p.PrerequisiteCourse)
                    .Where(c => c.IsActive)
                    .ToListAsync();

                int importedCount = 0;
                var existingRules = await _context.RegistrationRules
                    .Where(r => r.CourseId.HasValue)
                    .ToListAsync();

                foreach (var course in courses)
                {
                    // Check if any criteria match
                    bool hasCriteria = (importGPA && course.MinGPA.HasValue) ||
                                      (importPrerequisites && course.Prerequisites.Any()) ||
                                      (importPassedHours && course.MinPassedHours.HasValue) ||
                                      (importCreditLimits && course.MaxStudents > 0 && course.MaxStudents < 1000);

                    if (!hasCriteria) continue;

                    // Check if rule already exists
                    var existingRule = existingRules.FirstOrDefault(r => r.CourseId == course.Id);

                    if (existingRule != null)
                    {
                        // Update existing rule
                        existingRule.ModifiedDate = DateTime.Now;
                        existingRule.IsActive = autoActivate;

                        if (importGPA) existingRule.MinimumGPA = course.MinGPA;
                        if (importPassedHours) existingRule.MinimumPassedHours = course.MinPassedHours;

                        // Build prerequisite description
                        if (importPrerequisites && course.Prerequisites.Any())
                        {
                            var prereqs = course.Prerequisites
                                .Select(p => p.PrerequisiteCourse?.CourseCode)
                                .Where(c => !string.IsNullOrEmpty(c))
                                .ToList();

                            if (prereqs.Any())
                            {
                                existingRule.Description = $"Prerequisites: {string.Join(", ", prereqs)}";
                            }
                        }
                    }
                    else
                    {
                        // Create new rule
                        var rule = new RegistrationRule
                        {
                            RuleName = $"{course.CourseCode} - {course.CourseName}",
                            Description = "Auto-generated from course requirements",
                            RuleType = RuleType.Prerequisite,
                            EnforcementLevel = EnforcementLevel.Block,
                            IsActive = autoActivate,
                            CourseId = course.Id,
                            CreatedDate = DateTime.Now,
                            ModifiedDate = DateTime.Now
                        };

                        if (importGPA && course.MinGPA.HasValue)
                            rule.MinimumGPA = course.MinGPA.Value;

                        if (importPassedHours && course.MinPassedHours.HasValue)
                            rule.MinimumPassedHours = course.MinPassedHours.Value;

                        // Build prerequisite description
                        if (importPrerequisites && course.Prerequisites.Any())
                        {
                            var prereqs = course.Prerequisites
                                .Select(p => p.PrerequisiteCourse?.CourseCode)
                                .Where(c => !string.IsNullOrEmpty(c))
                                .ToList();

                            if (prereqs.Any())
                            {
                                rule.Description = $"Prerequisites: {string.Join(", ", prereqs)}";
                            }
                        }

                        _context.RegistrationRules.Add(rule);
                        importedCount++;
                    }
                }

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    importedCount,
                    message = $"Imported {importedCount} rules successfully."
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = $"Error importing rules: {ex.Message}"
                });
            }
        }

        // Bulk Actions for Rules
        [HttpPost]
        public async Task<IActionResult> BulkActivateRules()
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            await _context.RegistrationRules.ForEachAsync(r => {
                r.IsActive = true;
                r.ModifiedDate = DateTime.Now;
            });
            await _context.SaveChangesAsync();

            SetSuccessMessage("All rules activated successfully.");
            return RedirectToAction(nameof(Rules));
        }

        [HttpPost]
        public async Task<IActionResult> BulkDeactivateRules()
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            await _context.RegistrationRules.ForEachAsync(r => {
                r.IsActive = false;
                r.ModifiedDate = DateTime.Now;
            });
            await _context.SaveChangesAsync();

            SetSuccessMessage("All rules deactivated successfully.");
            return RedirectToAction(nameof(Rules));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteRule(int id)
        {
            if (!IsAdminUser())
            {
                return Json(new { success = false, message = "Admin access required." });
            }

            var rule = await _context.RegistrationRules.FindAsync(id);
            if (rule != null)
            {
                _context.RegistrationRules.Remove(rule);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Rule deleted successfully." });
            }

            return Json(new { success = false, message = "Rule not found." });
        }

        // Duplicate Rule
        [HttpPost]
        public async Task<IActionResult> DuplicateRule(int id)
        {
            if (!IsAdminUser())
            {
                return Json(new { success = false, message = "Admin access required." });
            }

            var originalRule = await _context.RegistrationRules.FindAsync(id);
            if (originalRule == null)
            {
                return Json(new { success = false, message = "Rule not found." });
            }

            var newRule = new RegistrationRule
            {
                RuleName = $"{originalRule.RuleName} (Copy)",
                Description = originalRule.Description,
                RuleType = originalRule.RuleType,
                DepartmentId = originalRule.DepartmentId,
                MinimumGPA = originalRule.MinimumGPA,
                MinimumPassedHours = originalRule.MinimumPassedHours,
                MaximumCreditHours = originalRule.MaximumCreditHours,
                MinimumCreditHours = originalRule.MinimumCreditHours,
                GradeLevel = originalRule.GradeLevel,
                EnforcementLevel = originalRule.EnforcementLevel,
                IsActive = false,
                CourseId = originalRule.CourseId,
                CreatedDate = DateTime.Now,
                ModifiedDate = DateTime.Now
            };

            _context.RegistrationRules.Add(newRule);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // Validate All Rules
        [HttpPost]
        public async Task<IActionResult> ValidateAllRules()
        {
            if (!IsAdminUser())
            {
                return Json(new { success = false, message = "Admin access required." });
            }

            var rules = await _context.RegistrationRules.ToListAsync();
            var errors = new List<string>();

            foreach (var rule in rules)
            {
                // Validate each rule
                if (string.IsNullOrEmpty(rule.RuleName))
                    errors.Add($"Rule ID {rule.Id}: Missing rule name");

                if (rule.MinimumGPA.HasValue && (rule.MinimumGPA < 0 || rule.MinimumGPA > 4.0m))
                    errors.Add($"Rule '{rule.RuleName}': Invalid GPA value {rule.MinimumGPA}");

                if (rule.MaximumCreditHours.HasValue && rule.MinimumCreditHours.HasValue &&
                    rule.MaximumCreditHours < rule.MinimumCreditHours)
                    errors.Add($"Rule '{rule.RuleName}': Minimum credits ({rule.MinimumCreditHours}) exceeds maximum ({rule.MaximumCreditHours})");
            }

            return Json(new
            {
                valid = errors.Count == 0,
                errors = errors
            });
        }

        // Toggle All Rules
        [HttpPost]
        public async Task<IActionResult> ToggleAllRules()
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            var rules = await _context.RegistrationRules.ToListAsync();
            var anyActive = rules.Any(r => r.IsActive);

            foreach (var rule in rules)
            {
                rule.IsActive = !anyActive;
                rule.ModifiedDate = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            SetSuccessMessage($"All rules {(anyActive ? "deactivated" : "activated")} successfully.");
            return RedirectToAction(nameof(Rules));
        }

        // Export Rules
        public async Task<IActionResult> ExportRules(string format = "excel")
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            var rules = await _context.RegistrationRules
                .Include(r => r.Department)
                .Include(r => r.Course)
                .ToListAsync();

            if (format.ToLower() == "excel")
            {
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Rules");

                    // Headers
                    worksheet.Cells[1, 1].Value = "Rule Name";
                    worksheet.Cells[1, 2].Value = "Description";
                    worksheet.Cells[1, 3].Value = "Type";
                    worksheet.Cells[1, 4].Value = "Department";
                    worksheet.Cells[1, 5].Value = "Min GPA";
                    worksheet.Cells[1, 6].Value = "Min Hours";
                    worksheet.Cells[1, 7].Value = "Min Credits";
                    worksheet.Cells[1, 8].Value = "Max Credits";
                    worksheet.Cells[1, 9].Value = "Enforcement";
                    worksheet.Cells[1, 10].Value = "Status";

                    // Style headers
                    using (var range = worksheet.Cells[1, 1, 1, 10])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                    }

                    // Data
                    int row = 2;
                    foreach (var rule in rules)
                    {
                        worksheet.Cells[row, 1].Value = rule.RuleName;
                        worksheet.Cells[row, 2].Value = rule.Description ?? "";
                        worksheet.Cells[row, 3].Value = rule.RuleType.ToString();
                        worksheet.Cells[row, 4].Value = rule.Department?.Name ?? "";
                        worksheet.Cells[row, 5].Value = rule.MinimumGPA?.ToString("F2") ?? "";
                        worksheet.Cells[row, 6].Value = rule.MinimumPassedHours?.ToString() ?? "";
                        worksheet.Cells[row, 7].Value = rule.MinimumCreditHours?.ToString() ?? "";
                        worksheet.Cells[row, 8].Value = rule.MaximumCreditHours?.ToString() ?? "";
                        worksheet.Cells[row, 9].Value = rule.EnforcementLevel.ToString();
                        worksheet.Cells[row, 10].Value = rule.IsActive ? "Active" : "Inactive";
                        row++;
                    }

                    // Auto-fit columns
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    var fileName = $"RegistrationRules_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    var bytes = package.GetAsByteArray();

                    return File(bytes,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        fileName);
                }
            }

            SetErrorMessage("Invalid export format.");
            return RedirectToAction(nameof(Rules));
        }

        // Get Rule for Edit
        [HttpGet]
        public async Task<IActionResult> GetRuleForEdit(int id)
        {
            if (!IsAdminUser())
            {
                return Unauthorized();
            }

            var rule = await _context.RegistrationRules
                .Include(r => r.Department)
                .Include(r => r.Course)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (rule == null)
            {
                return NotFound();
            }

            return Json(new
            {
                id = rule.Id,
                ruleName = rule.RuleName,
                description = rule.Description,
                ruleType = rule.RuleType,
                departmentId = rule.DepartmentId,
                minimumGPA = rule.MinimumGPA,
                minimumPassedHours = rule.MinimumPassedHours,
                maximumCreditHours = rule.MaximumCreditHours,
                minimumCreditHours = rule.MinimumCreditHours,
                gradeLevel = rule.GradeLevel,
                enforcementLevel = rule.EnforcementLevel,
                isActive = rule.IsActive,
                courseId = rule.CourseId
            });
        }

        // Get Course Rules Preview
        [HttpGet]
        public async Task<IActionResult> GetCourseRulesPreview(bool importGPA, bool importPrerequisites, bool importPassedHours, bool importCreditLimits)
        {
            if (!IsAdminUser())
            {
                return Unauthorized();
            }

            var allCourses = await _context.Courses
                .Include(c => c.Prerequisites)
                .ThenInclude(p => p.PrerequisiteCourse)
                .ToListAsync();

            var filteredCourses = allCourses.AsEnumerable();

            if (importGPA)
                filteredCourses = filteredCourses.Where(c => c.MinGPA.HasValue);

            if (importPassedHours)
                filteredCourses = filteredCourses.Where(c => c.MinPassedHours.HasValue);

            if (importPrerequisites)
                filteredCourses = filteredCourses.Where(c => c.Prerequisites.Any());

            if (importCreditLimits)
                filteredCourses = filteredCourses.Where(c => c.MaxStudents != 1000);

            var preview = filteredCourses.Select(course => new
            {
                courseCode = course.CourseCode,
                ruleDescription = GetCourseRuleDescription(course, importGPA, importPrerequisites, importPassedHours, importCreditLimits)
            }).ToList();

            return Json(preview);
        }

        private string GetCourseRuleDescription(Course course, bool importGPA, bool importPrerequisites, bool importPassedHours, bool importCreditLimits)
        {
            var requirements = new List<string>();

            if (importGPA && course.MinGPA.HasValue)
                requirements.Add($"GPA ≥ {course.MinGPA.Value:F2}");

            if (importPassedHours && course.MinPassedHours.HasValue)
                requirements.Add($"Passed Hours ≥ {course.MinPassedHours.Value}");

            if (importPrerequisites && course.Prerequisites.Any())
            {
                var prereqs = course.Prerequisites.Select(p => p.PrerequisiteCourse?.CourseCode).Where(c => !string.IsNullOrEmpty(c));
                if (prereqs.Any())
                    requirements.Add($"Prerequisites: {string.Join(", ", prereqs)}");
            }

            if (importCreditLimits && course.MaxStudents != 1000)
                requirements.Add($"Max Students: {course.MaxStudents}");

            return string.Join("; ", requirements);
        }

        // Get Registration Stats
        [HttpGet]
        public async Task<IActionResult> GetRegistrationStats()
        {
            if (!IsAdminUser())
            {
                return Json(new { });
            }

            var currentSemester = await _context.Semesters.FirstOrDefaultAsync(s => s.IsCurrent);

            var stats = new
            {
                total = await _context.CourseRegistrations.CountAsync(),
                pending = await _context.CourseRegistrations
                    .CountAsync(r => r.Status == RegistrationStatus.Pending),
                approved = await _context.CourseRegistrations
                    .CountAsync(r => r.Status == RegistrationStatus.Approved),
                currentSemester = currentSemester != null ?
                    await _context.CourseRegistrations
                        .CountAsync(r => r.SemesterId == currentSemester.Id) : 0
            };

            return Json(stats);
        }

        // Export Analytics
        public async Task<IActionResult> ExportAnalytics(string format = "excel")
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            var analytics = await GenerateRegistrationAnalytics();

            if (format.ToLower() == "excel")
            {
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Analytics");

                    // Headers
                    worksheet.Cells[1, 1].Value = "Registration Analytics";
                    worksheet.Cells[1, 1].Style.Font.Bold = true;
                    worksheet.Cells[1, 1].Style.Font.Size = 14;

                    // Data
                    worksheet.Cells[3, 1].Value = "Total Registrations";
                    worksheet.Cells[3, 2].Value = analytics.TotalRegistrations;

                    worksheet.Cells[4, 1].Value = "Successful Registrations";
                    worksheet.Cells[4, 2].Value = analytics.SuccessfulRegistrations;

                    worksheet.Cells[5, 1].Value = "Failed Registrations";
                    worksheet.Cells[5, 2].Value = analytics.FailedRegistrations;

                    worksheet.Cells[6, 1].Value = "Success Rate";
                    worksheet.Cells[6, 2].Value = $"{analytics.SuccessRate:F1}%";

                    worksheet.Cells[8, 1].Value = "Generated Date";
                    worksheet.Cells[8, 2].Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

                    // Auto-fit columns
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    var fileName = $"RegistrationAnalytics_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    var bytes = package.GetAsByteArray();

                    return File(bytes,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        fileName);
                }
            }

            SetErrorMessage("Invalid export format.");
            return RedirectToAction(nameof(Analytics));
        }

        // Helper method for Rules view model
        private async Task<RulesManagementViewModel> GetRulesViewModel()
        {
            var rules = await _context.RegistrationRules
                .Include(r => r.Department)
                .Include(r => r.Course)
                .ToListAsync();

            var departments = await _context.Departments.ToListAsync();

            return new RulesManagementViewModel
            {
                Rules = rules,
                Departments = departments
            };
        }

        // Helper properties
        private new  bool IsStudentUser()
        {
            return HttpContext.Session.GetString("CurrentStudentId") != null;
        }

        private new  bool IsAdminUser()
        {
            return User.IsInRole("Admin") || User.IsInRole("SuperAdmin");
        }

        private new  string? CurrentStudentId => HttpContext.Session.GetString("CurrentStudentId");

        private new  IActionResult RedirectUnauthorized(string message)
        {
            SetErrorMessage(message);
            return RedirectToAction("Index", "Home");
        }

        private new void SetSuccessMessage(string message)
        {
            TempData["SuccessMessage"] = message;
        }

        private new void SetErrorMessage(string message)
        {
            TempData["ErrorMessage"] = message;
        }

        private new void SetWarningMessage(string message)
        {
            TempData["WarningMessage"] = message;
        }

        // Error view
        public IActionResult Error()
        {
            var errorViewModel = new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            };

            return View(errorViewModel);
        }

        private bool IsCurrentStudent(int studentId)
        {
            if (!IsStudentUser() || string.IsNullOrEmpty(CurrentStudentId))
                return false;

            return int.TryParse(CurrentStudentId, out int currentId) && currentId == studentId;
        }

    }
}