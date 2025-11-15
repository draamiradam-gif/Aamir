using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace StudentManagementSystem.Controllers
{
    public class AdminController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;

        public AdminController(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> CreateAdmin()
        {
            var user = new IdentityUser { UserName = "admin", Email = "admin@school.edu" };
            var result = await _userManager.CreateAsync(user, "admin123");

            if (result.Succeeded)
            {
                return Content("Admin user created successfully! Username: admin, Password: admin123");
            }

            return Content($"Error creating admin: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }
}