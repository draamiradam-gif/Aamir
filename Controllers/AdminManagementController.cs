using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;
using StudentManagementSystem.Models.ViewModels;
using StudentManagementSystem.Services;
using System.Security.Claims;

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

        public AdminManagementController(
            IAdminService adminService,
            UserManager<IdentityUser> userManager,
            ApplicationDbContext context,
            SignInManager<IdentityUser> signInManager,
            ILogger<CollegesController> logger)
        {
            _adminService = adminService;
            _userManager = userManager;
            _context = context;
            _signInManager = signInManager;
            _logger = logger;
        }

        [Authorize(Policy = "SuperAdminOnly")]
        public async Task<IActionResult> Index()
        {
            var privileges = await _adminService.GetAllAdminPrivilegesAsync();
            var viewModel = privileges.Select(p => new AdminPrivilegeViewModel
            {
                AdminId = p.AdminId,
                AdminName = p.Admin?.UserName ?? "N/A",
                Email = p.Admin?.Email ?? "N/A",
                AdminType = p.AdminType,
                Permissions = p.Permissions,
                UniversityScope = p.University?.Name,
                FacultyScope = p.Faculty?.Name,
                DepartmentScope = p.Department?.Name,
                IsActive = p.IsActive,
                CreatedDate = p.CreatedDate,
                CreatedBy = p.CreatedBy
            }).ToList();

            return View(viewModel);
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
            var applications = await _adminService.GetPendingApplicationsAsync();
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
            var application = await _adminService.GetApplicationByIdAsync(id);
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

            return View(viewModel);
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
                    await _adminService.CreateAdminFromApplicationAsync(id, User.Identity?.Name ?? "System");
                }

                TempData["SuccessMessage"] = $"Application {status.ToString().ToLower()} successfully";
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

            var allPermissions = Enum.GetValues<PermissionModule>().ToList();
            var model = new EditAdminPrivilegesViewModel
            {
                AdminId = privilege.AdminId,
                AdminName = privilege.Admin?.UserName ?? "N/A",
                Email = privilege.Admin?.Email ?? "N/A",
                AdminType = privilege.AdminType,
                CurrentPermissions = privilege.Permissions,
                AllPermissions = allPermissions
            };

            return View(model);
        }

        [HttpPost]
        [Authorize(Policy = "SuperAdminOnly")]
        public async Task<IActionResult> EditPrivileges(string id, List<PermissionModule> selectedPermissions)
        {
            var result = await _adminService.UpdateAdminPrivilegesAsync(id, selectedPermissions, User.Identity?.Name ?? "System");
            if (result)
            {
                TempData["SuccessMessage"] = "Privileges updated successfully";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to update privileges";
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
                                    // Here you would implement email sending logic
                                    // await SendWelcomeEmail(email, fullName, password);
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
                                    privilege.UpdatedBy = createdBy;
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
                        AdminEmail = a.Admin.Email ?? "N/A", // Handle null Email
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
                        AdminEmail = a.Admin.Email ?? "N/A", // Handle null Email
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
}