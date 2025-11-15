//// Controllers/StudentPortalController.cs
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using StudentManagementSystem.Data;
//using StudentManagementSystem.Models;
//using StudentManagementSystem.Services;
//using StudentManagementSystem.ViewModels;
//using System.Security.Claims;

//namespace StudentManagementSystem.Controllers
//{
//    [Authorize(Roles = "Student")]
//    public class StudentPortalController : Controller
//    {
//        private readonly ICourseRegistrationService _registrationService;
//        private readonly ApplicationDbContext _context;
//        private readonly ILogger<StudentPortalController> _logger;

//        public StudentPortalController(
//            ICourseRegistrationService registrationService,
//            ApplicationDbContext context,
//            ILogger<StudentPortalController> logger)
//        {
//            _registrationService = registrationService;
//            _context = context;
//            _logger = logger;
//        }

//        public async Task<IActionResult> Dashboard()
//        {
//            var studentId = GetCurrentStudentId();
//            if (studentId == null) return RedirectToAction("Login", "Account");

//            var student = await _context.Students
//                .Include(s => s.StudentAcademicProfile)
//                    .ThenInclude(sap => sap.Department)
//                .Include(s => s.StudentAcademicProfile)
//                    .ThenInclude(sap => sap.Branch)
//                .FirstOrDefaultAsync(s => s.Id == studentId);

//            var currentSemester = await _context.Semesters
//                .FirstOrDefaultAsync(s => s.IsCurrent);

//            var registeredCourses = await _context.StudentCourses
//                .Include(sc => sc.CourseOffering)
//                    .ThenInclude(co => co.Course)
//                .Include(sc => sc.CourseOffering)
//                    .ThenInclude(co => co.Semester)
//                .Where(sc => sc.StudentId == studentId &&
//                           sc.ApprovalStatus == ApprovalStatus.Approved &&
//                           !sc.IsCompleted)
//                .ToListAsync();

//            var viewModel = new StudentDashboardViewModel
//            {
//                Student = student,
//                CurrentSemester = currentSemester,
//                RegisteredCourses = registeredCourses,
//                CumulativeGPA = student?.StudentAcademicProfile?.CumulativeGPA ?? 0.0m,
//                TotalCreditsCompleted = student?.StudentAcademicProfile?.TotalCreditsCompleted ?? 0
//            };

//            return View(viewModel);
//        }

//        public async Task<IActionResult> CourseRegistration()
//        {
//            var studentId = GetCurrentStudentId();
//            if (studentId == null) return RedirectToAction("Login", "Account");

//            var currentSemester = await _context.Semesters
//                .FirstOrDefaultAsync(s => s.IsCurrent);

//            if (currentSemester == null)
//            {
//                TempData["ErrorMessage"] = "No active semester found.";
//                return RedirectToAction("Dashboard");
//            }

//            var availableCourses = await _registrationService.GetAvailableCourses(studentId.Value, currentSemester.Id);
//            var recommendedCourses = await _registrationService.GetRecommendedCourses(studentId.Value);

//            var viewModel = new CourseRegistrationViewModel
//            {
//                CurrentSemester = currentSemester,
//                AvailableCourses = availableCourses,
//                RecommendedCourses = recommendedCourses,
//                RegisteredCourses = await GetRegisteredCourses(studentId.Value)
//            };

//            return View(viewModel);
//        }

//        [HttpPost]
//        public async Task<IActionResult> RegisterForCourse(int courseOfferingId)
//        {
//            var studentId = GetCurrentStudentId();
//            if (studentId == null) return RedirectToAction("Login", "Account");

//            var result = await _registrationService.RegisterForCourse(studentId.Value, courseOfferingId);

//            if (result.Success)
//            {
//                TempData["SuccessMessage"] = result.Message;
//            }
//            else
//            {
//                TempData["ErrorMessage"] = result.Message;
//                if (result.Errors.Any())
//                {
//                    TempData["ValidationErrors"] = string.Join("<br>", result.Errors);
//                }
//            }

//            return RedirectToAction("CourseRegistration");
//        }

//        [HttpPost]
//        public async Task<IActionResult> DropCourse(int studentCourseId)
//        {
//            var studentId = GetCurrentStudentId();
//            if (studentId == null) return RedirectToAction("Login", "Account");

//            var result = await _registrationService.DropCourse(studentCourseId);

//            if (result.Success)
//            {
//                TempData["SuccessMessage"] = result.Message;
//            }
//            else
//            {
//                TempData["ErrorMessage"] = result.Message;
//            }

//            return RedirectToAction("CourseRegistration");
//        }

//        public async Task<IActionResult> AcademicRecord()
//        {
//            var studentId = GetCurrentStudentId();
//            if (studentId == null) return RedirectToAction("Login", "Account");

//            var completedCourses = await _context.StudentCourses
//                .Include(sc => sc.CourseOffering)
//                    .ThenInclude(co => co.Course)
//                .Include(sc => sc.CourseOffering)
//                    .ThenInclude(co => co.Semester)
//                .Where(sc => sc.StudentId == studentId &&
//                           sc.IsCompleted &&
//                           sc.ApprovalStatus == ApprovalStatus.Approved)
//                .OrderByDescending(sc => sc.CourseOffering.Semester.AcademicYear)
//                .ThenBy(sc => sc.CourseOffering.Semester.SemesterType)
//                .ToListAsync();

//            var transcript = new StudentTranscript
//            {
//                Student = await _context.Students
//                    .Include(s => s.StudentAcademicProfile)
//                        .ThenInclude(sap => sap.Department)
//                    .Include(s => s.StudentAcademicProfile)
//                        .ThenInclude(sap => sap.Branch)
//                    .FirstOrDefaultAsync(s => s.Id == studentId),
//                CompletedCourses = completedCourses,
//                CumulativeGPA = await _registrationService.CalculateGPA(studentId.Value)
//            };

//            return View(transcript);
//        }

//        private int? GetCurrentStudentId()
//        {
//            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
//            if (userId == null) return null;

//            var student = _context.Students.FirstOrDefault(s => s.UserId == userId);
//            return student?.Id;
//        }

//        private async Task<List<StudentCourse>> GetRegisteredCourses(int studentId)
//        {
//            return await _context.StudentCourses
//                .Include(sc => sc.CourseOffering)
//                    .ThenInclude(co => co.Course)
//                .Include(sc => sc.CourseOffering)
//                    .ThenInclude(co => co.Semester)
//                .Where(sc => sc.StudentId == studentId &&
//                           sc.ApprovalStatus == ApprovalStatus.Approved &&
//                           !sc.IsCompleted)
//                .ToListAsync();
//        }
//    }

//    public class StudentDashboardViewModel
//    {
//        public Student? Student { get; set; }
//        public Semester? CurrentSemester { get; set; }
//        public List<StudentCourse> RegisteredCourses { get; set; } = new();
//        public decimal CumulativeGPA { get; set; }
//        public int TotalCreditsCompleted { get; set; }
//    }

//    public class CourseRegistrationViewModel
//    {
//        public Semester? CurrentSemester { get; set; }
//        public List<CourseOffering> AvailableCourses { get; set; } = new();
//        public List<CourseOffering> RecommendedCourses { get; set; } = new();
//        public List<StudentCourse> RegisteredCourses { get; set; } = new();
//    }
//}