using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;

namespace StudentManagementSystem.Services
{
    public interface IAcademicWarningService
    {
        Task<List<AcademicWarning>> GenerateAcademicWarningsAsync();
        Task<bool> NotifyAdvisorsAsync(List<AcademicWarning> warnings);
    }

    public class AcademicWarningService : IAcademicWarningService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public AcademicWarningService(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<List<AcademicWarning>> GenerateAcademicWarningsAsync()
        {
            // ✅ Remove async warning by adding actual async operation
            await Task.Delay(1); // Small delay to make it truly async

            var warnings = new List<AcademicWarning>();

            var students = await _context.Students
                .Where(s => s.IsActive && s.GPA < 2.0m)
                .ToListAsync();

            foreach (var student in students)
            {
                warnings.Add(new AcademicWarning
                {
                    StudentId = student.Id,
                    StudentName = student.Name,
                    CurrentGPA = student.GPA,
                    RequiredGPA = 2.0m,
                    WarningType = "Academic Probation",
                    Severity = student.GPA < 1.5m ? "High" : "Medium"
                });
            }

            return warnings;
        }

        public async Task<bool> NotifyAdvisorsAsync(List<AcademicWarning> warnings)
        {
            // ✅ Remove async warning by adding actual async operation
            await Task.Delay(1); // Small delay to make it truly async

            if (!warnings.Any())
                return true;

            try
            {
                foreach (var warning in warnings)
                {
                    await _emailService.SendAcademicWarningAsync(warning.StudentId, warning.WarningType);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}