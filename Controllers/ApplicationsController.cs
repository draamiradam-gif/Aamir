using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;
using StudentManagementSystem.Services;
using System;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace StudentManagementSystem.Controllers
{
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class ApplicationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly ILogger<ApplicationsController> _logger;

        public ApplicationsController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            IEmailService emailService,
            ILogger<ApplicationsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            _logger = logger;
        }

        // GET: Applications/Index - View all applications
        public async Task<IActionResult> Index()
        {
            var applications = await _context.AdminApplications
                .Include(a => a.University)
                .Include(a => a.Faculty)
                .Include(a => a.Department)
                .OrderByDescending(a => a.AppliedDate)
                .ToListAsync();

            ViewBag.PendingCount = applications.Count(a => a.Status == ApplicationStatus.Pending);
            ViewBag.TotalApplications = applications.Count;

            return View(applications);
        }

        // GET: Applications/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var application = await _context.AdminApplications
                .Include(a => a.University)
                .Include(a => a.Faculty)
                .Include(a => a.Department)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null)
            {
                return NotFound();
            }

            //return View(application);
            return View("Details", application);
        }

        // GET: Applications/Review/5 - Review page with all options
        //public async Task<IActionResult> Review(int id)
        //{
        //    var application = await _context.AdminApplications
        //        .Include(a => a.University)
        //        .Include(a => a.Faculty)
        //        .Include(a => a.Department)
        //        .FirstOrDefaultAsync(a => a.Id == id);

        //    if (application == null)
        //    {
        //        return NotFound();
        //    }

        //    // Pass admin types for dropdown
        //    ViewBag.AdminTypes = Enum.GetValues(typeof(AdminType))
        //        .Cast<AdminType>()
        //        .Select(v => new { Id = v, Name = v.ToString() })
        //        .ToList();

        //    return View(application);
        //}

        //public async Task<IActionResult> Review(int id)
        //{
        //    var application = await _context.AdminApplications
        //        .Include(a => a.University)
        //        .Include(a => a.Faculty)
        //        .Include(a => a.Department)
        //        .FirstOrDefaultAsync(a => a.Id == id);

        //    if (application == null)
        //    {
        //        return NotFound();
        //    }

        //    Console.WriteLine("DEBUG: In Review method - setting ViewBag.AdminTypes");

        //    // SIMPLE WORKING VERSION
        //    var adminTypes = new List<object>();

        //    // Add SuperAdmin
        //    adminTypes.Add(new { Id = 1, Name = "SuperAdmin" });
        //    adminTypes.Add(new { Id = 2, Name = "UniversityAdmin" });
        //    adminTypes.Add(new { Id = 3, Name = "FacultyAdmin" });
        //    adminTypes.Add(new { Id = 4, Name = "DepartmentAdmin" });
        //    adminTypes.Add(new { Id = 5, Name = "EmployeeAdmin" });
        //    adminTypes.Add(new { Id = 6, Name = "FinanceAdmin" });
        //    adminTypes.Add(new { Id = 7, Name = "StudentAdmin" });
        //    adminTypes.Add(new { Id = 8, Name = "CustomAdmin" });

        //    ViewBag.AdminTypes = adminTypes;

        //    // Debug: Check what we set
        //    Console.WriteLine($"DEBUG: Set {ViewBag.AdminTypes?.Count} admin types");

        //    return View("ReviewApplication", "AdminManagement"); 
        //}

        [HttpGet]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> ReviewWithType(int id)
        {
            var application = await _context.AdminApplications
                .Include(a => a.University)
                .Include(a => a.Faculty)
                .Include(a => a.Department)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null)
            {
                return NotFound();
            }

            // Add ViewBag.AdminTypes for dropdown
            ViewBag.AdminTypes = new List<object>
            {
                new { Id = 1, Name = "SuperAdmin" },
                new { Id = 2, Name = "UniversityAdmin" },
                new { Id = 3, Name = "FacultyAdmin" },
                new { Id = 4, Name = "DepartmentAdmin" },
                new { Id = 5, Name = "EmployeeAdmin" },
                new { Id = 6, Name = "FinanceAdmin" },
                new { Id = 7, Name = "StudentAdmin" },
                new { Id = 8, Name = "CustomAdmin" }
            };

            return View("~/Views/AdminManagement/ReviewApplication.cshtml", application);
        }

        
        // POST: Applications/Reject - Reject application
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string reviewNotes)
        {
            var application = await _context.AdminApplications.FindAsync(id);

            if (application == null)
            {
                TempData["ErrorMessage"] = "Application not found.";
                return RedirectToAction("Index");
            }

            try
            {
                application.Status = ApplicationStatus.Rejected;
                application.ReviewedDate = DateTime.UtcNow;
                application.ReviewedBy = User.Identity?.Name;
                application.ReviewNotes = reviewNotes;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Application has been rejected.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to reject application: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        // POST: Applications/Block - Block user from applying again
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Block(int id, string blockReason)
        {
            var application = await _context.AdminApplications.FindAsync(id);

            if (application == null)
            {
                TempData["ErrorMessage"] = "Application not found.";
                return RedirectToAction("Index");
            }

            try
            {
                // Update application status
                application.Status = ApplicationStatus.Blocked;
                application.IsBlocked = true;
                application.BlockReason = blockReason;
                application.BlockedDate = DateTime.UtcNow;
                application.BlockedBy = User.Identity?.Name;
                application.ReviewedDate = DateTime.UtcNow;
                application.ReviewedBy = User.Identity?.Name;

                await _context.SaveChangesAsync();

                // Also add to blocked users table (if you have it)
                var blockedUser = new BlockedUser
                {
                    Email = application.Email,
                    UserName = application.ApplicantName,
                    Reason = blockReason,
                    BlockedBy = User.Identity?.Name ?? "System",
                    BlockedDate = DateTime.UtcNow,
                    IsActive = true
                };

                _context.BlockedUsers.Add(blockedUser);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "User has been blocked from submitting further applications.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to block user: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        // POST: Applications/Unblock/5 - Unblock a user
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unblock(int id, string unblockReason = "")
        {
            var application = await _context.AdminApplications.FindAsync(id);

            if (application == null)
            {
                TempData["ErrorMessage"] = "Application not found.";
                return RedirectToAction("Index");
            }

            try
            {
                application.IsBlocked = false;
                application.BlockReason = null;
                application.BlockedDate = null;
                application.BlockedBy = null;

                // Update status back to whatever it was before blocking
                if (application.Status == ApplicationStatus.Blocked)
                {
                    application.Status = ApplicationStatus.Rejected; // Or keep original status
                }

                await _context.SaveChangesAsync();

                // Also update blocked users table
                var blockedUser = await _context.BlockedUsers
                    .FirstOrDefaultAsync(b => b.Email == application.Email && b.IsActive);

                if (blockedUser != null)
                {
                    blockedUser.IsActive = false;
                    blockedUser.UnblockedDate = DateTime.UtcNow;
                    blockedUser.UnblockedBy = User.Identity?.Name;
                    blockedUser.UnblockReason = unblockReason;

                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "User has been unblocked.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to unblock user: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        // GET: Applications/BlockedUsers - View all blocked users
        public async Task<IActionResult> BlockedUsers()
        {
            var blockedUsers = await _context.BlockedUsers
                .Where(b => b.IsActive)
                .OrderByDescending(b => b.BlockedDate)
                .ToListAsync();

            return View(blockedUsers);
        }

        // GET: Applications/Pending - View only pending applications
        public async Task<IActionResult> Pending()
        {
            var pendingApplications = await _context.AdminApplications
                .Where(a => a.Status == ApplicationStatus.Pending && !a.IsBlocked)
                .Include(a => a.University)
                .Include(a => a.Faculty)
                .Include(a => a.Department)
                .OrderByDescending(a => a.AppliedDate)
                .ToListAsync();

            ViewBag.PendingCount = pendingApplications.Count;

            return View(pendingApplications);
        }

        // GET: Applications/Approved - View approved applications
        public async Task<IActionResult> Approved()
        {
            var approvedApplications = await _context.AdminApplications
                .Where(a => a.Status == ApplicationStatus.Approved)
                .Include(a => a.University)
                .Include(a => a.Faculty)
                .Include(a => a.Department)
                .OrderByDescending(a => a.ReviewedDate)
                .ToListAsync();

            return View(approvedApplications);
        }

        // GET: Applications/Rejected - View rejected applications
        public async Task<IActionResult> Rejected()
        {
            var rejectedApplications = await _context.AdminApplications
                .Where(a => a.Status == ApplicationStatus.Rejected)
                .Include(a => a.University)
                .Include(a => a.Faculty)
                .Include(a => a.Department)
                .OrderByDescending(a => a.ReviewedDate)
                .ToListAsync();

            return View(rejectedApplications);
        }

        // GET: Applications/Blocked - View blocked applications
        public async Task<IActionResult> Blocked()
        {
            var blockedApplications = await _context.AdminApplications
                .Where(a => a.Status == ApplicationStatus.Blocked || a.IsBlocked)
                .Include(a => a.University)
                .Include(a => a.Faculty)
                .Include(a => a.Department)
                .OrderByDescending(a => a.BlockedDate)
                .ToListAsync();

            return View(blockedApplications);
        }


        [AllowAnonymous]
        [HttpGet]
        public IActionResult Apply()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(AdminApplication application)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Set default values
                    application.Status = ApplicationStatus.Pending;
                    application.AppliedDate = DateTime.Now;
                    application.CreatedDate = DateTime.UtcNow;

                    _context.AdminApplications.Add(application);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Your application has been submitted successfully! An admin will review it shortly.";
                    return RedirectToAction("ApplySuccess");
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Failed to submit application: {ex.Message}";
                }
            }

            return View(application);
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult ApplySuccess()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdminApplication application)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Set default values
                    application.Status = ApplicationStatus.Pending;
                    application.AppliedDate = DateTime.Now;

                    _context.AdminApplications.Add(application);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Your application has been submitted successfully!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Failed to submit application: {ex.Message}";
                }
            }

            return View(application);
        }
        

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Revoke(int id)
        {
            var application = await _context.AdminApplications.FindAsync(id);

            if (application == null)
            {
                TempData["ErrorMessage"] = "Application not found.";
                return RedirectToAction("Index");
            }

            try
            {
                application.Status = ApplicationStatus.Revoked;
                application.ReviewedDate = DateTime.UtcNow;
                application.ReviewedBy = User.Identity?.Name;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Application access has been revoked.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to revoke application: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var application = await _context.AdminApplications.FindAsync(id);

            if (application == null)
            {
                return NotFound();
            }

            return View(application);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AdminApplication application)
        {
            if (id != application.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(application);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Application updated successfully!";
                    return RedirectToAction("Index");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ApplicationExists(application.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return View(application);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var application = await _context.AdminApplications.FindAsync(id);

            if (application == null)
            {
                TempData["ErrorMessage"] = "Application not found.";
                return RedirectToAction("Index");
            }

            try
            {
                _context.AdminApplications.Remove(application);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Application deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to delete application: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        private bool ApplicationExists(int id)
        {
            return _context.AdminApplications.Any(e => e.Id == id);
        }

        //[HttpGet]
        //[Authorize(Roles = "SuperAdmin,Admin")]
        //public async Task<IActionResult> ReviewApplication(int id)
        //{
        //    var application = await _context.AdminApplications
        //        .Include(a => a.University)
        //        .Include(a => a.Faculty)
        //        .Include(a => a.Department)
        //        .FirstOrDefaultAsync(a => a.Id == id);

        //    if (application == null)
        //    {
        //        return NotFound();
        //    }

        //    //return View(application);
        //    return View("ReviewApplication", application);
        //}

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> ApproveWithType(int id, int assignedAdminType, string? reviewNotes = null)
        //{
        //    var application = await _context.AdminApplications.FindAsync(id);

        //    if (application == null)
        //    {
        //        TempData["ErrorMessage"] = "Application not found.";
        //        return RedirectToAction("Index");
        //    }

        //    try
        //    {
        //        // Convert int to AdminType enum
        //        if (!Enum.IsDefined(typeof(AdminType), assignedAdminType))
        //        {
        //            TempData["ErrorMessage"] = "Invalid admin type selected.";
        //            return RedirectToAction("Review", new { id });
        //        }

        //        var adminType = (AdminType)assignedAdminType;

        //        // Update application with assigned admin type
        //        application.AssignedAdminType = adminType;
        //        application.Status = ApplicationStatus.Approved;
        //        application.ReviewedDate = DateTime.UtcNow;
        //        application.ReviewedBy = User.Identity?.Name;
        //        application.ReviewNotes = reviewNotes;

        //        await _context.SaveChangesAsync();

        //        TempData["SuccessMessage"] = $"Application approved successfully! Admin type assigned: {adminType}";
        //    }
        //    catch (Exception ex)
        //    {
        //        TempData["ErrorMessage"] = $"Failed to approve application: {ex.Message}";
        //    }

        //    return RedirectToAction("Index");
        //}

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> ApproveWithType(int id, AdminType assignedAdminType, string reviewNotes = "")
        //{
        //    var application = await _context.AdminApplications.FindAsync(id);

        //    if (application == null)
        //    {
        //        TempData["ErrorMessage"] = "Application not found.";
        //        return RedirectToAction("Index");
        //    }

        //    try
        //    {
        //        // Update application status
        //        application.Status = ApplicationStatus.Approved;
        //        application.AssignedAdminType = assignedAdminType;
        //        application.ReviewedDate = DateTime.UtcNow;
        //        application.ReviewedBy = User.Identity?.Name;
        //        application.ReviewNotes = reviewNotes;

        //        // STEP 1: Check if user already exists
        //        var existingUser = await _userManager.FindByEmailAsync(application.Email);

        //        if (existingUser == null)
        //        {
        //            // Create new user
        //            var user = new IdentityUser
        //            {
        //                UserName = application.Email,
        //                Email = application.Email,
        //                EmailConfirmed = true,
        //                PhoneNumber = application.Phone,
        //                PhoneNumberConfirmed = false
        //            };

        //            // Generate random password
        //            var password = GenerateRandomPassword(12);

        //            // Create user
        //            var result = await _userManager.CreateAsync(user, password);

        //            if (!result.Succeeded)
        //            {
        //                throw new Exception($"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        //            }

        //            // Create admin privileges
        //            await CreateAdminPrivileges(user.Id, assignedAdminType, application);

        //            // Send welcome email with password
        //            await SendWelcomeEmail(application.Email, password, assignedAdminType);

        //            TempData["SuccessMessage"] = $"Application approved! User account created and assigned as {assignedAdminType}. Password sent to user.";
        //        }
        //        else
        //        {
        //            // User exists, just create admin privileges
        //            await CreateAdminPrivileges(existingUser.Id, assignedAdminType, application);

        //            TempData["SuccessMessage"] = $"Application approved! User already exists, assigned as {assignedAdminType}.";
        //        }

        //        await _context.SaveChangesAsync();
        //    }
        //    catch (Exception ex)
        //    {
        //        TempData["ErrorMessage"] = $"Failed to approve application: {ex.Message}";
        //    }

        //    return RedirectToAction("Index");
        //}

        //private async Task CreateAdminPrivileges(string userId, AdminType adminType, AdminApplication application)
        //{
        //    // Create AdminPrivilege record
        //    var adminPrivilege = new AdminPrivilege
        //    {
        //        AdminId = userId,
        //        AdminType = adminType,
        //        UniversityScope = application.UniversityId,
        //        FacultyScope = application.FacultyId,
        //        DepartmentScope = application.DepartmentId,
        //        CreatedBy = User.Identity?.Name ?? "System",
        //        CreatedDate = DateTime.UtcNow,
        //        IsActive = true,
        //        // Set default permissions based on admin type
        //        Permissions = PermissionHelper.GetDefaultPermissionsForAdminType(adminType)
        //    };

        //    _context.AdminPrivileges.Add(adminPrivilege);

        //    // Assign to Admin role
        //    var user = await _userManager.FindByIdAsync(userId);
        //    if (user != null && !await _userManager.IsInRoleAsync(user, "Admin"))
        //    {
        //        await _userManager.AddToRoleAsync(user, "Admin");
        //    }
        //}        

//        private async Task SendWelcomeEmailAsync(string email, string password, AdminType adminType)
//        {
//            try
//            {
//                var subject = "Your Admin Account Has Been Created";
//                var body = $@"
//<h2>Welcome to the Admin Portal!</h2>
//<p>Your admin application has been approved.</p>
//<p><strong>Admin Type:</strong> {adminType}</p>
//<p><strong>Login Email:</strong> {email}</p>
//<p><strong>Temporary Password:</strong> {password}</p>
//<p>Please log in and change your password immediately.</p>
//<br/>
//<p><em>This is an automated message. Please do not reply.</em></p>";

//                using var client = new SmtpClient("smtp.yourdomain.com", 587)
//                {
//                    Credentials = new NetworkCredential("your-email@domain.com", "your-password"),
//                    EnableSsl = true
//                };

//                var mailMessage = new MailMessage
//                {
//                    From = new MailAddress("noreply@yourdomain.com"),
//                    Subject = subject,
//                    Body = body,
//                    IsBodyHtml = true
//                };

//                mailMessage.To.Add(email);

//                await client.SendMailAsync(mailMessage);
//            }
//            catch (Exception ex)
//            {
//                // Log but don't throw - we don't want email failures to break admin creation
//                Console.WriteLine($"Failed to send welcome email: {ex.Message}");
//            }
//        }

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> CreateAdminFromApplication(int id)
//        {
//            var application = await _context.AdminApplications
//                .Include(a => a.University)
//                .Include(a => a.Faculty)
//                .Include(a => a.Department)
//                .FirstOrDefaultAsync(a => a.Id == id);

//            if (application == null || application.Status != ApplicationStatus.Approved)
//            {
//                TempData["ErrorMessage"] = "Application not found or not approved.";
//                return RedirectToAction("Index");
//            }

//            try
//            {
//                // Check if user already exists
//                var existingUser = await _userManager.FindByEmailAsync(application.Email);

//                if (existingUser == null)
//                {
//                    // Create new user
//                    var user = new IdentityUser
//                    {
//                        UserName = application.Email,
//                        Email = application.Email,
//                        EmailConfirmed = true,
//                        PhoneNumber = application.Phone,
//                        PhoneNumberConfirmed = false
//                    };

//                    // Generate random password
//                    var password = GenerateRandomPassword(12);

//                    // Create user
//                    var result = await _userManager.CreateAsync(user, password);

//                    if (!result.Succeeded)
//                    {
//                        throw new Exception($"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
//                    }

//                    // Create admin privileges
//                    await CreateAdminPrivileges(user.Id, application.AssignedAdminType ?? application.AppliedAdminType, application);

//                    // Send welcome email
//                    await SendWelcomeEmailAsync(application.Email, password, application.AssignedAdminType ?? application.AppliedAdminType);

//                    TempData["SuccessMessage"] = $"Admin account created! User account created and assigned as {application.AssignedAdminType ?? application.AppliedAdminType}. Password sent to user.";
//                }
//                else
//                {
//                    // User exists, just create admin privileges
//                    await CreateAdminPrivileges(existingUser.Id, application.AssignedAdminType ?? application.AppliedAdminType, application);

//                    TempData["SuccessMessage"] = $"Admin privileges assigned! User already exists, assigned as {application.AssignedAdminType ?? application.AppliedAdminType}.";
//                }

//                await _context.SaveChangesAsync();
//            }
//            catch (Exception ex)
//            {
//                TempData["ErrorMessage"] = $"Failed to create admin: {ex.Message}";
//            }

//            return RedirectToAction("Index");
//        }

        private string GenerateRandomPassword(int length = 12)
        {
            const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*()";
            var random = new Random();
            var chars = Enumerable.Repeat(validChars, length)
                .Select(s => s[random.Next(s.Length)])
                .ToArray();
            return new string(chars);
        }
               

        private async Task CreateAdminPrivileges(string userId, AdminType adminType, AdminApplication application)
        {
            // Create AdminPrivilege record
            var adminPrivilege = new AdminPrivilege
            {
                AdminId = userId,
                AdminType = adminType,
                UniversityScope = application.UniversityId,
                FacultyScope = application.FacultyId,
                DepartmentScope = application.DepartmentId,
                CreatedBy = User.Identity?.Name ?? "System",
                CreatedDate = DateTime.UtcNow,
                IsActive = true,
                Permissions = PermissionHelper.GetDefaultPermissionsForAdminType(adminType)
            };

            _context.AdminPrivileges.Add(adminPrivilege);

            // Assign to Admin role
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null && !await _userManager.IsInRoleAsync(user, "Admin"))
            {
                await _userManager.AddToRoleAsync(user, "Admin");
            }
        }

        private async Task SendWelcomeEmailAsync(string email, string password, AdminType adminType)
        {
            try
            {
                var subject = "Your Admin Account Has Been Created";
                var body = GetWelcomeEmailTemplate(email, password, adminType);

                await _emailService.SendEmailAsync(email, subject, body);
                _logger.LogInformation($"Welcome email sent to {email}");
            }
            catch (Exception ex)
            {
                // Log but don't throw - don't break admin creation if email fails
                _logger.LogError(ex, $"Failed to send welcome email to {email}");
            }
        }

        private string GetWelcomeEmailTemplate(string email, string password, AdminType adminType)
        {
            return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; text-align: center; color: white; border-radius: 10px 10px 0 0; }}
                    .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
                    .card {{ background: white; padding: 20px; border-radius: 8px; border-left: 4px solid #667eea; margin: 20px 0; }}
                    .warning {{ background: #fff3cd; padding: 15px; border-radius: 6px; border: 1px solid #ffeaa7; margin: 20px 0; color: #856404; }}
                    .btn {{ display: inline-block; background: #667eea; color: white; padding: 12px 30px; text-decoration: none; border-radius: 6px; font-weight: bold; }}
                    .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; color: #666; font-size: 12px; text-align: center; }}
                </style>
            </head>
            <body>
                <div class='header'>
                    <h1 style='margin: 0;'>Welcome to Student Management System</h1>
                    <p style='margin: 10px 0 0; opacity: 0.9;'>Your admin account has been created</p>
                </div>
    
                <div class='content'>
                    <h2 style='color: #333; margin-top: 0;'>Account Details</h2>
        
                    <div class='card'>
                        <p><strong>🎯 Admin Type:</strong> {adminType}</p>
                        <p><strong>📧 Login Email:</strong> {email}</p>
                        <p><strong>🔑 Temporary Password:</strong> <code style='background: #f1f1f1; padding: 5px 10px; border-radius: 4px; font-family: monospace;'>{password}</code></p>
                    </div>
        
                    <div class='warning'>
                        <p style='margin: 0;'><strong>⚠️ Important:</strong> Please log in and change your password immediately.</p>
                    </div>
        
                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='{Request.Scheme}://{Request.Host}/Account/Login' class='btn'>
                            Login to Your Account
                        </a>
                    </div>
        
                    <p>If you have any questions, please contact the system administrator.</p>
                </div>
    
                <div class='footer'>
                    <p>This is an automated message. Please do not reply to this email.</p>
                    <p>© {DateTime.Now.Year} Student Management System. All rights reserved.</p>
                </div>
            </body>
            </html>";
        }

        ////////////////
        ///
        private async Task SendStatusUpdateEmail(string email, string? applicantName, ApplicationStatus status, string? notes = null)
        {
            try
            {
                var subject = $"Admin Application {status}";
                var body = GetStatusUpdateEmailTemplate(applicantName ?? "Applicant", status, notes);

                await _emailService.SendEmailAsync(email, subject, body, new List<string>());
                _logger.LogInformation($"Status update email sent to {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send status email to {email}");
            }
        }

        private async Task SendBulkEmailToApplicants(List<string> emails, string subject, string body)
        {
            try
            {
                int successCount = 0;
                int failCount = 0;

                foreach (var email in emails)
                {
                    try
                    {
                        await _emailService.SendEmailAsync(email, subject, body, new List<string>());
                        successCount++;
                        _logger.LogInformation($"Bulk email sent to {email}");
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        _logger.LogError(ex, $"Failed to send bulk email to {email}");
                    }
                }

                _logger.LogInformation($"Bulk email completed: {successCount} sent, {failCount} failed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send bulk emails");
            }
        }


        private string GetStatusUpdateEmailTemplate(string applicantName, ApplicationStatus status, string? notes = null)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ 
            background: {(status == ApplicationStatus.Approved ? "linear-gradient(135deg, #28a745 0%, #20c997 100%)" :
                                 status == ApplicationStatus.Rejected ? "linear-gradient(135deg, #dc3545 0%, #fd7e14 100%)" :
                                 "linear-gradient(135deg, #007bff 0%, #6610f2 100%)")}; 
            padding: 30px; text-align: center; color: white; border-radius: 10px 10px 0 0; 
        }}
        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
        .card {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; }}
        .status {{ font-size: 24px; font-weight: bold; margin: 20px 0; }}
        .notes {{ background: #f8f9fa; padding: 15px; border-radius: 6px; margin: 20px 0; border-left: 4px solid #6c757d; }}
        .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; color: #666; font-size: 12px; text-align: center; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1 style='margin: 0;'>Admin Application Update</h1>
        <p style='margin: 10px 0 0; opacity: 0.9;'>Status: {status}</p>
    </div>
    
    <div class='content'>
        <p>Dear {applicantName},</p>
        
        <div class='card'>
            <div class='status' style='color: {(status == ApplicationStatus.Approved ? "#28a745" :
                                                   status == ApplicationStatus.Rejected ? "#dc3545" :
                                                   "#007bff")};'>
                Your application has been <strong>{status}</strong>
            </div>
            
            <p>This email is to inform you about the status of your admin application.</p>
            
            {(status == ApplicationStatus.Approved ?
                        "<p>Congratulations! Your application has been approved. You will receive further instructions about accessing the admin panel.</p>" :
                        status == ApplicationStatus.Rejected ?
                        "<p>We regret to inform you that your application was not approved at this time.</p>" :
                        "<p>Your application status has been updated.</p>")}
        </div>
        
        {(!string.IsNullOrEmpty(notes) ? $@"
        <div class='notes'>
            <strong>Review Notes:</strong>
            <p>{notes}</p>
        </div>
        " : "")}
        
        <p>If you have any questions, please contact the system administrator.</p>
    </div>
    
    <div class='footer'>
        <p>This is an automated message. Please do not reply to this email.</p>
        <p>© {DateTime.Now.Year} Student Management System. All rights reserved.</p>
    </div>
</body>
</html>";
        }

        // Update your Approve action to send email
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var application = await _context.AdminApplications.FindAsync(id);

            if (application == null)
            {
                TempData["ErrorMessage"] = "Application not found.";
                return RedirectToAction("Index");
            }

            try
            {
                application.Status = ApplicationStatus.Approved;
                application.ReviewedDate = DateTime.UtcNow;
                application.ReviewedBy = User.Identity?.Name;
                await _context.SaveChangesAsync();

                // Send email notification
                await SendStatusUpdateEmail(application.Email, application.ApplicantName, ApplicationStatus.Approved);

                TempData["SuccessMessage"] = "Application approved successfully! Email notification sent.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to approve application: {ex.Message}";
            }

            return RedirectToAction("Index");
        }
              

        // Update ApproveWithType to send email
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveWithType(int id, AdminType assignedAdminType, string reviewNotes = "")
        {
            var application = await _context.AdminApplications.FindAsync(id);

            if (application == null)
            {
                TempData["ErrorMessage"] = "Application not found.";
                return RedirectToAction("Index");
            }

            try
            {
                application.Status = ApplicationStatus.Approved;
                application.AssignedAdminType = assignedAdminType;
                application.ReviewedDate = DateTime.UtcNow;
                application.ReviewedBy = User.Identity?.Name;
                application.ReviewNotes = reviewNotes;
                await _context.SaveChangesAsync();

                // Send email notification
                await SendStatusUpdateEmail(application.Email, application.ApplicantName, ApplicationStatus.Approved,
                    $"Assigned Admin Type: {assignedAdminType}. {reviewNotes}");

                TempData["SuccessMessage"] = $"Application approved! User assigned as {assignedAdminType}. Email notification sent.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to approve application: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        // Add new Bulk Email action
        [HttpGet]
        public IActionResult SendBulkEmail()
        {
            var applications = _context.AdminApplications.ToList();
            return View(applications);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendBulkEmail(List<int> selectedApplications, string subject, string message)
        {
            try
            {
                var applications = await _context.AdminApplications
                    .Where(a => selectedApplications.Contains(a.Id))
                    .ToListAsync();

                var emails = applications.Select(a => a.Email).Distinct().ToList();

                if (!emails.Any())
                {
                    TempData["ErrorMessage"] = "No applications selected.";
                    return RedirectToAction("SendBulkEmail");
                }

                var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .content {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #007bff 0%, #6610f2 100%); padding: 20px; color: white; text-align: center; }}
        .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='header'>
        <h2>Admin Application Update</h2>
    </div>
    <div class='content'>
        {message}
        <p><br/>This is a bulk notification regarding your admin application.</p>
    </div>
    <div class='footer'>
        <p>This is an automated message. Please do not reply.</p>
        <p>© {DateTime.Now.Year} Student Management System</p>
    </div>
</body>
</html>";

                await SendBulkEmailToApplicants(emails, subject, body);

                TempData["SuccessMessage"] = $"Bulk email sent to {emails.Count} applicants successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to send bulk email: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        // Add Send Individual Email action
        [HttpGet]
        public async Task<IActionResult> SendEmail(int id)
        {
            var application = await _context.AdminApplications.FindAsync(id);
            if (application == null)
            {
                return NotFound();
            }

            ViewBag.Application = application;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendEmail(int id, string subject, string message)
        {
            var application = await _context.AdminApplications.FindAsync(id);
            if (application == null)
            {
                TempData["ErrorMessage"] = "Application not found.";
                return RedirectToAction("Index");
            }

            try
            {
                var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .content {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #007bff 0%, #6610f2 100%); padding: 20px; color: white; text-align: center; }}
        .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='header'>
        <h2>Message Regarding Your Application</h2>
    </div>
    <div class='content'>
        <p>Dear {application.ApplicantName},</p>
        {message}
        <p><br/>Sincerely,<br/>System Administrator</p>
    </div>
    <div class='footer'>
        <p>This is an automated message. Please do not reply.</p>
        <p>© {DateTime.Now.Year} Student Management System</p>
    </div>
</body>
</html>";

                await _emailService.SendEmailAsync(application.Email, subject, body, new List<string>());

                TempData["SuccessMessage"] = $"Email sent to {application.ApplicantName} successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to send email: {ex.Message}";
            }

            return RedirectToAction("Index");
        }
    }
}












//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.EntityFrameworkCore;
//using StudentManagementSystem.Data;
//using StudentManagementSystem.Models;
//using System;
//using System.Linq;
//using System.Threading.Tasks;

//namespace StudentManagementSystem.Controllers
//{
//    [Authorize(Roles = "SuperAdmin,Admin")]
//    public class ApplicationsController : Controller
//    {
//        private readonly ApplicationDbContext _context;

//        public ApplicationsController(ApplicationDbContext context)
//        {
//            _context = context;
//        }

//        public async Task<IActionResult> Index()
//        {
//            var applications = await _context.AdminApplications
//                .Include(a => a.University)
//                .Include(a => a.Faculty)
//                .Include(a => a.Department)
//                .OrderByDescending(a => a.AppliedDate)
//                .ToListAsync();

//            // Add pending count to ViewBag
//            ViewBag.PendingCount = applications.Count(a => a.Status == ApplicationStatus.Pending);

//            return View(applications);
//        }



//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Approve(int id)
//        {
//            var application = await _context.AdminApplications.FindAsync(id);

//            if (application == null)
//            {
//                TempData["ErrorMessage"] = "Application not found.";
//                return RedirectToAction("Index");
//            }

//            try
//            {
//                application.Status = ApplicationStatus.Approved;
//                application.ReviewedDate = DateTime.UtcNow;
//                application.ReviewedBy = User.Identity?.Name;
//                await _context.SaveChangesAsync();

//                TempData["SuccessMessage"] = "Application approved successfully!";
//            }
//            catch (Exception ex)
//            {
//                TempData["ErrorMessage"] = $"Failed to approve application: {ex.Message}";
//            }

//            return RedirectToAction("Index");
//        }

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Reject(int id, string notes)
//        {
//            var application = await _context.AdminApplications.FindAsync(id);

//            if (application == null)
//            {
//                TempData["ErrorMessage"] = "Application not found.";
//                return RedirectToAction("Index");
//            }

//            try
//            {
//                application.Status = ApplicationStatus.Rejected;
//                application.ReviewedDate = DateTime.UtcNow;
//                application.ReviewedBy = User.Identity?.Name;
//                application.ReviewNotes = notes;
//                await _context.SaveChangesAsync();

//                TempData["SuccessMessage"] = "Application has been rejected.";
//            }
//            catch (Exception ex)
//            {
//                TempData["ErrorMessage"] = $"Failed to reject application: {ex.Message}";
//            }

//            return RedirectToAction("Index");
//        }

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Revoke(int id)
//        {
//            var application = await _context.AdminApplications.FindAsync(id);

//            if (application == null)
//            {
//                TempData["ErrorMessage"] = "Application not found.";
//                return RedirectToAction("Index");
//            }

//            try
//            {
//                application.Status = ApplicationStatus.Revoked;
//                application.ReviewedDate = DateTime.UtcNow;
//                application.ReviewedBy = User.Identity?.Name;
//                await _context.SaveChangesAsync();

//                TempData["SuccessMessage"] = "Application access has been revoked.";
//            }
//            catch (Exception ex)
//            {
//                TempData["ErrorMessage"] = $"Failed to revoke application: {ex.Message}";
//            }

//            return RedirectToAction("Index");
//        }

//        [HttpGet]
//        public IActionResult Create()
//        {
//            return View();
//        }

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Create(AdminApplication application)
//        {
//            if (ModelState.IsValid)
//            {
//                try
//                {
//                    // Set default values
//                    application.Status = ApplicationStatus.Pending;
//                    application.AppliedDate = DateTime.Now;

//                    _context.AdminApplications.Add(application);
//                    await _context.SaveChangesAsync();

//                    TempData["SuccessMessage"] = "Your application has been submitted successfully!";
//                    return RedirectToAction("Index");
//                }
//                catch (Exception ex)
//                {
//                    TempData["ErrorMessage"] = $"Failed to submit application: {ex.Message}";
//                }
//            }

//            return View(application);
//        }

//        [HttpGet]
//        public async Task<IActionResult> Edit(int id)
//        {
//            var application = await _context.AdminApplications.FindAsync(id);

//            if (application == null)
//            {
//                return NotFound();
//            }

//            return View(application);
//        }

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Edit(int id, AdminApplication application)
//        {
//            if (id != application.Id)
//            {
//                return NotFound();
//            }

//            if (ModelState.IsValid)
//            {
//                try
//                {
//                    _context.Update(application);
//                    await _context.SaveChangesAsync();

//                    TempData["SuccessMessage"] = "Application updated successfully!";
//                    return RedirectToAction("Index");
//                }
//                catch (DbUpdateConcurrencyException)
//                {
//                    if (!ApplicationExists(application.Id))
//                    {
//                        return NotFound();
//                    }
//                    else
//                    {
//                        throw;
//                    }
//                }
//            }

//            return View(application);
//        }

//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Delete(int id)
//        {
//            var application = await _context.AdminApplications.FindAsync(id);

//            if (application == null)
//            {
//                TempData["ErrorMessage"] = "Application not found.";
//                return RedirectToAction("Index");
//            }

//            try
//            {
//                _context.AdminApplications.Remove(application);
//                await _context.SaveChangesAsync();

//                TempData["SuccessMessage"] = "Application deleted successfully!";
//            }
//            catch (Exception ex)
//            {
//                TempData["ErrorMessage"] = $"Failed to delete application: {ex.Message}";
//            }

//            return RedirectToAction("Index");
//        }

//        [HttpGet]
//        public async Task<IActionResult> Pending()
//        {
//            var pendingApplications = await _context.AdminApplications
//                .Where(a => a.Status == ApplicationStatus.Pending)
//                .Include(a => a.University)
//                .Include(a => a.Faculty)
//                .Include(a => a.Department)
//                .OrderByDescending(a => a.AppliedDate)
//                .ToListAsync();

//            ViewBag.PendingCount = pendingApplications.Count;

//            return View(pendingApplications);
//        }

//        [HttpGet]
//        public async Task<IActionResult> Approved()
//        {
//            var approvedApplications = await _context.AdminApplications
//                .Where(a => a.Status == ApplicationStatus.Approved)
//                .Include(a => a.University)
//                .Include(a => a.Faculty)
//                .Include(a => a.Department)
//                .OrderByDescending(a => a.ReviewedDate)
//                .ToListAsync();

//            return View(approvedApplications);
//        }

//        [HttpGet]
//        public async Task<IActionResult> Rejected()
//        {
//            var rejectedApplications = await _context.AdminApplications
//                .Where(a => a.Status == ApplicationStatus.Rejected)
//                .Include(a => a.University)
//                .Include(a => a.Faculty)
//                .Include(a => a.Department)
//                .OrderByDescending(a => a.ReviewedDate)
//                .ToListAsync();

//            return View(rejectedApplications);
//        }

//        private bool ApplicationExists(int id)
//        {
//            return _context.AdminApplications.Any(e => e.Id == id);
//        }

//        // In ApplicationsController.cs, add this public action (no authorization required)
//        [AllowAnonymous]
//        [HttpGet]
//        public IActionResult Apply()
//        {
//            return View();
//        }

//        [AllowAnonymous]
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Apply(AdminApplication application)
//        {
//            if (ModelState.IsValid)
//            {
//                try
//                {
//                    // Set default values
//                    application.Status = ApplicationStatus.Pending;
//                    application.AppliedDate = DateTime.Now;
//                    application.CreatedDate = DateTime.UtcNow;

//                    _context.AdminApplications.Add(application);
//                    await _context.SaveChangesAsync();

//                    TempData["SuccessMessage"] = "Your application has been submitted successfully! An admin will review it shortly.";
//                    return RedirectToAction("ApplySuccess");
//                }
//                catch (Exception ex)
//                {
//                    TempData["ErrorMessage"] = $"Failed to submit application: {ex.Message}";
//                }
//            }

//            return View(application);
//        }

//        [AllowAnonymous]
//        [HttpGet]
//        public IActionResult ApplySuccess()
//        {
//            return View();
//        }

//        // In AdminManagementController.cs
//        [HttpGet]
//        [Authorize(Roles = "SuperAdmin,Admin")]
//        public async Task<IActionResult> ReviewApplication(int id)
//        {
//            var application = await _context.AdminApplications
//                .Include(a => a.University)
//                .Include(a => a.Faculty)
//                .Include(a => a.Department)
//                .FirstOrDefaultAsync(a => a.Id == id);

//            if (application == null)
//            {
//                return NotFound();
//            }

//            //return View(application);
//            return View("ReviewApplication", application);
//        }

//        public async Task<IActionResult> Details(int id)
//        {
//            var application = await _context.AdminApplications
//                .Include(a => a.University)
//                .Include(a => a.Faculty)
//                .Include(a => a.Department)
//                .FirstOrDefaultAsync(a => a.Id == id);

//            if (application == null)
//            {
//                return NotFound();
//            }

//            //return View(application);
//            return View("Details", application);
//        }
//    }
//}