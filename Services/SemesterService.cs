using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;
using System.Threading.Tasks;

namespace StudentManagementSystem.Services
{
    public interface ISemesterService
    {
        Task<bool> SetCurrentSemesterAsync(int semesterId);
        Task<bool> CloseRegistrationAsync(int semesterId);
        Task<bool> CanDeleteSemesterAsync(int semesterId);
        Task<int> GetCourseCountAsync(int semesterId);
    }

    public class SemesterService : ISemesterService
    {
        private readonly ApplicationDbContext _context;

        public SemesterService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> SetCurrentSemesterAsync(int semesterId)
        {
            try
            {
                // Clear current flag from all semesters
                var currentSemesters = await _context.Semesters
                    .Where(s => s.IsCurrent)
                    .ToListAsync();

                foreach (var semester in currentSemesters)
                {
                    semester.IsCurrent = false;
                }

                // Set the selected semester as current
                var targetSemester = await _context.Semesters.FindAsync(semesterId);
                if (targetSemester != null)
                {
                    targetSemester.IsCurrent = true;
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> CloseRegistrationAsync(int semesterId)
        {
            try
            {
                var semester = await _context.Semesters.FindAsync(semesterId);
                if (semester != null)
                {
                    semester.IsRegistrationOpen = false;
                    await _context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> CanDeleteSemesterAsync(int semesterId)
        {
            return !await _context.Courses.AnyAsync(c => c.SemesterId == semesterId);
        }

        public async Task<int> GetCourseCountAsync(int semesterId)
        {
            return await _context.Courses
                .Where(c => c.SemesterId == semesterId && c.IsActive)
                .CountAsync();
        }
    }
}