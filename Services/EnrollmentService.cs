using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;

namespace StudentManagementSystem.Services{

    public class EnrollmentService : IEnrollmentService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EnrollmentService> _logger;

        public EnrollmentService(ApplicationDbContext context, ILogger<EnrollmentService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<EnrollmentResult> EnrollStudentAsync(EnrollmentRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Validate request
                var validationResult = await ValidateEnrollmentRequestAsync(request);
                if (!validationResult.IsValid)
                {
                    return new EnrollmentResult
                    {
                        Success = false,
                        Errors = validationResult.Errors
                    };
                }

                // 2. Check eligibility
                var eligibility = await CheckEligibilityAsync(request.StudentId, request.CourseId, request.SemesterId);
                if (!eligibility.IsEligible)
                {
                    return new EnrollmentResult
                    {
                        Success = false,
                        Errors = eligibility.MissingRequirements
                    };
                }

                CourseEnrollment enrollment;

                // 3. Check capacity
                if (!eligibility.HasAvailableSeats)
                {
                    // Add to waitlist instead
                    var waitlistResult = await AddToWaitlistAsync(new WaitlistRequest
                    {
                        StudentId = request.StudentId,
                        CourseId = request.CourseId,
                        SemesterId = request.SemesterId,
                        RequestedBy = request.RequestedBy
                    });

                    return new EnrollmentResult
                    {
                        Success = false,
                        Message = "Course is full. Added to waitlist.",
                        Warnings = new List<EnrollmentWarning>
                        {
                            new EnrollmentWarning
                            {
                                Type = WarningType.Waitlisted,
                                Message = $"Waitlist position: {waitlistResult.Position}"
                            }
                        }
                    };
                }

                // 4. Create enrollment
                enrollment = new CourseEnrollment
                {
                    StudentId = request.StudentId,
                    CourseId = request.CourseId,
                    SemesterId = request.SemesterId,
                    EnrollmentDate = DateTime.Now,
                    EnrollmentType = request.Type,
                    EnrollmentMethod = EnrollmentMethod.Web,
                    EnrollmentStatus = EnrollmentStatus.Active,
                    IsActive = true,
                    GradeStatus = GradeStatus.InProgress,
                    ApprovedBy = request.RequestedBy,
                    ApprovalDate = DateTime.Now,
                    LastActivityDate = DateTime.Now
                };

                enrollment.AddAuditEntry("Enrollment", request.RequestedBy ?? "System", request.Notes); // Fixed null

                _context.CourseEnrollments.Add(enrollment);
                await _context.SaveChangesAsync();

                // 5. Update course enrollment count
                await UpdateCourseEnrollmentCountAsync(request.CourseId, request.SemesterId);

                await transaction.CommitAsync();

                _logger.LogInformation("Student {StudentId} enrolled in course {CourseId} for semester {SemesterId}",
                    request.StudentId, request.CourseId, request.SemesterId);

                return new EnrollmentResult
                {
                    Success = true,
                    Message = "Successfully enrolled in course",
                    Enrollment = enrollment
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error enrolling student {StudentId} in course {CourseId}",
                    request.StudentId, request.CourseId);

                return new EnrollmentResult
                {
                    Success = false,
                    Errors = new List<string> { "An error occurred during enrollment." }
                };
            }
        }

        private async Task<EnrollmentValidationResult> ValidateEnrollmentRequestAsync(EnrollmentRequest request)
        {
            var result = new EnrollmentValidationResult();

            // Add your validation logic here
            await Task.Delay(1); // Remove this - placeholder

            result.IsValid = !result.Errors.Any();
            return result;
        }

        public async Task<EnrollmentEligibility> CheckEligibilityAsync(int studentId, int courseId, int semesterId)
        {
            var eligibility = new EnrollmentEligibility();

            var student = await _context.Students
                .Include(s => s.CourseEnrollments)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            var course = await _context.Courses
                .Include(c => c.Prerequisites)
                    .ThenInclude(p => p.PrerequisiteCourse)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            var semester = await _context.Semesters.FindAsync(semesterId);

            if (student == null || course == null || semester == null)
            {
                eligibility.MissingRequirements.Add("Student, course, or semester not found.");
                return eligibility;
            }

            // Check grade level
            if (student.GradeLevel != course.GradeLevel)
            {
                eligibility.MissingRequirements.Add($"Grade level mismatch. Required: {course.GradeLevel}");
            }

            // Check GPA requirement
            if (student.GPA < course.MinGPA)
            {
                eligibility.MissingRequirements.Add($"GPA requirement not met. Required: {course.MinGPA}");
                eligibility.RequiredGPA = course.MinGPA;
            }

            // Check passed hours requirement
            if (student.PassedHours < course.MinPassedHours)
            {
                eligibility.MissingRequirements.Add($"Passed hours requirement not met. Required: {course.MinPassedHours}");
                eligibility.RequiredPassedHours = course.MinPassedHours;
            }

            // Check prerequisites
            var missingPrereqs = await GetMissingPrerequisitesAsync(studentId, courseId);
            if (missingPrereqs.Any())
            {
                eligibility.MissingPrerequisites.AddRange(missingPrereqs);
            }

            // Check capacity
            var currentEnrollment = await _context.CourseEnrollments
                .CountAsync(ce => ce.CourseId == courseId &&
                                 ce.SemesterId == semesterId &&
                                 ce.IsActive);

            eligibility.HasAvailableSeats = currentEnrollment < course.MaxStudents;

            // Check time conflicts
            var conflicts = await CheckConflictsAsync(studentId, semesterId, new List<int> { courseId });
            eligibility.Conflicts = conflicts;

            eligibility.IsEligible = !eligibility.MissingRequirements.Any() &&
                                    !eligibility.MissingPrerequisites.Any() &&
                                    !eligibility.Conflicts.Any() &&
                                    eligibility.HasAvailableSeats;

            return eligibility;
        }

