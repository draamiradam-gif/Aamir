using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace StudentManagementSystem.Controllers
{
    public class TestController : BaseController
    {
        private new readonly UserManager<IdentityUser> _userManager;

        public TestController(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            return Json(new
            {
                UserCount = users.Count,
                Users = users.Select(u => new { u.Id, u.UserName, u.Email })
            });
        }

        [Authorize(Roles = "Admin")]
        public IActionResult AdminTest()
        {
            return Json(new { Message = "Admin access granted!" });
        }
    }
}