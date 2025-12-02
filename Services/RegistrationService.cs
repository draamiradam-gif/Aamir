using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;

namespace StudentManagementSystem.Services
{
    public interface IRegistrationService
    {
        Task<RegistrationResult> RegisterCourses(RegistrationRequest request);
        Task<StudentEligibility> CheckStudentEligibility(int studentId, int semesterId);
        Task<List<CourseEligibility>> GetEligibleCourses(int studentId, int semesterId);
        Task<bool> ValidatePrerequisites(int studentId, int courseId);
        Task<bool> CheckTimeConflicts(int studentId, List<int> courseIds, int semesterId);
        Task<int> CalculateAvailableCredits(int studentId, int semesterId);
        Task<RegistrationResult> DropCourse(int registrationId, string reason, string requestedBy);
        Task<List<CourseRegistration>> GetStudentRegistrations(int studentId, int semesterId);
        Task<List<RegistrationPeriod>> GetActiveRegistrationPeriods(int semesterId);
        Task<bool> ApproveRegistration(int registrationId, string approvedBy);
        Task<bool> RejectRegistration(int registrationId, string reason, string rejectedBy);
    }

    public class RegistrationService : IRegistrationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IStudentService _studentService;

        public RegistrationService(ApplicationDbContext context, IStudentService studentService)
        {
            _context = context;
            _studentService = studentService;
        }

        public async Task<RegistrationResult> RegisterCourses(RegistrationRequest request)
        {
            var result = new RegistrationResult();
            var student = await _context.Students
                .Include(s => s.CourseEnrollments)
                .FirstOrDefaultAsync(s => s.Id == request.StudentId);

            if (student == null)
            {
                result.Success = false;
                result.Errors.Add(new RegistrationError
                {
                    ErrorType = "StudentNotFound",
                    Message = "Student not found."
                });
                return result;
            }

            // Check registration period
            var activePeriod = await GetActiveRegistrationPeriod(request.SemesterId, request.RegistrationType);
            if (activePeriod == null)
            {
                result.Success = false;
                result.Errors.Add(new RegistrationError
                {
                    ErrorType = "RegistrationClosed",
                    Message = "Registration period is not active for the selected type."
                });
                return result;
            }

            result.CurrentGPA = student.GPA;
            result.PassedHours = student.PassedHours;

            // Validate each course
            foreach (var courseId in request.CourseIds)
            {
                var course = await _context.Courses
                    .Include(c => c.Prerequisites)
                    .ThenInclude(p => p.PrerequisiteCourse)
                    .FirstOrDefaultAsync(c => c.Id == courseId);

                if (course == null)
                {
                    result.Errors.Add(new RegistrationError
                    {
                        CourseCode = "Unknown",
                        ErrorType = "CourseNotFound",
                        Message = $"Course with ID {courseId} not found."
                    });
                    continue;
                }

                // Check prerequisites
                var prerequisiteResult = await ValidatePrerequisites(student.Id, courseId);
                if (!prerequisiteResult)
                {
                    result.Errors.Add(new RegistrationError
                    {
                        CourseCode = course.CourseCode,
                        ErrorType = "PrerequisitesNotMet",
                        Message = $"Prerequisites not met for {course.CourseCode}",
                        RuleType = RuleType.Prerequisite
                    });
                    continue;
                }

                // Check available seats
                if (!course.HasAvailableSeats)
                {
                    result.Errors.Add(new RegistrationError
                    {
                        CourseCode = course.CourseCode,
                        ErrorType = "CourseFull",
                        Message = $"{course.CourseCode} is full. No available seats."
                    });
                    continue;
                }

                // Check GPA requirement
                if (course.MinGPA.HasValue && student.GPA < course.MinGPA.Value)
                {
                    result.Errors.Add(new RegistrationError
                    {
                        CourseCode = course.CourseCode,
                        ErrorType = "GPARequirement",
                        Message = $"GPA requirement not met for {course.CourseCode}. Required: {course.MinGPA}, Current: {student.GPA}",
                        RuleType = RuleType.GPARequirement
                    });
                    continue;
                }

                // Check passed hours requirement
                if (course.MinPassedHours.HasValue && student.PassedHours < course.MinPassedHours.Value)
                {
                    result.Errors.Add(new RegistrationError
                    {
                        CourseCode = course.CourseCode,
                        ErrorType = "PassedHoursRequirement",
                        Message = $"Passed hours requirement not met for {course.CourseCode}. Required: {course.MinPassedHours}, Current: {student.PassedHours}",
                        RuleType = RuleType.Prerequisite
                    });
                    continue;
                }

                // Check credit limits
                var currentCredits = await CalculateCurrentCredits(student.Id, request.SemesterId);
                var maxCredits = await GetMaxAllowedCredits(student.Id, request.SemesterId);

                if (currentCredits + course.Credits > maxCredits)
                {
                    result.Warnings.Add(new RegistrationWarning
                    {
                        CourseCode = course.CourseCode,
                        WarningType = "CreditLimit",
                        Message = $"Adding {course.CourseCode} would exceed credit limit. Current: {currentCredits}, Max: {maxCredits}",
                        Recommendation = "Consider dropping other courses or request special permission."
                    });
                }

                // Create registration
                var registration = new CourseRegistration
                {
                    StudentId = student.Id,
                    CourseId = course.Id,
                    SemesterId = request.SemesterId,
                    RegistrationDate = DateTime.Now,
                    Status = RegistrationStatus.Pending,
                    RegistrationType = request.RegistrationType,
                    Remarks = request.Notes
                };

                _context.CourseRegistrations.Add(registration);
                result.Registrations.Add(registration);
                result.TotalCredits += course.Credits;
            }

            if (result.Registrations.Any())
            {
                await _context.SaveChangesAsync();
                result.Success = true;
                result.Message = $"Successfully registered for {result.Registrations.Count} courses with {result.TotalCredits} total credits.";
            }
            else
            {
                result.Success = false;
                result.Message = "No courses were successfully registered.";
            }

            return result;
        }

