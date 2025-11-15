//// Services/CourseRegistrationService.cs
//using Microsoft.EntityFrameworkCore;
//using StudentManagementSystem.Data;
//using StudentManagementSystem.Models;

//namespace StudentManagementSystem.Services
//{
//    public class CourseRegistrationService : ICourseRegistrationService
//    {
//        private readonly ApplicationDbContext _context;
//        private readonly ILogger<CourseRegistrationService> _logger;

//        public CourseRegistrationService(ApplicationDbContext context, ILogger<CourseRegistrationService> logger)
//        {
//            _context = context;
//            _logger = logger;
//        }

//        public async Task<RegistrationResult> RegisterForCourse(int studentId, int courseOfferingId)
//        {
//            var result = new RegistrationResult();

//            try
//            {
//                // Get student and course offering
//                var student = await _context.Students
//                    .Include(s => s.StudentAcademicProfile)
//                    .FirstOrDefaultAsync(s => s.Id == studentId);

//                var courseOffering = await _context.CourseOfferings
//                    .Include(co => co.Course)
//                    .Include(co => co.Semester)
//                    .FirstOrDefaultAsync(co => co.Id == courseOfferingId);

//                if (student == null || courseOffering == null)
//                {
//                    result.Success = false;
//                    result.Message = "Student or course offering not found.";
//                    return result;
//                }

//                // Check if already registered
//                var existingRegistration = await _context.StudentCourses
//                    .FirstOrDefaultAsync(sc => sc.StudentId == studentId &&
//                                             sc.CourseOfferingId == courseOfferingId &&
//                                             sc.ApprovalStatus != ApprovalStatus.Dropped);

//                if (existingRegistration != null)
//                {
//                    result.Success = false;
//                    result.Message = "You are already registered for this course.";
//                    return result;
//                }

//                // Validate registration
//                var validationResults = await ValidateRegistration(studentId, courseOfferingId);
//                if (!validationResults.Success)
//                {
//                    return validationResults;
//                }

//                // Check capacity
//                if (!courseOffering.HasAvailableSeats)
//                {
//                    // Add to waitlist if enabled
//                    if (courseOffering.WaitlistEnabled)
//                    {
//                        return await AddToWaitlist(studentId, courseOfferingId);
//                    }
//                    else
//                    {
//                        result.Success = false;
//                        result.Message = "Course is full. No seats available.";
//                        return result;
//                    }
//                }

//                // Create registration
//                var studentCourse = new StudentCourse
//                {
//                    StudentId = studentId,
//                    CourseOfferingId = courseOfferingId,
//                    RegistrationDate = DateTime.Now,
//                    ApprovalStatus = courseOffering.AutoApproveRegistrations ?
//                        ApprovalStatus.Approved : ApprovalStatus.Pending,
//                    ApprovedDate = courseOffering.AutoApproveRegistrations ? DateTime.Now : null,
//                    ApprovedBy = courseOffering.AutoApproveRegistrations ? "System" : null
//                };

//                _context.StudentCourses.Add(studentCourse);

//                // Update enrolled count if approved
//                if (studentCourse.ApprovalStatus == ApprovalStatus.Approved)
//                {
//                    courseOffering.Enrolled++;
//                }

//                await _context.SaveChangesAsync();

//                result.Success = true;
//                result.StudentCourse = studentCourse;
//                result.Message = studentCourse.ApprovalStatus == ApprovalStatus.Approved ?
//                    "Successfully registered for the course." :
//                    "Registration submitted for approval.";

//                return result;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error registering student {StudentId} for course offering {CourseOfferingId}",
//                    studentId, courseOfferingId);

//                result.Success = false;
//                result.Message = "An error occurred during registration.";
//                return result;
//            }
//        }

//        public async Task<RegistrationResult> DropCourse(int studentCourseId)
//        {
//            var result = new RegistrationResult();

//            try
//            {
//                var studentCourse = await _context.StudentCourses
//                    .Include(sc => sc.CourseOffering)
//                    .FirstOrDefaultAsync(sc => sc.Id == studentCourseId);

//                if (studentCourse == null)
//                {
//                    result.Success = false;
//                    result.Message = "Registration not found.";
//                    return result;
//                }

//                // Check if course can be dropped (not completed)
//                if (studentCourse.IsCompleted)
//                {
//                    result.Success = false;
//                    result.Message = "Cannot drop a completed course.";
//                    return result;
//                }

//                // Update registration status
//                studentCourse.ApprovalStatus = ApprovalStatus.Dropped;
//                studentCourse.CourseOffering.Enrolled--;

