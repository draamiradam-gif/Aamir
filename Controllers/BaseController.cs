using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StudentManagementSystem.Data;
using StudentManagementSystem.Services;




namespace StudentManagementSystem.Controllers
{
    public class BaseController : Controller
    {
        protected string CurrentLayout = "_Layout";
        protected string UserType = "Guest";
        protected int? CurrentStudentId = null;
        protected List<string> UserPermissions = new List<string>();

        // Change from private to protected and make them nullable
        protected readonly IPermissionService? _permissionService;
        protected readonly UserManager<IdentityUser>? _userManager;
        protected readonly SignInManager<IdentityUser>? _signInManager;

        // Parameterless constructor
        public BaseController()
        {
            // This constructor will be used by controllers that don't need these services
        }

        // Constructor with services
        public BaseController(
            IPermissionService permissionService,
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager)
        {
            _permissionService = permissionService;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public override async void OnActionExecuting(ActionExecutingContext context)
        {
            // Determine user type and set appropriate layout
            await DetermineUserType();
            SetLayout();

            base.OnActionExecuting(context);
        }

        private async Task DetermineUserType()
        {
            // Check if user is a student (has student session) - for session-based student login
            var studentId = HttpContext.Session.GetString("CurrentStudentId");
            if (!string.IsNullOrEmpty(studentId))
            {
                UserType = "Student";
                CurrentStudentId = int.Parse(studentId);
                return;
            }

            // Check if user is authenticated via Identity
            if (User.Identity?.IsAuthenticated == true)
            {
                // If we have the services, use permission-based system
                if (_userManager != null && _permissionService != null)
                {
                    var user = await _userManager.GetUserAsync(User);
                    if (user != null)
                    {
                        // Get user permissions
                        UserPermissions = await _permissionService.GetUserPermissionsAsync(user.Id);

                        // Determine user type based on permissions
                        if (UserPermissions.Any(p => p.StartsWith("Admin.")))
                        {
                            UserType = "Admin";
                        }
                        else if (UserPermissions.Any(p => p.StartsWith("Faculty.")))
                        {
                            UserType = "Faculty";
                        }
                        else if (UserPermissions.Any(p => p.StartsWith("Student.")))
                        {
                            UserType = "Student";
                        }
                        else
                        {
                            UserType = "User";
                        }
                        return;
                    }
                }

                // Fallback to simple role-based check
                if (User.IsInRole("Admin") || User.IsInRole("SuperAdmin"))
                {
                    UserType = "Admin";
                }
                else if (User.IsInRole("Faculty"))
                {
                    UserType = "Faculty";
                }
                else if (User.IsInRole("Student"))
                {
                    UserType = "Student";
                }
                else
                {
                    UserType = "User";
                }
            }
            else
            {
                UserType = "Guest";
            }
        }


        private void SetLayout()
        {
            switch (UserType)
            {
                case "Student":
                    CurrentLayout = "_StudentPortalLayout";
                    break;
                case "Admin":
                    CurrentLayout = "_Layout";
                    break;
                case "Faculty":
                    CurrentLayout = "_Layout";
                    break;
                case "Guest":
                default:
                    CurrentLayout = "_Layout";
                    break;
            }

            // Store in ViewData for views to use
            ViewData["Layout"] = CurrentLayout;
            ViewData["UserType"] = UserType;
            ViewData["CurrentStudentId"] = CurrentStudentId;
            ViewData["UserPermissions"] = UserPermissions;
        }


        // ✅ PERMISSION-BASED AUTHORIZATION METHODS
        protected bool HasPermission(string permissionName)
        {
            return UserPermissions.Contains(permissionName);
        }

        protected IActionResult? RedirectIfNoPermission(string permissionName, string redirectAction = "Index", string redirectController = "Home")
        {
            if (!HasPermission(permissionName))
            {
                SetErrorMessage($"Access denied. Required permission: {permissionName}");
                return RedirectToAction(redirectAction, redirectController);
            }
            return null;
        }

        protected async Task<bool> CheckPermissionAsync(string permissionName)
        {
            if (User.Identity?.IsAuthenticated != true) return false;
            if (_permissionService == null || _userManager == null) return false;

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return false;

            return await _permissionService.UserHasPermissionAsync(user.Id, permissionName);
        }



        protected bool IsAdminUser()
        {
            return UserType == "Admin" || User.IsInRole("Admin") || User.IsInRole("SuperAdmin");
        }

        protected bool IsStudentUser()
        {
            return UserType == "Student";
        }

        protected bool IsFacultyUser()
        {
            return UserType == "Faculty" || User.IsInRole("Faculty");
        }

        protected bool IsGuestUser()
        {
            return UserType == "Guest";
        }

        protected IActionResult RedirectUnauthorized(string message = "Access denied.")
        {
            SetErrorMessage(message);

            // Custom logic based on user type
            if (IsGuestUser())
            {
                // Guest users go to login
                return RedirectToAction("PortalAccess", "Home");
            }
            else if (IsStudentUser() && CurrentStudentId.HasValue)
            {
                // Students go to their dashboard
                return RedirectToAction("StudentDashboard", "Home", new { studentId = CurrentStudentId.Value });
            }
            else
            {
                // Everyone else goes to access denied
                return RedirectToAction("AccessDenied", "Home");
            }
        }


        // Global stats methods
        protected async Task SetGlobalStatsAsync()
        {
            try
            {
                var context = HttpContext.RequestServices.GetService<ApplicationDbContext>();
                if (context != null)
                {
                    ViewData["TotalStudents"] = await context.Students.CountAsync(s => s.IsActive);
                    ViewData["TotalCourses"] = await context.Courses.CountAsync(c => c.IsActive);

                    var currentSemester = await context.Semesters.FirstOrDefaultAsync(s => s.IsCurrent);
                    ViewData["ActiveSemester"] = currentSemester?.Name ?? "Not Set";

                    ViewData["TotalDepartments"] = await context.Departments.CountAsync(d => d.IsActive);
                    ViewData["TotalEnrollments"] = await context.CourseEnrollments.CountAsync(ce => ce.IsActive);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't break the page
                System.Diagnostics.Debug.WriteLine($"Error setting global stats: {ex.Message}");
            }
        }

        protected void SetGlobalStats()
        {
            try
            {
                var context = HttpContext.RequestServices.GetService<ApplicationDbContext>();
                if (context != null)
                {
                    ViewData["TotalStudents"] = context.Students.Count(s => s.IsActive);
                    ViewData["TotalCourses"] = context.Courses.Count(c => c.IsActive);

                    var currentSemester = context.Semesters.FirstOrDefault(s => s.IsCurrent);
                    ViewData["ActiveSemester"] = currentSemester?.Name ?? "Not Set";

                    ViewData["TotalDepartments"] = context.Departments.Count(d => d.IsActive);
                    ViewData["TotalEnrollments"] = context.CourseEnrollments.Count(ce => ce.IsActive);
                }
                else
                {
                    SetDefaultStats();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting global stats: {ex.Message}");
                SetDefaultStats();
            }
        }

        private void SetDefaultStats()
        {
            ViewData["TotalStudents"] = 0;
            ViewData["TotalCourses"] = 0;
            ViewData["ActiveSemester"] = "Not Available";
            ViewData["TotalDepartments"] = 0;
            ViewData["TotalEnrollments"] = 0;
            ViewData["RecentRegistrations"] = 0;
        }

        // Helper methods for messages
        protected void SetErrorMessage(string message)
        {
            TempData["Error"] = message;
        }

        protected void SetSuccessMessage(string message)
        {
            TempData["Success"] = message;
        }

        protected void SetWarningMessage(string message)
        {
            TempData["Warning"] = message;
        }

        //protected IActionResult RedirectUnauthorized(string message = "Access denied.")
        //{
        //    SetErrorMessage(message);

        //    // Smart logic: If user is student, send to student portal
        //    if (IsStudentUser() && CurrentStudentId.HasValue)
        //    {
        //        return RedirectToAction("StudentDashboard", "Registration", new { studentId = CurrentStudentId.Value });
        //    }

        //    // Default: Send to home page
        //    return RedirectToAction("Index", "Home");
        //}

        
    }
}