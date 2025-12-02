using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;
using System.Text.Json;

namespace StudentManagementSystem.Services
{
    public interface IGradingService
    {
        Task<bool> ApplyGradingTemplateToCourse(int courseId, int templateId);
        Task<GradingTemplate> CreateGradingTemplate(GradingTemplate template);
        Task<FinalGrade> CalculateFinalGrade(int studentId, int courseId, int semesterId);
        Task<bool> UpdateStudentGrade(StudentGrade grade);
        Task<List<StudentGrade>> GetStudentGrades(int studentId, int courseId, int semesterId);
        Task<GradingSummary> GetCourseGradingSummary(int courseId, int semesterId);
        Task<GradeReport> GenerateGradeReport(int courseId, int semesterId);
        Task<BulkGradeResult> ImportGradesFromExcel(Stream fileStream, int courseId, int semesterId);
        Task<Stream> ExportGradesToExcel(int courseId, int semesterId);
        Task<bool> ValidateGradingComponents(int courseId);
    }

    public class GradingService : IGradingService
    {
        private readonly ApplicationDbContext _context;

        public GradingService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<GradingTemplate> CreateGradingTemplate(GradingTemplate template)
        {
            _context.GradingTemplates.Add(template);
            await _context.SaveChangesAsync();
            return template;
        }

