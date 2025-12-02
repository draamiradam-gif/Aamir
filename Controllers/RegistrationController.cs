using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;
using StudentManagementSystem.Services;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text;
using System.Globalization;

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

        // Student Registration Portal
        public async Task<IActionResult> StudentPortal(int studentId)
        {
            if (!IsStudentUser() || CurrentStudentId != studentId)
            {
                return RedirectUnauthorized("Student access required.");
            }

            // Simple student access check
            if (!IsStudentUser() || CurrentStudentId != studentId)
            {
                SetErrorMessage("Invalid student access.");
                return RedirectToAction("StudentPortalEntry");
            }

            var student = await _context.Students
                .Include(s => s.StudentSemester)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null)
            {
                return NotFound();
            }

            // Set view data for student portal layout
            ViewBag.StudentName = HttpContext.Session.GetString("StudentName");
            ViewBag.CurrentGPA = student.GPA;
            ViewBag.PassedHours = student.PassedHours;
            ViewBag.CurrentCoursesCount = await _context.CourseEnrollments
                .CountAsync(ce => ce.StudentId == studentId && ce.IsActive && !ce.IsCompleted);

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

            var viewModel = new StudentPortalViewModel
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
        public async Task<IActionResult> RegisterCourses(RegistrationRequest request)
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

        // Admin Registration Management
        //public async Task<IActionResult> Management(int semesterId = 0)
        //{
        //    // Simple admin check
        //    if (!IsAdminUser())
        //    {
        //        return RedirectUnauthorized("Admin access required.");
        //    }

        //    var semesters = await _context.Semesters
        //        .Where(s => s.IsActive)
        //        .ToListAsync();

        //    var currentSemester = semesterId == 0
        //        ? await _context.Semesters.FirstOrDefaultAsync(s => s.IsCurrent)
        //        : await _context.Semesters.FindAsync(semesterId);

        //    if (currentSemester == null && semesters.Any())
        //    {
        //        currentSemester = semesters.First();
        //    }

        //    var registrations = currentSemester != null
        //        ? await _context.CourseRegistrations
        //            .Include(r => r.Student)
        //            .Include(r => r.Course)
        //            .Where(r => r.SemesterId == currentSemester.Id)
        //            .ToListAsync()
        //        : new List<CourseRegistration>();

        //    var viewModel = new RegistrationManagementViewModel
        //    {
        //        Semesters = semesters,
        //        SelectedSemester = currentSemester,
        //        Registrations = registrations,
        //        PendingCount = registrations.Count(r => r.Status == RegistrationStatus.Pending),
        //        ApprovedCount = registrations.Count(r => r.Status == RegistrationStatus.Approved),
        //        TotalCount = registrations.Count
        //    };

        //    return View(viewModel);
        //}

        // Approve/Reject Registration
        [HttpPost]
        public async Task<IActionResult> ApproveRegistration(int registrationId, int semesterId)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            var result = await _registrationService.ApproveRegistration(registrationId, User.Identity?.Name ?? "Admin");

            if (result)
            {
                SetSuccessMessage("Registration approved successfully.");
            }
            else
            {
                SetErrorMessage("Failed to approve registration.");
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

        // Registration Rules Management
        public async Task<IActionResult> Rules()
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            var rules = await _context.RegistrationRules
                .Include(r => r.Department)
                .ToListAsync();

            var departments = await _context.Departments.ToListAsync();

            var viewModel = new RulesManagementViewModel
            {
                Rules = rules,
                Departments = departments
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> SaveRule(RegistrationRule rule)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            if (ModelState.IsValid)
            {
                if (rule.Id == 0)
                {
                    _context.RegistrationRules.Add(rule);
                }
                else
                {
                    _context.RegistrationRules.Update(rule);
                }

                await _context.SaveChangesAsync();
                SetSuccessMessage("Registration rule saved successfully.");
            }
            else
            {
                SetErrorMessage("Please correct the errors below.");
            }

            return RedirectToAction(nameof(Rules));
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

            var viewModel = new PeriodsManagementViewModel
            {
                Periods = periods,
                Semesters = semesters
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> SavePeriod(RegistrationPeriod period)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            if (ModelState.IsValid)
            {
                if (period.Id == 0)
                {
                    _context.RegistrationPeriods.Add(period);
                }
                else
                {
                    _context.RegistrationPeriods.Update(period);
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
                await _context.SaveChangesAsync();
                SetSuccessMessage($"Rule {(rule.IsActive ? "activated" : "deactivated")} successfully.");
            }
            return RedirectToAction(nameof(Rules));
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
                await _context.SaveChangesAsync();
                SetSuccessMessage($"Period {(period.IsActive ? "activated" : "deactivated")} successfully.");
            }
            return RedirectToAction(nameof(Periods));
        }

        // Student Dashboard
        public async Task<IActionResult> StudentDashboard(int studentId)
        {
            // Simple student access check
            if (!IsStudentUser() || CurrentStudentId != studentId)
            {
                SetErrorMessage("Access denied. Please log in again.");
                return RedirectToAction("StudentPortalEntry");
            }

            var student = await _context.Students
                .Include(s => s.CourseEnrollments)
                .ThenInclude(ce => ce.Course)
                .Include(s => s.StudentDepartment)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null)
            {
                SetErrorMessage("Student not found.");
                return RedirectToAction("StudentPortalEntry");
            }

            // Set view data for layout
            ViewBag.StudentName = student.Name;
            ViewBag.CurrentGPA = student.GPA;
            ViewBag.PassedHours = student.PassedHours;
            ViewBag.CurrentCoursesCount = student.CourseEnrollments.Count(ce => ce.IsActive && !ce.IsCompleted);

            return View(student);
        }

        public IActionResult StudentLogout()
        {
            HttpContext.Session.Remove("CurrentStudentId");
            HttpContext.Session.Remove("StudentName");
            HttpContext.Session.Remove("StudentGPA");

            SetSuccessMessage("You have been logged out successfully.");
            return RedirectToAction("Index", "Home");
        }

        // My Grades
        public async Task<IActionResult> MyGrades(int studentId, int? semesterId)
        {
            // Simple student access check
            if (!IsStudentUser() || CurrentStudentId != studentId)
            {
                SetErrorMessage("Invalid student access.");
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

            // Set view data for student portal layout
            ViewBag.StudentName = HttpContext.Session.GetString("StudentName");
            ViewBag.CurrentGPA = student.GPA;
            ViewBag.PassedHours = student.PassedHours;
            ViewBag.CurrentCoursesCount = await _context.CourseEnrollments
                .CountAsync(ce => ce.StudentId == studentId && ce.IsActive && !ce.IsCompleted);

            ViewBag.Student = student;
            ViewBag.Semesters = semesters;
            ViewBag.SelectedSemesterId = semesterId;
            ViewBag.SemesterGPA = semesterGPA;

            return View(grades);
        }

        // My Schedule
        public async Task<IActionResult> MySchedule(int studentId)
        {
            // Simple student access check
            if (!IsStudentUser() || CurrentStudentId != studentId)
            {
                SetErrorMessage("Invalid student access.");
                return RedirectToAction("StudentPortalEntry");
            }

            var student = await _context.Students.FindAsync(studentId);
            if (student == null) return NotFound();

            // Set view data for student portal layout
            ViewBag.StudentName = HttpContext.Session.GetString("StudentName");
            ViewBag.CurrentGPA = student.GPA;
            ViewBag.PassedHours = student.PassedHours;
            ViewBag.CurrentCoursesCount = await _context.CourseEnrollments
                .CountAsync(ce => ce.StudentId == studentId && ce.IsActive && !ce.IsCompleted);

            // Mock schedule data
            var currentSchedule = new List<dynamic>
            {
                new { CourseCode = "MATH101", CourseName = "Calculus I", Days = "MWF", StartTime = "9:00 AM", EndTime = "9:50 AM", Room = "Science Bldg 101", Instructor = "Dr. Smith", Credits = 3 },
                new { CourseCode = "PHYS201", CourseName = "Physics I", Days = "TTH", StartTime = "10:00 AM", EndTime = "11:15 AM", Room = "Science Bldg 205", Instructor = "Dr. Johnson", Credits = 4 },
                new { CourseCode = "CS101", CourseName = "Introduction to Programming", Days = "MWF", StartTime = "1:00 PM", EndTime = "1:50 PM", Room = "CS Bldg 101", Instructor = "Dr. Wilson", Credits = 3 },
                new { CourseCode = "ENG102", CourseName = "Composition II", Days = "TTH", StartTime = "2:00 PM", EndTime = "3:15 PM", Room = "Humanities 302", Instructor = "Dr. Brown", Credits = 3 }
            };

            // Mock weekly schedule for calendar view
            var weeklySchedule = new List<dynamic>
            {
                new { Day = "Monday", CourseCode = "MATH101", CourseName = "Calculus I", StartHour = 9, EndHour = 10, Room = "SCI 101", Instructor = "Dr. Smith" },
                new { Day = "Monday", CourseCode = "CS101", CourseName = "Intro to Programming", StartHour = 13, EndHour = 14, Room = "CS 101", Instructor = "Dr. Wilson" },
                new { Day = "Tuesday", CourseCode = "PHYS201", CourseName = "Physics I", StartHour = 10, EndHour = 12, Room = "SCI 205", Instructor = "Dr. Johnson" },
                new { Day = "Tuesday", CourseCode = "ENG102", CourseName = "Composition II", StartHour = 14, EndHour = 16, Room = "HUM 302", Instructor = "Dr. Brown" },
                new { Day = "Wednesday", CourseCode = "MATH101", CourseName = "Calculus I", StartHour = 9, EndHour = 10, Room = "SCI 101", Instructor = "Dr. Smith" },
                new { Day = "Wednesday", CourseCode = "CS101", CourseName = "Intro to Programming", StartHour = 13, EndHour = 14, Room = "CS 101", Instructor = "Dr. Wilson" },
                new { Day = "Thursday", CourseCode = "PHYS201", CourseName = "Physics I", StartHour = 10, EndHour = 12, Room = "SCI 205", Instructor = "Dr. Johnson" },
                new { Day = "Thursday", CourseCode = "ENG102", CourseName = "Composition II", StartHour = 14, EndHour = 16, Room = "HUM 302", Instructor = "Dr. Brown" },
                new { Day = "Friday", CourseCode = "MATH101", CourseName = "Calculus I", StartHour = 9, EndHour = 10, Room = "SCI 101", Instructor = "Dr. Smith" }
            };

            ViewBag.Student = student;
            ViewBag.CurrentSchedule = currentSchedule;
            ViewBag.WeeklySchedule = weeklySchedule;

            return View(student);
        }

        // Unofficial Transcript
        public async Task<IActionResult> Transcript(int studentId)
        {
            // Simple student access check
            if (!IsStudentUser() || CurrentStudentId != studentId)
            {
                SetErrorMessage("Invalid student access.");
                return RedirectToAction("StudentPortalEntry");
            }

            var student = await _context.Students.FindAsync(studentId);
            if (student == null) return NotFound();

            // Set view data for student portal layout
            ViewBag.StudentName = HttpContext.Session.GetString("StudentName");
            ViewBag.CurrentGPA = student.GPA;
            ViewBag.PassedHours = student.PassedHours;
            ViewBag.CurrentCoursesCount = await _context.CourseEnrollments
                .CountAsync(ce => ce.StudentId == studentId && ce.IsActive && !ce.IsCompleted);

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

        // Helper method for calculating semester GPA
        public decimal CalculateSemesterGPA(List<StudentManagementSystem.Models.FinalGrade> grades)
        {
            var completedGrades = grades.Where(g => g.GradeStatus == GradeStatus.Completed && g.FinalGradePoints.HasValue).ToList();
            if (!completedGrades.Any()) return 0;

            var totalPoints = completedGrades.Sum(g => (g.Course?.Credits ?? 0) * (g.FinalGradePoints ?? 0m));
            var totalCredits = completedGrades.Sum(g => g.Course?.Credits ?? 0);

            return totalCredits > 0 ? totalPoints / totalCredits : 0;
        }

        // Academic Progress
        public async Task<IActionResult> AcademicProgress(int studentId)
        {
            // Simple student access check
            if (!IsStudentUser() || CurrentStudentId != studentId)
            {
                SetErrorMessage("Invalid student access.");
                return RedirectToAction("StudentPortalEntry");
            }

            var student = await _context.Students.FindAsync(studentId);
            if (student == null) return NotFound();

            // Set view data for student portal layout
            ViewBag.StudentName = HttpContext.Session.GetString("StudentName");
            ViewBag.CurrentGPA = student.GPA;
            ViewBag.PassedHours = student.PassedHours;
            ViewBag.CurrentCoursesCount = await _context.CourseEnrollments
                .CountAsync(ce => ce.StudentId == studentId && ce.IsActive && !ce.IsCompleted);

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

        // Course Materials
        public async Task<IActionResult> CourseMaterials(int studentId)
        {
            // Simple student access check
            if (!IsStudentUser() || CurrentStudentId != studentId)
            {
                SetErrorMessage("Invalid student access.");
                return RedirectToAction("StudentPortalEntry");
            }

            var student = await _context.Students.FindAsync(studentId);
            if (student == null) return NotFound();

            // Set view data for student portal layout
            ViewBag.StudentName = HttpContext.Session.GetString("StudentName");
            ViewBag.CurrentGPA = student.GPA;
            ViewBag.PassedHours = student.PassedHours;
            ViewBag.CurrentCoursesCount = await _context.CourseEnrollments
                .CountAsync(ce => ce.StudentId == studentId && ce.IsActive && !ce.IsCompleted);

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
                                         (s.Email == email || s.StudentId == studentId)); // Simple validation

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

        public IActionResult Error()
        {
            // Make sure you're using the Models namespace
            var errorViewModel = new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            };

            return View(errorViewModel);
        }
        /////////////
        ///
        public async Task<IActionResult> Management(int semesterId = 0, string filter = "all", string sortBy = "date", string sortOrder = "desc")
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
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

            var viewModel = new RegistrationManagementViewModel
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


        // NEW: Export Registrations
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
                var excelBytes = await _exportService.ExportRegistrationsToExcel(registrations);
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Registrations_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            }
            else if (format.ToLower() == "csv")
            {
                var csvBytes = await _exportService.ExportRegistrationsToCsv(registrations);
                return File(csvBytes, "text/csv", $"Registrations_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            }
            else if (format.ToLower() == "pdf")
            {
                var pdfBytes = await _exportService.ExportRegistrationsToPdf(registrations);
                return File(pdfBytes, "application/pdf", $"Registrations_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            }

            SetErrorMessage("Invalid export format.");
            return RedirectToAction(nameof(Management), new { semesterId });
        }

        // NEW: Import Registrations
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

            try
            {
                var result = await _importService.ImportRegistrations(file, semesterId, overwriteExisting);

                if (result.Success)
                {
                    SetSuccessMessage($"Successfully imported {result.ImportedCount} registrations. {result.SkippedCount} skipped.");

                    if (result.Errors.Any())
                    {
                        var errorSummary = string.Join("; ", result.Errors.Take(5));
                        SetWarningMessage($"Some records had issues: {errorSummary}");
                    }
                }
                else
                {
                    SetErrorMessage($"Import failed: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                SetErrorMessage($"Import error: {ex.Message}");
            }

            return RedirectToAction(nameof(Management), new { semesterId });
        }

        // NEW: Download Import Template
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

        // NEW: Bulk Operations
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

        // NEW: Student Registration History
        public async Task<IActionResult> StudentHistory(int studentId)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            var student = await _context.Students
                .Include(s => s.CourseEnrollments)
                .ThenInclude(ce => ce.Course)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null)
            {
                SetErrorMessage("Student not found.");
                return RedirectToAction("Index", "Students");
            }

            var registrationHistory = await _context.CourseRegistrations
                .Include(r => r.Course)
                .Include(r => r.Semester)
                .Where(r => r.StudentId == studentId)
                .OrderByDescending(r => r.Semester != null ? r.Semester.StartDate : DateTime.MinValue)
                .ToListAsync();

            var viewModel = new StudentRegistrationHistoryViewModel
            {
                Student = student,
                RegistrationHistory = registrationHistory
            };

            return View(viewModel);
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

        // ... other helper methods for analytics ...

        // NEW: Enhanced View Models
        public class RegistrationReportViewModel
        {
            public List<Semester> Semesters { get; set; } = new List<Semester>();
            public Semester? SelectedSemester { get; set; }
            public RegistrationReportData ReportData { get; set; } = new RegistrationReportData();
        }

        public class StudentRegistrationHistoryViewModel
        {
            public Student Student { get; set; } = new Student();
            public List<CourseRegistration> RegistrationHistory { get; set; } = new List<CourseRegistration>();
        }

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

        // NEW: Analytics Page
        public async Task<IActionResult> Analytics()
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            var analytics = await GenerateRegistrationAnalytics();
            return View(analytics);
        }

        // NEW: Reports Page
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

            var viewModel = new RegistrationReportViewModel
            {
                Semesters = semesters,
                SelectedSemester = currentSemester,
                ReportData = reportData
            };

            return View(viewModel);
        }

        // Helper method for analytics
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
                (decimal)studentCourseCounts.Average() : 0; // Fixed double to decimal conversion

            // Status distribution
            var statusGroups = await _context.CourseRegistrations
                .Where(r => r.SemesterId == semesterId)
                .GroupBy(r => r.Status)
                .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                .ToListAsync();

            reportData.StatusDistribution = statusGroups.ToDictionary(g => g.Status, g => g.Count);

            return reportData;
        }
    }
    
}
