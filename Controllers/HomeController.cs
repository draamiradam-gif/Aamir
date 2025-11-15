using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;
using StudentManagementSystem.Models.ViewModels;
using System.Diagnostics;
using System.Threading.Tasks;

namespace StudentManagementSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                ViewBag.TotalUniversities = await _context.Universities.CountAsync(u => u.IsActive);
                ViewBag.TotalColleges = await _context.Colleges.CountAsync(c => c.IsActive);
                ViewBag.TotalStudents = await _context.Students.CountAsync();
                ViewBag.TotalCourses = await _context.Courses.CountAsync(c => c.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard statistics");
                // Set default values if there's an error
                ViewBag.TotalUniversities = 0;
                ViewBag.TotalColleges = 0;
                ViewBag.TotalStudents = 0;
                ViewBag.TotalCourses = 0;
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public async Task<IActionResult> Hierarchy(string scope = "all")
        {
            try
            {
                var universities = await _context.Universities
                    .Include(u => u.Colleges)
                        .ThenInclude(c => c.Departments)
                            .ThenInclude(d => d.Branches)
                    .Where(u => u.IsActive)
                    .OrderBy(u => u.Name)
                    .ToListAsync();

                var totalColleges = universities.Sum(u => u.Colleges.Count);
                var totalDepartments = universities.Sum(u => u.Colleges.Sum(c => c.Departments.Count));
                var totalBranches = universities.Sum(u => u.Colleges.Sum(c => c.Departments.Sum(d => d.Branches.Count)));
                var totalSemesters = await _context.Semesters.CountAsync(s => s.IsActive);
                var totalStudents = await _context.Students.CountAsync();
                var totalCourses = await _context.Courses.CountAsync(c => c.IsActive);

                var viewModel = new UniversityHierarchyViewModel
                {
                    Universities = universities,
                    TotalUniversities = universities.Count,
                    TotalColleges = totalColleges,
                    TotalDepartments = totalDepartments,
                    TotalBranches = totalBranches,
                    TotalSemesters = totalSemesters,
                    TotalStudents = totalStudents,
                    TotalCourses = totalCourses,
                    AccessScope = scope
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading hierarchy");
                return View(new UniversityHierarchyViewModel());
            }
        }
    }

    public class ErrorViewModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}