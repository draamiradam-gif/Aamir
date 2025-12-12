using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;
using StudentManagementSystem.Models.ViewModels;
using StudentManagementSystem.Services;
using System;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace StudentManagementSystem.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class UserManagementController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IPermissionService _permissionService;
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<UserManagementController> _logger;

        public UserManagementController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IPermissionService permissionService,
            ApplicationDbContext context,
            IEmailService emailService,
            ILogger<UserManagementController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _permissionService = permissionService;
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> Index()
        {
            // Get all users
            var users = await _userManager.Users.ToListAsync();

            // Get roles for each user
            var userRoles = new Dictionary<string, List<string>>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRoles[user.Id] = roles.ToList();
            }

            // Get admin details for display
            var adminDetailsDict = new Dictionary<string, AdminPrivilege>();
            try
            {
                var adminDetails = await _context.AdminPrivileges
                    .Include(ap => ap.University)
                    .Include(ap => ap.Faculty)
                    .Include(ap => ap.Department)
                    .Where(ap => ap.IsActive)
                    .ToListAsync();

                foreach (var adminPriv in adminDetails)
                {
                    adminDetailsDict[adminPriv.AdminId] = adminPriv;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error loading admin details");
            }

            ViewBag.UserRoles = userRoles;
            ViewBag.AdminDetails = adminDetailsDict;

            return View(users);
        }

        //[HttpGet]
        //[Authorize(Roles = "SuperAdmin,Admin")]
        //public async Task<IActionResult> Index()
        //{
        //    // Get all users
        //    var users = await _userManager.Users.ToListAsync();

        //    // Get users with admin privileges
        //    var adminUsers = await _context.AdminPrivileges
        //        .Include(ap => ap.Admin)
        //        .Where(ap => ap.IsActive)
        //        .Select(ap => ap.Admin)
        //        .Distinct()
        //        .ToListAsync();

        //    // Get roles for each user
        //    var userRoles = new Dictionary<string, List<string>>();
        //    foreach (var user in users)
        //    {
        //        var roles = await _userManager.GetRolesAsync(user);
        //        userRoles[user.Id] = roles.ToList();

        //        // Add admin role if they have admin privileges
        //        if (adminUsers.Any(au => au.Id == user.Id))
        //        {
        //            userRoles[user.Id].Add("Admin");
        //        }
        //    }

        //// Get admin details for display
        //var adminDetails = await _context.AdminPrivileges
        //        .Include(ap => ap.University)
        //        .Include(ap => ap.Faculty)
        //        .Include(ap => ap.Department)
        //        .Where(ap => ap.IsActive)
        //        .ToListAsync();

        //    ViewBag.UserRoles = userRoles;
        //    ViewBag.AdminDetails = adminDetails.ToDictionary(ad => ad.AdminId);

        //    return View(users);
        //}

        //[HttpGet]
        //public IActionResult Create()
        //{
        //    ViewBag.Roles = _roleManager.Roles.ToList();
        //    return View();
        //}

        //[HttpPost]
        //public async Task<IActionResult> Create(CreateUserViewModel model)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        var user = new IdentityUser
        //        {
        //            UserName = model.Email,
        //            Email = model.Email,
        //            EmailConfirmed = true
        //        };

        //        var result = await _userManager.CreateAsync(user, model.Password);

        //        if (result.Succeeded)
        //        {
        //            // Assign selected roles
        //            if (model.SelectedRoles != null && model.SelectedRoles.Any())
        //            {
        //                await _userManager.AddToRolesAsync(user, model.SelectedRoles);
        //            }

        //            TempData["SuccessMessage"] = "User created successfully";
        //            return RedirectToAction("Index");
        //        }

        //        foreach (var error in result.Errors)
        //        {
        //            ModelState.AddModelError(string.Empty, error.Description);
        //        }
        //    }

        //    ViewBag.Roles = _roleManager.Roles.ToList();
        //    return View(model);
        //}

        //[HttpGet]
        //public async Task<IActionResult> Edit(string id)
        //{
        //    var user = await _userManager.FindByIdAsync(id);
        //    if (user == null)
        //    {
        //        TempData["ErrorMessage"] = "User not found";
        //        return RedirectToAction("Index");
        //    }

        //    var userRoles = await _userManager.GetRolesAsync(user);
        //    var allRoles = _roleManager.Roles.ToList();

        //    var model = new EditUserViewModel
        //    {
        //        Id = user.Id,
        //        Email = user.Email ?? string.Empty,
        //        UserName = user.UserName ?? string.Empty,
        //        SelectedRoles = userRoles.ToList(),
        //        AllRoles = allRoles // No more null reference warning
        //    };

        //    return View(model);
        //}

        //[HttpPost]
        //public async Task<IActionResult> Edit(EditUserViewModel model)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        var user = await _userManager.FindByIdAsync(model.Id);
        //        if (user == null)
        //        {
        //            TempData["ErrorMessage"] = "User not found";
        //            return RedirectToAction("Index");
        //        }

        //        user.Email = model.Email;
        //        user.UserName = model.Email;

        //        var result = await _userManager.UpdateAsync(user);

        //        if (result.Succeeded)
        //        {
        //            // Update roles
        //            var currentRoles = await _userManager.GetRolesAsync(user);
        //            await _userManager.RemoveFromRolesAsync(user, currentRoles);

        //            if (model.SelectedRoles != null)
        //            {
        //                await _userManager.AddToRolesAsync(user, model.SelectedRoles);
        //            }

        //            TempData["SuccessMessage"] = "User updated successfully";
        //            return RedirectToAction("Index");
        //        }

        //        foreach (var error in result.Errors)
        //        {
        //            ModelState.AddModelError(string.Empty, error.Description);
        //        }
        //    }

        //    model.AllRoles = _roleManager.Roles.ToList();
        //    return View(model);
        //}

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new CreateUserViewModel
            {
                AvailableTemplates = await _context.AdminPrivilegeTemplates
                    .Where(t => t.IsActive)
                    .ToListAsync()
            };

            ViewBag.Roles = await _roleManager.Roles.ToListAsync();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new IdentityUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    // Assign selected roles
                    if (model.SelectedRoles != null && model.SelectedRoles.Any())
                    {
                        foreach (var roleName in model.SelectedRoles)
                        {
                            if (await _roleManager.RoleExistsAsync(roleName))
                            {
                                await _userManager.AddToRoleAsync(user, roleName);
                            }
                        }
                    }

                    // If admin role is assigned and template is selected, create admin privileges
                    if (model.SelectedRoles != null &&
                        (model.SelectedRoles.Contains("Admin") || model.SelectedRoles.Contains("SuperAdmin")) &&
                        model.TemplateId.HasValue)
                    {
                        var template = await _context.AdminPrivilegeTemplates
                            .FirstOrDefaultAsync(t => t.Id == model.TemplateId.Value);

                        if (template != null)
                        {
                            var adminPrivilege = new AdminPrivilege
                            {
                                AdminId = user.Id,
                                AdminType = template.AdminType,
                                CreatedBy = User.Identity?.Name ?? "System",
                                CreatedDate = DateTime.UtcNow,
                                IsActive = true,
                                Permissions = template.DefaultPermissions
                            };

                            _context.AdminPrivileges.Add(adminPrivilege);
                            await _context.SaveChangesAsync();
                        }
                    }

                    TempData["SuccessMessage"] = "User created successfully.";
                    return RedirectToAction("Index");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // Reload templates if validation fails
            model.AvailableTemplates = await _context.AdminPrivilegeTemplates
                .Where(t => t.IsActive)
                .ToListAsync();

            ViewBag.Roles = await _roleManager.Roles.ToListAsync();
            return View(model);
        }

        // Edit user using your EditUserViewModel
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var userRoles = await _userManager.GetRolesAsync(user);

            var model = new EditUserViewModel
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                UserName = user.UserName ?? string.Empty,
                SelectedRoles = userRoles.ToList(),
                AllRoles = await _roleManager.Roles.ToListAsync()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(model.Id);
                if (user == null)
                {
                    return NotFound();
                }

                // Update basic info
                user.Email = model.Email;
                user.UserName = model.UserName;

                var updateResult = await _userManager.UpdateAsync(user);
                if (updateResult.Succeeded)
                {
                    // Update roles
                    var currentRoles = await _userManager.GetRolesAsync(user);

                    // Remove roles no longer selected
                    var rolesToRemove = currentRoles.Except(model.SelectedRoles);
                    foreach (var role in rolesToRemove)
                    {
                        await _userManager.RemoveFromRoleAsync(user, role);
                    }

                    // Add new roles
                    var rolesToAdd = model.SelectedRoles.Except(currentRoles);
                    foreach (var role in rolesToAdd)
                    {
                        if (await _roleManager.RoleExistsAsync(role))
                        {
                            await _userManager.AddToRoleAsync(user, role);
                        }
                    }

                    TempData["SuccessMessage"] = "User updated successfully.";
                    return RedirectToAction("Index");
                }

                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            model.AllRoles = await _roleManager.Roles.ToListAsync();
            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Index");
            }

            // Prevent deleting yourself
            if (user.Id == _userManager.GetUserId(User))
            {
                TempData["ErrorMessage"] = "You cannot delete your own account.";
                return RedirectToAction("Index");
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "User deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to delete user.";
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            var adminPrivilege = await _context.AdminPrivileges
                .Include(ap => ap.University)
                .Include(ap => ap.Faculty)
                .Include(ap => ap.Department)
                .FirstOrDefaultAsync(ap => ap.AdminId == user.Id);

            var viewModel = new UserViewModel
            {
                Id = user.Id,
                Email = user.Email,
                UserName = user.UserName,
                Roles = roles.ToList()
            };

            ViewBag.AdminPrivilege = adminPrivilege;
            ViewBag.User = user;

            return View(viewModel);
        }

        private async Task SendUserEmailAsync(string? email, string? userName, string subject, string message)
        {
            if (string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("Cannot send email: email is null or empty");
                return;
            }

            try
            {
                var body = GetUserEmailTemplate(userName ?? email, message);
                await _emailService.SendEmailAsync(email, subject, body, new List<string>());
                _logger.LogInformation($"Email sent to user {userName ?? "Unknown"} ({email})");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email to {email}");
                throw;
            }
        }

        private async Task SendBulkUserEmailAsync(List<IdentityUser> users, string subject, string message)
        {
            try
            {
                int successCount = 0;
                int failCount = 0;

                foreach (var user in users)
                {
                    try
                    {
                        var body = GetUserEmailTemplate(user.UserName ?? user.Email ?? "User", message);
                        await _emailService.SendEmailAsync(user.Email ?? string.Empty, subject, body, new List<string>());
                        successCount++;
                        _logger.LogInformation($"Bulk email sent to {user.UserName ?? user.Email}");
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        _logger.LogError(ex, $"Failed to send bulk email to {user.Email}");
                    }
                }

                _logger.LogInformation($"Bulk email to users completed: {successCount} sent, {failCount} failed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send bulk emails to users");
            }
        }


        private string GetUserEmailTemplate(string userName, string message)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #007bff 0%, #6610f2 100%); padding: 30px; text-align: center; color: white; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .message {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; }}
        .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; color: #666; font-size: 12px; text-align: center; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1 style='margin: 0;'>System Announcement</h1>
        <p style='margin: 10px 0 0; opacity: 0.9;'>Student Management System</p>
    </div>
    
    <div class='content'>
        <p>Dear {userName},</p>
        
        <div class='message'>
            {message}
        </div>
        
        <p>Please contact the system administrator if you have any questions.</p>
    </div>
    
    <div class='footer'>
        <p>This is an automated message. Please do not reply to this email.</p>
        <p>© {DateTime.Now.Year} Student Management System. All rights reserved.</p>
    </div>
</body>
</html>";
        }


        // Email actions
        [HttpGet]
        public async Task<IActionResult> SendUserEmail(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            ViewBag.User = user;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendUserEmail(string id, string subject, string message)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Index");
            }

            try
            {
                // Fixed: Handle null email and username
                await SendUserEmailAsync(user.Email, user.UserName, subject, message);
                TempData["SuccessMessage"] = $"Email sent to {user.UserName ?? user.Email ?? "User"} successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to send email: {ex.Message}";
            }

            return RedirectToAction("Index");
        }


        [HttpGet]
        public async Task<IActionResult> SendBulkUserEmail()
        {
            var users = await _userManager.Users.ToListAsync();
            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendBulkUserEmail(List<string> selectedUsers, string subject, string message)
        {
            try
            {
                var users = await _userManager.Users
                    .Where(u => selectedUsers.Contains(u.Id))
                    .ToListAsync();

                if (!users.Any())
                {
                    TempData["ErrorMessage"] = "No users selected.";
                    return RedirectToAction("SendBulkUserEmail");
                }

                await SendBulkUserEmailAsync(users, subject, message);

                TempData["SuccessMessage"] = $"Bulk email sent to {users.Count} users successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to send bulk email: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

    }
}