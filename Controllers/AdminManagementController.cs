using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;
using StudentManagementSystem.Models.ViewModels;
using StudentManagementSystem.ViewModels;
using StudentManagementSystem.Services;
using System.Security.Claims;
using System.Text.Encodings.Web;

// Use MailKit only (remove System.Net.Mail)
using MailKit.Security;
using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Options;
using System.Net;

// REMOVE these:
// using System.Net.Mail;
// using SmtpClient = System.Net.Mail.SmtpClient;
// using MailMessage = System.Net.Mail.MailMessage;
// using MailAddress = System.Net.Mail.MailAddress;



namespace StudentManagementSystem.Controllers
{
    [Authorize]
    public class AdminManagementController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<CollegesController> _logger;
        private readonly IEmailService _emailService;
        private readonly ICompositeViewEngine _viewEngine;
        private readonly EmailSettings _emailSettings;

        public AdminManagementController(
            IAdminService adminService,
            UserManager<IdentityUser> userManager,
            ApplicationDbContext context,
            SignInManager<IdentityUser> signInManager,
            ILogger<CollegesController> logger,
            IEmailService emailService,
            ICompositeViewEngine viewEngine,
            EmailSettings emailSettings)  // Add this parameter
        {
            _adminService = adminService;
            _userManager = userManager;
            _context = context;
            _signInManager = signInManager;
            _logger = logger;
            _emailService = emailService;
            _viewEngine = viewEngine;  // Add this
            _emailSettings = emailSettings;
        }

        //[Authorize(Policy = "SuperAdminOnly")]
        //public async Task<IActionResult> Index(string filter = "all", string search = "", string sort = "created", string order = "desc")
        //{
        //    try
        //    {
        //        // Get data
        //        var privileges = await _adminService.GetAllAdminPrivilegesAsync();

        //        // Get approved applications
        //        var approvedApplications = await _context.AdminApplications
        //            .Include(a => a.University)
        //            .Include(a => a.Faculty)
        //            .Include(a => a.Department)
        //            .Where(a => a.Status == ApplicationStatus.Approved && string.IsNullOrEmpty(a.ApplicantId))
        //            .ToListAsync();

        //        // Create combined list
        //        var allAdmins = new List<AdminPrivilegeViewModel>();

        //        // Add existing admins
        //        allAdmins.AddRange(privileges.Select(p => new AdminPrivilegeViewModel
        //        {
        //            AdminId = p.AdminId,
        //            AdminName = p.Admin?.UserName ?? "Unknown",
        //            Email = p.Admin?.Email ?? "Unknown",
        //            AdminType = p.AdminType,
        //            Permissions = p.Permissions ?? new List<PermissionModule>(),
        //            UniversityScope = p.University?.Name,
        //            FacultyScope = p.Faculty?.Name,
        //            DepartmentScope = p.Department?.Name,
        //            IsActive = p.IsActive,
        //            CreatedDate = p.CreatedDate,
        //            CreatedBy = p.CreatedBy ?? "System",
        //            IsFromApplication = false,
        //            LastLoginDate = p.Admin?.LockoutEnd?.DateTime
        //        }));

        //        // Add approved applications
        //        allAdmins.AddRange(approvedApplications.Select(app => new AdminPrivilegeViewModel
        //        {
        //            AdminId = app.ApplicantId ?? $"APP_{app.Id}",
        //            AdminName = app.ApplicantName,
        //            Email = app.Email,
        //            AdminType = app.AppliedAdminType,
        //            Permissions = new List<PermissionModule>(),
        //            UniversityScope = app.University?.Name,
        //            FacultyScope = app.Faculty?.Name,
        //            DepartmentScope = app.Department?.Name,
        //            IsActive = false,
        //            CreatedDate = app.ReviewedDate ?? DateTime.Now,
        //            CreatedBy = app.ReviewedBy ?? "System",
        //            IsFromApplication = true,
        //            ApplicationId = app.Id
        //        }));

        //        // Apply search
        //        if (!string.IsNullOrEmpty(search))
        //        {
        //            search = search.ToLower();
        //            allAdmins = allAdmins.Where(a =>
        //                (a.AdminName?.ToLower().Contains(search) ?? false) ||
        //                (a.Email?.ToLower().Contains(search) ?? false) ||
        //                a.AdminType.ToString().ToLower().Contains(search) ||
        //                (a.UniversityScope?.ToLower().Contains(search) ?? false) ||
        //                (a.FacultyScope?.ToLower().Contains(search) ?? false) ||
        //                (a.DepartmentScope?.ToLower().Contains(search) ?? false)
        //            ).ToList();
        //        }

        //        // Apply filter
        //        switch (filter.ToLower())
        //        {
        //            case "active":
        //                allAdmins = allAdmins.Where(a => a.IsActive && !a.IsFromApplication).ToList();
        //                break;
        //            case "inactive":
        //                allAdmins = allAdmins.Where(a => !a.IsActive && !a.IsFromApplication).ToList();
        //                break;
        //            case "new":
        //                allAdmins = allAdmins.Where(a => a.IsFromApplication).ToList();
        //                break;
        //            case "superadmin":
        //                allAdmins = allAdmins.Where(a => a.AdminType == AdminType.SuperAdmin).ToList();
        //                break;
        //            case "university":
        //                allAdmins = allAdmins.Where(a => a.AdminType == AdminType.UniversityAdmin).ToList();
        //                break;
        //            case "faculty":
        //                allAdmins = allAdmins.Where(a => a.AdminType == AdminType.FacultyAdmin).ToList();
        //                break;
        //            case "department":
        //                allAdmins = allAdmins.Where(a => a.AdminType == AdminType.DepartmentAdmin).ToList();
        //                break;
        //            case "finance":
        //                allAdmins = allAdmins.Where(a => a.AdminType == AdminType.FinanceAdmin).ToList();
        //                break;
        //            case "student":
        //                allAdmins = allAdmins.Where(a => a.AdminType == AdminType.StudentAdmin).ToList();
        //                break;
        //        }

        //        // Apply sorting
        //        bool isDescending = order.ToLower() == "desc";
        //        allAdmins = sort.ToLower() switch
        //        {
        //            "name" => isDescending ?
        //                allAdmins.OrderByDescending(a => a.AdminName).ToList() :
        //                allAdmins.OrderBy(a => a.AdminName).ToList(),
        //            "type" => isDescending ?
        //                allAdmins.OrderByDescending(a => a.AdminType).ToList() :
        //                allAdmins.OrderBy(a => a.AdminType).ToList(),
        //            "status" => isDescending ?
        //                allAdmins.OrderByDescending(a => a.IsActive).ThenByDescending(a => a.IsFromApplication).ToList() :
        //                allAdmins.OrderBy(a => a.IsActive).ThenBy(a => a.IsFromApplication).ToList(),
        //            "email" => isDescending ?
        //                allAdmins.OrderByDescending(a => a.Email).ToList() :
        //                allAdmins.OrderBy(a => a.Email).ToList(),
        //            _ => isDescending ?
        //                allAdmins.OrderByDescending(a => a.CreatedDate).ToList() :
        //                allAdmins.OrderBy(a => a.CreatedDate).ToList()
        //        };

        //        // ViewBag data
        //        ViewBag.CurrentFilter = filter;
        //        ViewBag.CurrentSearch = search;
        //        ViewBag.CurrentSort = sort;
        //        ViewBag.CurrentOrder = order;
        //        ViewBag.PendingApplicationsCount = await _context.AdminApplications
        //            .CountAsync(a => a.Status == ApplicationStatus.Pending);
        //        ViewBag.TotalAdmins = allAdmins.Count;
        //        ViewBag.ActiveCount = allAdmins.Count(a => a.IsActive && !a.IsFromApplication);
        //        ViewBag.InactiveCount = allAdmins.Count(a => !a.IsActive && !a.IsFromApplication);
        //        ViewBag.NewApprovalsCount = allAdmins.Count(a => a.IsFromApplication);

        //        return View(allAdmins);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error loading admin management index");
        //        TempData["ErrorMessage"] = "Error loading admin data";
        //        return View(new List<AdminPrivilegeViewModel>());
        //    }
        //}

        [Authorize(Policy = "SuperAdminOnly")]
        public async Task<IActionResult> Index(string filter = "all", string search = "", string sort = "created", string order = "desc")
        {
            try
            {
                // Get data
                var privileges = await _adminService.GetAllAdminPrivilegesAsync();

                // Get approved applications
                var approvedApplications = await _context.AdminApplications
                    .Include(a => a.University)
                    .Include(a => a.Faculty)
                    .Include(a => a.Department)
                    .Where(a => a.Status == ApplicationStatus.Approved && string.IsNullOrEmpty(a.ApplicantId))
                    .ToListAsync();

                // Create combined list
                var allAdmins = new List<AdminPrivilegeViewModel>();

                // Add existing admins
                allAdmins.AddRange(privileges.Select(p => new AdminPrivilegeViewModel
                {
                    AdminId = p.AdminId,
                    AdminName = p.Admin?.UserName ?? "Unknown",
                    Email = p.Admin?.Email ?? "Unknown",
                    AdminType = p.AdminType,
                    Permissions = p.Permissions ?? new List<PermissionModule>(),
                    UniversityScope = p.University?.Name,
                    FacultyScope = p.Faculty?.Name,
                    DepartmentScope = p.Department?.Name,
                    IsActive = p.IsActive,
                    CreatedDate = p.CreatedDate,
                    CreatedBy = p.CreatedBy ?? "System",
                    IsFromApplication = false,
                    LastLoginDate = p.Admin?.LockoutEnd?.DateTime
                }));

                // Add approved applications
                allAdmins.AddRange(approvedApplications.Select(app => new AdminPrivilegeViewModel
                {
                    AdminId = app.ApplicantId ?? $"APP_{app.Id}",
                    AdminName = app.ApplicantName,
                    Email = app.Email,
                    AdminType = app.AppliedAdminType,
                    Permissions = new List<PermissionModule>(),
                    UniversityScope = app.University?.Name,
                    FacultyScope = app.Faculty?.Name,
                    DepartmentScope = app.Department?.Name,
                    IsActive = false,
                    CreatedDate = app.ReviewedDate ?? DateTime.Now,
                    CreatedBy = app.ReviewedBy ?? "System",
                    IsFromApplication = true,
                    ApplicationId = app.Id
                }));

                // Apply search
                if (!string.IsNullOrEmpty(search))
                {
                    search = search.ToLower();
                    allAdmins = allAdmins.Where(a =>
                        (a.AdminName?.ToLower().Contains(search) ?? false) ||
                        (a.Email?.ToLower().Contains(search) ?? false) ||
                        a.AdminType.ToString().ToLower().Contains(search) ||
                        (a.UniversityScope?.ToLower().Contains(search) ?? false) ||
                        (a.FacultyScope?.ToLower().Contains(search) ?? false) ||
                        (a.DepartmentScope?.ToLower().Contains(search) ?? false)
                    ).ToList();
                }

                // Apply filter - CHANGED SECTION
                switch (filter.ToLower())
                {
                    case "active":
                        allAdmins = allAdmins.Where(a => a.IsActive && !a.IsFromApplication).ToList();
                        break;
                    case "inactive":
                        allAdmins = allAdmins.Where(a => !a.IsActive && !a.IsFromApplication).ToList();
                        break;
                    case "all":
                        // Show all real admins (active AND inactive) but NOT applications
                        allAdmins = allAdmins.Where(a => !a.IsFromApplication).ToList();
                        break;
                    case "new":
                        allAdmins = allAdmins.Where(a => a.IsFromApplication).ToList();
                        break;
                    case "superadmin":
                        allAdmins = allAdmins.Where(a => a.AdminType == AdminType.SuperAdmin && !a.IsFromApplication).ToList();
                        break;
                    case "university":
                        allAdmins = allAdmins.Where(a => a.AdminType == AdminType.UniversityAdmin && !a.IsFromApplication).ToList();
                        break;
                    case "faculty":
                        allAdmins = allAdmins.Where(a => a.AdminType == AdminType.FacultyAdmin && !a.IsFromApplication).ToList();
                        break;
                    case "department":
                        allAdmins = allAdmins.Where(a => a.AdminType == AdminType.DepartmentAdmin && !a.IsFromApplication).ToList();
                        break;
                    case "finance":
                        allAdmins = allAdmins.Where(a => a.AdminType == AdminType.FinanceAdmin && !a.IsFromApplication).ToList();
                        break;
                    case "student":
                        allAdmins = allAdmins.Where(a => a.AdminType == AdminType.StudentAdmin && !a.IsFromApplication).ToList();
                        break;
                    default:
                        // Default to showing all real admins (including inactive)
                        allAdmins = allAdmins.Where(a => !a.IsFromApplication).ToList();
                        break;
                }

                // Apply sorting
                bool isDescending = order.ToLower() == "desc";
                allAdmins = sort.ToLower() switch
                {
                    "name" => isDescending ?
                        allAdmins.OrderByDescending(a => a.AdminName).ToList() :
                        allAdmins.OrderBy(a => a.AdminName).ToList(),
                    "type" => isDescending ?
                        allAdmins.OrderByDescending(a => a.AdminType).ToList() :
                        allAdmins.OrderBy(a => a.AdminType).ToList(),
                    "status" => isDescending ?
                        allAdmins.OrderByDescending(a => a.IsActive).ThenByDescending(a => a.IsFromApplication).ToList() :
                        allAdmins.OrderBy(a => a.IsActive).ThenBy(a => a.IsFromApplication).ToList(),
                    "email" => isDescending ?
                        allAdmins.OrderByDescending(a => a.Email).ToList() :
                        allAdmins.OrderBy(a => a.Email).ToList(),
                    _ => isDescending ?
                        allAdmins.OrderByDescending(a => a.CreatedDate).ToList() :
                        allAdmins.OrderBy(a => a.CreatedDate).ToList()
                };

                // ViewBag data
                ViewBag.CurrentFilter = filter;
                ViewBag.CurrentSearch = search;
                ViewBag.CurrentSort = sort;
                ViewBag.CurrentOrder = order;
                ViewBag.PendingApplicationsCount = await _context.AdminApplications
                    .CountAsync(a => a.Status == ApplicationStatus.Pending);
                ViewBag.TotalAdmins = allAdmins.Count;
                ViewBag.ActiveCount = allAdmins.Count(a => a.IsActive && !a.IsFromApplication);
                ViewBag.InactiveCount = allAdmins.Count(a => !a.IsActive && !a.IsFromApplication);
                ViewBag.NewApprovalsCount = allAdmins.Count(a => a.IsFromApplication);

                return View(allAdmins);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin management index");
                TempData["ErrorMessage"] = "Error loading admin data";
                return View(new List<AdminPrivilegeViewModel>());
            }
        }

