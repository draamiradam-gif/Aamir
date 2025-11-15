using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;

namespace StudentManagementSystem.Controllers
{
    public class DatabaseCheckController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DatabaseCheckController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var canConnect = await _context.Database.CanConnectAsync();
                ViewBag.CanConnect = canConnect;

                if (canConnect)
                {
                    // Check if Students table exists
                    var studentsTableExists = await _context.Students.AnyAsync();
                    ViewBag.StudentsTableExists = studentsTableExists;
                    ViewBag.StudentCount = await _context.Students.CountAsync();

                    // List all tables
                    var connection = _context.Database.GetDbConnection();
                    await connection.OpenAsync();
                    var tables = connection.GetSchema("Tables");
                    ViewBag.Tables = tables;
                }

                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View();
            }
        }
        //****************
        // Add this to any controller temporarily
        public IActionResult ProjectStructure()
        {
            var structure = new List<string>();
            var projectPath = Directory.GetCurrentDirectory();

            // Get all files
            var files = Directory.GetFiles(projectPath, "*.*", SearchOption.AllDirectories)
                .Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\"))
                .Select(f => f.Replace(projectPath, ""))
                .OrderBy(f => f)
                .ToList();

            ViewBag.ProjectFiles = files;
            return View();
        }
    }
}