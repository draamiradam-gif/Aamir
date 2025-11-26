using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;

namespace StudentManagementSystem.Services
{
    public class ComprehensiveGradeService : IComprehensiveGradeService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ComprehensiveGradeService> _logger;

        public ComprehensiveGradeService(ApplicationDbContext context, ILogger<ComprehensiveGradeService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<CourseEvaluation> CreateEvaluationAsync(CourseEvaluation evaluation)
        {
            _context.CourseEvaluations.Add(evaluation);
            await _context.SaveChangesAsync();
            return evaluation;
        }

        public async Task<bool> UpdateEvaluationAsync(CourseEvaluation evaluation)
        {
            try
            {
                _context.CourseEvaluations.Update(evaluation);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating evaluation {EvaluationId}", evaluation.Id);
                return false;
            }
        }

        public async Task<List<CourseEvaluation>> GetCourseEvaluationsAsync(int courseId)
        {
            return await _context.CourseEvaluations
                .Include(ce => ce.EvaluationType)
                .Include(ce => ce.StudentGrades)
                .Where(ce => ce.CourseId == courseId)
                .OrderBy(ce => ce.EvaluationDate)
                .ToListAsync();
        }

        public async Task<bool> AssignGradeAsync(StudentGrade grade)
        {
            try
            {
                var existingGrade = await _context.StudentGrades
                    .FirstOrDefaultAsync(sg => sg.StudentId == grade.StudentId &&
                                             sg.CourseEvaluationId == grade.CourseEvaluationId);

                if (existingGrade != null)
                {
                    // Update existing grade
                    existingGrade.Score = grade.Score;
                    existingGrade.MaxScore = grade.MaxScore;
                    existingGrade.Comments = grade.Comments;
                    existingGrade.IsAbsent = grade.IsAbsent;
                    existingGrade.IsExcused = grade.IsExcused;
                    existingGrade.ExcuseReason = grade.ExcuseReason;
                    existingGrade.GradedDate = DateTime.Now;
                    existingGrade.GradedBy = grade.GradedBy;
                }
                else
                {
                    // Add new grade
                    _context.StudentGrades.Add(grade);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning grade for student {StudentId}, evaluation {EvaluationId}",
                    grade.StudentId, grade.CourseEvaluationId);
                return false;
            }
        }

        public async Task<bool> BulkAssignGradesAsync(List<StudentGrade> grades)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                foreach (var grade in grades)
                {
                    await AssignGradeAsync(grade);
                }

                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error in bulk grade assignment");
                return false;
            }
        }

        // ... Implement other methods (ImportGradesFromExcelAsync, CalculateStudentCourseGradeAsync, etc.)

        public async Task<bool> ImportGradesFromExcelAsync(Stream fileStream, int evaluationId)
        {
            // Implementation for Excel import
            await Task.Delay(100); // Placeholder
            return true;
        }

        public async Task<StudentGrade?> GetStudentGradeAsync(int studentId, int evaluationId)
        {
            return await _context.StudentGrades
                .FirstOrDefaultAsync(sg => sg.StudentId == studentId && sg.CourseEvaluationId == evaluationId);
        }

        public async Task<List<StudentGrade>> GetEvaluationGradesAsync(int evaluationId)
        {
            return await _context.StudentGrades
                .Include(sg => sg.Student)
                .Where(sg => sg.CourseEvaluationId == evaluationId)
                .ToListAsync();
        }

        public async Task<decimal> CalculateStudentCourseGradeAsync(int studentId, int courseId)
        {
            // Implementation for final grade calculation
            await Task.Delay(100); // Placeholder
            return 85.5m;
        }

        public async Task<bool> CalculateAllFinalGradesAsync(int courseId)
        {
            // Implementation for calculating all final grades
            await Task.Delay(100); // Placeholder
            return true;
        }

        public async Task<CourseGradeSummary> GetCourseGradeSummaryAsync(int courseId)
        {
            // Implementation for course grade summary
            await Task.Delay(100); // Placeholder
            return new CourseGradeSummary();
        }

        public async Task<GradingTemplate> CreateGradingTemplateAsync(GradingTemplate template)
        {
            _context.GradingTemplates.Add(template);
            await _context.SaveChangesAsync();
            return template;
        }

        public async Task<bool> ApplyTemplateToCourseAsync(int templateId, int courseId)
        {
            // Implementation for applying template
            await Task.Delay(100); // Placeholder
            return true;
        }

        public async Task<List<GradingTemplate>> GetGradingTemplatesAsync()
        {
            return await _context.GradingTemplates
                .Include(gt => gt.Items)
                .ThenInclude(gti => gti.EvaluationType)
                .Where(gt => gt.IsActive)
                .ToListAsync();
        }

        public async Task<EvaluationStatistics> GetEvaluationStatisticsAsync(int evaluationId)
        {
            // Implementation for evaluation statistics
            await Task.Delay(100); // Placeholder
            return new EvaluationStatistics();
        }

        public async Task<GradeDistribution> GetGradeDistributionAsync(int courseId)
        {
            // Implementation for grade distribution
            await Task.Delay(100); // Placeholder
            return new GradeDistribution();
        }

        public async Task<StudentTranscript> GenerateTranscriptAsync(int studentId)
        {
            var transcript = new StudentTranscript();

            try
            {
                var student = await _context.Students
                    .Include(s => s.CourseEnrollments)
                    .ThenInclude(ce => ce.Course)
                    .Include(s => s.StudentDepartment)
                    .FirstOrDefaultAsync(s => s.Id == studentId);

                if (student == null)
                    return transcript;

                transcript.Student = student;

                // Get all completed enrollments with grades
                var enrollments = await _context.CourseEnrollments
                    .Include(e => e.Course)
                    .Include(e => e.Semester)
                    .Where(e => e.StudentId == studentId &&
                               e.GradeStatus == GradeStatus.Completed &&
                               e.Grade.HasValue)
                    .OrderBy(e => e.SemesterId)
                    .ThenBy(e => e.Course!.CourseCode)
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

        //////////////////
        ///
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
                        var credits = enrollment.Course.Credits;
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

        //public async Task<StudentTranscript> GenerateTranscriptAsync(int studentId)
        //{
        //    var transcript = new StudentTranscript();

        //    try
        //    {
        //        var student = await _context.Students
        //            .Include(s => s.CourseEnrollments)
        //            .ThenInclude(ce => ce.Course)
        //            .Include(s => s.StudentDepartment)
        //            .FirstOrDefaultAsync(s => s.Id == studentId);

        //        if (student == null)
        //            return transcript;

        //        transcript.Student = student;

        //        // Get all completed enrollments with grades
        //        var enrollments = await _context.CourseEnrollments
        //            .Include(e => e.Course)
        //            .Include(e => e.Semester)
        //            .Where(e => e.StudentId == studentId &&
        //                       e.GradeStatus == GradeStatus.Completed &&
        //                       e.Grade.HasValue)
        //            .OrderBy(e => e.SemesterId)
        //            .ThenBy(e => e.Course!.CourseCode)
        //            .ToListAsync();

        //        transcript.Enrollments = enrollments;
        //        transcript.GPA = await CalculateStudentGPAAsync(studentId);
        //        transcript.GeneratedDate = DateTime.Now;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error generating transcript for student {StudentId}", studentId);
        //    }

        //    return transcript;
        //}

        //// ✅ ADD OTHER MISSING METHODS WITH BASIC IMPLEMENTATIONS:

        //public async Task<bool> ImportGradesFromExcelAsync(Stream fileStream, int evaluationId)
        //{
        //    try
        //    {
        //        using var package = new ExcelPackage(fileStream);
        //        var worksheet = package.Workbook.Worksheets[0];

        //        // Basic Excel import implementation
        //        for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
        //        {
        //            var studentIdStr = worksheet.Cells[row, 1].Value?.ToString();
        //            var scoreStr = worksheet.Cells[row, 2].Value?.ToString();

        //            if (int.TryParse(studentIdStr, out int studentId) &&
        //                decimal.TryParse(scoreStr, out decimal score))
        //            {
        //                var grade = new StudentGrade
        //                {
        //                    StudentId = studentId,
        //                    CourseEvaluationId = evaluationId,
        //                    Score = score,
        //                    MaxScore = 100,
        //                    GradedBy = "System Import",
        //                    GradedDate = DateTime.Now
        //                };

        //                await AssignGradeAsync(grade);
        //            }
        //        }

        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error importing grades from Excel for evaluation {EvaluationId}", evaluationId);
        //        return false;
        //    }
        //}

        //public async Task<CourseGradeSummary> GetCourseGradeSummaryAsync(int courseId)
        //{
        //    var summary = new CourseGradeSummary
        //    {
        //        CourseId = courseId,
        //        CourseName = "Course Name", // You'll want to fetch the actual course name
        //        TotalStudents = 0,
        //        GradedStudents = 0,
        //        AverageGrade = 0,
        //        HighestGrade = 0,
        //        LowestGrade = 0,
        //        GradeDistribution = new Dictionary<string, int>(),
        //        StudentGrades = new List<StudentCourseGrade>()
        //    };

        //    try
        //    {
        //        var course = await _context.Courses.FindAsync(courseId);
        //        if (course != null)
        //        {
        //            summary.CourseName = course.CourseName;

        //            // Get enrollments for this course
        //            var enrollments = await _context.CourseEnrollments
        //                .Include(e => e.Student)
        //                .Where(e => e.CourseId == courseId && e.Grade.HasValue)
        //                .ToListAsync();

        //            summary.TotalStudents = await _context.CourseEnrollments
        //                .CountAsync(e => e.CourseId == courseId && e.IsActive);

        //            summary.GradedStudents = enrollments.Count;

        //            if (enrollments.Any())
        //            {
        //                summary.AverageGrade = enrollments.Average(e => e.Grade!.Value);
        //                summary.HighestGrade = enrollments.Max(e => e.Grade!.Value);
        //                summary.LowestGrade = enrollments.Min(e => e.Grade!.Value);

        //                // Simple grade distribution
        //                summary.GradeDistribution = new Dictionary<string, int>
        //                {
        //                    { "A", enrollments.Count(e => e.Grade >= 90) },
        //                    { "B", enrollments.Count(e => e.Grade >= 80 && e.Grade < 90) },
        //                    { "C", enrollments.Count(e => e.Grade >= 70 && e.Grade < 80) },
        //                    { "D", enrollments.Count(e => e.Grade >= 60 && e.Grade < 70) },
        //                    { "F", enrollments.Count(e => e.Grade < 60) }
        //                };
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error getting course grade summary for course {CourseId}", courseId);
        //    }

        //    return summary;
        //}

        //public async Task<bool> CalculateAllFinalGradesAsync(int courseId)
        //{
        //    try
        //    {
        //        // Get all students enrolled in the course
        //        var enrollments = await _context.CourseEnrollments
        //            .Include(e => e.Student)
        //            .Where(e => e.CourseId == courseId && e.IsActive)
        //            .ToListAsync();

        //        foreach (var enrollment in enrollments)
        //        {
        //            // Calculate final grade based on evaluations
        //            var finalGrade = await CalculateStudentCourseGradeAsync(enrollment.StudentId, courseId);

        //            // Update the enrollment with final grade
        //            enrollment.Grade = finalGrade;
        //            enrollment.CalculateGrade(); // This should update GradeLetter and GradePoints
        //            enrollment.GradeStatus = GradeStatus.Completed;
        //        }

        //        await _context.SaveChangesAsync();
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error calculating final grades for course {CourseId}", courseId);
        //        return false;
        //    }
        //}

        //public async Task<decimal> CalculateStudentCourseGradeAsync(int studentId, int courseId)
        //{
        //    try
        //    {
        //        // Get all evaluations for this course
        //        var evaluations = await _context.CourseEvaluations
        //            .Where(e => e.CourseId == courseId && e.IsGraded)
        //            .ToListAsync();

        //        // Get student grades for these evaluations
        //        var studentGrades = await _context.StudentGrades
        //            .Where(sg => sg.StudentId == studentId &&
        //                        evaluations.Select(e => e.Id).Contains(sg.CourseEvaluationId))
        //            .ToListAsync();

        //        decimal weightedTotal = 0;
        //        decimal totalWeight = 0;

        //        foreach (var evaluation in evaluations)
        //        {
        //            var studentGrade = studentGrades.FirstOrDefault(sg => sg.CourseEvaluationId == evaluation.Id);
        //            if (studentGrade != null && evaluation.Weight > 0)
        //            {
        //                var percentage = (studentGrade.Score / evaluation.MaxScore) * 100;
        //                weightedTotal += percentage * (evaluation.Weight / 100);
        //                totalWeight += evaluation.Weight;
        //            }
        //        }

        //        return totalWeight > 0 ? Math.Round(weightedTotal * (100 / totalWeight), 2) : 0;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error calculating course grade for student {StudentId}, course {CourseId}", studentId, courseId);
        //        return 0;
        //    }
        //}

        //public async Task<EvaluationStatistics> GetEvaluationStatisticsAsync(int evaluationId)
        //{
        //    var statistics = new EvaluationStatistics
        //    {
        //        EvaluationId = evaluationId,
        //        EvaluationName = "Evaluation",
        //        AverageScore = 0,
        //        MedianScore = 0,
        //        StandardDeviation = 0,
        //        HighestScore = 0,
        //        LowestScore = 0,
        //        TotalSubmissions = 0,
        //        TotalStudents = 0
        //    };

        //    try
        //    {
        //        var evaluation = await _context.CourseEvaluations
        //            .Include(e => e.Course)
        //            .FirstOrDefaultAsync(e => e.Id == evaluationId);

        //        if (evaluation != null)
        //        {
        //            statistics.EvaluationName = evaluation.Title;
        //            statistics.TotalStudents = evaluation.Course?.CurrentEnrollment ?? 0;

        //            var grades = await _context.StudentGrades
        //                .Where(sg => sg.CourseEvaluationId == evaluationId && sg.Score > 0)
        //                .ToListAsync();

        //            statistics.TotalSubmissions = grades.Count;

        //            if (grades.Any())
        //            {
        //                var scores = grades.Select(g => (double)g.Score).ToList();
        //                statistics.AverageScore = (decimal)scores.Average();
        //                statistics.HighestScore = (decimal)scores.Max();
        //                statistics.LowestScore = (decimal)scores.Min();

        //                // Simple median calculation
        //                var sortedScores = scores.OrderBy(s => s).ToList();
        //                statistics.MedianScore = (decimal)(sortedScores.Count % 2 == 0
        //                    ? (sortedScores[sortedScores.Count / 2 - 1] + sortedScores[sortedScores.Count / 2]) / 2.0
        //                    : sortedScores[sortedScores.Count / 2]);
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error getting evaluation statistics for evaluation {EvaluationId}", evaluationId);
        //    }

        //    return statistics;
        //}

        //public async Task<GradeDistribution> GetGradeDistributionAsync(int courseId)
        //{
        //    var distribution = new GradeDistribution
        //    {
        //        CourseId = courseId,
        //        Distribution = new Dictionary<string, int>(),
        //        PassRate = 0,
        //        FailRate = 0,
        //        TotalStudents = 0
        //    };

        //    try
        //    {
        //        var enrollments = await _context.CourseEnrollments
        //            .Where(e => e.CourseId == courseId && e.Grade.HasValue)
        //            .ToListAsync();

        //        distribution.TotalStudents = enrollments.Count;

        //        if (enrollments.Any())
        //        {
        //            distribution.Distribution = new Dictionary<string, int>
        //            {
        //                { "A", enrollments.Count(e => e.Grade >= 90) },
        //                { "B", enrollments.Count(e => e.Grade >= 80 && e.Grade < 90) },
        //                { "C", enrollments.Count(e => e.Grade >= 70 && e.Grade < 80) },
        //                { "D", enrollments.Count(e => e.Grade >= 60 && e.Grade < 70) },
        //                { "F", enrollments.Count(e => e.Grade < 60) }
        //            };

        //            var passingGrades = enrollments.Count(e => e.Grade >= 60);
        //            distribution.PassRate = distribution.TotalStudents > 0
        //                ? Math.Round((passingGrades * 100.0m) / distribution.TotalStudents, 2)
        //                : 0;
        //            distribution.FailRate = 100 - distribution.PassRate;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error getting grade distribution for course {CourseId}", courseId);
        //    }

        //    return distribution;
        //}

    }
}