        // Add these methods to your existing EnrollmentService class

        private async Task UpdateCourseEnrollmentCountAsync(int courseId, int semesterId)
        {
            var course = await _context.Courses.FindAsync(courseId);
            if (course != null)
            {
                _logger.LogInformation("Enrollment count updated for course {CourseId}", courseId);
            }
        }

        public async Task<EnrollmentResult> DropStudentAsync(int enrollmentId, string? reason = null)
        {
            try
            {
                var enrollment = await _context.CourseEnrollments.FindAsync(enrollmentId);
                if (enrollment == null)
                {
                    return new EnrollmentResult { Success = false, Errors = new List<string> { "Enrollment not found." } };
                }

                enrollment.IsActive = false;
                enrollment.EnrollmentStatus = EnrollmentStatus.Dropped;
                enrollment.DropReason = reason;
                enrollment.DropDate = DateTime.Now;
                enrollment.LastActivityDate = DateTime.Now;
                enrollment.AddAuditEntry("Dropped", "System", reason); // Fixed null

                await _context.SaveChangesAsync();

                return new EnrollmentResult { Success = true, Message = "Student dropped from course successfully." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dropping enrollment {EnrollmentId}", enrollmentId);
                return new EnrollmentResult { Success = false, Errors = new List<string> { "An error occurred while dropping the student." } };
            }
        }

        public async Task<EnrollmentResult> WithdrawStudentAsync(int enrollmentId, string? reason = null)
        {
            try
            {
                var enrollment = await _context.CourseEnrollments.FindAsync(enrollmentId);
                if (enrollment == null)
                {
                    return new EnrollmentResult { Success = false, Errors = new List<string> { "Enrollment not found." } };
                }

                enrollment.IsActive = false;
                enrollment.EnrollmentStatus = EnrollmentStatus.Withdrawn;
                enrollment.DropReason = reason;
                enrollment.DropDate = DateTime.Now;
                enrollment.LastActivityDate = DateTime.Now;
                enrollment.AddAuditEntry("Withdrawn", "System", reason); // Fixed null

                await _context.SaveChangesAsync();

                return new EnrollmentResult { Success = true, Message = "Student withdrawn from course successfully." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error withdrawing enrollment {EnrollmentId}", enrollmentId);
                return new EnrollmentResult { Success = false, Errors = new List<string> { "An error occurred while withdrawing the student." } };
            }
        }

        public async Task<BulkEnrollmentResult> BulkEnrollStudentsAsync(BulkEnrollmentRequest request)
        {
            // Use existing bulk enrollment logic
            return await BulkEnrollInCoursesAsync(request.SemesterId, request.CourseIds, request.StudentIds);
        }

        public async Task<BulkEnrollmentResult> BulkDropStudentsAsync(BulkDropRequest request)
        {
            var result = new BulkEnrollmentResult
            {
                SemesterName = "Bulk Drop Operation",
                ProcessedAt = DateTime.Now
            };

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                int successCount = 0;
                var studentResults = new List<StudentEnrollmentResult>();

                foreach (var enrollmentId in request.EnrollmentIds)
                {
                    var enrollment = await _context.CourseEnrollments
                        .Include(e => e.Student)
                        .Include(e => e.Course)
                        .FirstOrDefaultAsync(e => e.Id == enrollmentId);

                    var studentResult = new StudentEnrollmentResult
                    {
                        StudentId = enrollment?.StudentId ?? 0,
                        StudentName = enrollment?.Student?.Name ?? "Unknown",
                        StudentCode = enrollment?.Student?.StudentId ?? "Unknown"
                    };

                    if (enrollment != null)
                    {
                        try
                        {
                            enrollment.IsActive = false;
                            enrollment.EnrollmentStatus = EnrollmentStatus.Dropped;
                            enrollment.DropReason = request.Reason;
                            enrollment.DropDate = DateTime.Now;
                            enrollment.LastActivityDate = DateTime.Now;
                            enrollment.AddAuditEntry("Bulk Dropped", request.RequestedBy ?? "System", request.Reason);

                            studentResult.CourseResults.Add(new CourseEnrollmentResult
                            {
                                CourseId = enrollment.CourseId,
                                CourseCode = enrollment.Course?.CourseCode ?? "Unknown",
                                CourseName = enrollment.Course?.CourseName ?? "Unknown",
                                Success = true,
                                Message = "Successfully dropped"
                            });
                            successCount++;
                        }
                        catch (Exception ex)
                        {
                            studentResult.CourseResults.Add(new CourseEnrollmentResult
                            {
                                CourseId = enrollment.CourseId,
                                CourseCode = enrollment.Course?.CourseCode ?? "Unknown",
                                CourseName = enrollment.Course?.CourseName ?? "Unknown",
                                Success = false,
                                Message = $"Error: {ex.Message}"
                            });
                        }
                    }
                    else
                    {
                        studentResult.CourseResults.Add(new CourseEnrollmentResult
                        {
                            Success = false,
                            Message = "Enrollment not found"
                        });
                    }

                    studentResult.Status = studentResult.CourseResults.All(r => r.Success) ? "Success" : "Failed";
                    studentResults.Add(studentResult);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                result.TotalStudents = request.EnrollmentIds.Count;
                result.SuccessfullyEnrolled = successCount;
                result.FailedEnrollments = request.EnrollmentIds.Count - successCount;
                result.Results = studentResults;

                return result;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error in bulk drop operation");

                result.SuccessfullyEnrolled = 0;
                result.FailedEnrollments = request.EnrollmentIds.Count;
                return result;
            }
        }

        public async Task<List<EnrollmentConflict>> CheckConflictsAsync(int studentId, int semesterId, List<int> courseIds)
        {
            var conflicts = new List<EnrollmentConflict>();

            if (!courseIds.Any())
                return conflicts;

            try
            {
                // Get student's current enrollments for the semester
                var currentEnrollments = await _context.CourseEnrollments
                    .Include(ce => ce.Course)
                    .Where(ce => ce.StudentId == studentId &&
                                ce.SemesterId == semesterId &&
                                ce.IsActive &&
                                ce.EnrollmentStatus == EnrollmentStatus.Active)
                    .ToListAsync();

                // Get the courses the student wants to enroll in
                var requestedCourses = await _context.Courses
                    .Where(c => courseIds.Contains(c.Id))
                    .ToListAsync();

                // Check for duplicate enrollment
                foreach (var courseId in courseIds)
                {
                    var alreadyEnrolled = currentEnrollments.Any(ce => ce.CourseId == courseId);
                    if (alreadyEnrolled)
                    {
                        var course = requestedCourses.FirstOrDefault(c => c.Id == courseId);
                        conflicts.Add(new EnrollmentConflict
                        {
                            Type = "Duplicate Enrollment",
                            Description = "Already enrolled in this course",
                            Details = $"You are already enrolled in {course?.CourseCode ?? "this course"}"
                        });
                    }
                }

                // Check for time conflicts (if you have course schedules/times)
                // This is a placeholder - implement based on your schedule system
                var scheduleConflicts = await CheckScheduleConflictsAsync(studentId, semesterId, courseIds);
                conflicts.AddRange(scheduleConflicts);

                // Check for maximum course load
                var maxCourseLoad = 6; // Adjust based on your system
                if (currentEnrollments.Count + courseIds.Count > maxCourseLoad)
                {
                    conflicts.Add(new EnrollmentConflict
                    {
                        Type = "Maximum Course Load",
                        Description = "Exceeds maximum allowed courses",
                        Details = $"You can only enroll in {maxCourseLoad} courses maximum. Currently enrolled in {currentEnrollments.Count} courses."
                    });
                }

                return conflicts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking conflicts for student {StudentId}", studentId);
                return conflicts;
            }
        }



        private async Task<List<EnrollmentConflict>> CheckScheduleConflictsAsync(int studentId, int semesterId, List<int> courseIds)
        {
            var conflicts = new List<EnrollmentConflict>();

            // Placeholder for schedule conflict checking
            // Implement based on your course scheduling system
            // This would check if course times overlap

            await Task.CompletedTask; // Remove this when implementing actual logic
            return conflicts;
        }

        public async Task<List<Course>> GetRecommendedCoursesAsync(int studentId, int semesterId)
        {
            // Implement course recommendation logic
            return await GetAvailableCoursesAsync(studentId, semesterId);
        }

        public async Task<WaitlistResult> AddToWaitlistAsync(WaitlistRequest request)
        {
            try
            {
                // Get current waitlist position
                var currentPosition = await _context.WaitlistEntries
                    .Where(w => w.CourseId == request.CourseId && w.SemesterId == request.SemesterId && w.IsActive)
                    .CountAsync();

                var waitlistEntry = new WaitlistEntry
                {
                    StudentId = request.StudentId,
                    CourseId = request.CourseId,
                    SemesterId = request.SemesterId,
                    Position = currentPosition + 1,
                    AddedDate = DateTime.Now,
                    IsActive = true
                };

                _context.WaitlistEntries.Add(waitlistEntry);
                await _context.SaveChangesAsync();

                return new WaitlistResult
                {
                    Success = true,
                    Message = "Added to waitlist",
                    Position = waitlistEntry.Position,
                    WaitlistEntry = waitlistEntry
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding student to waitlist");
                return new WaitlistResult { Success = false, Message = "Error adding to waitlist" };
            }
        }

        public async Task<WaitlistResult> ProcessWaitlistAsync(int courseId, int semesterId)
        {
            // Add actual implementation with awaits
            await Task.Delay(1); // Remove this - placeholder
            return new WaitlistResult { Success = true, Message = "Waitlist processed" };
        }


        public async Task<List<WaitlistEntry>> GetWaitlistAsync(int courseId, int semesterId)
        {
            return await _context.WaitlistEntries
                .Where(w => w.CourseId == courseId && w.SemesterId == semesterId && w.IsActive)
                .OrderBy(w => w.Position)
                .Include(w => w.Student)
                .ToListAsync();
        }

        public async Task<EnrollmentReport> GenerateEnrollmentReportAsync(int semesterId)
        {
            var semester = await _context.Semesters.FindAsync(semesterId);

            var enrollments = await _context.CourseEnrollments
                .Where(e => (semesterId == 0 || e.SemesterId == semesterId) && e.IsActive)
                .ToListAsync();

            var courseStats = await _context.Courses
                .Where(c => c.IsActive && (semesterId == 0 || c.SemesterId == semesterId))
                .Select(c => new CourseEnrollmentStats
                {
                    CourseId = c.Id,
                    CourseCode = c.CourseCode,
                    CourseName = c.CourseName, // NOW THIS WILL WORK
                    CurrentEnrollment = c.CourseEnrollments.Count(ce => ce.IsActive && ce.EnrollmentStatus == EnrollmentStatus.Active),
                    MaxCapacity = c.MaxStudents,
                    WaitlistCount = _context.WaitlistEntries.Count(w => w.CourseId == c.Id && w.IsActive)
                })
                .ToListAsync();

            return new EnrollmentReport
            {
                SemesterId = semesterId,
                SemesterName = semester?.Name ?? "All Semesters",
                TotalEnrollments = enrollments.Count,
                ActiveEnrollments = enrollments.Count(e => e.EnrollmentStatus == EnrollmentStatus.Active),
                WaitlistedEnrollments = enrollments.Count(e => e.EnrollmentStatus == EnrollmentStatus.Waitlisted),
                CourseStats = courseStats
            };
        }

        // ADD THIS METHOD (Waitlist management)
        public async Task<List<WaitlistEntry>> GetAllWaitlistEntriesAsync()
        {
            return await _context.WaitlistEntries
                .Include(w => w.Student)
                .Include(w => w.Course)
                .Include(w => w.Semester)
                .Where(w => w.IsActive)
                .OrderBy(w => w.CourseId)
                .ThenBy(w => w.Position)
                .ToListAsync();
        }

        public async Task<CourseDemandAnalysis> AnalyzeCourseDemandAsync(int courseId, int semesterId)
        {
            var course = await _context.Courses.FindAsync(courseId);
            var enrollments = await _context.CourseEnrollments
                .CountAsync(e => e.CourseId == courseId && e.SemesterId == semesterId && e.IsActive);
            var waitlistCount = await _context.WaitlistEntries
                .CountAsync(w => w.CourseId == courseId && w.SemesterId == semesterId && w.IsActive);

            var analysis = new CourseDemandAnalysis
            {
                CourseId = courseId,
                CourseCode = course?.CourseCode ?? "Unknown",
                CurrentEnrollment = enrollments,
                WaitlistCount = waitlistCount,
                EnrollmentRate = course != null && course.MaxStudents > 0 ?
                    (decimal)enrollments / course.MaxStudents * 100 : 0
            };

            return analysis;
        }

        

        /// <summary>
        /// //////////////
        /// </summary>

        // Get available courses for a student based on grade level
        public async Task<List<Course>> GetAvailableCoursesAsync(int studentId, int semesterId)
        {
            var student = await _context.Students
                .Include(s => s.CourseEnrollments)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null)
                return new List<Course>();

            return await _context.Courses
                .Where(c => c.SemesterId == semesterId &&
                           c.GradeLevel == student.GradeLevel &&
                           c.IsActive &&
                           c.HasAvailableSeats &&
                           !student.CourseEnrollments.Any(ce => ce.CourseId == c.Id && ce.IsActive))
                .Include(c => c.CourseDepartment)
                .Include(c => c.CourseSemester)
                .ToListAsync();
        }

        // Get student's current enrollments
        public async Task<List<CourseEnrollment>> GetStudentEnrollmentsAsync(int studentId, int semesterId)
        {
            return await _context.CourseEnrollments
                .Include(ce => ce.Course)
                .Include(ce => ce.Semester)
                .Where(ce => ce.StudentId == studentId &&
                            ce.SemesterId == semesterId &&
                            ce.IsActive)
                .OrderBy(ce => ce.Course!.CourseName)
                .ToListAsync();
        }

        // Enroll student in a course
        public async Task<bool> EnrollStudentInCourseAsync(int studentId, int courseId, int semesterId)
        {
            try
            {
                // Check if enrollment is possible
                if (!await CanStudentEnrollInCourseAsync(studentId, courseId, semesterId))
                    return false;

                var enrollment = new CourseEnrollment
                {
                    StudentId = studentId,
                    CourseId = courseId,
                    SemesterId = semesterId,
                    EnrollmentDate = DateTime.Now,
                    IsActive = true,
                    GradeStatus = GradeStatus.InProgress
                };

                _context.CourseEnrollments.Add(enrollment);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // Check if student can enroll in course
        public async Task<bool> CanStudentEnrollInCourseAsync(int studentId, int courseId, int semesterId)
        {
            var student = await _context.Students
                .Include(s => s.CourseEnrollments)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (student == null || course == null)
                return false;

            // Check grade level match
            if (student.GradeLevel != course.GradeLevel)
                return false;

            // Check if already enrolled
            if (student.CourseEnrollments.Any(ce => ce.CourseId == courseId && ce.SemesterId == semesterId && ce.IsActive))
                return false;

            // Check course capacity
            var currentEnrollment = await _context.CourseEnrollments
                .CountAsync(ce => ce.CourseId == courseId && ce.SemesterId == semesterId && ce.IsActive);

            if (currentEnrollment >= course.MaxStudents)
                return false;

            // Check student GPA requirement
            if (student.GPA < course.MinGPA)
                return false;

            // Check passed hours requirement
            if (student.PassedHours < course.MinPassedHours)
                return false;
            var missingPrerequisites = await GetMissingPrerequisitesAsync(studentId, courseId);
            if (missingPrerequisites.Any())
                return false;

            return true;
        }

        

        public async Task<BulkEnrollmentResult> BulkEnrollInSemesterAsync(int semesterId, List<int> studentIds)
        {
            var result = new BulkEnrollmentResult();
            var semester = await _context.Semesters.FindAsync(semesterId);
            result.SemesterName = semester?.Name ?? "Unknown Semester";

            // Get all active courses for the semester
            var semesterCourses = await _context.Courses
                .Where(c => c.SemesterId == semesterId && c.IsActive)
                .Include(c => c.Prerequisites)
                .ToListAsync();

            foreach (var studentId in studentIds)
            {
                var student = await _context.Students
                    .Include(s => s.CourseEnrollments)
                    .FirstOrDefaultAsync(s => s.Id == studentId);

                if (student == null) continue;

                var studentResult = new StudentEnrollmentResult
                {
                    StudentId = studentId,
                    StudentName = student.Name,
                    StudentCode = student.StudentId
                };

                int successfullyEnrolled = 0;

                foreach (var course in semesterCourses)
                {
                    var courseResult = new CourseEnrollmentResult
                    {
                        CourseId = course.Id,
                        CourseCode = course.CourseCode,
                        CourseName = course.CourseName
                    };

                    try
                    {
                        // Check if already enrolled
                        if (student.CourseEnrollments.Any(ce => ce.CourseId == course.Id && ce.SemesterId == semesterId && ce.IsActive))
                        {
                            courseResult.Success = false;
                            courseResult.Message = "Already enrolled";
                            studentResult.CourseResults.Add(courseResult);
                            continue;
                        }

                        // Validate prerequisites
                        var missingPrereqs = await GetMissingPrerequisitesAsync(studentId, course.Id);
                        if (missingPrereqs.Any())
                        {
                            courseResult.Success = false;
                            courseResult.Message = $"Missing prerequisites: {string.Join(", ", missingPrereqs)}";
                            studentResult.CourseResults.Add(courseResult);
                            continue;
                        }

                        // Check course capacity
                        var currentEnrollment = await _context.CourseEnrollments
                            .CountAsync(ce => ce.CourseId == course.Id && ce.SemesterId == semesterId && ce.IsActive);

                        if (currentEnrollment >= course.MaxStudents)
                        {
                            courseResult.Success = false;
                            courseResult.Message = "Course is full";
                            studentResult.CourseResults.Add(courseResult);
                            continue;
                        }

                        // Check student requirements
                        if (student.GPA < course.MinGPA)
                        {
                            courseResult.Success = false;
                            courseResult.Message = $"GPA requirement not met (required: {course.MinGPA})";
                            studentResult.CourseResults.Add(courseResult);
                            continue;
                        }

                        if (student.PassedHours < course.MinPassedHours)
                        {
                            courseResult.Success = false;
                            courseResult.Message = $"Passed hours requirement not met (required: {course.MinPassedHours})";
                            studentResult.CourseResults.Add(courseResult);
                            continue;
                        }

                        // Create enrollment
                        var enrollment = new CourseEnrollment
                        {
                            StudentId = studentId,
                            CourseId = course.Id,
                            SemesterId = semesterId,
                            EnrollmentDate = DateTime.Now,
                            IsActive = true,
                            GradeStatus = GradeStatus.InProgress
                        };

                        _context.CourseEnrollments.Add(enrollment);
                        courseResult.Success = true;
                        courseResult.Message = "Successfully enrolled";
                        successfullyEnrolled++;
                    }
                    catch (Exception ex)
                    {
                        courseResult.Success = false;
                        courseResult.Message = $"Error: {ex.Message}";
                    }

                    studentResult.CourseResults.Add(courseResult);
                }

                // Determine student status
                studentResult.Status = successfullyEnrolled == semesterCourses.Count ? "Success" :
                                      successfullyEnrolled > 0 ? "Partial" : "Failed";

                result.Results.Add(studentResult);
            }

            await _context.SaveChangesAsync();

            result.TotalStudents = studentIds.Count;
            result.SuccessfullyEnrolled = result.Results.Count(r => r.Status != "Failed");
            result.FailedEnrollments = result.Results.Count(r => r.Status == "Failed");

            return result;
        }

        public async Task<CourseEnrollmentResult> QuickEnrollInCourseAsync(int studentId, int courseId, int semesterId)
        {
            var course = await _context.Courses.FindAsync(courseId);
            var result = new CourseEnrollmentResult
            {
                CourseId = courseId,
                CourseCode = course?.CourseCode ?? "Unknown",
                CourseName = course?.CourseName ?? "Unknown"
            };

            try
            {
                if (!await CanStudentEnrollInCourseAsync(studentId, courseId, semesterId))
                {
                    result.Success = false;
                    result.Message = "Cannot enroll - requirements not met";
                    return result;
                }

                var enrollment = new CourseEnrollment
                {
                    StudentId = studentId,
                    CourseId = courseId,
                    SemesterId = semesterId,
                    EnrollmentDate = DateTime.Now,
                    IsActive = true,
                    GradeStatus = GradeStatus.InProgress
                };

                _context.CourseEnrollments.Add(enrollment);
                await _context.SaveChangesAsync();

                result.Success = true;
                result.Message = "Successfully enrolled in course";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Enrollment failed: {ex.Message}";
            }

            return result;
        }

        private async Task<List<string>> GetMissingPrerequisitesAsync(int studentId, int courseId)
        {
            var prerequisites = await _context.CoursePrerequisites
                .Where(cp => cp.CourseId == courseId && cp.IsRequired)
                .Include(cp => cp.PrerequisiteCourse)
                .ToListAsync();

            var missing = new List<string>();

            foreach (var prereq in prerequisites)
            {
                var hasPassed = await _context.CourseEnrollments
                    .AnyAsync(ce => ce.StudentId == studentId &&
                                   ce.CourseId == prereq.PrerequisiteCourseId &&
                                   ce.Grade >= (prereq.MinGrade ?? 60) && // Default passing grade
                                   ce.GradeStatus == GradeStatus.Completed);

                if (!hasPassed)
                    missing.Add(prereq.PrerequisiteCourse?.CourseCode ?? "Unknown");
            }

            return missing;
        }

        // Add to Services/EnrollmentService.cs
        public async Task<BulkEnrollmentResult> BulkEnrollInCoursesAsync(int semesterId, List<int> courseIds, List<int> studentIds)
        {
            var result = new BulkEnrollmentResult();
            var semester = await _context.Semesters.FindAsync(semesterId);
            result.SemesterName = semester?.Name ?? "Unknown Semester";

            // Get the specific courses to enroll in
            var courses = await _context.Courses
                .Where(c => courseIds.Contains(c.Id) && c.SemesterId == semesterId && c.IsActive)
                .Include(c => c.Prerequisites)
                .ToListAsync();

            foreach (var studentId in studentIds)
            {
                var student = await _context.Students
                    .Include(s => s.CourseEnrollments)
                    .FirstOrDefaultAsync(s => s.Id == studentId);

                if (student == null) continue;

                var studentResult = new StudentEnrollmentResult
                {
                    StudentId = studentId,
                    StudentName = student.Name,
                    StudentCode = student.StudentId
                };

                int successfullyEnrolled = 0;

                foreach (var course in courses)
                {
                    var courseResult = new CourseEnrollmentResult
                    {
                        CourseId = course.Id,
                        CourseCode = course.CourseCode,
                        CourseName = course.CourseName
                    };

                    try
                    {
                        // Check if already enrolled - SemesterId is int so no nullable check needed
                        if (student.CourseEnrollments.Any(ce => ce.CourseId == course.Id && ce.SemesterId == semesterId && ce.IsActive))
                        {
                            courseResult.Success = false;
                            courseResult.Message = "Already enrolled";
                            studentResult.CourseResults.Add(courseResult);
                            continue;
                        }

                        // Validate prerequisites
                        var missingPrereqs = await GetMissingPrerequisitesAsync(studentId, course.Id);
                        if (missingPrereqs.Any())
                        {
                            courseResult.Success = false;
                            courseResult.Message = $"Missing prerequisites: {string.Join(", ", missingPrereqs)}";
                            studentResult.CourseResults.Add(courseResult);
                            continue;
                        }

                        // Check course capacity
                        var currentEnrollment = await _context.CourseEnrollments
                            .CountAsync(ce => ce.CourseId == course.Id && ce.SemesterId == semesterId && ce.IsActive);

                        if (currentEnrollment >= course.MaxStudents)
                        {
                            courseResult.Success = false;
                            courseResult.Message = "Course is full";
                            studentResult.CourseResults.Add(courseResult);
                            continue;
                        }

                        // Check student requirements
                        if (student.GPA < course.MinGPA)
                        {
                            courseResult.Success = false;
                            courseResult.Message = $"GPA requirement not met (required: {course.MinGPA})";
                            studentResult.CourseResults.Add(courseResult);
                            continue;
                        }

                        if (student.PassedHours < course.MinPassedHours)
                        {
                            courseResult.Success = false;
                            courseResult.Message = $"Passed hours requirement not met (required: {course.MinPassedHours})";
                            studentResult.CourseResults.Add(courseResult);
                            continue;
                        }

                        // Create enrollment
                        var enrollment = new CourseEnrollment
                        {
                            StudentId = studentId,
                            CourseId = course.Id,
                            SemesterId = semesterId, // Direct assignment since it's int
                            EnrollmentDate = DateTime.Now,
                            IsActive = true,
                            GradeStatus = GradeStatus.InProgress
                        };

                        _context.CourseEnrollments.Add(enrollment);
                        courseResult.Success = true;
                        courseResult.Message = "Successfully enrolled";
                        successfullyEnrolled++;
                    }
                    catch (Exception ex)
                    {
                        courseResult.Success = false;
                        courseResult.Message = $"Error: {ex.Message}";
                    }

                    studentResult.CourseResults.Add(courseResult);
                }

                // Determine student status
                studentResult.Status = successfullyEnrolled == courses.Count ? "Success" :
                                      successfullyEnrolled > 0 ? "Partial" : "Failed";

                result.Results.Add(studentResult);
            }

            await _context.SaveChangesAsync();

            result.TotalStudents = studentIds.Count;
            result.SuccessfullyEnrolled = result.Results.Count(r => r.Status != "Failed");
            result.FailedEnrollments = result.Results.Count(r => r.Status == "Failed");

            return result;
        }

        public async Task<BulkEnrollmentResult> ProcessBulkEnrollmentAsync(BulkEnrollmentRequest request)
        {
            // Create result first to ensure it's initialized
            var result = new BulkEnrollmentResult
            {
                SemesterName = (await _context.Semesters.FindAsync(request.SemesterId))?.Name ?? "Unknown Semester",
                ProcessedAt = DateTime.Now,
                Results = new List<StudentEnrollmentResult>(),
                Message = "Processing completed"
            };

            // === NULL CHECKS ===
            if (request == null)
            {
                result.TotalStudents = 0;
                result.SuccessfullyEnrolled = 0;
                result.FailedEnrollments = 0;
                result.Message = "Request is null";
                return result;
            }

            if (request.StudentIds == null || !request.StudentIds.Any())
            {
                result.TotalStudents = 0;
                result.SuccessfullyEnrolled = 0;
                result.FailedEnrollments = 0;
                result.Message = "No students selected";
                return result;
            }

            if (request.CourseIds == null || !request.CourseIds.Any())
            {
                result.TotalStudents = 0;
                result.SuccessfullyEnrolled = 0;
                result.FailedEnrollments = 0;
                result.Message = "No courses selected";
                return result;
            }

            var studentResults = new List<StudentEnrollmentResult>();

            foreach (var studentId in request.StudentIds)
            {
                var student = await _context.Students.FindAsync(studentId);
                if (student == null) continue;

                var studentResult = new StudentEnrollmentResult
                {
                    StudentId = studentId,
                    StudentName = student.Name ?? "Unknown",
                    StudentCode = student.StudentId ?? "Unknown",
                    CourseResults = new List<CourseEnrollmentResult>(),
                    Status = "Pending"
                    // REMOVED: Summary assignment - it's computed automatically
                };

                int successfulEnrollments = 0;

                foreach (var courseId in request.CourseIds)
                {
                    var course = await _context.Courses.FindAsync(courseId);
                    if (course == null) continue;

                    var courseResult = new CourseEnrollmentResult
                    {
                        CourseId = courseId,
                        CourseCode = course.CourseCode ?? "Unknown",
                        CourseName = course.CourseName ?? "Unknown",
                        Success = false,
                        Message = "Not processed"
                    };

                    try
                    {
                        var enrollmentRequest = new EnrollmentRequest
                        {
                            StudentId = studentId,
                            CourseId = courseId,
                            SemesterId = request.SemesterId,
                            Type = request.Type,
                            RequestedBy = request.RequestedBy ?? "Bulk Enrollment System"
                        };

                        var enrollmentResult = await EnrollStudentAsync(enrollmentRequest);

                        if (enrollmentResult.Success)
                        {
                            courseResult.Success = true;
                            courseResult.Message = "Successfully enrolled";
                            successfulEnrollments++;
                        }
                        else
                        {
                            courseResult.Success = false;
                            courseResult.Message = string.Join(", ", enrollmentResult.Errors);
                        }
                    }
                    catch (Exception ex)
                    {
                        courseResult.Success = false;
                        courseResult.Message = $"Error: {ex.Message}";
                    }

                    studentResult.CourseResults.Add(courseResult);
                }

                // Update student result status (NO Summary assignment - it's computed)
                studentResult.Status = successfulEnrollments == request.CourseIds.Count ? "Success" :
                                      successfulEnrollments > 0 ? "Partial" : "Failed";

                studentResults.Add(studentResult);
            }

            result.Results = studentResults;
            result.TotalStudents = request.StudentIds.Count;
            result.SuccessfullyEnrolled = studentResults.Count(r => r.Status != "Failed");
            result.FailedEnrollments = studentResults.Count(r => r.Status == "Failed");

            // Set final message
            result.Message = $"Bulk enrollment completed: {result.SuccessfullyEnrolled} students successfully enrolled, {result.FailedEnrollments} failed";

            return result;
        }

        /*
        public async Task<BulkEnrollmentResult> ProcessBulkEnrollmentAsync(BulkEnrollmentRequest request)
        {
            var result = new BulkEnrollmentResult
            {
                SemesterName = (await _context.Semesters.FindAsync(request.SemesterId))?.Name ?? "Unknown",
                ProcessedAt = DateTime.Now
            };

            var studentResults = new List<StudentEnrollmentResult>();

            foreach (var studentId in request.StudentIds)
            {
                var student = await _context.Students.FindAsync(studentId);
                var studentResult = new StudentEnrollmentResult
                {
                    StudentId = studentId,
                    StudentName = student?.Name ?? "Unknown",
                    StudentCode = student?.StudentId ?? "Unknown"
                };

                int successfulEnrollments = 0;

                foreach (var courseId in request.CourseIds)
                {
                    var course = await _context.Courses.FindAsync(courseId);
                    var courseResult = new CourseEnrollmentResult
                    {
                        CourseId = courseId,
                        CourseCode = course?.CourseCode ?? "Unknown",
                        CourseName = course?.CourseName ?? "Unknown"
                    };

                    try
                    {
                        var enrollmentRequest = new EnrollmentRequest
                        {
                            StudentId = studentId,
                            CourseId = courseId,
                            SemesterId = request.SemesterId,
                            Type = request.Type, // NOW THIS WILL WORK
                            RequestedBy = request.RequestedBy ?? "Bulk Enrollment System"
                        };

                        var enrollmentResult = await EnrollStudentAsync(enrollmentRequest);

                        if (enrollmentResult.Success)
                        {
                            courseResult.Success = true;
                            courseResult.Message = "Successfully enrolled";
                            successfulEnrollments++;
                        }
                        else
                        {
                            courseResult.Success = false;
                            courseResult.Message = string.Join(", ", enrollmentResult.Errors);
                        }
                    }
                    catch (Exception ex)
                    {
                        courseResult.Success = false;
                        courseResult.Message = $"Error: {ex.Message}";
                    }

                    studentResult.CourseResults.Add(courseResult);
                }

                studentResult.Status = successfulEnrollments == request.CourseIds.Count ? "Success" :
                                      successfulEnrollments > 0 ? "Partial" : "Failed";
                studentResults.Add(studentResult);
            }

            result.Results = studentResults;
            result.TotalStudents = request.StudentIds.Count;
            result.SuccessfullyEnrolled = studentResults.Count(r => r.Status != "Failed");
            result.FailedEnrollments = studentResults.Count(r => r.Status == "Failed");

            return result;
        } 
         */
    }

}

    
    




