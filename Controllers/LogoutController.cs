// Create a new file: LogoutController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace StudentManagementSystem.Controllers
{
    public class LogoutController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;

        public LogoutController(SignInManager<IdentityUser> signInManager)
        {
            _signInManager = signInManager;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Index()
        {
            // Clear all session data
            HttpContext.Session.Clear();

            // Remove all session keys
            foreach (var key in HttpContext.Session.Keys.ToList())
            {
                HttpContext.Session.Remove(key);
            }

            // Sign out from Identity
            await _signInManager.SignOutAsync();

            // Clear authentication cookies
            Response.Cookies.Delete(".AspNetCore.Identity.Application");
            Response.Cookies.Delete(".AspNetCore.Session");

            TempData["SuccessMessage"] = "Successfully logged out";
            return RedirectToAction("PortalAccess", "Home");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForceLogout()
        {
            // This can be used for force logout when session is stuck
            HttpContext.Session.Clear();

            foreach (var cookie in Request.Cookies.Keys)
            {
                if (cookie.Contains("Identity") || cookie.Contains("Session") || cookie.Contains("Auth"))
                {
                    Response.Cookies.Delete(cookie);
                }
            }

            return RedirectToAction("PortalAccess", "Home");
        }
    }
}