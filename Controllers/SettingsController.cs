using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace StudentManagementSystem.Controllers
{
    [Authorize(Roles = "SuperAdmin,Admin,UniversityAdmin,FacultyAdmin,DepartmentAdmin,FinanceAdmin,StudentAdmin")]
    public class SettingsController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        public SettingsController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            SignInManager<IdentityUser> signInManager,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _environment = environment;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var userRoles = await _userManager.GetRolesAsync(currentUser);
            var isSuperAdmin = userRoles.Contains("SuperAdmin");

            var model = new SystemSettingsViewModel
            {
                CurrentUserRoles = userRoles.ToList(),
                IsSuperAdmin = isSuperAdmin,
                ThemeSettings = new ThemeSettings
                {
                    AvailableThemes = new List<string> { "dark", "light", "blue", "green", "purple" },
                    DefaultTheme = "dark",
                    AllowUserThemeSelection = true
                },
                SecuritySettings = new SecuritySettings
                {
                    RequireTwoFactorAuth = false,
                    SessionTimeoutMinutes = 60,
                    MaxLoginAttempts = 5,
                    PasswordExpiryDays = 90
                },
                EmailSettings = new EmailSettings
                {
                    SmtpServer = "smtp.gmail.com",
                    SmtpPort = 587,
                    SenderEmail = "noreply@academicpro.edu",
                    SenderName = "AcademicPro System",
                    Username = "",
                    Password = "",
                    UseSSL = true,
                    EnableNotifications = true
                },
                BackupSettings = new BackupSettings
                {
                    AutoBackupEnabled = true,
                    BackupFrequencyDays = 7,
                    KeepBackupsForDays = 30,
                    LastBackupDate = null,
                    BackupPath = "/Backups"
                }
            };

            ViewBag.Roles = await _roleManager.Roles.ToListAsync();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveGeneralSettings(GeneralSettings settings)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Invalid settings data";
                return RedirectToAction("Index");
            }

            TempData["SuccessMessage"] = "General settings saved successfully";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin")]
        public IActionResult SaveSecuritySettings(SecuritySettings settings)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Invalid security settings";
                return RedirectToAction("Index");
            }

            TempData["SuccessMessage"] = "Security settings saved successfully";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveEmailSettings(EmailSettings settings)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Invalid email settings";
                return RedirectToAction("Index");
            }

            TempData["SuccessMessage"] = "Email settings saved successfully";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin")]
        public IActionResult SaveBackupSettings(BackupSettings settings)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Invalid backup settings";
                return RedirectToAction("Index");
            }

            TempData["SuccessMessage"] = "Backup settings saved successfully";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> PerformBackup()
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(connectionString))
                {
                    TempData["ErrorMessage"] = "Database connection string not found.";
                    return RedirectToAction("Index");
                }

                var backupFileName = $"Backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
                var backupDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Backups");

                if (!Directory.Exists(backupDirectory))
                {
                    Directory.CreateDirectory(backupDirectory);
                }

                var fullBackupPath = Path.Combine(backupDirectory, backupFileName);

                var databaseName = new SqlConnectionStringBuilder(connectionString).InitialCatalog;
                if (string.IsNullOrEmpty(databaseName))
                {
                    databaseName = "StudentManagementSystem";
                }

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var backupCommand = $@"
                        BACKUP DATABASE [{databaseName}] 
                        TO DISK = '{fullBackupPath.Replace("\\", "\\\\")}'
                        WITH FORMAT, 
                             MEDIANAME = 'AcademicPro_Backup',
                             NAME = 'Full Backup of {databaseName}',
                             DESCRIPTION = 'Backup created on {DateTime.Now:yyyy-MM-dd HH:mm:ss}',
                             STATS = 10";

                    using (var command = new SqlCommand(backupCommand, connection))
                    {
                        await command.ExecuteNonQueryAsync();
                    }
                }

                CleanOldBackups(backupDirectory);
                TempData["SuccessMessage"] = $"Database backup completed successfully! File: {backupFileName}";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Backup failed: {ex.Message}";
                Console.WriteLine($"Backup error: {ex}");
            }

            return RedirectToAction("Index");
        }

        private void CleanOldBackups(string backupDirectory)
        {
            try
            {
                var backupFiles = Directory.GetFiles(backupDirectory, "*.bak");
                var thirtyDaysAgo = DateTime.Now.AddDays(-30);

                foreach (var file in backupFiles)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.CreationTime < thirtyDaysAgo)
                    {
                        fileInfo.Delete();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cleaning old backups: {ex.Message}");
            }
        }

        [HttpGet]
        [Authorize(Roles = "SuperAdmin")]
        public IActionResult BackupHistory()
        {
            var backupDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Backups");
            if (!Directory.Exists(backupDirectory))
            {
                return View(new List<BackupInfo>());
            }

            var backupFiles = Directory.GetFiles(backupDirectory, "*.bak");
            var backups = new List<BackupInfo>();

            foreach (var file in backupFiles)
            {
                var fileInfo = new FileInfo(file);
                backups.Add(new BackupInfo
                {
                    FileName = fileInfo.Name,
                    FilePath = fileInfo.FullName,
                    FileSize = fileInfo.Length,
                    Created = fileInfo.CreationTime,
                    CreatedBy = "System",
                    DatabaseName = "StudentManagementSystem",
                    Success = true
                });
            }

            return View(backups.OrderByDescending(b => b.Created).ToList()); // Fixed typo here
        }

        [HttpGet]
        [Authorize(Roles = "SuperAdmin")]
        public IActionResult DownloadBackup(string fileName)
        {
            try
            {
                var backupDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Backups");
                var filePath = Path.Combine(backupDirectory, fileName);

                if (!System.IO.File.Exists(filePath))
                {
                    TempData["ErrorMessage"] = "Backup file not found.";
                    return RedirectToAction("BackupHistory");
                }

                var fileBytes = System.IO.File.ReadAllBytes(filePath);
                return File(fileBytes, "application/octet-stream", fileName);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Download failed: {ex.Message}";
                return RedirectToAction("BackupHistory");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin")]
        public IActionResult DeleteBackup(string fileName)
        {
            try
            {
                var backupDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Backups");
                var filePath = Path.Combine(backupDirectory, fileName);

                if (!System.IO.File.Exists(filePath))
                {
                    TempData["ErrorMessage"] = "Backup file not found.";
                    return RedirectToAction("BackupHistory");
                }

                System.IO.File.Delete(filePath);
                TempData["SuccessMessage"] = $"Backup '{fileName}' deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Delete failed: {ex.Message}";
            }

            return RedirectToAction("BackupHistory");
        }

        [HttpGet]
        public async Task<IActionResult> SystemInfo()
        {
            var systemInfo = new SystemInfoViewModel
            {
                ApplicationName = "AcademicPro Management System",
                Version = "2.0.0",
                AspNetCoreVersion = "8.0.0",
                Environment = "Development",
                ServerName = Environment.MachineName,
                OSVersion = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                DatabaseProvider = "SQL Server",
                Uptime = "24 hours",
                TotalUsers = await _userManager.Users.CountAsync(),
                TotalRoles = await _roleManager.Roles.CountAsync()
            };

            return View(systemInfo);
        }
    }

    // View Models
    public class SystemSettingsViewModel
    {
        public List<string> CurrentUserRoles { get; set; } = new List<string>();
        public bool IsSuperAdmin { get; set; }
        public ThemeSettings ThemeSettings { get; set; } = new ThemeSettings();
        public SecuritySettings SecuritySettings { get; set; } = new SecuritySettings();
        public EmailSettings EmailSettings { get; set; } = new EmailSettings();
        public BackupSettings BackupSettings { get; set; } = new BackupSettings();
    }

    public class GeneralSettings
    {
        public string ApplicationName { get; set; } = "AcademicPro";
        public string InstitutionName { get; set; } = "Academic University";
        public string ContactEmail { get; set; } = "admin@academicpro.edu";
        public string ContactPhone { get; set; } = "+1 (555) 123-4567";
        public string TimeZone { get; set; } = "UTC";
        public string DateFormat { get; set; } = "MM/dd/yyyy";
        public bool MaintenanceMode { get; set; } = false;
    }

    public class ThemeSettings
    {
        public List<string> AvailableThemes { get; set; } = new List<string>();
        public string DefaultTheme { get; set; } = "dark";
        public bool AllowUserThemeSelection { get; set; } = true;
    }

    public class SecuritySettings
    {
        public bool RequireTwoFactorAuth { get; set; } = false;
        public int SessionTimeoutMinutes { get; set; } = 60;
        public int MaxLoginAttempts { get; set; } = 5;
        public int PasswordExpiryDays { get; set; } = 90;
        public bool ForcePasswordChangeOnFirstLogin { get; set; } = true;
        public bool EnableAuditLogging { get; set; } = true;
    }

    //public class EmailSettings
    //{
    //    public string SmtpServer { get; set; } = "";
    //    public int SmtpPort { get; set; } = 587;
    //    public bool UseSSL { get; set; } = true;
    //    public string SenderEmail { get; set; } = "";
    //    public string SenderName { get; set; } = "";
    //    public string Username { get; set; } = "";
    //    public string Password { get; set; } = "";
    //    public bool EnableNotifications { get; set; } = true;

    //    public bool ShowSenderEmail { get; set; } = true; // Default: show email
    //    public bool BccSystemEmail { get; set; } = false; // Default: don't BCC
    //    public string SystemBccEmail { get; set; } = string.Empty;
    //}

    public class BackupSettings
    {
        public bool AutoBackupEnabled { get; set; } = true;
        public int BackupFrequencyDays { get; set; } = 7;
        public int KeepBackupsForDays { get; set; } = 30;
        public string BackupPath { get; set; } = "/Backups";
        public DateTime? LastBackupDate { get; set; } = null;
    }

    public class SystemInfoViewModel
    {
        public string ApplicationName { get; set; } = "";
        public string Version { get; set; } = "";
        public string AspNetCoreVersion { get; set; } = "";
        public string Environment { get; set; } = "";
        public string ServerName { get; set; } = "";
        public string OSVersion { get; set; } = "";
        public string DatabaseProvider { get; set; } = "";
        public string Uptime { get; set; } = "";
        public int TotalUsers { get; set; }
        public int TotalRoles { get; set; }
        public DateTime ServerTime => DateTime.Now;
    }

    


}