using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StudentManagementSystem.Models;
using StudentManagementSystem.Services;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace StudentManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IAdminService _adminService;

        public AccountController(
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            IAdminService adminService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _adminService = adminService;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null) // Add ? to make it nullable
        {
            ViewData["ReturnUrl"] = returnUrl ?? Url.Content("~/"); // Provide default value
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null) // Add ? to make it nullable
        {
            ViewData["ReturnUrl"] = returnUrl ?? Url.Content("~/"); // Provide default value

            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(
                    model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    // Check if user is admin and redirect accordingly
                    var user = await _userManager.FindByEmailAsync(model.Email);
                    if (user != null)
                    {
                        // Check if user has any admin privileges
                        var isSuperAdmin = await _adminService.IsSuperAdminAsync(user.Id);
                        var adminPrivilege = await _adminService.GetAdminPrivilegeAsync(user.Id);

                        if (isSuperAdmin || adminPrivilege != null)
                        {
                            return RedirectToAction("Index", "AdminManagement");
                        }
                    }

                    return RedirectToLocal(returnUrl);
                }

                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }

            return View(model);
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        //private IActionResult RedirectToLocal(string returnUrl)
        //{
        //    if (Url.IsLocalUrl(returnUrl))
        //    {
        //        return Redirect(returnUrl);
        //    }
        //    else
        //    {
        //        return RedirectToAction("Index", "Home");
        //    }
        //}
    }

    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }
}