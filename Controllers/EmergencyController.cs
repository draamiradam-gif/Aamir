using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;

namespace StudentManagementSystem.Controllers
{
    public class EmergencyController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EmergencyController(ApplicationDbContext context)
        {
            _context = context;
        }

        [Route("/emergency")]
        public async Task<IActionResult> Index()
        {
            ViewBag.CanConnect = await _context.Database.CanConnectAsync();
            ViewBag.ConnectionString = _context.Database.GetConnectionString();

            return View();
        }

        [Route("/health")]
        public IActionResult HealthCheck()
        {
            return Content("OK");
        }
    }
}