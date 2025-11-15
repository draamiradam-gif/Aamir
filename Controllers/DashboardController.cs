using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentManagementSystem.Services;
using StudentManagementSystem.ViewModels;
using StudentManagementSystem.Models;

namespace StudentManagementSystem.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IStudentService _studentService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(IStudentService studentService, ILogger<DashboardController> logger)
        {
            _studentService = studentService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var students = await _studentService.GetAllStudentsAsync();

                var viewModel = new DashboardViewModel
                {
                    TotalStudents = students.Count,
                    StudentsThisMonth = students.Count(s => s.CreatedDate >= DateTime.Now.AddMonths(-1)),
                    AverageGPA = students.Any() ? students.Average(s => s.GPA) : 0,
                    AveragePercentage = students.Any() ? students.Average(s => s.Percentage) : 0,
                    RecentStudents = students.OrderByDescending(s => s.CreatedDate).Take(10).ToList(),
                    TotalImports = 15,
                    TotalExports = 8
                };

                // Department Statistics
                var departmentGroups = students
                    .Where(s => !string.IsNullOrEmpty(s.Department))
                    .GroupBy(s => s.Department)
                    .Select(g => new DepartmentStats
                    {
                        DepartmentName = g.Key ?? "Unknown",
                        StudentCount = g.Count(),
                        Percentage = students.Count > 0 ? (decimal)g.Count() / students.Count * 100 : 0
                    })
                    .OrderByDescending(d => d.StudentCount)
                    .ToList();

                viewModel.DepartmentStatistics = departmentGroups;

                // GPA Distribution - FIXED with decimal literals
                viewModel.GPAStatistics = new List<GPAStats>
                {
                    new GPAStats { Range = "4.0 - 3.5", StudentCount = students.Count(s => s.GPA >= 3.5m), Color = "#28a745" },
                    new GPAStats { Range = "3.4 - 3.0", StudentCount = students.Count(s => s.GPA >= 3.0m && s.GPA < 3.5m), Color = "#20c997" },
                    new GPAStats { Range = "2.9 - 2.5", StudentCount = students.Count(s => s.GPA >= 2.5m && s.GPA < 3.0m), Color = "#ffc107" },
                    new GPAStats { Range = "2.4 - 2.0", StudentCount = students.Count(s => s.GPA >= 2.0m && s.GPA < 2.5m), Color = "#fd7e14" },
                    new GPAStats { Range = "Below 2.0", StudentCount = students.Count(s => s.GPA < 2.0m), Color = "#dc3545" }
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard");
                TempData["Error"] = "Error loading dashboard data.";
                return View(new DashboardViewModel());
            }
        }

        
    }
}