using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;
using StudentManagementSystem.Services;

namespace StudentManagementSystem.Controllers
{
    [Authorize(Policy = "SuperAdminOnly")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAdminService _adminService;

        public ReportsController(ApplicationDbContext context, IAdminService adminService)
        {
            _context = context;
            _adminService = adminService;
        }

        public async Task<IActionResult> Index()
        {
            var model = new ReportsDashboardViewModel
            {
                TotalAdmins = await _context.AdminPrivileges.CountAsync(),
                TotalApplications = await _context.AdminApplications.CountAsync(),
                RecentActivityCount = await _context.AdminApplications
                    .Where(a => a.AppliedDate >= DateTime.Now.AddDays(-7))
                    .CountAsync()
            };

            return View(model);
        }

        public async Task<IActionResult> AdminActivityReport(DateTime? startDate, DateTime? endDate)
        {
            startDate ??= DateTime.Now.AddDays(-30);
            endDate ??= DateTime.Now;

            var activities = await _context.AdminApplications
                .Where(a => a.AppliedDate >= startDate && a.AppliedDate <= endDate)
                .Include(a => a.University)
                .Include(a => a.Faculty)
                .Include(a => a.Department)
                .OrderByDescending(a => a.AppliedDate)
                .ToListAsync();

            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;

            return View(activities);
        }

        public async Task<IActionResult> PermissionUsageReport()
        {
            var privileges = await _context.AdminPrivileges.ToListAsync();

            var permissionUsage = privileges
                .SelectMany(p => p.Permissions)
                .GroupBy(p => p)
                .Select(g => new PermissionUsageViewModel
                {
                    Permission = g.Key,
                    UsageCount = g.Count(),
                    Percentage = (g.Count() * 100.0) / privileges.Count
                })
                .OrderByDescending(p => p.UsageCount)
                .ToList();

            return View(permissionUsage);
        }

        public async Task<IActionResult> ExportAdminAuditReport()
        {
            try
            {
                var admins = await _adminService.GetAllAdminPrivilegesAsync();
                var applications = await _context.AdminApplications.ToListAsync();

                // In a real implementation, you'd generate a PDF or Excel file
                // For now, return a JSON file as example
                var reportData = new
                {
                    GeneratedAt = DateTime.Now,
                    TotalAdmins = admins.Count,
                    TotalApplications = applications.Count,
                    Admins = admins.Select(a => new
                    {
                        a.Admin.UserName,
                        a.AdminType,
                        a.CreatedDate,
                        PermissionCount = a.Permissions.Count
                    }),
                    Applications = applications.Select(a => new
                    {
                        a.ApplicantName,
                        a.AppliedAdminType,
                        a.Status,
                        a.AppliedDate
                    })
                };

                var json = System.Text.Json.JsonSerializer.Serialize(reportData, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });

                var bytes = System.Text.Encoding.UTF8.GetBytes(json);
                return File(bytes, "application/json", $"admin-audit-report-{DateTime.Now:yyyyMMddHHmmss}.json");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to generate report: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> SystemUsageReport()
        {
            var thirtyDaysAgo = DateTime.Now.AddDays(-30);

            var report = new SystemUsageReportViewModel
            {
                NewAdminsCount = await _context.AdminPrivileges
                    .Where(a => a.CreatedDate >= thirtyDaysAgo)
                    .CountAsync(),

                ApplicationsCount = await _context.AdminApplications
                    .Where(a => a.AppliedDate >= thirtyDaysAgo)
                    .CountAsync(),

                ApprovedApplications = await _context.AdminApplications
                    .Where(a => a.AppliedDate >= thirtyDaysAgo && a.Status == ApplicationStatus.Approved)
                    .CountAsync(),

                RejectedApplications = await _context.AdminApplications
                    .Where(a => a.AppliedDate >= thirtyDaysAgo && a.Status == ApplicationStatus.Rejected)
                    .CountAsync()
            };

            return View(report);
        }
    }

    public class ReportsDashboardViewModel
    {
        public int TotalAdmins { get; set; }
        public int TotalApplications { get; set; }
        public int RecentActivityCount { get; set; }
    }

    public class PermissionUsageViewModel
    {
        public PermissionModule Permission { get; set; }
        public int UsageCount { get; set; }
        public double Percentage { get; set; }
    }

    public class SystemUsageReportViewModel
    {
        public int NewAdminsCount { get; set; }
        public int ApplicationsCount { get; set; }
        public int ApprovedApplications { get; set; }
        public int RejectedApplications { get; set; }
    }
}