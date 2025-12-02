using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;
using StudentManagementSystem.Services;
using System.Linq;
using System.Threading.Tasks;

namespace StudentManagementSystem.Controllers
{
    [Authorize(Policy = "SuperAdminOnly")]
    public class SystemController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public SystemController(IConfiguration configuration, ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _configuration = configuration;
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Settings()
        {
            var model = new SystemSettingsViewModel
            {
                AdminSessionTimeout = _configuration.GetValue<int>("AdminSettings:SessionTimeout", 60),
                MaxLoginAttempts = _configuration.GetValue<int>("AdminSettings:MaxLoginAttempts", 5),
                Enable2FA = _configuration.GetValue<bool>("AdminSettings:Enable2FA", true),
                RequireStrongPasswords = _configuration.GetValue<bool>("AdminSettings:RequireStrongPasswords", true),
                AutoLogoutInactive = _configuration.GetValue<bool>("AdminSettings:AutoLogoutInactive", true),
                EmailNotifications = _configuration.GetValue<bool>("AdminSettings:EmailNotifications", true),
                AuditLogRetentionDays = _configuration.GetValue<int>("AdminSettings:AuditLogRetentionDays", 365)
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSettings(SystemSettingsViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Simulate saving settings
                    await Task.Delay(100); // Small delay to make it truly async

                    TempData["SuccessMessage"] = "System settings updated successfully!";
                    return RedirectToAction("Settings");
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Failed to update settings: {ex.Message}";
                }
            }

            return View("Settings", model);
        }

        public async Task<IActionResult> BackupDatabase()
        {
            try
            {
                // Simulate backup process
                await Task.Delay(2000); // Simulate backup time

                return Json(new { success = true, message = "Database backup completed successfully", timestamp = DateTime.Now });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Backup failed: {ex.Message}" });
            }
        }

        public async Task<IActionResult> SystemHealth()
        {
            var healthInfo = new SystemHealthViewModel
            {
                DatabaseStatus = await CheckDatabaseStatus(),
                ServerTime = DateTime.Now,
                Uptime = TimeSpan.FromDays(30), // Example
                ActiveAdmins = await _context.AdminPrivileges.CountAsync(a => a.IsActive),
                PendingApplications = await _context.AdminApplications.CountAsync(a => a.Status == ApplicationStatus.Pending),
                TotalUsers = await _userManager.Users.CountAsync() // Fixed: Use UserManager instead of DbSet
            };

            return View(healthInfo);
        }

        public async Task<IActionResult> Logs()
        {
            // Simulate loading logs with async operation
            await Task.Delay(100);

            var logs = new List<SystemLogViewModel>
            {
                new() { Timestamp = DateTime.Now.AddHours(-1), Level = "INFO", Message = "System started successfully" },
                new() { Timestamp = DateTime.Now.AddHours(-2), Level = "WARNING", Message = "High memory usage detected" },
                new() { Timestamp = DateTime.Now.AddHours(-3), Level = "ERROR", Message = "Failed to send email notification" }
            };

            return View(logs);
        }

        private async Task<string> CheckDatabaseStatus()
        {
            try
            {
                var canConnect = await _context.Database.CanConnectAsync();
                return canConnect ? "Healthy" : "Unavailable";
            }
            catch
            {
                return "Error";
            }
        }
    }

    public class SystemHealthViewModel
    {
        public string DatabaseStatus { get; set; } = string.Empty;
        public DateTime ServerTime { get; set; }
        public TimeSpan Uptime { get; set; }
        public int ActiveAdmins { get; set; }
        public int PendingApplications { get; set; }
        public int TotalUsers { get; set; }
    }

    public class SystemLogViewModel
    {
        public DateTime Timestamp { get; set; }
        public string Level { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}