        public async Task<StudentEligibility> CheckStudentEligibility(int studentId, int semesterId)
        {
            var student = await _context.Students
                .Include(s => s.CourseEnrollments)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null)
                return new StudentEligibility { IsEligible = false };

            var eligibility = new StudentEligibility
            {
                StudentId = student.Id,
                StudentName = student.Name,
                GPA = student.GPA,
                PassedHours = student.PassedHours,
                CurrentCredits = await CalculateCurrentCredits(studentId, semesterId),
                MaxAllowedCredits = await GetMaxAllowedCredits(studentId, semesterId)
            };

            // Check basic eligibility
            if (student.GPA >= 2.0m) // Minimum GPA requirement
            {
                eligibility.IsEligible = true;
                eligibility.EligibilityReasons.Add("Meets minimum GPA requirement");
            }
            else
            {
                eligibility.EligibilityReasons.Add("Does not meet minimum GPA requirement (2.0)");
            }

            if (student.IsActive)
            {
                eligibility.EligibilityReasons.Add("Student account is active");
            }
            else
            {
                eligibility.IsEligible = false;
                eligibility.EligibilityReasons.Add("Student account is not active");
            }

            // Get eligible courses
            eligibility.EligibleCourses = await GetEligibleCourses(studentId, semesterId);