//                // Process waitlist if available
//                if (studentCourse.CourseOffering.WaitlistEnabled)
//                {
//                    await ProcessWaitlist(studentCourse.CourseOfferingId);
//                }

//                await _context.SaveChangesAsync();

//                result.Success = true;
//                result.Message = "Course dropped successfully.";
//                return result;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error dropping course registration {StudentCourseId}", studentCourseId);
//                result.Success = false;
//                result.Message = "An error occurred while dropping the course.";
//                return result;
//            }
//        }

//        public async Task<List<CourseOffering>> GetAvailableCourses(int studentId, int semesterId)
//        {
//            var student = await _context.Students
//                .Include(s => s.StudentAcademicProfile)
//                .FirstOrDefaultAsync(s => s.Id == studentId);

//            if (student?.StudentAcademicProfile == null)
//                return new List<CourseOffering>();

//            var availableCourses = await _context.CourseOfferings
//                .Include(co => co.Course)
//                    .ThenInclude(c => c.Prerequisites)
//                        .ThenInclude(p => p.PrerequisiteCourse)
//                .Include(co => co.Semester)
//                .Where(co => co.SemesterId == semesterId &&
//                           co.IsRegistrationOpen &&
//                           co.ShowToStudents &&
//                           co.Course.IsActive)
//                .ToListAsync();

//            // Filter courses based on student's academic profile and prerequisites
//            var filteredCourses = availableCourses.Where(co =>
//            {
//                // Check year requirement
//                if (co.Course.RecommendedYear > student.StudentAcademicProfile.CurrentYear)
//                    return false;

//                // Check GPA requirement
//                if (co.Course.MinimumGPARequired.HasValue &&
//                    student.StudentAcademicProfile.CumulativeGPA < co.Course.MinimumGPARequired.Value)
//                    return false;

//                // Check prerequisites
//                if (co.Course.RequirePrerequisites && !CheckPrerequisitesMet(studentId, co.Course).Result)
//                    return false;

//                return true;
//            }).ToList();

//            return filteredCourses;
//        }

//        public async Task<List<CourseOffering>> GetRecommendedCourses(int studentId)
//        {
//            var student = await _context.Students
//                .Include(s => s.StudentAcademicProfile)
//                .FirstOrDefaultAsync(s => s.Id == studentId);

//            if (student?.StudentAcademicProfile == null)
//                return new List<CourseOffering>();

//            var currentSemester = await _context.Semesters
//                .FirstOrDefaultAsync(s => s.IsCurrent);

//            if (currentSemester == null)
//                return new List<CourseOffering>();

//            var availableCourses = await GetAvailableCourses(studentId, currentSemester.Id);

//            // Filter for recommended courses based on student's year and completed courses
//            var completedCourses = await _context.StudentCourses
//                .Include(sc => sc.CourseOffering)
//                    .ThenInclude(co => co.Course)
//                .Where(sc => sc.StudentId == studentId &&
//                           sc.IsCompleted &&
//                           sc.ApprovalStatus == ApprovalStatus.Approved)
//                .Select(sc => sc.CourseOffering.CourseId)
//                .ToListAsync();

//            var recommendedCourses = availableCourses.Where(co =>
//            {
//                // Courses for current year
//                if (co.Course.RecommendedYear == student.StudentAcademicProfile.CurrentYear &&
//                    co.Course.RecommendedSemester == student.StudentAcademicProfile.CurrentSemester)
//                    return true;

//                // Core courses not yet taken
//                if (co.Course.AutoAssignToStudents &&
//                    !completedCourses.Contains(co.CourseId))
//                    return true;

//                return false;
//            }).ToList();

//            return recommendedCourses;
//        }

//        public async Task<bool> CheckPrerequisites(int studentId, int courseId)
//        {
//            return await CheckPrerequisitesMet(studentId, courseId);
//        }

//        public async Task<bool> CheckGPARequirement(int studentId, int courseId)
//        {
//            var student = await _context.Students
//                .Include(s => s.StudentAcademicProfile)
//                .FirstOrDefaultAsync(s => s.Id == studentId);

//            var course = await _context.Courses.FindAsync(courseId);

//            if (student?.StudentAcademicProfile == null || course == null)
//                return false;

//            return !course.MinimumGPARequired.HasValue ||
//                   student.StudentAcademicProfile.CumulativeGPA >= course.MinimumGPARequired.Value;
//        }

//        public async Task<bool> CheckTimeConflict(int studentId, int courseOfferingId)
//        {
//            var targetOffering = await _context.CourseOfferings
//                .FirstOrDefaultAsync(co => co.Id == courseOfferingId);

//            if (targetOffering?.Schedule == null)
//                return false;