        public async Task<bool> ApplyGradingTemplateToCourse(int courseId, int templateId)
        {
            var course = await _context.Courses.FindAsync(courseId);
            var template = await _context.GradingTemplates
                .Include(t => t.Components)
                .FirstOrDefaultAsync(t => t.Id == templateId);

            if (course == null || template == null)
                return false;

            // Remove existing components
            var existingComponents = _context.GradingComponents.Where(gc => gc.CourseId == courseId);
            _context.GradingComponents.RemoveRange(existingComponents);

            // Create new components from template
            foreach (var component in template.Components)
            {
                var gradingComponent = new GradingComponent
                {
                    CourseId = courseId,
                    Name = component.Name,
                    ComponentType = component.ComponentType,
                    WeightPercentage = component.WeightPercentage,
                    MaximumMarks = component.MaximumMarks,
                    Description = component.Name,
                    IsActive = true,
                    IncludeInFinalGrade = component.IsRequired
                };
                _context.GradingComponents.Add(gradingComponent);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<FinalGrade> CalculateFinalGrade(int studentId, int courseId, int semesterId)
        {
            var grades = await _context.StudentGrades
                .Include(g => g.GradingComponent)
                .Where(g => g.StudentId == studentId &&
                           g.CourseId == courseId &&
                           g.SemesterId == semesterId &&
                           g.GradingComponent!.IncludeInFinalGrade)
                .ToListAsync();

            var totalWeightedScore = grades.Sum(g => g.WeightedScore);
            var totalPercentage = grades.Any() ? (totalWeightedScore / grades.Sum(g => g.GradingComponent?.WeightPercentage ?? 0)) * 100 : 0;

            var finalGrade = await _context.FinalGrades
                .FirstOrDefaultAsync(fg => fg.StudentId == studentId &&
                                         fg.CourseId == courseId &&
                                         fg.SemesterId == semesterId);

            if (finalGrade == null)
            {
                finalGrade = new FinalGrade
                {
                    StudentId = studentId,
                    CourseId = courseId,
                    SemesterId = semesterId
                };
                _context.FinalGrades.Add(finalGrade);
            }

            // Calculate grade points and letter
            var (gradePoints, gradeLetter) = CalculateGradePointsAndLetter(totalPercentage);

            finalGrade.FinalPercentage = totalPercentage;
            finalGrade.FinalGradePoints = gradePoints;
            finalGrade.FinalGradeLetter = gradeLetter;
            finalGrade.TotalMarksObtained = grades.Sum(g => g.MarksObtained);
            finalGrade.TotalMaximumMarks = grades.Sum(g => g.GradingComponent?.MaximumMarks ?? 0);
            finalGrade.GradeStatus = totalPercentage >= 50 ? GradeStatus.Completed : GradeStatus.Failed;
            finalGrade.CalculationDate = DateTime.Now;

            // Store grade breakdown
            var breakdown = grades.Select(g => new
            {
                Component = g.GradingComponent?.Name,
                Marks = g.MarksObtained,
                MaxMarks = g.GradingComponent?.MaximumMarks,
                Percentage = g.Percentage,
                Weight = g.GradingComponent?.WeightPercentage
            });
            finalGrade.GradeBreakdown = JsonSerializer.Serialize(breakdown);

            await _context.SaveChangesAsync();

            // Update course enrollment
            var enrollment = await _context.CourseEnrollments
                .FirstOrDefaultAsync(ce => ce.StudentId == studentId &&
                                         ce.CourseId == courseId &&
                                         ce.SemesterId == semesterId);

            if (enrollment != null)
            {
                enrollment.Grade = totalPercentage;
                enrollment.GradeLetter = gradeLetter;
                enrollment.GradePoints = gradePoints;
                enrollment.CalculateGrade();
                await _context.SaveChangesAsync();
            }

            return finalGrade;
        }

        private (decimal points, string letter) CalculateGradePointsAndLetter(decimal percentage)
        {
            return percentage switch
            {
                >= 96 => (4.0m, "A+"),
                >= 92 => (3.7m, "A"),
                >= 88 => (3.4m, "A-"),
                >= 84 => (3.2m, "B+"),
                >= 80 => (3.0m, "B"),
                >= 76 => (2.8m, "B-"),
                >= 72 => (2.6m, "C+"),
                >= 68 => (2.4m, "C"),
                >= 64 => (2.2m, "C-"),
                >= 60 => (2.0m, "D+"),
                >= 55 => (1.5m, "D"),
                >= 50 => (1.0m, "D-"),
                _ => (0.0m, "F")

            };
        }

        public async Task<bool> UpdateStudentGrade(StudentGrade grade)
        {
            var existingGrade = await _context.StudentGrades
                .FirstOrDefaultAsync(g => g.StudentId == grade.StudentId &&
                                         g.GradingComponentId == grade.GradingComponentId &&
                                         g.CourseId == grade.CourseId &&
                                         g.SemesterId == grade.SemesterId);

            if (existingGrade == null)
            {
                _context.StudentGrades.Add(grade);
            }
            else
            {
                existingGrade.MarksObtained = grade.MarksObtained;
                existingGrade.IsAbsent = grade.IsAbsent;
                existingGrade.IsExempted = grade.IsExempted;
                existingGrade.Remarks = grade.Remarks;
                existingGrade.GradedDate = DateTime.Now;
            }

            // Calculate percentage
            var component = await _context.GradingComponents.FindAsync(grade.GradingComponentId);
            if (component != null && component.MaximumMarks > 0)
            {
                grade.Percentage = (grade.MarksObtained / component.MaximumMarks) * 100;
                var (points, letter) = CalculateGradePointsAndLetter(grade.Percentage);
                grade.GradePoints = points;
                grade.GradeLetter = letter;
            }

            await _context.SaveChangesAsync();

            // Recalculate final grade
            await CalculateFinalGrade(grade.StudentId, grade.CourseId, grade.SemesterId);

            return true;
        }

        public async Task<List<StudentGrade>> GetStudentGrades(int studentId, int courseId, int semesterId)
        {
            return await _context.StudentGrades
                .Include(g => g.GradingComponent)
                .Where(g => g.StudentId == studentId &&
                           g.CourseId == courseId &&
                           g.SemesterId == semesterId)
                .OrderBy(g => g.GradingComponent!.ComponentType)
                .ToListAsync();
        }

        public async Task<GradingSummary> GetCourseGradingSummary(int courseId, int semesterId)
        {
            var enrollments = await _context.CourseEnrollments
                .Include(ce => ce.Student)
                .Where(ce => ce.CourseId == courseId && ce.SemesterId == semesterId)
                .ToListAsync();

            var finalGrades = await _context.FinalGrades
                .Where(fg => fg.CourseId == courseId && fg.SemesterId == semesterId)
                .ToListAsync();

            var summary = new GradingSummary
            {
                TotalStudents = enrollments.Count,
                StudentsGraded = finalGrades.Count(fg => fg.GradeStatus == GradeStatus.Completed || fg.GradeStatus == GradeStatus.Failed),
                AverageGrade = finalGrades.Any() ? finalGrades.Average(fg => fg.FinalPercentage) : 0,
                HighestGrade = finalGrades.Any() ? finalGrades.Max(fg => fg.FinalPercentage) : 0,
                LowestGrade = finalGrades.Any() ? finalGrades.Min(fg => fg.FinalPercentage) : 0,
                PassRate = finalGrades.Any() ?
                    (decimal)finalGrades.Count(fg => fg.FinalPercentage >= 50) / finalGrades.Count * 100 : 0
            };

            // Calculate grade distribution for plus/minus grading scale
            summary.ACount = finalGrades.Count(fg => fg.FinalGradeLetter.StartsWith("A"));
            summary.BCount = finalGrades.Count(fg => fg.FinalGradeLetter.StartsWith("B"));
            summary.CCount = finalGrades.Count(fg => fg.FinalGradeLetter.StartsWith("C"));
            summary.DCount = finalGrades.Count(fg => fg.FinalGradeLetter.StartsWith("D"));
            summary.FCount = finalGrades.Count(fg => fg.FinalGradeLetter == "F");

            return summary;
        }

        public async Task<GradeReport> GenerateGradeReport(int courseId, int semesterId)
        {
            var course = await _context.Courses
                .Include(c => c.CourseDepartment)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            var semester = await _context.Semesters.FindAsync(semesterId);
            var finalGrades = await _context.FinalGrades
                .Include(fg => fg.Student)
                .Where(fg => fg.CourseId == courseId && fg.SemesterId == semesterId)
                .ToListAsync();

            return new GradeReport
            {
                Course = course,
                Semester = semester,
                FinalGrades = finalGrades,
                GeneratedDate = DateTime.Now,
                Summary = await GetCourseGradingSummary(courseId, semesterId)
            };
        }

        public async Task<BulkGradeResult> ImportGradesFromExcel(Stream fileStream, int courseId, int semesterId)
        {
            // Implementation for Excel import - you can implement this later
            var result = new BulkGradeResult
            {
                Success = true,
                Message = "Excel import functionality to be implemented",
                TotalRecords = 0,
                ProcessedRecords = 0,
                FailedRecords = 0
            };

            // Add actual EPPlus implementation here when ready
            await Task.CompletedTask; // Fix the async warning
            return result;
        }

        public async Task<Stream> ExportGradesToExcel(int courseId, int semesterId)
        {
            // Implementation for Excel export - you can implement this later
            var memoryStream = new MemoryStream();

            // Add actual EPPlus implementation here when ready
            await Task.CompletedTask; // Fix the async warning
            return memoryStream;
        }

        public async Task<bool> ValidateGradingComponents(int courseId)
        {
            var components = await _context.GradingComponents
                .Where(gc => gc.CourseId == courseId && gc.IsActive)
                .ToListAsync();

            var totalWeight = components.Sum(c => c.WeightPercentage);
            return Math.Abs(totalWeight - 100) < 0.01m; // Allow small rounding errors
        }
    }


public class GradingSummary
    {
        public int TotalStudents { get; set; }
        public int StudentsGraded { get; set; }
        public decimal AverageGrade { get; set; }
        public decimal HighestGrade { get; set; }
        public decimal LowestGrade { get; set; }
        public decimal PassRate { get; set; }
        public int ACount { get; set; }
        public int BCount { get; set; }
        public int CCount { get; set; }
        public int DCount { get; set; }
        public int FCount { get; set; }
    }

    public class GradeReport
    {
        public Course? Course { get; set; }
        public Semester? Semester { get; set; }
        public List<FinalGrade> FinalGrades { get; set; } = new List<FinalGrade>();
        public DateTime GeneratedDate { get; set; }
        public GradingSummary Summary { get; set; } = new GradingSummary();
    }

    public class BulkGradeResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int TotalRecords { get; set; }
        public int ProcessedRecords { get; set; }
        public int FailedRecords { get; set; }
        public List<BulkGradeError> Errors { get; set; } = new List<BulkGradeError>();
    }

    public class BulkGradeError
    {
        public int RowNumber { get; set; }
        public string StudentId { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }
}