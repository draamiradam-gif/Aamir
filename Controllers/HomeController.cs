using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;
using StudentManagementSystem.Models.ViewModels;
using System.Diagnostics;
using System.Threading.Tasks;

namespace StudentManagementSystem.Controllers
{
    public class HomeController(
        ApplicationDbContext context,
        ILogger<HomeController> logger,
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager) : Controller
    {
        private readonly ApplicationDbContext _context = context;
        private readonly ILogger<HomeController> _logger = logger;
        private readonly UserManager<IdentityUser> _userManager = userManager;
        private readonly SignInManager<IdentityUser> _signInManager = signInManager;
        //private List<IdentityRole>? allRoles;

        //public async Task<IActionResult> Index()
        //{
        //    // FIXED: Proper null checking
        //    if ((User.Identity?.IsAuthenticated == false) && string.IsNullOrEmpty(HttpContext.Session.GetString("CurrentStudentId")))
        //    {
        //        return RedirectToAction("PortalAccess");
        //    }

        //    var studentId = HttpContext.Session.GetString("CurrentStudentId");
        //    if (!string.IsNullOrEmpty(studentId))
        //    {
        //        return RedirectToAction("StudentDashboard", new { studentId = studentId });
        //    }

        //    await SetGlobalStatsAsync();
        //    return View();
        //}
        public async Task<IActionResult> Index()
        {
            var users = _userManager.Users.ToList();
            var userRoles = new Dictionary<string, List<string>>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRoles[user.Id] = roles.ToList();
            }

            ViewBag.UserRoles = userRoles;
            return View(users);
        }

        [HttpGet]
        public IActionResult Create()
        {
            var roles = new List<IdentityRole>();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new IdentityUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Assign selected roles
                    if (model.SelectedRoles != null && model.SelectedRoles.Any())
                    {
                        await _userManager.AddToRolesAsync(user, model.SelectedRoles);
                    }

                    TempData["SuccessMessage"] = "User created successfully";
                    return RedirectToAction("Index");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            var roles = new List<IdentityRole>(); 
            return View(model);
        }

        private async Task<List<IdentityRole>> GetAllRolesAsync()
        {
            var roleManager = HttpContext.RequestServices.GetService<RoleManager<IdentityRole>>();
            if (roleManager == null)
                return new List<IdentityRole>();

            // If Roles is IQueryable and supports async, use ToListAsync
            // return await roleManager.Roles.ToListAsync();

            // Otherwise, use Task.Run
            return await Task.Run(() => roleManager.Roles.ToList());
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id, List<IdentityRole>? allRoles)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found";
                return RedirectToAction("Index");
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var roles = new List<IdentityRole>();

            var model = new EditUserViewModel
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                UserName = user.UserName ?? string.Empty,
                SelectedRoles = userRoles.ToList(),
                AllRoles = allRoles
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(model.Id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found";
                    return RedirectToAction("Index");
                }

