using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;
using System.Diagnostics;

namespace StudentManagementSystem.Controllers
{
    [Route("debug")]
    public class DatabaseCheckController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DatabaseCheckController> _logger;

        public DatabaseCheckController(ApplicationDbContext context, ILogger<DatabaseCheckController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("db")]
        public async Task<IActionResult> CheckDatabase()
        {
            try
            {
                var canConnect = await _context.Database.CanConnectAsync();

                var result = new
                {
                    Status = canConnect ? "Connected" : "Not Connected",
                    ConnectionString = _context.Database.GetConnectionString(),
                    Students = canConnect ? await _context.Students.CountAsync() : 0,
                    Courses = canConnect ? await _context.Courses.CountAsync() : 0,
                    Users = canConnect ? await _context.Users.CountAsync() : 0,
                    Time = DateTime.Now
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    Status = "Error",
                    Message = ex.Message,
                    StackTrace = ex.StackTrace,
                    ConnectionString = _context.Database.GetConnectionString(),
                    Time = DateTime.Now
                });
            }
        }

        [HttpGet("migrate")]
        public async Task<IActionResult> ApplyMigrations()
        {
            try
            {
                await _context.Database.MigrateAsync();
                return Content("✅ Migrations applied successfully!");
            }
            catch (Exception ex)
            {
                return Content($"❌ Migration failed: {ex.Message}\n\n{ex.StackTrace}");
            }
        }

        [HttpGet("tables")]
        public async Task<IActionResult> ListTables()
        {
            try
            {
                var tableList = new List<string>();

                // Try to get table list (SQL Server specific)
                var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                var command = connection.CreateCommand();
                command.CommandText = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'";

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        tableList.Add(reader.GetString(0));
                    }
                }

                await connection.CloseAsync();

                return Json(new
                {
                    Tables = tableList,
                    Count = tableList.Count
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    Error = ex.Message,
                    Tables = new List<string>()
                });
            }
        }
    }
}