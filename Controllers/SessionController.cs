using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;
using System.Linq;
using System.Threading.Tasks;

namespace StudentManagementSystem.Controllers
{
    [Authorize(Policy = "SuperAdminOnly")]
    public class SessionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SessionController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> ActiveSessions()
        {
            // Simulate loading active sessions
            await Task.Delay(100);

            var sessions = new List<ActiveSessionViewModel>
            {
                new() { SessionId = "1", UserName = "superadmin@localhost", LoginTime = DateTime.Now.AddHours(-1), IpAddress = "192.168.1.100" },
                new() { SessionId = "2", UserName = "admin@localhost", LoginTime = DateTime.Now.AddMinutes(-30), IpAddress = "192.168.1.101" }
            };

            return View(sessions);
        }

        [HttpPost]
        public async Task<IActionResult> TerminateSession(string sessionId)
        {
            try
            {
                // Simulate session termination
                await Task.Delay(100);

                return Json(new { success = true, message = $"Session {sessionId} terminated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Failed to terminate session: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> TerminateAllSessions()
        {
            try
            {
                // Simulate terminating all sessions
                await Task.Delay(200);

                return Json(new { success = true, message = "All sessions terminated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Failed to terminate sessions: {ex.Message}" });
            }
        }
    }

    public class ActiveSessionViewModel
    {
        public string SessionId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime LoginTime { get; set; }
        public string IpAddress { get; set; } = string.Empty;
    }
}