            return eligibility;
        }

        public async Task<List<CourseEligibility>> GetEligibleCourses(int studentId, int semesterId)
        {
            var eligibleCourses = new List<CourseEligibility>();
            var courses = await _context.Courses
                .Include(c => c.Prerequisites)
                .ThenInclude(p => p.PrerequisiteCourse)
                .Where(c => c.IsActive && c.SemesterId == semesterId)
                .ToListAsync();

            foreach (var course in courses)
            {
                var eligibility = new CourseEligibility
                {
                    CourseId = course.Id,
                    CourseCode = course.CourseCode,
                    CourseName = course.CourseName,
                    Credits = course.Credits,
                    HasSeatsAvailable = course.HasAvailableSeats
                };

                // Check prerequisites
                var prerequisitesMet = await ValidatePrerequisites(studentId, course.Id);
                if (!prerequisitesMet)
                {
                    var missingPrereqs = await GetMissingPrerequisites(studentId, course.Id);
                    eligibility.MissingPrerequisites.AddRange(missingPrereqs);
                }
                else
                {
                    eligibility.Requirements.Add("All prerequisites met");
                }

                // Check GPA requirement
                var student = await _context.Students.FindAsync(studentId);
                if (course.MinGPA.HasValue && student != null)
                {
                    if (student.GPA >= course.MinGPA.Value)
                    {
                        eligibility.Requirements.Add($"GPA requirement met (Required: {course.MinGPA}, Current: {student.GPA})");
                    }
                    else
                    {
                        eligibility.Requirements.Add($"GPA requirement not met (Required: {course.MinGPA}, Current: {student.GPA})");
                    }
                }

                // Check passed hours requirement
                if (course.MinPassedHours.HasValue && student != null)
                {
                    if (student.PassedHours >= course.MinPassedHours.Value)
                    {
                        eligibility.Requirements.Add($"Passed hours requirement met (Required: {course.MinPassedHours}, Current: {student.PassedHours})");
                    }
                    else
                    {
                        eligibility.Requirements.Add($"Passed hours requirement not met (Required: {course.MinPassedHours}, Current: {student.PassedHours})");
                    }
                }

                eligibility.IsEligible = prerequisitesMet &&
                                       eligibility.HasSeatsAvailable &&
                                       !eligibility.MissingPrerequisites.Any();

                eligibleCourses.Add(eligibility);
            }

            return eligibleCourses;
        }

        public async Task<bool> ValidatePrerequisites(int studentId, int courseId)
        {
            var prerequisites = await _context.CoursePrerequisites
                .Include(p => p.PrerequisiteCourse)
                .Where(p => p.CourseId == courseId)
                .ToListAsync();

            if (!prerequisites.Any())
                return true;

            var completedCourses = await _context.CourseEnrollments
                .Where(e => e.StudentId == studentId && e.GradeStatus == GradeStatus.Completed && e.Grade >= 50)
                .Select(e => e.CourseId)
                .ToListAsync();

            foreach (var prerequisite in prerequisites)
            {
                if (!completedCourses.Contains(prerequisite.PrerequisiteCourseId))
                    return false;

                // Check minimum grade requirement if specified
                if (prerequisite.MinGrade.HasValue)
                {
                    var grade = await _context.CourseEnrollments
                        .Where(e => e.StudentId == studentId &&
                                   e.CourseId == prerequisite.PrerequisiteCourseId &&
                                   e.GradeStatus == GradeStatus.Completed)
                        .Select(e => e.Grade)
                        .FirstOrDefaultAsync();

                    if (grade < prerequisite.MinGrade.Value)
                        return false;
                }
            }

            return true;
        }

        // FIXED: Add missing interface method
        public async Task<int> CalculateAvailableCredits(int studentId, int semesterId)
        {
            var currentCredits = await CalculateCurrentCredits(studentId, semesterId);
            var maxAllowedCredits = await GetMaxAllowedCredits(studentId, semesterId);
            return maxAllowedCredits - currentCredits;
        }

        // FIXED: Remove async warning
        public Task<bool> CheckTimeConflicts(int studentId, List<int> courseIds, int semesterId)
        {
            // This would require a CourseSchedule entity with day/time information
            // For now, return false (no conflicts)
            return Task.FromResult(false);
        }

        // Helper methods
        private async Task<RegistrationPeriod?> GetActiveRegistrationPeriod(int semesterId, RegistrationType type)
        {
            return await _context.RegistrationPeriods
                .FirstOrDefaultAsync(rp => rp.SemesterId == semesterId &&
                                         rp.RegistrationType == type &&
                                         rp.IsActive &&
                                         rp.IsOpen);
        }

        private async Task<int> CalculateCurrentCredits(int studentId, int semesterId)
        {
            return await _context.CourseRegistrations
                .Include(r => r.Course)
                .Where(r => r.StudentId == studentId &&
                           r.SemesterId == semesterId &&
                           (r.Status == RegistrationStatus.Approved || r.Status == RegistrationStatus.Pending))
                .SumAsync(r => r.Course!.Credits);
        }

        private async Task<int> GetMaxAllowedCredits(int studentId, int semesterId)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student == null) return 18; // Default

            // You can implement more complex logic based on GPA, standing, etc.
            if (student.GPA >= 3.5m) return 21; // Honors students
            if (student.GPA >= 3.0m) return 18; // Good standing
            if (student.GPA >= 2.0m) return 15; // Minimum requirement
            return 12; // Academic probation
        }

        private async Task<List<string>> GetMissingPrerequisites(int studentId, int courseId)
        {
            var missing = new List<string>();
            var prerequisites = await _context.CoursePrerequisites
                .Include(p => p.PrerequisiteCourse)
                .Where(p => p.CourseId == courseId)
                .ToListAsync();

            var completedCourses = await _context.CourseEnrollments
                .Where(e => e.StudentId == studentId && e.GradeStatus == GradeStatus.Completed)
                .Select(e => e.CourseId)
                .ToListAsync();

            foreach (var prerequisite in prerequisites)
            {
                if (!completedCourses.Contains(prerequisite.PrerequisiteCourseId))
                {
                    missing.Add($"{prerequisite.PrerequisiteCourse?.CourseCode} - {prerequisite.PrerequisiteCourse?.CourseName}");
                }
            }

            return missing;
        }

        public async Task<RegistrationResult> DropCourse(int registrationId, string reason, string requestedBy)
        {
            var result = new RegistrationResult();
            var registration = await _context.CourseRegistrations
                .Include(r => r.Course)
                .FirstOrDefaultAsync(r => r.Id == registrationId);

            if (registration == null)
            {
                result.Success = false;
                result.Errors.Add(new RegistrationError { ErrorType = "RegistrationNotFound", Message = "Registration not found." });
                return result;
            }

            registration.Status = RegistrationStatus.Dropped;
            registration.Remarks = $"{registration.Remarks} | Dropped: {reason} (By: {requestedBy})";

            await _context.SaveChangesAsync();

            result.Success = true;
            result.Message = $"Successfully dropped {registration.Course?.CourseCode}";
            return result;
        }

        public async Task<List<CourseRegistration>> GetStudentRegistrations(int studentId, int semesterId)
        {
            return await _context.CourseRegistrations
                .Include(r => r.Course)
                .Include(r => r.Semester)
                .Where(r => r.StudentId == studentId && r.SemesterId == semesterId)
                .OrderBy(r => r.Priority)
                .ToListAsync();
        }

        public async Task<List<RegistrationPeriod>> GetActiveRegistrationPeriods(int semesterId)
        {
            return await _context.RegistrationPeriods
                .Where(rp => rp.SemesterId == semesterId && rp.IsActive && rp.IsOpen)
                .ToListAsync();
        }

        public async Task<bool> ApproveRegistration(int registrationId, string approvedBy)
        {
            var registration = await _context.CourseRegistrations.FindAsync(registrationId);
            if (registration == null) return false;

            registration.Status = RegistrationStatus.Approved;
            registration.ApprovedBy = approvedBy;
            registration.ApprovalDate = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RejectRegistration(int registrationId, string reason, string rejectedBy)
        {
            var registration = await _context.CourseRegistrations.FindAsync(registrationId);
            if (registration == null) return false;

            registration.Status = RegistrationStatus.Rejected;
            registration.Remarks = $"{registration.Remarks} | Rejected: {reason} (By: {rejectedBy})";

            await _context.SaveChangesAsync();
            return true;
        }
    }
}