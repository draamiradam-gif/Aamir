using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;
using StudentManagementSystem.Services;

namespace StudentManagementSystem.Controllers
{
    public class GradeRevisionsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IGradeService _gradeService;
        private readonly IEmailService _emailService;

        public GradeRevisionsController(ApplicationDbContext context, IGradeService gradeService, IEmailService emailService)
        {
            _context = context;
            _gradeService = gradeService;
            _emailService = emailService;
        }

        // GET: Grade revisions pending approval
        public async Task<IActionResult> Index()
        {
            var pendingRevisions = await _context.CourseEnrollments
                .Include(e => e.Student)
                .Include(e => e.Course)
                .Where(e => e.GradeRevisionRequested && e.GradeRevisionStatus == GradeRevisionStatus.Pending)
                .ToListAsync();

            return View(pendingRevisions);
        }

        // POST: Approve grade revision
        [HttpPost]
        public async Task<IActionResult> Approve(int enrollmentId, string comments)
        {
            var enrollment = await _context.CourseEnrollments
                .Include(e => e.Student)
                .Include(e => e.Course)
                .FirstOrDefaultAsync(e => e.Id == enrollmentId);

            if (enrollment == null)
            {
                return NotFound();
            }

            enrollment.ApproveGradeRevision(User.Identity?.Name ?? "System", comments);
            await _context.SaveChangesAsync();

            // Notify student
            await _emailService.SendGradeRevisionStatusAsync(enrollmentId, "Approved");

            TempData["Success"] = "Grade revision approved successfully";
            return RedirectToAction(nameof(Index));
        }

        // POST: Reject grade revision
        [HttpPost]
        public async Task<IActionResult> Reject(int enrollmentId, string comments)
        {
            var enrollment = await _context.CourseEnrollments
                .Include(e => e.Student)
                .Include(e => e.Course)
                .FirstOrDefaultAsync(e => e.Id == enrollmentId);

            if (enrollment == null)
            {
                return NotFound();
            }

            enrollment.RejectGradeRevision(User.Identity?.Name ?? "System", comments);
            await _context.SaveChangesAsync();

            // Notify student
            await _emailService.SendGradeRevisionStatusAsync(enrollmentId, "Rejected");

            TempData["Success"] = "Grade revision rejected";
            return RedirectToAction(nameof(Index));
        }
    }
}