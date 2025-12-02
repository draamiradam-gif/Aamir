using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;
using StudentManagementSystem.Services;

namespace StudentManagementSystem.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AdminAccess")]
    public class AdminApiController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly ApplicationDbContext _context;

        public AdminApiController(IAdminService adminService, ApplicationDbContext context)
        {
            _adminService = adminService;
            _context = context;
        }

        [HttpGet("admins")]
        public async Task<IActionResult> GetAdmins()
        {
            try
            {
                var admins = await _adminService.GetAllAdminPrivilegesAsync();
                var result = admins.Select(a => new
                {
                    a.AdminId,
                    a.Admin.UserName,
                    a.Admin.Email,
                    a.AdminType,
                    Permissions = a.Permissions,
                    IsActive = a.IsActive,
                    a.CreatedDate
                });

                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        [HttpPost("approve-application")]
        public async Task<IActionResult> ApproveApplication([FromBody] ApproveApplicationRequest request)
        {
            try
            {
                var result = await _adminService.ReviewApplicationAsync(
                    request.ApplicationId,
                    request.Status,
                    User.Identity?.Name ?? "API",
                    request.ReviewNotes ?? "");

                if (result && request.Status == ApplicationStatus.Approved)
                {
                    await _adminService.CreateAdminFromApplicationAsync(request.ApplicationId, User.Identity?.Name ?? "API");
                }

                return Ok(new { success = true, message = $"Application {request.Status.ToString().ToLower()} successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        [HttpGet("dashboard-stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var stats = new
                {
                    TotalAdmins = await _context.AdminPrivileges.CountAsync(),
                    PendingApplications = await _context.AdminApplications.CountAsync(a => a.Status == ApplicationStatus.Pending),
                    ActiveAdmins = await _context.AdminPrivileges.CountAsync(a => a.IsActive),
                    TotalUsers = await _context.Users.CountAsync()
                };

                return Ok(new { success = true, data = stats });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }
    }
}