        private async Task<List<AdminApplication>> GetApprovedApplicationsAsync()
        {
            return await _context.AdminApplications
                .Include(a => a.University)
                .Include(a => a.Faculty)
                .Include(a => a.Department)
                .Where(a => a.Status == ApplicationStatus.Approved && string.IsNullOrEmpty(a.ApplicantId))
                .ToListAsync();
        }

        private async Task<List<AdminPrivilegeViewModel>> CreateCombinedAdminViewModels(
    List<AdminPrivilege> privileges,
    List<AdminApplication> approvedApplications)
        {
            // Create admin view models
            var adminViewModels = await Task.Run(() => privileges.Select(p => new AdminPrivilegeViewModel
            {
                AdminId = p.AdminId,
                AdminName = p.Admin?.UserName ?? "Unknown",
                Email = p.Admin?.Email ?? "Unknown",
                AdminType = p.AdminType,
                Permissions = p.Permissions ?? new List<PermissionModule>(),
                UniversityScope = p.University?.Name,
                FacultyScope = p.Faculty?.Name,
                DepartmentScope = p.Department?.Name,
                IsActive = p.IsActive,
                CreatedDate = p.CreatedDate,
                CreatedBy = p.CreatedBy ?? "System",
                IsFromApplication = false,
                LastLoginDate = p.Admin?.LockoutEnd?.DateTime
            }).ToList());

            // Create application view models
            var applicationViewModels = await Task.Run(() => approvedApplications.Select(app => new AdminPrivilegeViewModel
            {
                AdminId = app.ApplicantId ?? $"APP_{app.Id}",
                AdminName = app.ApplicantName,
                Email = app.Email,
                AdminType = app.AppliedAdminType,
                Permissions = new List<PermissionModule>(),
                UniversityScope = app.University?.Name,
                FacultyScope = app.Faculty?.Name,
                DepartmentScope = app.Department?.Name,
                IsActive = false,
                CreatedDate = app.ReviewedDate ?? DateTime.Now,
                CreatedBy = app.ReviewedBy ?? "System",
                IsFromApplication = true,
                ApplicationId = app.Id,
                ApplicationData = app
            }).ToList());

            // Combine both lists
            return adminViewModels.Concat(applicationViewModels).ToList();
        }

        private List<AdminPrivilegeViewModel> ApplySearch(List<AdminPrivilegeViewModel> admins, string search)
        {
            if (string.IsNullOrEmpty(search)) return admins;

            search = search.ToLower();
            return admins.Where(a =>
                (a.AdminName?.ToLower().Contains(search) ?? false) ||
                (a.Email?.ToLower().Contains(search) ?? false) ||
                a.AdminType.ToString().ToLower().Contains(search) ||
                (a.UniversityScope?.ToLower().Contains(search) ?? false) ||
                (a.FacultyScope?.ToLower().Contains(search) ?? false) ||
                (a.DepartmentScope?.ToLower().Contains(search) ?? false) ||
                (a.CreatedBy?.ToLower().Contains(search) ?? false)
            ).ToList();
        }

        private List<AdminPrivilegeViewModel> ApplyFilter(List<AdminPrivilegeViewModel> admins, string filter)
        {
            return filter?.ToLower() switch
            {
                "active" => admins.Where(a => a.IsActive && !a.IsFromApplication).ToList(),
                "inactive" => admins.Where(a => !a.IsActive && !a.IsFromApplication).ToList(),
                "new" => admins.Where(a => a.IsFromApplication).ToList(),
                "superadmin" => admins.Where(a => a.AdminType == AdminType.SuperAdmin).ToList(),
                "university" => admins.Where(a => a.AdminType == AdminType.UniversityAdmin).ToList(),
                "faculty" => admins.Where(a => a.AdminType == AdminType.FacultyAdmin).ToList(),
                "department" => admins.Where(a => a.AdminType == AdminType.DepartmentAdmin).ToList(),
                "finance" => admins.Where(a => a.AdminType == AdminType.FinanceAdmin).ToList(),
                "student" => admins.Where(a => a.AdminType == AdminType.StudentAdmin).ToList(),
                "custom" => admins.Where(a => a.AdminType == AdminType.CustomAdmin).ToList(),
                _ => admins
            };
        }

        private List<AdminPrivilegeViewModel> ApplySorting(List<AdminPrivilegeViewModel> admins, string sort, string order)
        {
            var isDescending = order?.ToLower() == "desc";

            return sort?.ToLower() switch
            {
                "name" => isDescending ?
                    admins.OrderByDescending(a => a.AdminName).ToList() :
                    admins.OrderBy(a => a.AdminName).ToList(),
                "type" => isDescending ?
                    admins.OrderByDescending(a => a.AdminType).ToList() :
                    admins.OrderBy(a => a.AdminType).ToList(),
                "status" => isDescending ?
                    admins.OrderByDescending(a => a.IsActive).ThenByDescending(a => a.IsFromApplication).ToList() :
                    admins.OrderBy(a => a.IsActive).ThenBy(a => a.IsFromApplication).ToList(),
                "email" => isDescending ?
                    admins.OrderByDescending(a => a.Email).ToList() :
                    admins.OrderBy(a => a.Email).ToList(),
                "created" => isDescending ?
                    admins.OrderByDescending(a => a.CreatedDate).ToList() :
                    admins.OrderBy(a => a.CreatedDate).ToList(),
                _ => isDescending ?
                    admins.OrderByDescending(a => a.CreatedDate).ToList() :
                    admins.OrderBy(a => a.CreatedDate).ToList()
            };
        }

        private async Task<int> GetPendingApplicationsCountAsync()
        {
            return await _context.AdminApplications
                .CountAsync(a => a.Status == ApplicationStatus.Pending);
        }

        [HttpGet]
        [Authorize(Policy = "SuperAdminOnly")]
        public IActionResult CompleteSetup(int id) // Changed from async Task<IActionResult>
        {
            return RedirectToAction("SetupAdmin", "AdminManagement", new { applicationId = id });
        }
        //[HttpGet]
        //[Authorize(Policy = "SuperAdminOnly")]
        //public async Task<IActionResult> Create()
        //{
        //    var model = new CreateAdminViewModel
        //    {
        //        // Load templates from database
        //        AvailableTemplates = await _context.AdminPrivilegeTemplates
        //            .Where(t => t.IsActive)
        //            .OrderBy(t => t.AdminType)
        //            .ThenBy(t => t.TemplateName)
        //            .ToListAsync(),
        //        Universities = await _context.Universities.Where(u => u.IsActive).ToListAsync(),
        //        Colleges = await _context.Colleges.Where(c => c.IsActive).ToListAsync(),
        //        Departments = await _context.Departments.Where(d => d.IsActive).ToListAsync()
        //    };

        //    return View(model);
        //}

        [HttpGet]
        [Authorize(Policy = "SuperAdminOnly")]
        public async Task<IActionResult> Create()
        {
            // DEBUG: Log to console
            Console.WriteLine("Loading Create Admin page...");

            var templates = await _context.AdminPrivilegeTemplates.ToListAsync();
            Console.WriteLine($"Found {templates.Count} templates in database");

            var model = new CreateAdminViewModel
            {
                AvailableTemplates = templates, // Don't filter by IsActive for now
                Universities = await _context.Universities.Where(u => u.IsActive).ToListAsync(),
                Colleges = await _context.Colleges.Where(c => c.IsActive).ToListAsync(),
                Departments = await _context.Departments.Where(d => d.IsActive).ToListAsync()
            };

            Console.WriteLine($"Loaded {model.AvailableTemplates.Count} templates to view model");

            return View(model);
        }


        [HttpPost]
        [Authorize(Policy = "SuperAdminOnly")]
        public async Task<IActionResult> Create(CreateAdminViewModel model)
        {
            if (ModelState.IsValid)
            {
                var currentUser = User.Identity?.Name ?? "System";

                if (model.TemplateId.HasValue)
                {
                    var template = await _context.AdminPrivilegeTemplates
                        .FirstOrDefaultAsync(t => t.Id == model.TemplateId.Value);
                    if (template != null)
                    {
                        model.SelectedPermissions = template.DefaultPermissions;
                    }
                }

                var result = await _adminService.CreateAdminWithPrivilegesAsync(model, currentUser);
                if (result)
                {
                    TempData["SuccessMessage"] = "Admin created successfully";
                    return RedirectToAction("Index");
                }
                TempData["ErrorMessage"] = "Failed to create admin";
            }

            // RELOAD templates on validation error
            model.AvailableTemplates = await _context.AdminPrivilegeTemplates
                .Where(t => t.IsActive)
                .OrderBy(t => t.AdminType)
                .ThenBy(t => t.TemplateName)
                .ToListAsync();
            model.Universities = await _context.Universities.Where(u => u.IsActive).ToListAsync();
            model.Colleges = await _context.Colleges.Where(c => c.IsActive).ToListAsync();
            model.Departments = await _context.Departments.Where(d => d.IsActive).ToListAsync();

            return View(model);
        }

