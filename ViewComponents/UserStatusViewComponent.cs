using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace StudentManagementSystem.ViewComponents
{
    public class UserStatusViewComponent : ViewComponent
    {
        private readonly UserManager<IdentityUser> _userManager;

        public UserStatusViewComponent(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            var isAuthenticated = HttpContext.User.Identity?.IsAuthenticated ?? false;
            var isAdmin = false;

            if (user != null)
            {
                isAdmin = await _userManager.IsInRoleAsync(user, "Admin") ||
                         await _userManager.IsInRoleAsync(user, "SuperAdmin");
            }

            var model = new UserStatusViewModel
            {
                IsAuthenticated = isAuthenticated,
                IsAdmin = isAdmin,
                UserName = user?.UserName ?? string.Empty
            };

            return View(model);
        }
    }

    public class UserStatusViewModel
    {
        public bool IsAuthenticated { get; set; }
        public bool IsAdmin { get; set; }
        public string UserName { get; set; } = string.Empty;
    }
}