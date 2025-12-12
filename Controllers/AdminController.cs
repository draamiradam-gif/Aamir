using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Services;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;
using StudentManagementSystem.Models.ViewModels;
using System.Diagnostics;

namespace StudentManagementSystem.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IPermissionService _permissionService;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public AdminController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            IPermissionService permissionService,
            RoleManager<IdentityRole> roleManager,
            SignInManager<IdentityUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _permissionService = permissionService;
            _roleManager = roleManager;
            _signInManager = signInManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Dashboard()
        {
            return View();
        }

        //[HttpGet]
        //public IActionResult SystemSettings()
        //{
        //    if (!IsAdminUser())
        //    {
        //        return RedirectToAction("AccessDenied", "Home");
        //    }

        //    var roles = _roleManager.Roles.ToList();
        //    ViewBag.Roles = roles;

        //    return View();
        //}

        [HttpGet]
        public IActionResult Backup()
        {
            if (!IsAdminUser())
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            return View();
        }

        [HttpGet]
        public IActionResult Logs()
        {
            if (!IsAdminUser())
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            return View();
        }

        //[HttpGet]
        //public IActionResult Analytics()
        //{
        //    if (!IsAdminUser())
        //    {
        //        return RedirectToAction("AccessDenied", "Home");
        //    }

        //    return View();
        //}

        [HttpGet]
        public async Task<IActionResult> Analytics()
        {
            if (!IsAdminUser())
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            // Add some stats
            ViewBag.TotalUsers = await _userManager.Users.CountAsync();
            ViewBag.TotalStudents = await _context.Students.CountAsync(s => s.IsActive);
            ViewBag.TotalCourses = await _context.Courses.CountAsync(c => c.IsActive);

            return View();
        }
        private bool IsAdminUser()
        {
            return User.Identity?.IsAuthenticated == true &&
                   (User.IsInRole("Admin") || User.IsInRole("SuperAdmin"));
        }

        public async Task<IActionResult> Permissions()
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            var permissions = await _permissionService.GetAllPermissionsAsync();
            var roles = await _roleManager.Roles.ToListAsync();

            ViewBag.Roles = roles;
            return View(permissions);
        }

        [HttpPost]
        public async Task<IActionResult> AssignPermission(string roleId, int permissionId)
        {
            if (!IsAdminUser())
            {
                return Json(new { success = false, message = "Access denied" });
            }

            var result = await _permissionService.AssignPermissionToRoleAsync(roleId, permissionId);
            return Json(new { success = result, message = result ? "Permission assigned" : "Failed to assign permission" });
        }

        [HttpPost]
        public async Task<IActionResult> RemovePermission(string roleId, int permissionId)
        {
            if (!IsAdminUser())
            {
                return Json(new { success = false, message = "Access denied" });
            }

            var result = await _permissionService.RemovePermissionFromRoleAsync(roleId, permissionId);
            return Json(new { success = result, message = result ? "Permission removed" : "Failed to remove permission" });
        }

        public async Task<IActionResult> Roles()
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            var roles = await _roleManager.Roles.ToListAsync();
            return View(roles);
        }

        public async Task<IActionResult> Users()
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            var users = await _userManager.Users.ToListAsync();
            var userRoles = new List<UserRolesViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRoles.Add(new UserRolesViewModel
                {
                    UserId = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    Roles = roles.ToList()
                });
            }

            return View(userRoles);
        }

        [HttpPost]
        public async Task<IActionResult> AssignRole(string userId, string roleName)
        {
            if (!IsAdminUser())
            {
                return Json(new { success = false, message = "Access denied" });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            var result = await _userManager.AddToRoleAsync(user, roleName);
            return Json(new { success = result.Succeeded, message = result.Succeeded ? "Role assigned" : "Failed to assign role" });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveRole(string userId, string roleName)
        {
            if (!IsAdminUser())
            {
                return Json(new { success = false, message = "Access denied" });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            var result = await _userManager.RemoveFromRoleAsync(user, roleName);
            return Json(new { success = result.Succeeded, message = result.Succeeded ? "Role removed" : "Failed to remove role" });
        }

        private IActionResult RedirectUnauthorized(string message = "Access denied.")
        {
            TempData["ErrorMessage"] = message;
            return RedirectToAction("AccessDenied", "Home");
        }

        [AllowAnonymous]
        public IActionResult AdminLogin()
        {
            if (User.Identity?.IsAuthenticated == true && IsAdminUser())
            {
                return RedirectToAction("Index");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // Clear all session data
            HttpContext.Session.Clear();

            // Sign out from Identity
            await _signInManager.SignOutAsync();

            // Clear any custom session variables
            foreach (var key in HttpContext.Session.Keys)
            {
                HttpContext.Session.Remove(key);
            }

            TempData["SuccessMessage"] = "Successfully logged out from admin portal";
            return RedirectToAction("PortalAccess", "Home");
        }

        [HttpGet]
        public IActionResult Settings()
        {
            return RedirectToAction("Index", "Settings");
        }

        [HttpGet]
        [Authorize(Roles = "SuperAdmin")]
        public IActionResult SystemSettings()
        {
            return RedirectToAction("Index", "Settings");
        }

        [HttpGet]
        [Authorize(Roles = "SuperAdmin")]
        public IActionResult AdminSettings()
        {
            return RedirectToAction("SystemSettings", "Admin");
        }

        //[HttpGet]
        //public IActionResult Backup()
        //{
        //    if (!IsAdminUser())
        //    {
        //        return RedirectToAction("AccessDenied", "Home");
        //    }
        //    return View();
        //}

        //[HttpGet]
        //public IActionResult SystemSettings()
        //{
        //    if (!IsAdminUser())
        //    {
        //        return RedirectToAction("AccessDenied", "Home");
        //    }
        //    return View();
        //}

        //[HttpGet]
        //public async Task<IActionResult> Permissions()

    }

    public class UserRolesViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
    }
}