//            var studentCourses = await _context.StudentCourses
//                .Include(sc => sc.CourseOffering)
//                .Where(sc => sc.StudentId == studentId &&
//                           sc.ApprovalStatus == ApprovalStatus.Approved &&
//                           !sc.IsCompleted)
//                .Select(sc => sc.CourseOffering)
//                .ToListAsync();

//            // Simple schedule conflict check (you can enhance this with proper time parsing)
//            foreach (var course in studentCourses)
//            {
//                if (course.Schedule == targetOffering.Schedule)
//                    return true;
//            }

//            return false;
//        }

//        public async Task<decimal> CalculateGPA(int studentId)
//        {
//            var completedCourses = await _context.StudentCourses
//                .Include(sc => sc.CourseOffering)
//                    .ThenInclude(co => co.Course)
//                .Where(sc => sc.StudentId == studentId &&
//                           sc.IsCompleted &&
//                           sc.GradePoints.HasValue)
//                .ToListAsync();

//            if (!completedCourses.Any())
//                return 0.0m;

//            var totalPoints = completedCourses.Sum(sc => sc.GradePointsEarned ?? 0);
//            var totalCredits = completedCourses.Sum(sc => sc.CourseOffering.Course.Credits);

//            return totalCredits > 0 ? totalPoints / totalCredits : 0.0m;
//        }

//        public async Task UpdateAcademicProfile(int studentId)
//        {
//            var profile = await _context.StudentAcademicProfiles
//                .FirstOrDefaultAsync(sap => sap.StudentId == studentId);

//            if (profile == null)
//                return;

//            // Update GPA
//            profile.CumulativeGPA = await CalculateGPA(studentId);

//            // Update credits
//            var completedCourses = await _context.StudentCourses
//                .Include(sc => sc.CourseOffering)
//                    .ThenInclude(co => co.Course)
//                .Where(sc => sc.StudentId == studentId && sc.IsCompleted)
//                .ToListAsync();

//            profile.TotalCreditsCompleted = completedCourses.Sum(sc => sc.CourseOffering.Course.Credits);
//            profile.TotalCreditsAttempted = profile.TotalCreditsCompleted; // Simplified

//            // Update academic status based on GPA
//            profile.AcademicStatus = profile.CumulativeGPA >= 2.0m ?
//                AcademicStatus.GoodStanding : AcademicStatus.AcademicProbation;

//            await _context.SaveChangesAsync();
//        }

//        private async Task<RegistrationResult> ValidateRegistration(int studentId, int courseOfferingId)
//        {
//            var result = new RegistrationResult();
//            var errors = new List<string>();

//            var courseOffering = await _context.CourseOfferings
//                .Include(co => co.Course)
//                .FirstOrDefaultAsync(co => co.Id == courseOfferingId);

//            if (courseOffering == null)
//            {
//                errors.Add("Course offering not found.");
//                return CreateErrorResult("Invalid course offering.", errors);
//            }

//            // Check prerequisites
//            if (!await CheckPrerequisitesMet(studentId, courseOffering.Course))
//            {
//                errors.Add("Prerequisites not met.");
//            }

//            // Check GPA requirement
//            if (!await CheckGPARequirement(studentId, courseOffering.CourseId))
//            {
//                errors.Add("GPA requirement not met.");
//            }

//            // Check time conflict
//            if (await CheckTimeConflict(studentId, courseOfferingId))
//            {
//                errors.Add("Schedule conflict with existing courses.");
//            }

//            // Check maximum credits
//            if (await WouldExceedMaxCredits(studentId, courseOffering.Course.Credits))
//            {
//                errors.Add("Would exceed maximum allowed credits for semester.");
//            }

//            if (errors.Any())
//            {
//                return CreateErrorResult("Registration validation failed.", errors);
//            }

//            result.Success = true;
//            return result;
//        }

//        private async Task<bool> CheckPrerequisitesMet(int studentId, Course course)
//        {
//            if (!course.Prerequisites.Any(p => p.IsActive))
//                return true;

//            var completedCourses = await _context.StudentCourses
//                .Include(sc => sc.CourseOffering)
//                .Where(sc => sc.StudentId == studentId &&
//                           sc.IsCompleted &&
//                           sc.Grade != null &&
//                           sc.ApprovalStatus == ApprovalStatus.Approved)
//                .Select(sc => new { sc.CourseOffering.CourseId, sc.Grade })
//                .ToListAsync();

//            foreach (var prereq in course.Prerequisites.Where(p => p.IsActive))
//            {
//                var completedCourse = completedCourses.FirstOrDefault(cc => cc.CourseId == prereq.PrerequisiteCourseId);
//                if (completedCourse == null || !IsGradeSufficient(completedCourse.Grade, prereq.MinimumGrade))
//                    return false;
//            }