        [HttpGet]
        public async Task<JsonResult> GetTemplatePermissions(int templateId)
        {
            try
            {
                var template = await _context.AdminPrivilegeTemplates
                    .FirstOrDefaultAsync(t => t.Id == templateId);

                if (template == null)
                {
                    return Json(new { success = false, message = "Template not found" });
                }

                return Json(new
                {
                    success = true,
                    templateName = template.TemplateName,
                    description = template.Description,
                    permissionCount = template.DefaultPermissions.Count,
                    permissions = template.DefaultPermissions.Select(p => p.ToString()).ToList()
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Apply()
        {
            var model = new AdminApplicationFormViewModel
            {
                Universities = await _context.Universities.Where(u => u.IsActive).ToListAsync(),
                Colleges = await _context.Colleges.Where(c => c.IsActive).ToListAsync(),
                Departments = await _context.Departments.Where(d => d.IsActive).ToListAsync()
            };

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Apply(AdminApplicationFormViewModel model)
        {
            if (ModelState.IsValid)
            {
                var application = new AdminApplication
                {
                    ApplicantName = model.ApplicantName,
                    Email = model.Email,
                    Phone = model.Phone,
                    AppliedAdminType = model.AppliedAdminType,
                    UniversityId = string.IsNullOrEmpty(model.UniversityId) ? null : int.Parse(model.UniversityId),
                    FacultyId = string.IsNullOrEmpty(model.FacultyId) ? null : int.Parse(model.FacultyId),
                    DepartmentId = string.IsNullOrEmpty(model.DepartmentId) ? null : int.Parse(model.DepartmentId),
                    Justification = model.Justification,
                    Experience = model.Experience,
                    Qualifications = model.Qualifications,
                    AppliedDate = DateTime.Now,
                    Status = Models.ApplicationStatus.Pending
                };

                var result = await _adminService.SubmitApplicationAsync(application);
                if (result)
                {
                    TempData["SuccessMessage"] = "Application submitted successfully. We will review it soon.";
                    return RedirectToAction("ApplicationStatus");
                }
                TempData["ErrorMessage"] = "Failed to submit application";
            }

            model.Universities = await _context.Universities.Where(u => u.IsActive).ToListAsync();
            model.Colleges = await _context.Colleges.Where(c => c.IsActive).ToListAsync();
            model.Departments = await _context.Departments.Where(d => d.IsActive).ToListAsync();

            return View(model);
        }

        [Authorize(Policy = "SuperAdminOnly")]
        public async Task<IActionResult> Applications()
        {
            var applications = await _context.AdminApplications
                .Include(a => a.University)
                .Include(a => a.Faculty)
                .Include(a => a.Department)
                .Where(a => a.Status == ApplicationStatus.Pending)
                .ToListAsync();

            var viewModel = applications.Select(a => new AdminApplicationViewModel
            {
                Id = a.Id,
                ApplicantName = a.ApplicantName,
                Email = a.Email,
                Phone = a.Phone,
                AppliedAdminType = a.AppliedAdminType,
                Justification = a.Justification,
                Experience = a.Experience,
                Qualifications = a.Qualifications,
                Status = a.Status,
                AppliedDate = a.AppliedDate,
                ReviewedDate = a.ReviewedDate,
                ReviewedBy = a.ReviewedBy,
                ReviewNotes = a.ReviewNotes,
                UniversityName = a.University?.Name,
                FacultyName = a.Faculty?.Name,
                DepartmentName = a.Department?.Name
            }).ToList();

            return View(viewModel);
        }

        [HttpGet]
        [Authorize(Policy = "SuperAdminOnly")]
        public async Task<IActionResult> ReviewApplication(int id)
        {
            var application = await _context.AdminApplications
                .Include(a => a.University)
                .Include(a => a.Faculty)
                .Include(a => a.Department)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null)
            {
                TempData["ErrorMessage"] = "Application not found";
                return RedirectToAction("Applications");
            }

            var viewModel = new AdminApplicationViewModel
            {
                Id = application.Id,
                ApplicantName = application.ApplicantName,
                Email = application.Email,
                Phone = application.Phone,
                AppliedAdminType = application.AppliedAdminType,
                Justification = application.Justification,
                Experience = application.Experience,
                Qualifications = application.Qualifications,
                Status = application.Status,
                AppliedDate = application.AppliedDate,
                UniversityName = application.University?.Name,
                FacultyName = application.Faculty?.Name,
                DepartmentName = application.Department?.Name
            };

            //return View(viewModel);
            return RedirectToAction("Index", "Settings");
        }

        [HttpPost]
        [Authorize(Policy = "SuperAdminOnly")]
        public async Task<IActionResult> ReviewApplication(int id, Models.ApplicationStatus status, string reviewNotes)
        {
            var result = await _adminService.ReviewApplicationAsync(id, status, User.Identity?.Name ?? "System", reviewNotes);
            if (result)
            {
                if (status == Models.ApplicationStatus.Approved)
                {
                    TempData["SuccessMessage"] = "Application approved. Please complete admin account setup.";
                    return RedirectToAction("SetupAdmin", new { applicationId = id });
                }
                else
                {
                    TempData["SuccessMessage"] = $"Application {status.ToString().ToLower()} successfully";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to review application";
            }

            return RedirectToAction("Applications");
        }

        [HttpGet]
        [Authorize(Policy = "SuperAdminOnly")]
        public async Task<IActionResult> EditPrivileges(string id)
        {
            var privilege = await _adminService.GetAdminPrivilegeAsync(id);
            if (privilege == null)
            {
                TempData["ErrorMessage"] = "Admin not found";
                return RedirectToAction("Index");
            }

            var model = new CreateAdminViewModel
            {
                // Use AdminId and AdminName for edit
                AdminId = privilege.AdminId,
                AdminName = privilege.Admin?.UserName ?? "N/A",

                // Set form fields
                FullName = privilege.Admin?.UserName ?? "N/A",
                Email = privilege.Admin?.Email ?? "N/A",
                AdminType = privilege.AdminType,
                SelectedPermissions = privilege.Permissions ?? new List<PermissionModule>(),
                CurrentPermissions = privilege.Permissions ?? new List<PermissionModule>(), // ADD THIS LINE

                // Set scope - check if privilege has these properties
                UniversityScope = privilege.UniversityId?.ToString(),
                FacultyScope = privilege.FacultyId?.ToString(),
                DepartmentScope = privilege.DepartmentId?.ToString(),

                // Populate dropdown data
                AvailableTemplates = await _context.AdminPrivilegeTemplates
                    .Where(t => t.IsActive)
                    .ToListAsync(),
                Universities = await _context.Universities.Where(u => u.IsActive).ToListAsync(),
                Colleges = await _context.Colleges.Where(c => c.IsActive).ToListAsync(),
                Departments = await _context.Departments.Where(d => d.IsActive).ToListAsync(),

                // Clear passwords for edit
                Password = string.Empty,
                ConfirmPassword = string.Empty
            };

            return View(model);
        }

        [HttpPost]
        [Authorize(Policy = "SuperAdminOnly")]
        public async Task<IActionResult> EditPrivileges(string id, CreateAdminViewModel model)
        {
            var result = await _adminService.UpdateAdminPrivilegesAsync(
                id,
                model.SelectedPermissions,
                User.Identity?.Name ?? "System",
                model.AdminType,
                model.UniversityScope,
                model.FacultyScope,
                model.DepartmentScope,
                model.Password // This can be empty/null
            );

            if (result)
            {
                TempData["SuccessMessage"] = "Admin privileges updated successfully";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to update admin privileges";
            }

            return RedirectToAction("Index");
        }


        [Authorize(Policy = "SuperAdminOnly")]
        public async Task<IActionResult> Dashboard()
        {
            var dashboardData = await _adminService.GetDashboardDataAsync();
            return View(dashboardData);
        }

        public async Task<IActionResult> MyApplicationStatus()
        {
            var userEmail = User.Identity?.Name;
            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Apply");
            }

            var applications = await _context.AdminApplications
                .Include(a => a.University)
                .Include(a => a.Faculty)
                .Include(a => a.Department)
                .Where(a => a.Email == userEmail)
                .OrderByDescending(a => a.AppliedDate)
                .ToListAsync();

            var viewModel = applications.Select(a => new AdminApplicationViewModel
            {
                Id = a.Id,
                ApplicantName = a.ApplicantName,
                Email = a.Email,
                Phone = a.Phone,
                AppliedAdminType = a.AppliedAdminType,
                Justification = a.Justification,
                Status = a.Status,
                AppliedDate = a.AppliedDate,
                ReviewedDate = a.ReviewedDate,
                ReviewedBy = a.ReviewedBy,
                ReviewNotes = a.ReviewNotes,
                UniversityName = a.University?.Name,
                FacultyName = a.Faculty?.Name,
                DepartmentName = a.Department?.Name
            }).ToList();

            return View(viewModel);
        }



        [HttpGet]
        [Authorize(Policy = "SuperAdminOnly")]
        public async Task<IActionResult> ExportAdmins(string format = "excel")
        {
            try
            {
                var admins = await _context.AdminPrivileges
                    .Include(a => a.Admin)
                    .Include(a => a.University)
                    .Include(a => a.Faculty)
                    .Include(a => a.Department)
                    .ToListAsync();

                using (var package = new OfficeOpenXml.ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Admins");

                    // Format headers
                    using (var headerRange = worksheet.Cells[1, 1, 1, 10])
                    {
                        headerRange.Style.Font.Bold = true;
                        headerRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                        headerRange.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                        headerRange.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                    }

                    // Add headers
                    worksheet.Cells[1, 1].Value = "ID";
                    worksheet.Cells[1, 2].Value = "Email";
                    worksheet.Cells[1, 3].Value = "Admin Type";
                    worksheet.Cells[1, 4].Value = "University Scope";
                    worksheet.Cells[1, 5].Value = "Faculty Scope";
                    worksheet.Cells[1, 6].Value = "Department Scope";
                    worksheet.Cells[1, 7].Value = "Permissions";
                    worksheet.Cells[1, 8].Value = "Status";
                    worksheet.Cells[1, 9].Value = "Created Date";
                    worksheet.Cells[1, 10].Value = "Created By";

                    // Add data
                    int row = 2;
                    foreach (var admin in admins)
                    {
                        worksheet.Cells[row, 1].Value = admin.Id;
                        worksheet.Cells[row, 2].Value = admin.Admin?.Email ?? "N/A";
                        worksheet.Cells[row, 3].Value = admin.AdminType.ToString();
                        worksheet.Cells[row, 4].Value = admin.University?.Name ?? "Global";
                        worksheet.Cells[row, 5].Value = admin.Faculty?.Name ?? "Global";
                        worksheet.Cells[row, 6].Value = admin.Department?.Name ?? "Global";

                        // Get permissions
                        var permissions = admin.Permissions;
                        var permissionNames = permissions.Select(p => p.ToString().Replace('_', ' '));
                        worksheet.Cells[row, 7].Value = string.Join(", ", permissionNames);

                        worksheet.Cells[row, 8].Value = admin.IsActive ? "Active" : "Inactive";
                        worksheet.Cells[row, 9].Value = admin.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss");
                        worksheet.Cells[row, 10].Value = admin.CreatedBy;

                        // Format status cell
                        var statusCell = worksheet.Cells[row, 8];
                        if (admin.IsActive)
                        {
                            statusCell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                            statusCell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGreen);
                        }
                        else
                        {
                            statusCell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                            statusCell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightCoral);
                        }

                        row++;
                    }

                    // Auto fit columns
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    // Add summary sheet
                    var summarySheet = package.Workbook.Worksheets.Add("Summary");

                    // Add summary statistics
                    summarySheet.Cells[1, 1].Value = "Admin Export Summary";
                    summarySheet.Cells[1, 1].Style.Font.Size = 16;
                    summarySheet.Cells[1, 1].Style.Font.Bold = true;

                    summarySheet.Cells[3, 1].Value = "Total Admins:";
                    summarySheet.Cells[3, 2].Value = admins.Count;

                    summarySheet.Cells[4, 1].Value = "Active Admins:";
                    summarySheet.Cells[4, 2].Value = admins.Count(a => a.IsActive);

                    summarySheet.Cells[5, 1].Value = "Inactive Admins:";
                    summarySheet.Cells[5, 2].Value = admins.Count(a => !a.IsActive);

                    // Admin type distribution
                    var adminTypes = admins.GroupBy(a => a.AdminType)
                                          .Select(g => new { Type = g.Key, Count = g.Count() })
                                          .OrderByDescending(g => g.Count);

                    summarySheet.Cells[7, 1].Value = "Admin Type Distribution:";
                    summarySheet.Cells[7, 1].Style.Font.Bold = true;

                    int summaryRow = 8;
                    foreach (var type in adminTypes)
                    {
                        summarySheet.Cells[summaryRow, 1].Value = type.Type.ToString();
                        summarySheet.Cells[summaryRow, 2].Value = type.Count;
                        summaryRow++;
                    }

                    summarySheet.Cells[summarySheet.Dimension.Address].AutoFitColumns();

                    // Return the file
                    var fileName = $"Admins_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    var stream = new MemoryStream(package.GetAsByteArray());

                    return File(stream, contentType, fileName);
                }
            }
            catch (Exception ex)
            {
                // Log the error
                _logger.LogError(ex, "Error exporting admins");

                // Return error message
                TempData["ErrorMessage"] = $"Failed to export admins: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        [Authorize(Policy = "SuperAdminOnly")]
        public async Task<IActionResult> ExportApplications(string format = "excel")
        {
            try
            {
                var filePath = Path.Combine(Path.GetTempPath(), $"Applications_Export_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
                var result = await _adminService.ExportApplicationsToExcelAsync(filePath);

                if (result && System.IO.File.Exists(filePath))
                {
                    var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                    System.IO.File.Delete(filePath);
                    return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Applications_Export.xlsx");
                }

                TempData["ErrorMessage"] = "Failed to export applications";
                return RedirectToAction("Applications");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Export failed: {ex.Message}";
                return RedirectToAction("Applications");
            }
        }

        [HttpGet]
        [Authorize(Policy = "SuperAdminOnly")]
        public IActionResult ImportAdmins()
        {
            var model = new AdminImportViewModel();
            return View(model);
        }

        [HttpPost]
        [Authorize(Policy = "SuperAdminOnly")]
        public async Task<IActionResult> ImportAdmins(AdminImportViewModel model)
        {
            if (model.ImportFile == null || model.ImportFile.Length == 0)
            {
                ModelState.AddModelError("ImportFile", "Please select a file to import");
                return View(model);
            }

            // Validate file extension
            var allowedExtensions = new[] { ".xlsx", ".xls", ".csv" };
            var fileExtension = Path.GetExtension(model.ImportFile.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(fileExtension))
            {
                ModelState.AddModelError("ImportFile", "Invalid file type. Please upload Excel (.xlsx, .xls) or CSV files.");
                return View(model);
            }

            // Validate file size (max 10MB)
            if (model.ImportFile.Length > 10 * 1024 * 1024)
            {
                ModelState.AddModelError("ImportFile", "File size exceeds 10MB limit.");
                return View(model);
            }

            try
            {
                var tempFilePath = Path.GetTempFileName();

                // Save uploaded file to temp location
                using (var stream = new FileStream(tempFilePath, FileMode.Create))
                {
                    await model.ImportFile.CopyToAsync(stream);
                }

                // Process the file
                var importResult = await ProcessAdminImportFile(tempFilePath, model.OverwriteExisting, model.SendWelcomeEmail, User.Identity?.Name ?? "System");

                // Clean up temp file
                System.IO.File.Delete(tempFilePath);

                if (importResult.Success)
                {
                    TempData["SuccessMessage"] = $"Import completed successfully! {importResult.ImportedCount} admin(s) imported, {importResult.SkippedCount} skipped.";

                    if (importResult.Errors.Any())
                    {
                        TempData["WarningMessage"] = $"Some records had errors: {string.Join(", ", importResult.Errors.Take(3))}";
                    }

                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["ErrorMessage"] = $"Import failed: {importResult.ErrorMessage}";
                    if (importResult.Errors.Any())
                    {
                        TempData["WarningMessage"] = $"Errors: {string.Join(", ", importResult.Errors.Take(5))}";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error during import: {ex.Message}";
            }

            return View(model);
        }

        private async Task<AdminImportResult> ProcessAdminImportFile(string filePath, bool overwriteExisting, bool sendWelcomeEmail, string createdBy)
        {
            var result = new AdminImportResult();

            try
            {
                using (var package = new OfficeOpenXml.ExcelPackage(new FileInfo(filePath)))
                {
                    var worksheet = package.Workbook.Worksheets.FirstOrDefault();

                    if (worksheet == null)
                    {
                        result.ErrorMessage = "No worksheets found in the file";
                        return result;
                    }

                    // Validate headers
                    var headers = new Dictionary<string, int>();
                    for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                    {
                        var header = worksheet.Cells[1, col].Text?.Trim();
                        if (!string.IsNullOrEmpty(header))
                        {
                            headers[header.ToLowerInvariant()] = col;
                        }
                    }

                    // Check required headers
                    var requiredHeaders = new[] { "email", "fullname", "admintype", "password" };
                    foreach (var required in requiredHeaders)
                    {
                        if (!headers.ContainsKey(required))
                        {
                            result.ErrorMessage = $"Missing required column: {required}";
                            return result;
                        }
                    }

                    // Process rows
                    for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                    {
                        try
                        {
                            var email = worksheet.Cells[row, headers["email"]].Text?.Trim();
                            var fullName = worksheet.Cells[row, headers["fullname"]].Text?.Trim();
                            var adminTypeStr = worksheet.Cells[row, headers["admintype"]].Text?.Trim();
                            var password = worksheet.Cells[row, headers["password"]].Text?.Trim();
                            var permissionsStr = headers.ContainsKey("permissions") ?
                                worksheet.Cells[row, headers["permissions"]].Text?.Trim() : "";

                            // Validate required fields
                            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(fullName) ||
                                string.IsNullOrEmpty(adminTypeStr) || string.IsNullOrEmpty(password))
                            {
                                result.Errors.Add($"Row {row}: Missing required fields");
                                result.SkippedCount++;
                                continue;
                            }

                            // Validate AdminType
                            if (!Enum.TryParse<AdminType>(adminTypeStr, out var adminType))
                            {
                                result.Errors.Add($"Row {row}: Invalid AdminType '{adminTypeStr}'");
                                result.SkippedCount++;
                                continue;
                            }

                            // Parse permissions
                            var permissions = new List<PermissionModule>();
                            if (!string.IsNullOrEmpty(permissionsStr))
                            {
                                foreach (var perm in permissionsStr.Split(','))
                                {
                                    var trimmedPerm = perm.Trim();
                                    if (Enum.TryParse<PermissionModule>(trimmedPerm, out var permission))
                                    {
                                        permissions.Add(permission);
                                    }
                                    else
                                    {
                                        // Try case-insensitive parse
                                        var permissionMatch = Enum.GetValues<PermissionModule>()
                                            .FirstOrDefault(p => p.ToString().Equals(trimmedPerm, StringComparison.OrdinalIgnoreCase));
                                        if (permissionMatch != default)
                                        {
                                            permissions.Add(permissionMatch);
                                        }
                                        else
                                        {
                                            result.Errors.Add($"Row {row}: Invalid permission '{trimmedPerm}'");
                                        }
                                    }
                                }
                            }

                            // Check if admin already exists
                            var existingAdmin = await _userManager.FindByEmailAsync(email);

                            if (existingAdmin != null && !overwriteExisting)
                            {
                                result.Errors.Add($"Row {row}: Admin with email '{email}' already exists (skipped)");
                                result.SkippedCount++;
                                continue;
                            }

                            if (existingAdmin == null)
                            {
                                // Create new admin
                                var user = new IdentityUser
                                {
                                    UserName = email,
                                    Email = email,
                                    EmailConfirmed = true
                                };

                                var createResult = await _userManager.CreateAsync(user, password);

                                if (!createResult.Succeeded)
                                {
                                    result.Errors.Add($"Row {row}: Failed to create user: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
                                    result.SkippedCount++;
                                    continue;
                                }

                                // Create admin privileges
                                var privilege = new AdminPrivilege
                                {
                                    AdminId = user.Id,
                                    AdminType = adminType,
                                    CreatedBy = createdBy,
                                    CreatedDate = DateTime.Now,
                                    IsActive = true
                                };

                                // Set permissions using the property (which will save to PermissionsData)
                                privilege.Permissions = permissions;

                                // Add scope if provided
                                if (headers.ContainsKey("universityscope"))
                                {
                                    var universityIdStr = worksheet.Cells[row, headers["universityscope"]].Text?.Trim();
                                    if (int.TryParse(universityIdStr, out var universityId) && universityId > 0)
                                    {
                                        // Verify university exists
                                        var universityExists = await _context.Universities.AnyAsync(u => u.Id == universityId);
                                        if (universityExists)
                                        {
                                            privilege.UniversityScope = universityId;
                                        }
                                        else
                                        {
                                            result.Errors.Add($"Row {row}: University with ID '{universityId}' not found");
                                        }
                                    }
                                }

                                if (headers.ContainsKey("facultyscope"))
                                {
                                    var facultyIdStr = worksheet.Cells[row, headers["facultyscope"]].Text?.Trim();
                                    if (int.TryParse(facultyIdStr, out var facultyId) && facultyId > 0)
                                    {
                                        // Verify faculty/college exists
                                        var facultyExists = await _context.Colleges.AnyAsync(c => c.Id == facultyId);
                                        if (facultyExists)
                                        {
                                            privilege.FacultyScope = facultyId;
                                        }
                                        else
                                        {
                                            result.Errors.Add($"Row {row}: Faculty/College with ID '{facultyId}' not found");
                                        }
                                    }
                                }

                                if (headers.ContainsKey("departmentscope"))
                                {
                                    var departmentIdStr = worksheet.Cells[row, headers["departmentscope"]].Text?.Trim();
                                    if (int.TryParse(departmentIdStr, out var departmentId) && departmentId > 0)
                                    {
                                        // Verify department exists
                                        var departmentExists = await _context.Departments.AnyAsync(d => d.Id == departmentId);
                                        if (departmentExists)
                                        {
                                            privilege.DepartmentScope = departmentId;
                                        }
                                        else
                                        {
                                            result.Errors.Add($"Row {row}: Department with ID '{departmentId}' not found");
                                        }
                                    }
                                }

                                _context.AdminPrivileges.Add(privilege);
                                result.ImportedCount++;

                                // Send welcome email if requested
                                if (sendWelcomeEmail)
                                {
                                    try
                                    {
                                        await SendWelcomeEmailAsync(email, fullName, password, adminType);
                                    }
                                    catch (Exception ex)
                                    {
                                        result.Errors.Add($"Row {row}: Failed to send welcome email: {ex.Message}");
                                    }
                                }
                            }
                            else if (overwriteExisting)
                            {
                                // Update existing admin
                                var privilege = await _context.AdminPrivileges
                                    .FirstOrDefaultAsync(ap => ap.AdminId == existingAdmin.Id);

                                if (privilege != null)
                                {
                                    privilege.AdminType = adminType;
                                    privilege.Permissions = permissions; // This updates PermissionsData
                                    privilege.ModifiedDate = DateTime.Now;
                                    // Note: There's no UpdatedBy property, only CreatedBy

                                    result.ImportedCount++;
                                }
                                else
                                {
                                    // Admin exists but no privilege record - create one
                                    var newPrivilege = new AdminPrivilege
                                    {
                                        AdminId = existingAdmin.Id,
                                        AdminType = adminType,
                                        Permissions = permissions,
                                        CreatedBy = createdBy,
                                        CreatedDate = DateTime.Now,
                                        IsActive = true
                                    };

                                    _context.AdminPrivileges.Add(newPrivilege);
                                    result.ImportedCount++;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            result.Errors.Add($"Row {row}: Error - {ex.Message}");
                            result.SkippedCount++;
                        }
                    }

                    await _context.SaveChangesAsync();
                    result.Success = true;
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Error processing file: {ex.Message}";
                result.Success = false;
            }

            return result;
        }

        [HttpGet]
        [Authorize(Policy = "SuperAdminOnly")]
        public async Task<IActionResult> BulkOperations()
        {
            var admins = await _adminService.GetAllAdminPrivilegesAsync();
            var model = new BulkAdminOperationViewModel
            {
                AvailableAdmins = admins.Select(a => new AdminPrivilegeViewModel
                {
                    AdminId = a.AdminId,
                    AdminName = a.Admin?.UserName ?? "N/A",
                    Email = a.Admin?.Email ?? "N/A",
                    AdminType = a.AdminType,
                    Permissions = a.Permissions
                }).ToList()
            };

            return View(model);
        }

        
        [HttpPost]
        [Authorize(Policy = "SuperAdminOnly")]
        public async Task<IActionResult> BulkOperations(BulkAdminOperationViewModel model)
        {
            if (ModelState.IsValid && model.SelectedAdminIds.Any())
            {
                bool result = false;

                switch (model.OperationType)
                {
                    case BulkOperationType.UpdatePermissions:
                        result = await _adminService.BulkUpdatePermissionsAsync(
                            model.SelectedAdminIds, model.NewPermissions, User.Identity?.Name ?? "System");
                        break;

                    case BulkOperationType.ChangeAdminType when model.NewAdminType.HasValue:
                        result = await _adminService.BulkChangeAdminTypeAsync(
                            model.SelectedAdminIds, model.NewAdminType.Value, User.Identity?.Name ?? "System");
                        break;

                    case BulkOperationType.Activate:
                        foreach (var adminId in model.SelectedAdminIds)
                        {
                            await _adminService.ActivateAdminAsync(adminId, User.Identity?.Name ?? "System");
                        }
                        result = true;
                        break;

                    case BulkOperationType.Deactivate:
                        foreach (var adminId in model.SelectedAdminIds)
                        {
                            await _adminService.DeactivateAdminAsync(adminId, User.Identity?.Name ?? "System");
                        }
                        result = true;
                        break;
                }

                if (result)
                {
                    TempData["SuccessMessage"] = $"Bulk operation completed successfully for {model.SelectedAdminIds.Count} admins";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to perform bulk operation";
                }

                return RedirectToAction("Index");
            }

            var admins = await _adminService.GetAllAdminPrivilegesAsync();
            model.AvailableAdmins = admins.Select(a => new AdminPrivilegeViewModel
            {
                AdminId = a.AdminId,
                AdminName = a.Admin?.UserName ?? "N/A",
                Email = a.Admin?.Email ?? "N/A",
                AdminType = a.AdminType,
                Permissions = a.Permissions
            }).ToList();

            TempData["ErrorMessage"] = "Please select at least one admin and valid operation parameters";
            return View(model);
        }

        [HttpPost]
        [Authorize(Policy = "SuperAdminOnly")]
        public async Task<IActionResult> DeactivateAdmin(string id)
        {
            var result = await _adminService.DeactivateAdminAsync(id, User.Identity?.Name ?? "System");
            if (result)
            {
                TempData["SuccessMessage"] = "Admin deactivated successfully";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to deactivate admin";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [Authorize(Policy = "SuperAdminOnly")]
        public async Task<IActionResult> ActivateAdmin(string id)
        {
            var result = await _adminService.ActivateAdminAsync(id, User.Identity?.Name ?? "System");
            if (result)
            {
                TempData["SuccessMessage"] = "Admin activated successfully";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to activate admin";
            }

            return RedirectToAction("Index");
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Access()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult AdminLogin()
        {
            if (User.Identity?.IsAuthenticated == true && IsAdminUser())
            {
                return RedirectToAction("Index", "Admin");
            }

            return View();
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult AdminPortal()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Admin") || User.IsInRole("SuperAdmin"))
                {
                    return RedirectToAction("Index", "Admin");
                }
                return RedirectToAction("AccessDenied", "Home");
            }

            return RedirectToAction("AdminLogin");
        }

        private bool IsAdminUser()
        {
            return User.IsInRole("Admin") || User.IsInRole("SuperAdmin");
        }

        // Add this method to your AdminManagementController.cs
        [HttpGet]
        [Authorize(Policy = "SuperAdminOnly")]
        public IActionResult DownloadTemplate()
        {
            try
            {
                // Create a simple Excel template using EPPlus
                using (var package = new OfficeOpenXml.ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Admin Import Template");

                    // Format header row
                    using (var headerRange = worksheet.Cells[1, 1, 1, 8])
                    {
                        headerRange.Style.Font.Bold = true;
                        headerRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                        headerRange.Style.Font.Color.SetColor(System.Drawing.Color.Black);
                    }

                    // Add headers with descriptions
                    worksheet.Cells[1, 1].Value = "Email";
                    worksheet.Cells[1, 2].Value = "FullName";
                    worksheet.Cells[1, 3].Value = "AdminType";
                    worksheet.Cells[1, 4].Value = "Password";
                    worksheet.Cells[1, 5].Value = "Permissions";
                    worksheet.Cells[1, 6].Value = "UniversityScope";
                    worksheet.Cells[1, 7].Value = "FacultyScope";
                    worksheet.Cells[1, 8].Value = "DepartmentScope";

                    // Add example data in second row
                    worksheet.Cells[2, 1].Value = "admin@university.edu";
                    worksheet.Cells[2, 2].Value = "John Doe";
                    worksheet.Cells[2, 3].Value = "UniversityAdmin";
                    worksheet.Cells[2, 4].Value = "TempPass123!";
                    worksheet.Cells[2, 5].Value = "Courses.View,Students.View,Registration.Manage";
                    worksheet.Cells[2, 6].Value = "1";
                    worksheet.Cells[2, 7].Value = "2";
                    worksheet.Cells[2, 8].Value = "3";

                    // Add example data in third row (different type)
                    worksheet.Cells[3, 1].Value = "faculty@university.edu";
                    worksheet.Cells[3, 2].Value = "Jane Smith";
                    worksheet.Cells[3, 3].Value = "FacultyAdmin";
                    worksheet.Cells[3, 4].Value = "TempPass456!";
                    worksheet.Cells[3, 5].Value = "Faculty.Dashboard,Faculty.Courses,Faculty.Grades";
                    worksheet.Cells[3, 6].Value = "";
                    worksheet.Cells[3, 7].Value = "2";
                    worksheet.Cells[3, 8].Value = "";

                    // Add admin type options in separate sheet
                    var optionsSheet = package.Workbook.Worksheets.Add("Admin Types & Options");

                    // Admin types
                    optionsSheet.Cells[1, 1].Value = "Valid Admin Types:";
                    optionsSheet.Cells[2, 1].Value = "SuperAdmin";
                    optionsSheet.Cells[3, 1].Value = "UniversityAdmin";
                    optionsSheet.Cells[4, 1].Value = "FacultyAdmin";
                    optionsSheet.Cells[5, 1].Value = "DepartmentAdmin";
                    optionsSheet.Cells[6, 1].Value = "FinanceAdmin";
                    optionsSheet.Cells[7, 1].Value = "StudentAdmin";

                    // Permission options
                    optionsSheet.Cells[1, 3].Value = "Available Permissions (comma-separated):";
                    optionsSheet.Cells[2, 3].Value = "Courses.View";
                    optionsSheet.Cells[3, 3].Value = "Courses.Create";
                    optionsSheet.Cells[4, 3].Value = "Courses.Edit";
                    optionsSheet.Cells[5, 3].Value = "Courses.Delete";
                    optionsSheet.Cells[6, 3].Value = "Students.View";
                    optionsSheet.Cells[7, 3].Value = "Students.Create";
                    optionsSheet.Cells[8, 3].Value = "Students.Edit";
                    optionsSheet.Cells[9, 3].Value = "Students.Delete";
                    optionsSheet.Cells[10, 3].Value = "Registration.View";
                    optionsSheet.Cells[11, 3].Value = "Registration.Manage";
                    optionsSheet.Cells[12, 3].Value = "Grades.View";
                    optionsSheet.Cells[13, 3].Value = "Grades.Manage";
                    optionsSheet.Cells[14, 3].Value = "Admin.Dashboard";
                    optionsSheet.Cells[15, 3].Value = "Admin.Users";
                    optionsSheet.Cells[16, 3].Value = "Admin.Roles";

                    // Instructions sheet
                    var instructionsSheet = package.Workbook.Worksheets.Add("Instructions");
                    instructionsSheet.Cells[1, 1].Value = "Admin Import Template Instructions";
                    instructionsSheet.Cells[2, 1].Value = "1. Fill in the data in the first sheet";
                    instructionsSheet.Cells[3, 1].Value = "2. Required fields: Email, FullName, AdminType, Password";
                    instructionsSheet.Cells[4, 1].Value = "3. Passwords must meet complexity requirements (min 6 chars, uppercase, lowercase, number)";
                    instructionsSheet.Cells[5, 1].Value = "4. AdminType must be one of the valid types listed in the second sheet";
                    instructionsSheet.Cells[6, 1].Value = "5. Permissions are comma-separated (see second sheet for options)";
                    instructionsSheet.Cells[7, 1].Value = "6. Scope IDs (UniversityScope, FacultyScope, DepartmentScope) are optional";
                    instructionsSheet.Cells[8, 1].Value = "7. Save the file and upload it using the import form";

                    // Auto fit columns
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                    optionsSheet.Cells[optionsSheet.Dimension.Address].AutoFitColumns();
                    instructionsSheet.Cells[instructionsSheet.Dimension.Address].AutoFitColumns();

                    // Return the file
                    var fileName = $"Admin_Import_Template_{DateTime.Now:yyyyMMdd}.xlsx";
                    var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    var stream = new MemoryStream(package.GetAsByteArray());

                    return File(stream, contentType, fileName);
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error creating template: {ex.Message}";
                return RedirectToAction("ImportAdmins");
            }
        }

        // Add these methods to your AdminManagementController.cs

        [HttpGet]
        [Authorize(Policy = "SuperAdminOnly")]
        public IActionResult ExportDashboard()
        {
            try
            {
                // Create Excel package for dashboard data
                using (var package = new OfficeOpenXml.ExcelPackage())
                {
                    var dashboardSheet = package.Workbook.Worksheets.Add("Dashboard Stats");

                    // Add dashboard statistics
                    dashboardSheet.Cells[1, 1].Value = "Admin Management Dashboard Report";
                    dashboardSheet.Cells[1, 1].Style.Font.Size = 16;
                    dashboardSheet.Cells[1, 1].Style.Font.Bold = true;

                    // Get statistics
                    var totalAdmins = _context.AdminPrivileges.Count();
                    var activeAdmins = _context.AdminPrivileges.Count(a => a.IsActive);
                    var pendingApps = _context.AdminApplications.Count(a => a.Status == ApplicationStatus.Pending);
                    var totalApps = _context.AdminApplications.Count();

                    // Add stats
                    dashboardSheet.Cells[3, 1].Value = "Total Admins:";
                    dashboardSheet.Cells[3, 2].Value = totalAdmins;

                    dashboardSheet.Cells[4, 1].Value = "Active Admins:";
                    dashboardSheet.Cells[4, 2].Value = activeAdmins;

                    dashboardSheet.Cells[5, 1].Value = "Pending Applications:";
                    dashboardSheet.Cells[5, 2].Value = pendingApps;

                    dashboardSheet.Cells[6, 1].Value = "Total Applications:";
                    dashboardSheet.Cells[6, 2].Value = totalApps;

                    // Add admin types distribution
                    var adminTypes = _context.AdminPrivileges
                        .GroupBy(a => a.AdminType)
                        .Select(g => new { Type = g.Key, Count = g.Count() })
                        .ToList();

                    dashboardSheet.Cells[8, 1].Value = "Admin Types Distribution:";
                    dashboardSheet.Cells[8, 1].Style.Font.Bold = true;

                    int row = 9;
                    foreach (var type in adminTypes)
                    {
                        dashboardSheet.Cells[row, 1].Value = type.Type.ToString();
                        dashboardSheet.Cells[row, 2].Value = type.Count;
                        row++;
                    }

                    // Add recent activity
                    var recentAdmins = _context.AdminPrivileges
                        .OrderByDescending(a => a.CreatedDate)
                        .Take(10)
                        .Include(a => a.Admin)
                        .ToList();

                    dashboardSheet.Cells[row + 2, 1].Value = "Recent Admin Additions:";
                    dashboardSheet.Cells[row + 2, 1].Style.Font.Bold = true;

                    row += 3;
                    dashboardSheet.Cells[row, 1].Value = "Email";
                    dashboardSheet.Cells[row, 2].Value = "Admin Type";
                    dashboardSheet.Cells[row, 3].Value = "Created Date";
                    dashboardSheet.Cells[row, 4].Value = "Status";

                    foreach (var admin in recentAdmins)
                    {
                        row++;
                        dashboardSheet.Cells[row, 1].Value = admin.Admin?.Email ?? "N/A";
                        dashboardSheet.Cells[row, 2].Value = admin.AdminType.ToString();
                        dashboardSheet.Cells[row, 3].Value = admin.CreatedDate.ToString("yyyy-MM-dd HH:mm");
                        dashboardSheet.Cells[row, 4].Value = admin.IsActive ? "Active" : "Inactive";
                    }

                    // Auto fit columns
                    dashboardSheet.Cells[dashboardSheet.Dimension.Address].AutoFitColumns();

                    // Return the file
                    var fileName = $"Admin_Dashboard_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    var stream = new MemoryStream(package.GetAsByteArray());

                    return File(stream, contentType, fileName);
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error exporting dashboard: {ex.Message}";
                return RedirectToAction("Dashboard");
            }
        }


        [HttpGet]
        [Authorize(Policy = "SuperAdminOnly")]
        public async Task<IActionResult> AuditLogs()
        {
            try
            {
                // Get audit logs from AdminPrivileges (modifications)
                var auditLogs = await _context.AdminPrivileges
                    .Include(a => a.Admin)
                    .Include(a => a.University)
                    .Include(a => a.Faculty)
                    .Include(a => a.Department)
                    .Where(a => a.ModifiedDate != null) // ModifiedDate is nullable, so this check is OK
                    .OrderByDescending(a => a.ModifiedDate)
                    .Select(a => new AuditLogViewModel
                    {
                        Id = a.Id,
                        AdminEmail = a.Admin!.Email ?? "N/A", // Use null-forgiving operator since Admin is included
                        AdminType = a.AdminType,
                        Action = "Modified",
                        Details = $"Admin privileges updated for {a.AdminType}",
                        Timestamp = a.ModifiedDate!.Value, // Use ! to indicate we know it's not null due to Where clause
                        PerformedBy = a.UpdatedBy ?? "System",
                        OldValue = "Previous settings",
                        NewValue = $"Type: {a.AdminType}, Active: {a.IsActive}"
                    })
                    .ToListAsync();

                // Get creation logs
                var creationLogs = await _context.AdminPrivileges
                    .Include(a => a.Admin)
                    .OrderByDescending(a => a.CreatedDate)
                    .Select(a => new AuditLogViewModel
                    {
                        Id = a.Id,
                        AdminEmail = a.Admin!.Email ?? "N/A", // Use null-forgiving operator
                        AdminType = a.AdminType,
                        Action = "Created",
                        Details = $"New admin account created",
                        Timestamp = a.CreatedDate,
                        PerformedBy = a.CreatedBy,
                        OldValue = "N/A",
                        NewValue = $"Type: {a.AdminType}"
                    })
                    .ToListAsync();

                // Combine and sort
                var allLogs = auditLogs.Concat(creationLogs)
                    .OrderByDescending(l => l.Timestamp)
                    .Take(100)
                    .ToList();

                return View(allLogs);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading audit logs: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        [Authorize(Policy = "SuperAdminOnly")]
        public IActionResult ExportAuditLogs()
        {
            try
            {
                // Get audit logs
                var logs = _context.AdminPrivileges
                    .Include(a => a.Admin)
                    .Where(a => a.ModifiedDate != null)
                    .OrderByDescending(a => a.ModifiedDate)
                    .Take(500)
                    .ToList();

                using (var package = new OfficeOpenXml.ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Audit Logs");

                    // Headers
                    worksheet.Cells[1, 1].Value = "Admin Email";
                    worksheet.Cells[1, 2].Value = "Admin Type";
                    worksheet.Cells[1, 3].Value = "Action";
                    worksheet.Cells[1, 4].Value = "Created Date";
                    worksheet.Cells[1, 5].Value = "Modified Date";
                    worksheet.Cells[1, 6].Value = "Created By";
                    worksheet.Cells[1, 7].Value = "Updated By";
                    worksheet.Cells[1, 8].Value = "Status";

                    // Format headers
                    using (var headerRange = worksheet.Cells[1, 1, 1, 8])
                    {
                        headerRange.Style.Font.Bold = true;
                        headerRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    }

                    // Add data
                    int row = 2;
                    foreach (var log in logs)
                    {
                        worksheet.Cells[row, 1].Value = log.Admin?.Email ?? "N/A";
                        worksheet.Cells[row, 2].Value = log.AdminType.ToString();
                        worksheet.Cells[row, 3].Value = log.ModifiedDate.HasValue ? "Modified" : "Created";
                        worksheet.Cells[row, 4].Value = log.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss");
                        worksheet.Cells[row, 5].Value = log.ModifiedDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A";
                        worksheet.Cells[row, 6].Value = log.CreatedBy;
                        worksheet.Cells[row, 7].Value = log.UpdatedBy ?? "N/A";
                        worksheet.Cells[row, 8].Value = log.IsActive ? "Active" : "Inactive";
                        row++;
                    }

                    // Auto fit columns
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    // Return file
                    var fileName = $"Admin_Audit_Logs_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    var stream = new MemoryStream(package.GetAsByteArray());

                    return File(stream, contentType, fileName);
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error exporting audit logs: {ex.Message}";
                return RedirectToAction("AuditLogs");
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetTemplateDetails(int templateId)
        {
            try
            {
                var template = await _context.AdminPrivilegeTemplates
                    .FirstOrDefaultAsync(t => t.Id == templateId);

                if (template == null)
                {
                    return Json(new { success = false, message = "Template not found" });
                }

                return Json(new
                {
                    success = true,
                    templateName = template.TemplateName,
                    description = template.Description,
                    permissionCount = template.DefaultPermissions?.Count ?? 0,
                    permissions = template.DefaultPermissions?.Select(p => p.ToString()).ToList() ?? new List<string>()
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }



        [HttpGet]
        [Authorize(Policy = "SuperAdminOnly")]
        public async Task<IActionResult> ManageTemplates()
        {
            var templates = await _context.AdminPrivilegeTemplates
                .Where(t => t.IsActive)
                .ToListAsync();

            return View(templates);
        }

        [HttpGet]
        [Authorize(Policy = "SuperAdminOnly")]
        public IActionResult CreateTemplate()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Policy = "SuperAdminOnly")]
        public async Task<IActionResult> CreateTemplate(AdminPrivilegeTemplate model)
        {
            if (ModelState.IsValid)
            {
                model.CreatedDate = DateTime.Now;
                model.IsActive = true;

                _context.AdminPrivilegeTemplates.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Template created successfully";
                return RedirectToAction("ManageTemplates");
            }

            return View(model);
        }

        // In your AdminManagementController.cs
        [HttpGet]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> CreateAdmin()
        {
            var viewModel = new CreateAdminViewModel
            {
                // Populate dropdown lists
                Universities = await _context.Universities.ToListAsync(),
                Colleges = await _context.Colleges.ToListAsync(),
                Departments = await _context.Departments.ToListAsync(),
                AvailableTemplates = await _context.AdminPrivilegeTemplates
                    .Where(t => t.IsActive)
                    .ToListAsync()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> CreateAdmin(CreateAdminViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Create Identity User
                    var user = new IdentityUser
                    {
                        UserName = model.Email,
                        Email = model.Email,
                        EmailConfirmed = true,
                        PhoneNumberConfirmed = false
                    };

                    // Generate random password
                    var password = GenerateRandomPassword(12);

                    // Create user
                    var result = await _userManager.CreateAsync(user, password);

                    if (result.Succeeded)
                    {
                        // Create AdminPrivilege record
                        var adminPrivilege = new AdminPrivilege
                        {
                            AdminId = user.Id,
                            AdminType = model.AdminType,
                            UniversityScope = string.IsNullOrEmpty(model.UniversityScope) ? null : int.Parse(model.UniversityScope),
                            FacultyScope = string.IsNullOrEmpty(model.FacultyScope) ? null : int.Parse(model.FacultyScope),
                            DepartmentScope = string.IsNullOrEmpty(model.DepartmentScope) ? null : int.Parse(model.DepartmentScope),
                            CreatedBy = User.Identity?.Name ?? "System",
                            CreatedDate = DateTime.UtcNow,
                            IsActive = true,
                            Permissions = model.SelectedPermissions
                        };

                        // If template selected, apply template permissions
                        if (model.TemplateId.HasValue)
                        {
                            var template = await _context.AdminPrivilegeTemplates
                                .FirstOrDefaultAsync(t => t.Id == model.TemplateId.Value);

                            if (template != null)
                            {
                                adminPrivilege.Permissions = template.DefaultPermissions;
                            }
                        }

                        _context.AdminPrivileges.Add(adminPrivilege);
                        await _context.SaveChangesAsync();

                        // Assign Admin role
                        await _userManager.AddToRoleAsync(user, "Admin");

                        // Send welcome email
                        try
                        {
                            await SendWelcomeEmailAsync(model.Email, password, model.AdminType);
                            TempData["SuccessMessage"] = $"Admin created successfully! Password sent to {model.Email}.";
                        }
                        catch (Exception ex)
                        {
                            TempData["SuccessMessage"] = $"Admin created successfully! Password: {password}";
                            TempData["WarningMessage"] = $"Email could not be sent: {ex.Message}";
                        }

                        return RedirectToAction("Index", "AdminManagement");
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, $"Error creating admin: {ex.Message}");
                }
            }

            // Reload dropdown data if validation fails
            model.Universities = await _context.Universities.ToListAsync();
            model.Colleges = await _context.Colleges.ToListAsync();
            model.Departments = await _context.Departments.ToListAsync();
            model.AvailableTemplates = await _context.AdminPrivilegeTemplates
                .Where(t => t.IsActive)
                .ToListAsync();

            return View(model);
        }

        private string GenerateRandomPassword(int length = 12)
        {
            const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*()";
            var random = new Random();
            var chars = Enumerable.Repeat(validChars, length)
                .Select(s => s[random.Next(s.Length)])
                .ToArray();
            return new string(chars);
        }

        private async Task SendWelcomeEmailAsync(string email, string password, AdminType adminType)
        {
            var subject = "Your Admin Account Has Been Created";
            var body = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; text-align: center; color: white;'>
                    <h1 style='margin: 0; font-size: 28px;'>Welcome to Student Management System</h1>
                    <p style='margin: 10px 0 0; opacity: 0.9;'>Your admin account has been created</p>
                </div>
    
                <div style='padding: 30px; background: #f9f9f9;'>
                    <h2 style='color: #333; margin-top: 0;'>Account Details</h2>
        
                    <div style='background: white; padding: 20px; border-radius: 8px; border-left: 4px solid #667eea; margin: 20px 0;'>
                        <p><strong>🎯 Admin Type:</strong> {adminType}</p>
                        <p><strong>📧 Login Email:</strong> {email}</p>
                        <p><strong>🔑 Temporary Password:</strong> <code style='background: #f1f1f1; padding: 5px 10px; border-radius: 4px;'>{password}</code></p>
                    </div>
        
                    <div style='background: #fff3cd; padding: 15px; border-radius: 6px; border: 1px solid #ffeaa7; margin: 20px 0;'>
                        <p style='margin: 0; color: #856404;'><strong>⚠️ Important:</strong> Please log in and change your password immediately.</p>
                    </div>
        
                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='{Request.Scheme}://{Request.Host}/Account/Login' 
                           style='background: #667eea; color: white; padding: 12px 30px; text-decoration: none; border-radius: 6px; font-weight: bold; display: inline-block;'>
                            Login to Your Account
                        </a>
                    </div>
                </div>
    
                <div style='background: #f1f1f1; padding: 20px; text-align: center; font-size: 12px; color: #666;'>
                    <p style='margin: 0;'>This is an automated message. Please do not reply to this email.</p>
                    <p style='margin: 10px 0 0;'>© {DateTime.Now.Year} Student Management System. All rights reserved.</p>
                </div>
            </div>";

            await _emailService.SendEmailAsync(email, subject, body, new List<string>());
        }

        private async Task SendWelcomeEmailAsync(string email, string fullName, string password, AdminType adminType)
        {
            var subject = "Your Admin Account Has Been Created";
            var body = $@"
<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
    <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; text-align: center; color: white;'>
        <h1 style='margin: 0; font-size: 28px;'>Welcome to Student Management System</h1>
        <p style='margin: 10px 0 0; opacity: 0.9;'>Your admin account has been created</p>
    </div>
    
    <div style='padding: 30px; background: #f9f9f9;'>
        <h2 style='color: #333; margin-top: 0;'>Account Details</h2>
        
        <div style='background: white; padding: 20px; border-radius: 8px; border-left: 4px solid #667eea; margin: 20px 0;'>
            <p><strong>👤 Name:</strong> {fullName}</p>
            <p><strong>🎯 Admin Type:</strong> {adminType}</p>
            <p><strong>📧 Login Email:</strong> {email}</p>
            <p><strong>🔑 Temporary Password:</strong> <code style='background: #f1f1f1; padding: 5px 10px; border-radius: 4px;'>{password}</code></p>
        </div>
        
        <div style='background: #fff3cd; padding: 15px; border-radius: 6px; border: 1px solid #ffeaa7; margin: 20px 0;'>
            <p style='margin: 0; color: #856404;'><strong>⚠️ Important:</strong> Please log in and change your password immediately.</p>
        </div>
        
        <div style='text-align: center; margin: 30px 0;'>
            <a href='{Request.Scheme}://{Request.Host}/Account/Login' 
               style='background: #667eea; color: white; padding: 12px 30px; text-decoration: none; border-radius: 6px; font-weight: bold; display: inline-block;'>
                Login to Your Account
            </a>
        </div>
    </div>
    
    <div style='background: #f1f1f1; padding: 20px; text-align: center; font-size: 12px; color: #666;'>
        <p style='margin: 0;'>This is an automated message. Please do not reply to this email.</p>
        <p style='margin: 10px 0 0;'>© {DateTime.Now.Year} Student Management System. All rights reserved.</p>
    </div>
</div>";

            await _emailService.SendEmailAsync(email, subject, body, new List<string>());
        }

        // Add missing methods for admin actions:

        //[HttpPost]
        //[Authorize(Policy = "SuperAdminOnly")]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> ToggleAdminStatus(string adminId, string action)
        //{
        //    try
        //    {
        //        if (string.IsNullOrEmpty(adminId))
        //            return Json(new { success = false, message = "Admin ID is required" });

        //        var admin = await _userManager.FindByIdAsync(adminId);
        //        if (admin == null)
        //            return Json(new { success = false, message = "Admin not found" });

        //        var privilege = await _context.AdminPrivileges
        //            .FirstOrDefaultAsync(ap => ap.AdminId == adminId);

        //        if (privilege == null)
        //            return Json(new { success = false, message = "Admin privilege not found" });

        //        if (action == "activate")
        //        {
        //            privilege.IsActive = true;
        //            privilege.UpdatedBy = User.Identity?.Name ?? "System";
        //            privilege.ModifiedDate = DateTime.Now;

        //            // Reactivate user account
        //            admin.LockoutEnabled = false;
        //            admin.LockoutEnd = null;
        //            await _userManager.UpdateAsync(admin);

        //            // Send activation notification
        //            await SendAdminStatusEmailAsync(admin.Email, "activated", User.Identity?.Name);
        //        }
        //        else if (action == "deactivate")
        //        {
        //            privilege.IsActive = false;
        //            privilege.UpdatedBy = User.Identity?.Name ?? "System";
        //            privilege.ModifiedDate = DateTime.Now;

        //            // Deactivate user account
        //            admin.LockoutEnabled = true;
        //            admin.LockoutEnd = DateTimeOffset.MaxValue;
        //            await _userManager.UpdateAsync(admin);

        //            // Send deactivation notification
        //            await SendAdminStatusEmailAsync(admin.Email, "deactivated", User.Identity?.Name);
        //        }
        //        else
        //        {
        //            return Json(new { success = false, message = "Invalid action" });
        //        }

        //        await _context.SaveChangesAsync();
        //        return Json(new { success = true, message = $"Admin {action}d successfully" });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error toggling admin status");
        //        return Json(new { success = false, message = $"Error: {ex.Message}" });
        //    }
        //}

        [HttpPost]
        public async Task<IActionResult> ToggleAdminStatus(string adminId, string action)
        {
            try
            {
                var admin = await _userManager.FindByIdAsync(adminId);
                if (admin == null)
                    return Json(new { success = false, message = "Admin not found" });

                var privilege = await _context.AdminPrivileges
                    .FirstOrDefaultAsync(p => p.AdminId == adminId);

                if (privilege == null)
                    return Json(new { success = false, message = "Admin privilege not found" });

                if (action == "activate")
                {
                    privilege.IsActive = true;
                    admin.LockoutEnabled = false;
                    admin.LockoutEnd = null;
                }
                else if (action == "deactivate")
                {
                    privilege.IsActive = false;
                    admin.LockoutEnabled = true;
                    admin.LockoutEnd = DateTimeOffset.MaxValue;
                }

                await _userManager.UpdateAsync(admin);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"Admin {action}d successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling admin status");
                return Json(new { success = false, message = "Error updating status" });
            }
        }


        //[HttpPost]
        //[Authorize(Policy = "SuperAdminOnly")]
        //public async Task<IActionResult> DeleteAdmin(string adminId)
        //{
        //    try
        //    {
        //        if (string.IsNullOrEmpty(adminId))
        //        {
        //            return Json(new { success = false, message = "Admin ID is required" });
        //        }

        //        var admin = await _userManager.FindByIdAsync(adminId);
        //        if (admin == null)
        //        {
        //            return Json(new { success = false, message = "Admin not found" });
        //        }

        //        // Prevent deleting yourself
        //        if (admin.Id == _userManager.GetUserId(User))
        //        {
        //            return Json(new { success = false, message = "You cannot delete your own account" });
        //        }

        //        // Check if it's an admin from application (not yet a full user)
        //        var privilege = await _context.AdminPrivileges
        //            .Include(ap => ap.Admin)
        //            .FirstOrDefaultAsync(ap => ap.AdminId == adminId);

        //        // If it's from application and not a real user yet, just delete the privilege
        //        if (privilege != null && string.IsNullOrEmpty(privilege.Admin?.Id))
        //        {
        //            _context.AdminPrivileges.Remove(privilege);
        //            await _context.SaveChangesAsync();

        //            return Json(new { success = true, message = "Admin application deleted successfully" });
        //        }

        //        // Delete admin privilege first
        //        if (privilege != null)
        //        {
        //            _context.AdminPrivileges.Remove(privilege);
        //        }

        //        // Delete the user account
        //        var result = await _userManager.DeleteAsync(admin);
        //        if (result.Succeeded)
        //        {
        //            await _context.SaveChangesAsync();

        //            // Send deletion notification
        //            await SendAdminDeletionEmailAsync(admin.Email, User.Identity?.Name);

        //            return Json(new { success = true, message = "Admin deleted successfully" });
        //        }
        //        else
        //        {
        //            return Json(new { success = false, message = $"Failed to delete admin: {string.Join(", ", result.Errors.Select(e => e.Description))}" });
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error deleting admin");
        //        return Json(new { success = false, message = $"Error deleting admin: {ex.Message}" });
        //    }
        //}
        [HttpPost]
        public async Task<IActionResult> DeleteAdmin(string adminId)
        {
            try
            {
                var admin = await _userManager.FindByIdAsync(adminId);
                if (admin == null)
                    return Json(new { success = false, message = "Admin not found" });

                // Prevent deleting yourself
                if (admin.Id == _userManager.GetUserId(User))
                    return Json(new { success = false, message = "Cannot delete your own account" });

                var privilege = await _context.AdminPrivileges
                    .FirstOrDefaultAsync(p => p.AdminId == adminId);

                if (privilege != null)
                    _context.AdminPrivileges.Remove(privilege);

                var result = await _userManager.DeleteAsync(admin);

                if (result.Succeeded)
                {
                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Admin deleted successfully" });
                }

                return Json(new { success = false, message = "Failed to delete admin" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting admin");
                return Json(new { success = false, message = "Error deleting admin" });
            }
        }
        //[HttpPost]
        //[Authorize(Policy = "SuperAdminOnly")]
        //public async Task<JsonResult> SendEmail(string to, string subject, string message)
        //{
        //    try
        //    {
        //        if (string.IsNullOrEmpty(to) || string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(message))
        //        {
        //            return Json(new { success = false, message = "All fields are required" });
        //        }

        //        var emailBody = $@"
        //        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
        //            <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; text-align: center; color: white;'>
        //                <h1 style='margin: 0; font-size: 28px;'>Admin Message</h1>
        //                <p style='margin: 10px 0 0; opacity: 0.9;'>Student Management System</p>
        //            </div>

        //            <div style='padding: 30px; background: #f9f9f9;'>
        //                <div style='background: white; padding: 20px; border-radius: 8px; border-left: 4px solid #667eea; margin: 20px 0;'>
        //                    <p>{message.Replace("\n", "<br>")}</p>
        //                </div>

        //                <p style='color: #666; font-size: 14px;'>
        //                    This message was sent by: {User.Identity?.Name ?? "System Administrator"}<br>
        //                    Date: {DateTime.Now:MMMM dd, yyyy HH:mm}
        //                </p>
        //            </div>

        //            <div style='background: #f1f1f1; padding: 20px; text-align: center; font-size: 12px; color: #666;'>
        //                <p style='margin: 0;'>This is an automated message. Please do not reply to this email.</p>
        //                <p style='margin: 10px 0 0;'>© {DateTime.Now.Year} Student Management System. All rights reserved.</p>
        //            </div>
        //        </div>";

        //        await _emailService.SendEmailAsync(to, subject, emailBody, new List<string>());

        //        // Log the email sending
        //        _logger.LogInformation($"Email sent to {to} by {User.Identity?.Name}");

        //        return Json(new { success = true, message = "Email sent successfully" });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error sending email");
        //        return Json(new { success = false, message = $"Error sending email: {ex.Message}" });
        //    }
        //}


        //[HttpPost]
        //[Authorize(Policy = "SuperAdminOnly")]
        //public async Task<IActionResult> BulkAction(List<string> adminIds, string action)
        //{
        //    try
        //    {
        //        if (adminIds == null || !adminIds.Any())
        //        {
        //            return Json(new { success = false, message = "No admins selected" });
        //        }

        //        int successCount = 0;
        //        int failCount = 0;
        //        var errors = new List<string>();

        //        foreach (var adminId in adminIds)
        //        {
        //            try
        //            {
        //                switch (action)
        //                {
        //                    case "activate":
        //                        await ToggleAdminStatus(adminId, "activate");
        //                        successCount++;
        //                        break;

        //                    case "deactivate":
        //                        await ToggleAdminStatus(adminId, "deactivate");
        //                        successCount++;
        //                        break;

        //                    case "delete":
        //                        var admin = await _userManager.FindByIdAsync(adminId);
        //                        if (admin != null && admin.Id != _userManager.GetUserId(User))
        //                        {
        //                            // Delete admin privilege
        //                            var privilege = await _context.AdminPrivileges
        //                                .FirstOrDefaultAsync(ap => ap.AdminId == adminId);
        //                            if (privilege != null)
        //                            {
        //                                _context.AdminPrivileges.Remove(privilege);
        //                            }

        //                            // Delete user
        //                            await _userManager.DeleteAsync(admin);
        //                            successCount++;
        //                        }
        //                        else
        //                        {
        //                            failCount++;
        //                            errors.Add($"Cannot delete your own account or admin not found: {adminId}");
        //                        }
        //                        break;

        //                    default:
        //                        failCount++;
        //                        errors.Add($"Invalid action: {action}");
        //                        break;
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                failCount++;
        //                errors.Add($"Error processing admin {adminId}: {ex.Message}");
        //                _logger.LogError(ex, $"Error in bulk action for admin {adminId}");
        //            }
        //        }

        //        await _context.SaveChangesAsync();

        //        var message = $"Bulk operation completed: {successCount} successful, {failCount} failed";
        //        if (errors.Any())
        //        {
        //            message += $" - Errors: {string.Join("; ", errors.Take(3))}";
        //        }

        //        return Json(new
        //        {
        //            success = true,
        //            message = message,
        //            details = new
        //            {
        //                successCount,
        //                failCount,
        //                errors = errors.Take(5).ToList()
        //            }
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error in bulk action");
        //        return Json(new { success = false, message = $"Error: {ex.Message}" });
        //    }
        //}

        [HttpGet]
        [Authorize(Policy = "SuperAdminOnly")]
        public async Task<IActionResult> SetupAdmin(int applicationId)
        {
            try
            {
                var application = await _context.AdminApplications
                    .Include(a => a.University)
                    .Include(a => a.Faculty)
                    .Include(a => a.Department)
                    .FirstOrDefaultAsync(a => a.Id == applicationId);

                if (application == null)
                {
                    TempData["ErrorMessage"] = "Application not found";
                    return RedirectToAction("Index");
                }

                // Check if already processed
                if (application.Status != ApplicationStatus.Approved)
                {
                    TempData["ErrorMessage"] = "Application must be approved first";
                    return RedirectToAction("Applications", "Applications");
                }

                var viewModel = new CreateAdminViewModel
                {
                    Email = application.Email,
                    FullName = application.ApplicantName,
                    AdminType = application.AppliedAdminType,
                    UniversityScope = application.UniversityId?.ToString(),
                    FacultyScope = application.FacultyId?.ToString(),
                    DepartmentScope = application.DepartmentId?.ToString(),
                    Universities = await _context.Universities.ToListAsync(),
                    Colleges = await _context.Colleges.ToListAsync(),
                    Departments = await _context.Departments.ToListAsync(),
                    AvailableTemplates = await _context.AdminPrivilegeTemplates
                        .Where(t => t.IsActive)
                        .ToListAsync()
                };

                ViewBag.ApplicationId = applicationId;
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading setup admin page");
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [Authorize(Policy = "SuperAdminOnly")]
        public async Task<IActionResult> SetupAdmin(int applicationId, CreateAdminViewModel model)
        {
            try
            {
                var application = await _context.AdminApplications
                    .Include(a => a.University)
                    .Include(a => a.Faculty)
                    .Include(a => a.Department)
                    .FirstOrDefaultAsync(a => a.Id == applicationId);

                if (application == null)
                {
                    TempData["ErrorMessage"] = "Application not found";
                    return RedirectToAction("Index");
                }

                if (!ModelState.IsValid)
                {
                    // Reload dropdowns and return to view
                    model.Universities = await _context.Universities.ToListAsync();
                    model.Colleges = await _context.Colleges.ToListAsync();
                    model.Departments = await _context.Departments.ToListAsync();
                    model.AvailableTemplates = await _context.AdminPrivilegeTemplates
                        .Where(t => t.IsActive)
                        .ToListAsync();

                    ViewBag.ApplicationId = applicationId;
                    return View(model);
                }

                // Create the admin using your existing CreateAdmin logic
                var createViewModel = new CreateAdminViewModel
                {
                    Email = model.Email,
                    FullName = model.FullName,
                    AdminType = model.AdminType,
                    SelectedPermissions = model.SelectedPermissions,
                    TemplateId = model.TemplateId,
                    UniversityScope = model.UniversityScope,
                    FacultyScope = model.FacultyScope,
                    DepartmentScope = model.DepartmentScope,
                    Password = GenerateRandomPassword(12), // Generate password
                    ConfirmPassword = "" // Will be validated in service
                };

                var currentUser = User.Identity?.Name ?? "System";
                var result = await _adminService.CreateAdminWithPrivilegesAsync(createViewModel, currentUser);

                if (result)
                {
                    // Update application with the actual admin ID
                    var newAdmin = await _userManager.FindByEmailAsync(model.Email);
                    if (newAdmin != null)
                    {
                        application.ApplicantId = newAdmin.Id;
                        application.Status = ApplicationStatus.Approved;
                        application.ReviewedBy = currentUser;
                        application.ReviewedDate = DateTime.Now;

                        await _context.SaveChangesAsync();
                    }

                    TempData["SuccessMessage"] = "Admin account created successfully from application";
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to create admin account. Please check the logs.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting up admin from application");
                TempData["ErrorMessage"] = $"Error creating admin account: {ex.Message}";
            }

            // Reload data if failed
            model.Universities = await _context.Universities.ToListAsync();
            model.Colleges = await _context.Colleges.ToListAsync();
            model.Departments = await _context.Departments.ToListAsync();
            model.AvailableTemplates = await _context.AdminPrivilegeTemplates
                .Where(t => t.IsActive)
                .ToListAsync();

            ViewBag.ApplicationId = applicationId;
            return View(model);
        }


        //[HttpGet]
        //[Authorize(Policy = "SuperAdminOnly")]
        //public async Task<IActionResult> ActivityLog(string id)
        //{
        //    // Placeholder - implement your activity log logic
        //    ViewBag.AdminId = id;
        //    ViewBag.AdminName = (await _userManager.FindByIdAsync(id))?.UserName ?? "Unknown";
        //    return View();
        //}
        private async Task SendPasswordResetEmailAsync(string? email, string newPassword)
        {
            // Check for null or empty email
            if (string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("Cannot send password reset email: email is null or empty");
                return;
            }

            try
            {
                var subject = "Password Reset - Student Management System";
                var body = $@"
<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
    <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; text-align: center; color: white; border-radius: 10px 10px 0 0;'>
        <h1 style='margin: 0; font-size: 28px;'>Password Reset Notification</h1>
        <p style='margin: 10px 0 0; opacity: 0.9;'>Your password has been reset by administrator</p>
    </div>
    
    <div style='padding: 30px; background: #f9f9f9; border-radius: 0 0 10px 10px;'>
        <h2 style='color: #333; margin-top: 0;'>New Password Information</h2>
        
        <div style='background: white; padding: 20px; border-radius: 8px; border-left: 4px solid #667eea; margin: 20px 0;'>
            <p><strong>🔑 Your new temporary password:</strong></p>
            <div style='background: #f1f1f1; padding: 15px; border-radius: 6px; margin: 15px 0; text-align: center; font-family: monospace; font-size: 18px;'>
                {newPassword}
            </div>
            <p style='color: #666; font-size: 14px;'>
                For security reasons, please change your password immediately after logging in.
            </p>
        </div>
        
        <div style='background: #fff3cd; padding: 15px; border-radius: 6px; border: 1px solid #ffeaa7; margin: 20px 0;'>
            <p style='margin: 0; color: #856404;'>
                <strong>⚠️ Security Notice:</strong> 
                Do not share this password with anyone. 
                If you did not request this password reset, please contact your system administrator immediately.
            </p>
        </div>
        
        <div style='text-align: center; margin: 30px 0;'>
            <a href='{Request.Scheme}://{Request.Host}/Account/Login' 
               style='background: #667eea; color: white; padding: 12px 30px; text-decoration: none; border-radius: 6px; font-weight: bold; display: inline-block;'>
                Login to Your Account
            </a>
        </div>
    </div>
    
    <div style='background: #f1f1f1; padding: 20px; text-align: center; font-size: 12px; color: #666; margin-top: 20px; border-radius: 0 0 10px 10px;'>
        <p style='margin: 0;'>This is an automated message. Please do not reply to this email.</p>
        <p style='margin: 10px 0 0;'>© {DateTime.Now.Year} Student Management System. All rights reserved.</p>
    </div>
</div>";

                await _emailService.SendEmailAsync(email, subject, body, new List<string>());
                _logger.LogInformation($"Password reset email sent to {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send password reset email to {email}");
                throw;
            }
        }

        //[HttpPost]
        //[Authorize(Policy = "SuperAdminOnly")]
        //public async Task<IActionResult> ResetPassword(string id)
        //{
        //    try
        //    {
        //        var admin = await _userManager.FindByIdAsync(id);
        //        if (admin == null)
        //        {
        //            return Json(new { success = false, message = "Admin not found" });
        //        }

        //        // Generate new password
        //        var newPassword = GenerateRandomPassword(10);

        //        // Reset password
        //        var token = await _userManager.GeneratePasswordResetTokenAsync(admin);
        //        var result = await _userManager.ResetPasswordAsync(admin, token, newPassword);

        //        if (result.Succeeded)
        //        {
        //            // Send email with new password
        //            await SendPasswordResetEmailAsync(admin.Email, newPassword);
        //            return Json(new { success = true, message = $"Password reset. New password sent to {admin.Email}" });
        //        }
        //        else
        //        {
        //            return Json(new { success = false, message = string.Join(", ", result.Errors.Select(e => e.Description)) });
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error resetting password");
        //        return Json(new { success = false, message = $"Error: {ex.Message}" });
        //    }
        //}
        


        [HttpPost]
        //[Authorize(Policy = "SuperAdminOnly")]        
        public async Task<IActionResult> SendEmail(string to, string subject, string message)
        {
            try
            {
                if (string.IsNullOrEmpty(to) || string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(message))
                {
                    return Json(new { success = false, message = "All fields are required" });
                }

                var emailBody = $@"
        <div style='font-family: Arial, sans-serif;'>
            <h3>{subject}</h3>
            <div style='background: #f5f5f5; padding: 20px; border-radius: 5px;'>
                {message.Replace("\n", "<br>")}
            </div>
            <p style='color: #666; margin-top: 20px;'>
                Sent from: {User.Identity?.Name}<br>
                Date: {DateTime.Now:MMMM dd, yyyy HH:mm}
            </p>
        </div>";

                await _emailService.SendEmailAsync(to, subject, emailBody);

                return Json(new { success = true, message = "Email sent successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpPost]
        [Authorize(Policy = "SuperAdminOnly")]
        public async Task<IActionResult> BulkAction(List<string> adminIds, string action)
        {
            try
            {
                if (adminIds == null || !adminIds.Any())
                {
                    return Json(new { success = false, message = "No admins selected" });
                }

                int successCount = 0;
                int failCount = 0;
                var errors = new List<string>();

                foreach (var adminId in adminIds)
                {
                    try
                    {
                        switch (action)
                        {
                            case "activate":
                                await ToggleAdminStatus(adminId, "activate");
                                successCount++;
                                break;

                            case "deactivate":
                                await ToggleAdminStatus(adminId, "deactivate");
                                successCount++;
                                break;

                            case "delete":
                                var admin = await _userManager.FindByIdAsync(adminId);
                                if (admin != null && admin.Id != _userManager.GetUserId(User))
                                {
                                    // Delete admin privilege
                                    var privilege = await _context.AdminPrivileges
                                        .FirstOrDefaultAsync(ap => ap.AdminId == adminId);
                                    if (privilege != null)
                                    {
                                        _context.AdminPrivileges.Remove(privilege);
                                    }

                                    // Delete user
                                    await _userManager.DeleteAsync(admin);
                                    successCount++;
                                }
                                else
                                {
                                    failCount++;
                                    errors.Add($"Cannot delete your own account or admin not found: {adminId}");
                                }
                                break;

                            default:
                                failCount++;
                                errors.Add($"Invalid action: {action}");
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        errors.Add($"Error processing admin {adminId}: {ex.Message}");
                        _logger.LogError(ex, $"Error in bulk action for admin {adminId}");
                    }
                }

                await _context.SaveChangesAsync();

                var message = $"Bulk operation completed: {successCount} successful, {failCount} failed";
                if (errors.Any())
                {
                    message += $" - Errors: {string.Join("; ", errors.Take(3))}";
                }

                return Json(new
                {
                    success = true,
                    message = message,
                    details = new
                    {
                        successCount,
                        failCount,
                        errors = errors.Take(5).ToList()
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk action");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }
        
        [HttpGet]
        [Authorize(Policy = "SuperAdminOnly")]
        public async Task<IActionResult> ActivityLog(string id)
        {
            var admin = await _userManager.FindByIdAsync(id);
            if (admin == null)
            {
                return NotFound();
            }

            // Get audit logs for this admin
            var auditLogs = await _context.AdminPrivileges
                .Where(ap => ap.AdminId == id && ap.ModifiedDate != null)
                .OrderByDescending(ap => ap.ModifiedDate)
                .Select(ap => new
                {
                    Action = "Modified",
                    Details = $"Admin privileges updated",
                    Timestamp = ap.ModifiedDate!.Value,
                    PerformedBy = ap.UpdatedBy ?? "System"
                })
                .ToListAsync();

            ViewBag.AdminId = id;
            ViewBag.AdminName = admin.UserName;
            ViewBag.AuditLogs = auditLogs;

            return View();
        }

        [HttpPost]
        [Authorize(Policy = "SuperAdminOnly")]
        [HttpPost]
        [Authorize(Policy = "SuperAdminOnly")]
        public async Task<IActionResult> ResetPassword(string id)
        {
            try
            {
                var admin = await _userManager.FindByIdAsync(id);
                if (admin == null)
                {
                    return Json(new { success = false, message = "Admin not found" });
                }

                // Check if email is null or empty
                if (string.IsNullOrEmpty(admin.Email))
                {
                    return Json(new { success = false, message = "Admin email is not set. Cannot send password reset email." });
                }

                // Generate new password
                var newPassword = GenerateRandomPassword(10);

                // Reset password
                var token = await _userManager.GeneratePasswordResetTokenAsync(admin);
                var result = await _userManager.ResetPasswordAsync(admin, token, newPassword);

                if (result.Succeeded)
                {
                    // Send email with new password - admin.Email is guaranteed not null here
                    await SendPasswordResetEmailAsync(admin.Email!, newPassword);
                    return Json(new { success = true, message = $"Password reset. New password sent to {admin.Email}" });
                }
                else
                {
                    return Json(new { success = false, message = string.Join(", ", result.Errors.Select(e => e.Description)) });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // Helper methods for email notifications
        private async Task SendAdminStatusEmailAsync(string? email, string status, string? performedBy)
        {
            if (string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("Cannot send admin status email: email is null or empty");
                return;
            }

            var subject = $"Admin Account {status.ToUpper()} - Student Management System";
            var body = $@"
<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
    <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 30px; text-align: center; color: white; border-radius: 10px 10px 0 0;'>
        <h1 style='margin: 0; font-size: 28px;'>Account Status Update</h1>
        <p style='margin: 10px 0 0; opacity: 0.9;'>Your admin account has been {status}</p>
    </div>
    
    <div style='padding: 30px; background: #f9f9f9; border-radius: 0 0 10px 10px;'>
        <h2 style='color: #333; margin-top: 0;'>Account Status Changed</h2>
        
        <div style='background: white; padding: 20px; border-radius: 8px; border-left: 4px solid #667eea; margin: 20px 0;'>
            <p><strong>📋 Action:</strong> Your admin account has been <strong>{status}</strong></p>
            <p><strong>👤 Performed By:</strong> {performedBy ?? "System Administrator"}</p>
            <p><strong>📅 Date & Time:</strong> {DateTime.Now:MMMM dd, yyyy HH:mm}</p>
            
            <div style='margin-top: 20px; padding: 15px; background: #f8f9fa; border-radius: 6px;'>
                <p style='margin: 0; color: #666;'>
                    {(status == "deactivated"
                                ? "Your account access has been temporarily suspended. Please contact your system administrator for more information."
                                : "Your account access has been restored. You can now log in and use the system.")}
                </p>
            </div>
        </div>
        
        <div style='text-align: center; margin: 30px 0;'>
            <a href='{Request.Scheme}://{Request.Host}/Account/Login' 
               style='background: #667eea; color: white; padding: 12px 30px; text-decoration: none; border-radius: 6px; font-weight: bold; display: inline-block;'>
                Login to Your Account
            </a>
        </div>
    </div>
    
    <div style='background: #f1f1f1; padding: 20px; text-align: center; font-size: 12px; color: #666; margin-top: 20px; border-radius: 0 0 10px 10px;'>
        <p style='margin: 0;'>This is an automated message. Please do not reply to this email.</p>
        <p style='margin: 10px 0 0;'>© {DateTime.Now.Year} Student Management System. All rights reserved.</p>
    </div>
</div>";

            await _emailService.SendEmailAsync(email, subject, body, new List<string>());
        }


        private async Task SendAdminDeletionEmailAsync(string? email, string? performedBy)
        {
            if (string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("Cannot send admin deletion email: email is null or empty");
                return;
            }

            var subject = "Admin Account Deleted - Student Management System";
            var body = $@"
<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
    <div style='background: linear-gradient(135deg, #dc3545 0%, #a71d2a 100%); padding: 30px; text-align: center; color: white; border-radius: 10px 10px 0 0;'>
        <h1 style='margin: 0; font-size: 28px;'>Account Deletion Notice</h1>
        <p style='margin: 10px 0 0; opacity: 0.9;'>Your admin account has been deleted</p>
    </div>
    
    <div style='padding: 30px; background: #f9f9f9; border-radius: 0 0 10px 10px;'>
        <h2 style='color: #333; margin-top: 0;'>Account Permanently Removed</h2>
        
        <div style='background: white; padding: 20px; border-radius: 8px; border-left: 4px solid #dc3545; margin: 20px 0;'>
            <p><strong>⚠️ Important Notice:</strong> Your admin account has been permanently deleted from the Student Management System.</p>
            <p><strong>👤 Action Performed By:</strong> {performedBy ?? "System Administrator"}</p>
            <p><strong>📅 Date & Time:</strong> {DateTime.Now:MMMM dd, yyyy HH:mm}</p>
            
            <div style='margin-top: 20px; padding: 15px; background: #fff3cd; border-radius: 6px; border: 1px solid #ffeaa7;'>
                <p style='margin: 0; color: #856404;'>
                    <strong>Note:</strong> All your access privileges have been revoked. 
                    If you believe this was done in error, please contact your system administrator immediately.
                </p>
            </div>
        </div>
        
        <div style='background: #f8d7da; padding: 15px; border-radius: 6px; border: 1px solid #f5c6cb; margin: 20px 0;'>
            <p style='margin: 0; color: #721c24;'>
                <strong>Final Notice:</strong> This action cannot be undone. 
                All associated data and access permissions have been permanently removed from the system.
            </p>
        </div>
    </div>
    
    <div style='background: #f1f1f1; padding: 20px; text-align: center; font-size: 12px; color: #666; margin-top: 20px; border-radius: 0 0 10px 10px;'>
        <p style='margin: 0;'>This is an automated message. Please do not reply to this email.</p>
        <p style='margin: 10px 0 0;'>© {DateTime.Now.Year} Student Management System. All rights reserved.</p>
    </div>
</div>";

            await _emailService.SendEmailAsync(email, subject, body, new List<string>());
        }

        [HttpPost]
        public async Task<IActionResult> DeleteApplication(int id)
        {
            try
            {
                var application = await _context.AdminApplications.FindAsync(id);
                if (application == null)
                    return Json(new { success = false, message = "Application not found" });

                _context.AdminApplications.Remove(application);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Application deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting application");
                return Json(new { success = false, message = "Error deleting application" });
            }
        }

        
        //[HttpGet]
        //public async Task<IActionResult> GetAdminDetails(string id)
        //{
        //    try
        //    {
        //        var admin = await _userManager.FindByIdAsync(id);
        //        if (admin == null)
        //        {
        //            return PartialView("_AdminDetailsNotFound");
        //        }

        //        var privilege = await _context.AdminPrivileges
        //            .Include(p => p.University)
        //            .Include(p => p.Faculty)
        //            .Include(p => p.Department)
        //            .FirstOrDefaultAsync(p => p.AdminId == id);

        //        var roles = await _userManager.GetRolesAsync(admin) ?? Array.Empty<string>();

        //        ViewBag.Admin = admin;
        //        ViewBag.Privilege = privilege;
        //        ViewBag.Roles = roles.ToList();

        //        return PartialView("_AdminDetailsPartial");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error getting admin details");
        //        return PartialView("_AdminDetailsError", ex.Message);
        //    }
        //}

        [HttpPost]
        public async Task<IActionResult> ResetAdminPassword(string adminId, string newPassword)
        {
            try
            {
                var admin = await _userManager.FindByIdAsync(adminId);
                if (admin == null)
                {
                    return Json(new { success = false, message = "Admin not found" });
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(admin);
                var result = await _userManager.ResetPasswordAsync(admin, token, newPassword);

                if (result.Succeeded)
                {
                    return Json(new { success = true, message = "Password reset successfully" });
                }
                else
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return Json(new { success = false, message = $"Failed: {errors}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetAdminDetails(string id)
        {
            try
            {
                var admin = await _adminService.GetAdminPrivilegeAsync(id);
                if (admin == null)
                    return Json(new { success = false, message = "Admin not found" });

                // Return data as JSON and let JavaScript handle the display
                return Json(new
                {
                    success = true,
                    data = new
                    {
                        id = admin.AdminId,
                        name = admin.Admin?.UserName ?? "Unknown",
                        email = admin.Admin?.Email ?? "Unknown",
                        type = admin.AdminType.ToString(),
                        status = admin.IsActive ? "Active" : "Inactive",
                        created = admin.CreatedDate.ToString("MMMM dd, yyyy"),
                        createdBy = admin.CreatedBy,
                        permissions = admin.Permissions?.Select(p => p.ToString()).ToList() ?? new List<string>(),
                        scope = new
                        {
                            university = admin.University?.Name,
                            faculty = admin.Faculty?.Name,
                            department = admin.Department?.Name
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private async Task<string> RenderPartialViewToString(string viewName, object model)
        {
            if (string.IsNullOrEmpty(viewName))
                viewName = ControllerContext.ActionDescriptor.ActionName;

            ViewData.Model = model;

            using (var writer = new StringWriter())
            {
                var viewResult = _viewEngine.FindView(ControllerContext, viewName, false);

                if (viewResult.View == null)
                {
                    throw new ArgumentNullException($"{viewName} does not match any available view");
                }

                var viewContext = new ViewContext(
                    ControllerContext,
                    viewResult.View,
                    ViewData,
                    TempData,
                    writer,
                    new HtmlHelperOptions()
                );

                await viewResult.View.RenderAsync(viewContext);
                return writer.ToString();
            }
        }

        [Authorize(Policy = "SuperAdminOnly")]
        public async Task<IActionResult> EmailAnnouncements()
        {
            // Get all users who can receive emails
            var allUsers = await _userManager.Users
                .Select(u => new { u.Id, u.UserName, u.Email, u.EmailConfirmed })
                .ToListAsync();

            var allAdmins = await _context.AdminPrivileges
                .Include(ap => ap.Admin)
                .Select(ap => new { ap.AdminId, ap.Admin.UserName, ap.Admin.Email })
                .ToListAsync();

            var model = new EmailAnnouncementsViewModel
            {
                UserCount = allUsers.Count,
                AdminCount = allAdmins.Count,
                TotalRecipients = allUsers.Count + allAdmins.Count
            };

            return View(model);
        }

        [HttpPost]
        [Authorize(Policy = "SuperAdminOnly")]
        public async Task<IActionResult> SendAnnouncement(EmailAnnouncementsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("EmailAnnouncements", model);
            }

            try
            {
                List<string> recipients = new List<string>();

                // Get recipients based on selection
                if (model.SendToEveryone)
                {
                    var allUsers = await _userManager.Users
                        .Where(u => u.EmailConfirmed && !string.IsNullOrEmpty(u.Email))
                        .Select(u => u.Email!)
                        .ToListAsync();
                    recipients.AddRange(allUsers.Where(email => !string.IsNullOrEmpty(email)));
                }
                else
                {
                    if (model.SendToAllAdmins)
                    {
                        var adminEmails = await _context.AdminPrivileges
                            .Include(ap => ap.Admin)
                            .Where(ap => ap.Admin != null && ap.Admin.EmailConfirmed && !string.IsNullOrEmpty(ap.Admin.Email))
                            .Select(ap => ap.Admin!.Email!)
                            .ToListAsync();
                        recipients.AddRange(adminEmails.Where(email => !string.IsNullOrEmpty(email)));
                    }

                    if (model.SelectedUsers != null && model.SelectedUsers.Any())
                    {
                        var selectedEmails = await _userManager.Users
                            .Where(u => model.SelectedUsers.Contains(u.Id) &&
                                   u.EmailConfirmed &&
                                   !string.IsNullOrEmpty(u.Email))
                            .Select(u => u.Email!)
                            .ToListAsync();
                        recipients.AddRange(selectedEmails.Where(email => !string.IsNullOrEmpty(email)));
                    }

                    if (model.CustomEmails != null && model.CustomEmails.Any())
                    {
                        // Filter out null/empty/whitespace emails
                        var validCustomEmails = model.CustomEmails
                            .Where(email => !string.IsNullOrWhiteSpace(email))
                            .Select(email => email!.Trim()) // Use ! to assert non-null
                            .ToList();
                        recipients.AddRange(validCustomEmails);
                    }
                }

                // Remove duplicates and ensure no nulls
                recipients = recipients
                    .Where(email => !string.IsNullOrEmpty(email))
                    .Distinct()
                    .ToList();

                if (!recipients.Any())
                {
                    TempData["ErrorMessage"] = "No valid recipients found";
                    return RedirectToAction("EmailAnnouncements");
                }

                // Send email to each recipient
                foreach (var email in recipients)
                {
                    await _emailService.SendEmailAsync(
                        email,
                        model.Subject,
                        model.Message,
                        new List<string>()
                    );
                }

                TempData["SuccessMessage"] = $"Announcement sent to {recipients.Count} recipients successfully";
                return RedirectToAction("EmailAnnouncements");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending announcement");
                TempData["ErrorMessage"] = $"Error sending announcement: {ex.Message}";
                return RedirectToAction("EmailAnnouncements");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUsersForEmail()
        {
            var users = await _userManager.Users
                .Where(u => !string.IsNullOrEmpty(u.Email))
                .Select(u => new
                {
                    id = u.Id,
                    name = u.UserName ?? "Unknown",
                    email = u.Email!,
                    isAdmin = _context.AdminPrivileges.Any(ap => ap.AdminId == u.Id)
                })
                .OrderBy(u => u.name)
                .ToListAsync();

            return Json(users);
        }

        public async Task SendEmailAsync(string to, string subject, string body, List<string> attachments)
        {
            try
            {
                Console.WriteLine($"Attempting to send email to: {to}");
                Console.WriteLine($"Using SMTP: {_emailSettings.SmtpServer}:{_emailSettings.SmtpPort}");
                Console.WriteLine($"Username: {_emailSettings.Username}");

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
                message.To.Add(new MailboxAddress("", to));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = body
                };

                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();

                // Add event handlers for debugging
                client.AuthenticationMechanisms.Remove("XOAUTH2");

                await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, SecureSocketOptions.StartTls);
                Console.WriteLine("Connected to SMTP server");

                await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password);
                Console.WriteLine("Authenticated successfully");

                await client.SendAsync(message);
                Console.WriteLine($"Email sent successfully to {to}");

                await client.DisconnectAsync(true);
                Console.WriteLine("Disconnected from SMTP server");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email sending failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        [HttpPost]
        [Authorize(Policy = "SuperAdminOnly")]
        public async Task<IActionResult> TestEmail()
        {
            try
            {
                var testEmail = "dr.aamir.adam@gmail.com";
                var subject = "Test Email from Student Management System";
                var body = $"<h1>Test Email</h1><p>This is a test email sent at {DateTime.Now}</p>";

                // Use fully qualified names
                using var message = new System.Net.Mail.MailMessage();
                message.From = new System.Net.Mail.MailAddress("dr.aamir.adam@gmail.com", "Student Management System");
                message.To.Add(new System.Net.Mail.MailAddress(testEmail));
                message.Subject = subject;
                message.Body = body;
                message.IsBodyHtml = true;

                using var client = new System.Net.Mail.SmtpClient("smtp.gmail.com", 587);
                client.EnableSsl = true;
                client.UseDefaultCredentials = false;
                client.Credentials = new System.Net.NetworkCredential("dr.aamir.adam@gmail.com", "amrc ocji pqhh ruql");

                await client.SendMailAsync(message);

                return Json(new { success = true, message = "Test email sent successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending test email");
                return Json(new
                {
                    success = false,
                    message = $"Error: {ex.Message}"
                });
            }
        }
        //[HttpPost]
        //[Authorize(Policy = "SuperAdminOnly")]
        //public async Task<IActionResult> TestEmail()
        //{
        //    try
        //    {
        //        using var message = new System.Net.Mail.MailMessage();
        //        message.From = new System.Net.Mail.MailAddress("dr.aamir.adam@gmail.com", "Student Management System");
        //        message.To.Add("dr.aamir.adam@gmail.com");
        //        message.Subject = "Test Email";
        //        message.Body = "<h1>Test</h1><p>This is a test</p>";
        //        message.IsBodyHtml = true;

        //        using var client = new System.Net.Mail.SmtpClient("smtp.gmail.com", 587);
        //        client.EnableSsl = true;
        //        client.UseDefaultCredentials = false;
        //        client.Credentials = new System.Net.NetworkCredential("dr.aamir.adam@gmail.com", "amrc ocji pqhh ruql");

        //        await client.SendMailAsync(message);

        //        return Json(new { success = true, message = "Email sent!" });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { success = false, message = ex.Message });
        //    }
        //}


        //[HttpGet]
        //[AllowAnonymous]
        //public async Task<IActionResult> MyApplicationStatus()
        //{
        //    // This method is marked async but doesn't have await
        //    var userEmail = User.Identity?.Name;
        //    if (string.IsNullOrEmpty(userEmail))
        //    {
        //        return RedirectToAction("Apply");
        //    }

        //    var applications = await _context.AdminApplications // Add await
        //        .Include(a => a.University)
        //        .Include(a => a.Faculty)
        //        .Include(a => a.Department)
        //        .Where(a => a.Email == userEmail)
        //        .OrderByDescending(a => a.AppliedDate)
        //        .ToListAsync();

        //    var viewModel = applications.Select(a => new AdminApplicationViewModel
        //    {
        //        Id = a.Id,
        //        ApplicantName = a.ApplicantName,
        //        Email = a.Email,
        //        Phone = a.Phone,
        //        AppliedAdminType = a.AppliedAdminType,
        //        Justification = a.Justification,
        //        Status = a.Status,
        //        AppliedDate = a.AppliedDate,
        //        ReviewedDate = a.ReviewedDate,
        //        ReviewedBy = a.ReviewedBy,
        //        ReviewNotes = a.ReviewNotes,
        //        UniversityName = a.University?.Name,
        //        FacultyName = a.Faculty?.Name,
        //        DepartmentName = a.Department?.Name
        //    }).ToList();

        //    return View(viewModel);
        //}


        //private readonly ILogger<AdminManagementController> _logger;

        //public AdminManagementController(
        //    IAdminService adminService,
        //    UserManager<IdentityUser> userManager,
        //    ApplicationDbContext context,
        //    SignInManager<IdentityUser> signInManager,
        //    ILogger<AdminManagementController> logger) // Add this
        //{
        //    _adminService = adminService;
        //    _userManager = userManager;
        //    _context = context;
        //    _signInManager = signInManager;
        //    _logger = logger; // Add this
        //}

    }

    public class AnalyticsViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalAdmins { get; set; }
        public int TotalStudents { get; set; }
        public int TotalFaculty { get; set; }
        public int ActiveUsersLast24Hours { get; set; }
        public string? SystemUptime { get; set; }
        public DateTime ServerTime { get; set; }
    }
}