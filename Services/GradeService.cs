using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;

namespace StudentManagementSystem.Services
{
    public class GradeService : IGradeService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<GradeService> _logger;

        public GradeService(ApplicationDbContext context, ILogger<GradeService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> AssignGradeAsync(int enrollmentId, decimal mark)
        {
            try
            {
                var enrollment = await _context.CourseEnrollments
                    .Include(e => e.Student)
                    .Include(e => e.Course)
                    .FirstOrDefaultAsync(e => e.Id == enrollmentId);

                if (enrollment == null)
                    return false;

                // Use the CalculateGrade method from CourseEnrollment
                enrollment.Grade = mark;
                enrollment.CalculateGrade(); // This uses your existing logic

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning grade for enrollment {EnrollmentId}", enrollmentId);
                return false;
            }
        }

        public async Task<bool> UpdateGradeAsync(int enrollmentId, decimal mark)
        {
            return await AssignGradeAsync(enrollmentId, mark);
        }

        public async Task<decimal> CalculateStudentGPAAsync(int studentId)
        {
            try
            {
                var completedEnrollments = await _context.CourseEnrollments
                    .Include(e => e.Course)
                    .Where(e => e.StudentId == studentId &&
                               e.GradeStatus == GradeStatus.Completed &&
                               e.GradePoints.HasValue &&
                               e.Course != null)
                    .ToListAsync();

                if (!completedEnrollments.Any())
                    return 0.0m;

                decimal totalGradePoints = 0.0m;
                int totalCredits = 0;

                foreach (var enrollment in completedEnrollments)
                {
                    if (enrollment.GradePoints.HasValue && enrollment.Course != null)
                    {
                        var credits = enrollment.Course.Credits; // This is already int, no need for ??
                        if (credits > 0)
                        {
                            totalGradePoints += enrollment.GradePoints.Value * credits;
                            totalCredits += credits;
                        }
                    }
                }

                return totalCredits > 0 ? Math.Round(totalGradePoints / totalCredits, 2) : 0.0m;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating GPA for student {StudentId}", studentId);
                return 0.0m;
            }
        }

        public async Task<decimal> CalculateSemesterGPAAsync(int studentId, int semesterId)
        {
            // Note: You might want to add SemesterId to CourseEnrollment later
            // For now, we'll calculate overall GPA
            return await CalculateStudentGPAAsync(studentId);
        }

        public async Task<StudentTranscript> GenerateTranscriptAsync(int studentId)
        {
            var transcript = new StudentTranscript();

            try
            {
                var student = await _context.Students
                    .Include(s => s.Department)
                    .FirstOrDefaultAsync(s => s.Id == studentId);

                if (student == null)
                    return transcript;

                // Populate student info - Your StudentTranscript model only has Student property
                transcript.Student = student;

                // Get all completed enrollments
                var enrollments = await _context.CourseEnrollments
                    .Include(e => e.Course)
                    .Where(e => e.StudentId == studentId && e.GradeStatus == GradeStatus.Completed)
                    .OrderBy(e => e.EnrollmentDate)
                    .ToListAsync();

                transcript.Enrollments = enrollments;
                transcript.GPA = await CalculateStudentGPAAsync(studentId);
                transcript.GeneratedDate = DateTime.Now;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating transcript for student {StudentId}", studentId);
            }

            return transcript;
        }

        public async Task<List<GradeScale>> GetActiveGradeScalesAsync()
        {
            return await _context.GradeScales
                .Where(gs => gs.IsActive)
                .OrderBy(gs => gs.MinPercentage)
                .ToListAsync();
        }

        public async Task<bool> InitializeDefaultGradeScalesAsync()
        {
            try
            {
                if (await _context.GradeScales.AnyAsync())
                    return true; // Already initialized

                var gradeScales = new List<GradeScale>
                {
                    new() { GradeLetter = "A+", Description = "Exceptional", MinPercentage = 96, MaxPercentage = 100, GradePoints = 4.0m, IsPassingGrade = true },
                    new() { GradeLetter = "A", Description = "Excellent", MinPercentage = 92, MaxPercentage = 95, GradePoints = 4.0m, IsPassingGrade = true },
                    new() { GradeLetter = "A-", Description = "Excellent", MinPercentage = 88, MaxPercentage = 91, GradePoints = 3.7m, IsPassingGrade = true },
                    new() { GradeLetter = "B+", Description = "Good", MinPercentage = 84, MaxPercentage = 87, GradePoints = 3.3m, IsPassingGrade = true },
                    new() { GradeLetter = "B", Description = "Good", MinPercentage = 80, MaxPercentage = 83, GradePoints = 3.0m, IsPassingGrade = true },
                    new() { GradeLetter = "B-", Description = "Good", MinPercentage = 76, MaxPercentage = 79, GradePoints = 2.7m, IsPassingGrade = true },
                    new() { GradeLetter = "C+", Description = "Satisfactory", MinPercentage = 72, MaxPercentage = 75, GradePoints = 2.3m, IsPassingGrade = true },
                    new() { GradeLetter = "C", Description = "Satisfactory", MinPercentage = 68, MaxPercentage = 71, GradePoints = 2.0m, IsPassingGrade = true },
                    new() { GradeLetter = "C-", Description = "Satisfactory", MinPercentage = 64, MaxPercentage = 67, GradePoints = 1.7m, IsPassingGrade = true },
                    new() { GradeLetter = "D+", Description = "Poor", MinPercentage = 60, MaxPercentage = 63, GradePoints = 1.3m, IsPassingGrade = true },
                    new() { GradeLetter = "D", Description = "Poor", MinPercentage = 55, MaxPercentage = 59, GradePoints = 1.0m, IsPassingGrade = true },
                    new() { GradeLetter = "D-", Description = "Poor", MinPercentage = 50, MaxPercentage = 54, GradePoints = 0.7m, IsPassingGrade = true },
                    new() { GradeLetter = "F", Description = "Failure", MinPercentage = 0, MaxPercentage = 49, GradePoints = 0.0m, IsPassingGrade = false }
                };

                await _context.GradeScales.AddRangeAsync(gradeScales);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing grade scales");
                return false;
            }
        }

        public Task<string> CalculateGradeFromMark(decimal mark)
        {
            var enrollment = new CourseEnrollment();
            enrollment.Grade = mark;
            enrollment.CalculateGrade();
            return Task.FromResult(enrollment.GradeLetter ?? "F");
        }

        public Task<decimal> CalculatePointsFromMark(decimal mark)
        {
            var enrollment = new CourseEnrollment();
            enrollment.Grade = mark;
            enrollment.CalculateGrade();
            return Task.FromResult(enrollment.GradePoints ?? 0.0m);
        }

        public async Task<List<CourseEnrollment>> GetStudentEnrollmentsAsync(int studentId)
        {
            return await _context.CourseEnrollments
                .Include(e => e.Course)
                .Where(e => e.StudentId == studentId)
                .OrderByDescending(e => e.EnrollmentDate)
                .ToListAsync();
        }

        public async Task<List<CourseEnrollment>> GetCourseEnrollmentsAsync(int courseId)
        {
            return await _context.CourseEnrollments
                .Include(e => e.Student)
                .Where(e => e.CourseId == courseId)
                .OrderBy(e => e.Student!.Name)
                .ToListAsync();
        }

        public async Task<bool> EnrollStudentInCourseAsync(int studentId, int courseId)
        {
            try
            {
                // Check if already enrolled
                var existingEnrollment = await _context.CourseEnrollments
                    .FirstOrDefaultAsync(e => e.StudentId == studentId && e.CourseId == courseId && e.IsActive);

                if (existingEnrollment != null)
                    return false; // Already enrolled

                var enrollment = new CourseEnrollment
                {
                    StudentId = studentId,
                    CourseId = courseId,
                    EnrollmentDate = DateTime.Now,
                    GradeStatus = GradeStatus.InProgress,
                    IsActive = true
                };

                await _context.CourseEnrollments.AddAsync(enrollment);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enrolling student {StudentId} in course {CourseId}", studentId, courseId);
                return false;
            }
        }
    }
}