//            return true;
//        }

//        private async Task<bool> CheckPrerequisitesMet(int studentId, int courseId)
//        {
//            var course = await _context.Courses
//                .Include(c => c.Prerequisites)
//                    .ThenInclude(p => p.PrerequisiteCourse)
//                .FirstOrDefaultAsync(c => c.Id == courseId);

//            return course != null && await CheckPrerequisitesMet(studentId, course);
//        }

//        private bool IsGradeSufficient(string? actualGrade, string requiredGrade)
//        {
//            if (string.IsNullOrEmpty(actualGrade)) return false;

//            var gradeOrder = new[] { "A", "B", "C", "D", "F" };
//            var actualIndex = Array.IndexOf(gradeOrder, actualGrade.ToUpper());
//            var requiredIndex = Array.IndexOf(gradeOrder, requiredGrade.ToUpper());

//            return actualIndex >= 0 && requiredIndex >= 0 && actualIndex <= requiredIndex;
//        }

//        private async Task<bool> WouldExceedMaxCredits(int studentId, int additionalCredits)
//        {
//            var currentSemester = await _context.Semesters
//                .FirstOrDefaultAsync(s => s.IsCurrent);

//            if (currentSemester == null) return false;

//            var currentCredits = await _context.StudentCourses
//                .Include(sc => sc.CourseOffering)
//                    .ThenInclude(co => co.Course)
//                .Where(sc => sc.StudentId == studentId &&
//                           sc.CourseOffering.SemesterId == currentSemester.Id &&
//                           sc.ApprovalStatus == ApprovalStatus.Approved &&
//                           !sc.IsCompleted)
//                .SumAsync(sc => sc.CourseOffering.Course.Credits);

//            // Get university max credits setting (default to 18)
//            var university = await _context.Universities.FirstOrDefaultAsync();
//            var maxCredits = university?.MaxCreditsPerSemester ?? 18;

//            return currentCredits + additionalCredits > maxCredits;
//        }

//        private async Task<RegistrationResult> AddToWaitlist(int studentId, int courseOfferingId)
//        {
//            var existingWaitlist = await _context.CourseWaitlists
//                .FirstOrDefaultAsync(w => w.StudentId == studentId &&
//                                        w.CourseOfferingId == courseOfferingId &&
//                                        w.IsActive);

//            if (existingWaitlist != null)
//            {
//                return CreateErrorResult("You are already on the waitlist for this course.");
//            }

//            var position = await _context.CourseWaitlists
//                .CountAsync(w => w.CourseOfferingId == courseOfferingId && w.IsActive) + 1;

//            var waitlistEntry = new CourseWaitlist
//            {
//                StudentId = studentId,
//                CourseOfferingId = courseOfferingId,
//                Position = position,
//                JoinDate = DateTime.Now,
//                IsActive = true
//            };

//            _context.CourseWaitlists.Add(waitlistEntry);
//            await _context.SaveChangesAsync();

//            var result = new RegistrationResult
//            {
//                Success = true,
//                Message = $"Added to waitlist. Your position is {position}."
//            };

//            return result;
//        }

//        private async Task ProcessWaitlist(int courseOfferingId)
//        {
//            var nextWaitlist = await _context.CourseWaitlists
//                .Include(w => w.Student)
//                .Where(w => w.CourseOfferingId == courseOfferingId &&
//                          w.IsActive)
//                .OrderBy(w => w.Position)
//                .FirstOrDefaultAsync();

//            if (nextWaitlist != null)
//            {
//                // Try to register the student
//                var registrationResult = await RegisterForCourse(nextWaitlist.StudentId, courseOfferingId);

//                if (registrationResult.Success)
//                {
//                    // Remove from waitlist if successfully registered
//                    nextWaitlist.IsActive = false;
//                    nextWaitlist.NotifiedDate = DateTime.Now;

//                    // Update positions for remaining waitlist entries
//                    var remainingWaitlist = await _context.CourseWaitlists
//                        .Where(w => w.CourseOfferingId == courseOfferingId &&
//                                  w.IsActive &&
//                                  w.Position > nextWaitlist.Position)
//                        .ToListAsync();

//                    foreach (var entry in remainingWaitlist)
//                    {
//                        entry.Position--;
//                    }

//                    await _context.SaveChangesAsync();
//                }
//            }
//        }

//        private RegistrationResult CreateErrorResult(string message, List<string>? errors = null)
//        {
//            return new RegistrationResult
//            {
//                Success = false,
//                Message = message,
//                Errors = errors ?? new List<string>()
//            };
//        }
//    }
//}