                user.Email = model.Email;
                user.UserName = model.Email;

                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    // Update roles
                    var currentRoles = await _userManager.GetRolesAsync(user);
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);

                    if (model.SelectedRoles != null)
                    {
                        await _userManager.AddToRolesAsync(user, model.SelectedRoles);
                    }

                    TempData["SuccessMessage"] = "User updated successfully";
                    return RedirectToAction("Index");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            var roles = new List<IdentityRole>();
            return View(model);
        }
        public async Task<IActionResult> PortalAccess()
        {
            // If already logged in, redirect to appropriate page
            if (User.Identity?.IsAuthenticated == true)
            {
                if (IsAdminUser())
                {
                    return RedirectToAction("Index", "AdminManagement");
                }
                return RedirectToAction("Index");
            }

            // Clear any existing session data when accessing portal
            HttpContext.Session.Clear();

            // If student is logged in via session, clear it
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("CurrentStudentId")))
            {
                HttpContext.Session.Remove("CurrentStudentId");
                HttpContext.Session.Remove("StudentName");
                HttpContext.Session.Remove("UserType");
            }

            await SetGlobalStatsAsync();
            return View();
        }

        private async Task SetGlobalStatsAsync()
        {
            try
            {
                ViewBag.TotalStudents = await _context.Students.CountAsync(s => s.IsActive);
                ViewBag.TotalCourses = await _context.Courses.CountAsync(c => c.IsActive);

                var currentSemester = await _context.Semesters.FirstOrDefaultAsync(s => s.IsCurrent);
                ViewBag.CurrentSemesterName = currentSemester?.Name ?? "Not Set";

                ViewBag.TotalDepartments = await _context.Departments.CountAsync(d => d.IsActive);
                ViewBag.TotalEnrollments = await _context.CourseEnrollments.CountAsync(ce => ce.IsActive);
                ViewBag.ActiveCourses = await _context.Courses.CountAsync(c => c.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting global stats");
                SetDefaultStats();
            }
        }

        private bool IsAdminUser()
        {
            return User.IsInRole("Admin") || User.IsInRole("SuperAdmin") ||
                   HttpContext.Session.GetString("UserType") == "Admin";
        }

        private void SetDefaultStats()
        {
            ViewBag.TotalStudents = 0;
            ViewBag.TotalCourses = 0;
            ViewBag.CurrentSemesterName = "Not Available";
            ViewBag.TotalDepartments = 0;
            ViewBag.TotalEnrollments = 0;
            ViewBag.ActiveCourses = 0;
        }

        private void SetErrorMessage(string message)
        {
            TempData["ErrorMessage"] = message;
        }

        private void SetSuccessMessage(string message)
        {
            TempData["SuccessMessage"] = message;
        }

        public async Task<IActionResult> Hierarchy(string scope = "all")
        {
            try
            {
                var universities = await _context.Universities
                    .Include(u => u.Colleges)
                        .ThenInclude(c => c.Departments)
                            .ThenInclude(d => d.Branches)
                    .Where(u => u.IsActive)
                    .OrderBy(u => u.Name)
                    .ToListAsync();

                var totalColleges = universities.Sum(u => u.Colleges.Count);
                var totalDepartments = universities.Sum(u => u.Colleges.Sum(c => c.Departments.Count));
                var totalBranches = universities.Sum(u => u.Colleges.Sum(c => c.Departments.Sum(d => d.Branches.Count)));
                var totalSemesters = await _context.Semesters.CountAsync(s => s.IsActive);
                var totalStudents = await _context.Students.CountAsync();
                var totalCourses = await _context.Courses.CountAsync(c => c.IsActive);

                var viewModel = new UniversityHierarchyViewModel
                {
                    Universities = universities,
                    TotalUniversities = universities.Count,
                    TotalColleges = totalColleges,
                    TotalDepartments = totalDepartments,
                    TotalBranches = totalBranches,
                    TotalSemesters = totalSemesters,
                    TotalStudents = totalStudents,
                    TotalCourses = totalCourses,
                    AccessScope = scope
                };

                await SetGlobalStatsAsync();
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading hierarchy");
                await SetGlobalStatsAsync();
                return View(new UniversityHierarchyViewModel());
            }
        }

        public async Task<IActionResult> Privacy()
        {
            await SetGlobalStatsAsync();
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }

        public async Task<IActionResult> AcademicCalendar()
        {
            await SetGlobalStatsAsync();

            var currentYear = DateTime.Now.Year;
            var semesters = await _context.Semesters
                .Where(s => s.StartDate.Year == currentYear || s.EndDate.Year == currentYear)
                .OrderBy(s => s.StartDate)
                .ToListAsync();

            ViewBag.Semesters = semesters;
            ViewBag.CurrentYear = currentYear;

            return View();
        }

        public async Task<IActionResult> FacultyDirectory()
        {
            await SetGlobalStatsAsync();

            var facultyMembers = new List<dynamic>
            {
                new { Name = "Dr. Sarah Johnson", Department = "Computer Science", Email = "sarah.johnson@university.edu", Phone = "(555) 123-4567", Office = "CS Building 301" },
                new { Name = "Dr. Michael Chen", Department = "Mathematics", Email = "michael.chen@university.edu", Phone = "(555) 123-4568", Office = "Math Building 205" },
                new { Name = "Dr. Emily Davis", Department = "Physics", Email = "emily.davis@university.edu", Phone = "(555) 123-4569", Office = "Science Building 102" },
                new { Name = "Dr. Robert Wilson", Department = "English", Email = "robert.wilson@university.edu", Phone = "(555) 123-4570", Office = "Humanities Building 415" }
            };

            ViewBag.FacultyMembers = facultyMembers;
            return View();
        }

        public async Task<IActionResult> Library()
        {
            await SetGlobalStatsAsync();
            return View();
        }

        public async Task<IActionResult> Reports()
        {
            if (!IsAdminUser())
            {
                SetErrorMessage("Access denied. Admin privileges required.");
                return RedirectToAction("Index");
            }

            await SetGlobalStatsAsync();

            var reportData = new
            {
                TotalStudents = await _context.Students.CountAsync(),
                ActiveCourses = await _context.Courses.CountAsync(c => c.IsActive),
                TotalEnrollments = await _context.CourseEnrollments.CountAsync(),
                RecentGrades = await _context.FinalGrades.CountAsync(fg => fg.CalculationDate >= DateTime.Now.AddMonths(-1)),
                PendingRegistrations = 0
            };

            ViewBag.ReportData = reportData;
            return View();
        }

        public IActionResult AccessDenied()
        {
            Response.StatusCode = 403;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminLogout()
        {
            // Clear all session data
            HttpContext.Session.Clear();

            // Remove specific admin keys
            HttpContext.Session.Remove("AdminUser");
            HttpContext.Session.Remove("AdminName");
            HttpContext.Session.Remove("UserType");

            // Sign out from Identity
            await _signInManager.SignOutAsync();

            TempData["SuccessMessage"] = "Successfully logged out";
            return RedirectToAction("PortalAccess");
        }

        public IActionResult StudentPortalEntry()
        {
            return View();
        }

        [HttpPost]
        public IActionResult StudentPortalEntry(string studentId, string password)
        {
            if (IsValidStudent(studentId, password))
            {
                HttpContext.Session.SetString("CurrentStudentId", studentId);
                HttpContext.Session.SetString("StudentName", GetStudentName(studentId));
                HttpContext.Session.SetString("UserType", "Student");

                SetSuccessMessage("Student login successful");
                return RedirectToAction("StudentDashboard", new { studentId });
            }

            SetErrorMessage("Invalid student ID or password");
            return View();
        }

        public async Task<IActionResult> StudentDashboard(string studentId)
        {
            if (string.IsNullOrEmpty(studentId) || HttpContext.Session.GetString("CurrentStudentId") != studentId)
            {
                return RedirectToAction("StudentPortalEntry");
            }

            ViewData["CurrentStudentId"] = studentId;
            ViewData["UserType"] = "Student";
            await SetGlobalStatsAsync();
            return View();
        }

        public async Task<IActionResult> StudentPortal(string studentId)
        {
            if (string.IsNullOrEmpty(studentId) || HttpContext.Session.GetString("CurrentStudentId") != studentId)
            {
                return RedirectToAction("StudentPortalEntry");
            }

            ViewData["CurrentStudentId"] = studentId;
            ViewData["UserType"] = "Student";
            await SetGlobalStatsAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult StudentLogout()
        {
            // Clear all session data
            HttpContext.Session.Clear();

            // Remove specific keys
            HttpContext.Session.Remove("CurrentStudentId");
            HttpContext.Session.Remove("StudentName");
            HttpContext.Session.Remove("UserType");

            // Sign out from Identity (if applicable)
            _signInManager.SignOutAsync().Wait();

            TempData["SuccessMessage"] = "Successfully logged out";
            return RedirectToAction("PortalAccess");
        }

        public async Task<IActionResult> Management()
        {
            if (!IsAdminUser())
            {
                SetErrorMessage("Access denied. Admin privileges required.");
                return RedirectToAction("Index");
            }
            await SetGlobalStatsAsync();
            return View();
        }

        public async Task<IActionResult> Rules()
        {
            if (!IsAdminUser())
            {
                SetErrorMessage("Access denied. Admin privileges required.");
                return RedirectToAction("Index");
            }
            await SetGlobalStatsAsync();
            return View();
        }

        public async Task<IActionResult> Periods()
        {
            if (!IsAdminUser())
            {
                SetErrorMessage("Access denied. Admin privileges required.");
                return RedirectToAction("Index");
            }
            await SetGlobalStatsAsync();
            return View();
        }

        private bool IsValidAdmin(string username, string password)
        {
            if (username == "superadmin@localhost" && password == "SuperAdmin123!")
                return true;

            var validAdmins = new Dictionary<string, string>
            {
                { "admin", "admin123" },
                { "test", "test" }
            };

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return false;

            var cleanUsername = username.Trim().ToLower();

            return validAdmins.TryGetValue(cleanUsername, out var expectedPassword)
                   && expectedPassword == password;
        }

        private bool IsValidStudent(string studentId, string password)
        {
            if (string.IsNullOrWhiteSpace(studentId) || string.IsNullOrWhiteSpace(password))
            {
                return false;
            }

            var validStudents = new Dictionary<string, string>
            {
                { "s12345", "password123" },
                { "s67890", "password456" },
                { "test", "test" }
            };

            var cleanStudentId = studentId.Trim().ToLower();

            if (validStudents.TryGetValue(cleanStudentId, out var storedPassword))
            {
                return storedPassword == password;
            }

            return false;
        }

        private string GetStudentName(string studentId)
        {
            var studentNames = new Dictionary<string, string>
            {
                { "s12345", "John Smith" },
                { "s67890", "Jane Doe" },
                { "test", "Test Student" }
            };

            return studentNames.TryGetValue(studentId.Trim().ToLower(), out var name)
                ? name
                : "Student";
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult AdminLogin()
        {
            if (User.Identity?.IsAuthenticated == true && IsAdminUser())
            {
                return RedirectToAction("Index", "AdminManagement");
            }
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> AdminLogin(string email, string password, bool rememberMe = false)
        {
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(email, password, rememberMe, false);

                if (result.Succeeded)
                {
                    var user = await _userManager.FindByEmailAsync(email);
                    if (user != null)
                    {
                        var isAdmin = await _userManager.IsInRoleAsync(user, "Admin") ||
                                     await _userManager.IsInRoleAsync(user, "SuperAdmin") ||
                                     await _userManager.IsInRoleAsync(user, "UniversityAdmin") ||
                                     await _userManager.IsInRoleAsync(user, "FacultyAdmin") ||
                                     await _userManager.IsInRoleAsync(user, "DepartmentAdmin");

                        var hasAdminPrivilege = await _context.AdminPrivileges
                            .AnyAsync(ap => ap.AdminId == user.Id && ap.IsActive);

                        if (isAdmin || hasAdminPrivilege)
                        {
                            SetSuccessMessage("Admin login successful!");
                            return RedirectToAction("Index", "AdminManagement");
                        }
                        else
                        {
                            await _signInManager.SignOutAsync();
                            SetErrorMessage("User does not have admin privileges.");
                        }
                    }
                }
                else
                {
                    SetErrorMessage("Invalid login attempt.");
                }
            }

            return View();
        }

        [Route("/test-db")]
        public async Task<IActionResult> TestDatabase()
        {
            try
            {
                var canConnect = await _context.Database.CanConnectAsync();
                return Json(new
                {
                    Success = true,
                    Connected = canConnect,
                    Message = canConnect ? "Database connected successfully" : "Database connection failed"
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    Success = false,
                    Connected = false,
                    Message = ex.Message,
                    StackTrace = ex.StackTrace
                });
            }
        }

        [Route("/test")]
        public IActionResult Test()
        {
            return Content("✅ Application is running!<br>" +
                          "✅ HomeController is working<br>" +
                          $"✅ Time: {DateTime.Now}<br>" +
                          $"✅ User: {User.Identity?.Name ?? "Not logged in"}<br>" +
                          "<a href='/'>Go to Home</a> | " +
                          "<a href='/Home/PortalAccess'>Go to Portal Access</a>",
                          "text/html");
        }

        [Route("/routes")]
        public IActionResult ListRoutes()
        {
            var routes = new List<string>
    {
        "/",
        "/Home/PortalAccess",
        "/Home/AdminLogin",
        "/Home/StudentPortalEntry",
        "/test",
        "/health",
        "/debug/db"
    };

            var html = "<h1>Available Routes</h1><ul>";
            foreach (var route in routes)
            {
                html += $"<li><a href='{route}'>{route}</a></li>";
            }
            html += "</ul>";

            return Content(html, "text/html");
        }
    }
}