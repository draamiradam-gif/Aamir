using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;
using System.Text;

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

            // Add actual validation logic here
            if (request.StudentId <= 0)
                result.Errors.Add("Invalid student ID");

            if (request.CourseId <= 0)
                result.Errors.Add("Invalid course ID");

            if (request.SemesterId <= 0)
                result.Errors.Add("Invalid semester ID");

            // Check if student exists
            var studentExists = await _context.Students.AnyAsync(s => s.Id == request.StudentId);
            if (!studentExists)
                result.Errors.Add("Student not found");

            // Check if course exists
            var courseExists = await _context.Courses.AnyAsync(c => c.Id == request.CourseId);
            if (!courseExists)
                result.Errors.Add("Course not found");

            result.IsValid = !result.Errors.Any();
            return result;
        }

        //////123
        //public async Task<EnrollmentEligibility> CheckEligibilityAsync(int studentId, int courseId, int semesterId)
        //{
        //    var eligibility = new EnrollmentEligibility();

        //    var student = await _context.Students
        //        .Include(s => s.CourseEnrollments)
        //        .FirstOrDefaultAsync(s => s.Id == studentId);

        //    var course = await _context.Courses
        //        .Include(c => c.Prerequisites)
        //            .ThenInclude(p => p.PrerequisiteCourse)
        //        .FirstOrDefaultAsync(c => c.Id == courseId);

        //    var semester = await _context.Semesters.FindAsync(semesterId);

        //    if (student == null || course == null || semester == null)
        //    {
        //        eligibility.MissingRequirements.Add("Student, course, or semester not found.");
        //        return eligibility;
        //    }

        //    // Check grade level
        //    if (student.GradeLevel != course.GradeLevel)
        //    {
        //        eligibility.MissingRequirements.Add($"Grade level mismatch. Required: {course.GradeLevel}");
        //    }

        //    // Check GPA requirement
        //    if (student.GPA < course.MinGPA)
        //    {
        //        eligibility.MissingRequirements.Add($"GPA requirement not met. Required: {course.MinGPA}");
        //        eligibility.RequiredGPA = course.MinGPA;
        //    }

        //    // Check passed hours requirement
        //    if (student.PassedHours < course.MinPassedHours)
        //    {
        //        eligibility.MissingRequirements.Add($"Passed hours requirement not met. Required: {course.MinPassedHours}");
        //        eligibility.RequiredPassedHours = course.MinPassedHours;
        //    }

        //    // Check prerequisites
        //    var missingPrereqs = await GetMissingPrerequisitesAsync(studentId, courseId);
        //    if (missingPrereqs.Any())
        //    {
        //        eligibility.MissingPrerequisites.AddRange(missingPrereqs);
        //    }

        //    // Check capacity
        //    var currentEnrollment = await _context.CourseEnrollments
        //        .CountAsync(ce => ce.CourseId == courseId &&
        //                         ce.SemesterId == semesterId &&
        //                         ce.IsActive);

        //    eligibility.HasAvailableSeats = currentEnrollment < course.MaxStudents;

        //    // Check time conflicts
        //    var conflicts = await CheckConflictsAsync(studentId, semesterId, new List<int> { courseId });
        //    eligibility.Conflicts = conflicts;

        //    eligibility.IsEligible = !eligibility.MissingRequirements.Any() &&
        //                            !eligibility.MissingPrerequisites.Any() &&
        //                            !eligibility.Conflicts.Any() &&
        //                            eligibility.HasAvailableSeats;

        //    return eligibility;
        //}

        public async Task<EnrollmentEligibility> CheckEligibilityAsync(int studentId, int courseId, int semesterId)
        {
            var eligibility = new EnrollmentEligibility();
            _logger.LogInformation("=== START CheckEligibilityAsync ===");
            _logger.LogInformation("Checking eligibility for Student={StudentId}, Course={CourseId}, Semester={SemesterId}",
                studentId, courseId, semesterId);

            // Use separate queries to avoid circular dependencies
            var studentExists = await _context.Students.AnyAsync(s => s.Id == studentId);
            var courseExists = await _context.Courses.AnyAsync(c => c.Id == courseId);
            var semesterExists = await _context.Semesters.AnyAsync(s => s.Id == semesterId);

            _logger.LogInformation("Exists checks - Student: {StudentExists}, Course: {CourseExists}, Semester: {SemesterExists}",
                studentExists, courseExists, semesterExists);

            if (!studentExists || !courseExists || !semesterExists)
            {
                eligibility.MissingRequirements.Add("Student, course, or semester not found.");
                _logger.LogWarning("Student, course, or semester not found");
                return eligibility;
            }

            // Get student data without relationships to avoid loops
            var student = await _context.Students
                .Where(s => s.Id == studentId)
                .Select(s => new {
                    s.GradeLevel,
                    s.GPA,
                    s.PassedHours,
                    s.Name
                })
                .FirstOrDefaultAsync();

            var course = await _context.Courses
                .Where(c => c.Id == courseId)
                .Select(c => new {
                    c.GradeLevel,
                    c.MinGPA,
                    c.MinPassedHours,
                    c.MaxStudents,
                    c.CourseName,
                    c.CourseCode
                })
                .FirstOrDefaultAsync();

            _logger.LogInformation("Student data: Name={StudentName}, GradeLevel={GradeLevel}, GPA={GPA}, PassedHours={PassedHours}",
                student?.Name, student?.GradeLevel, student?.GPA, student?.PassedHours);

            _logger.LogInformation("Course data: Name={CourseName}, Code={CourseCode}, GradeLevel={CourseGradeLevel}, MinGPA={MinGPA}, MinPassedHours={MinPassedHours}, MaxStudents={MaxStudents}",
                course?.CourseName, course?.CourseCode, course?.GradeLevel, course?.MinGPA, course?.MinPassedHours, course?.MaxStudents);

            if (student == null || course == null)
            {
                eligibility.MissingRequirements.Add("Student or course data not found.");
                _logger.LogWarning("Student or course data not found");
                return eligibility;
            }

            // Check grade level
            _logger.LogInformation("Checking grade level: Student={StudentGradeLevel}, Course={CourseGradeLevel}",
                student.GradeLevel, course.GradeLevel);

            if (student.GradeLevel != course.GradeLevel)
            {
                eligibility.MissingRequirements.Add($"Grade level mismatch. Required: {course.GradeLevel}");
                _logger.LogWarning("Grade level mismatch");
            }

            // Check GPA requirement
            _logger.LogInformation("Checking GPA: Student={StudentGPA}, Required={CourseMinGPA}",
                student.GPA, course.MinGPA);

            if (student.GPA < course.MinGPA)
            {
                eligibility.MissingRequirements.Add($"GPA requirement not met. Required: {course.MinGPA}");
                eligibility.RequiredGPA = course.MinGPA;
                _logger.LogWarning("GPA requirement not met");
            }

            // Check passed hours requirement
            _logger.LogInformation("Checking passed hours: Student={StudentPassedHours}, Required={CourseMinPassedHours}",
                student.PassedHours, course.MinPassedHours);

            if (student.PassedHours < course.MinPassedHours)
            {
                eligibility.MissingRequirements.Add($"Passed hours requirement not met. Required: {course.MinPassedHours}");
                eligibility.RequiredPassedHours = course.MinPassedHours;
                _logger.LogWarning("Passed hours requirement not met");
            }

            // Check prerequisites using a separate method
            var missingPrereqs = await GetMissingPrerequisitesAsync(studentId, courseId);
            if (missingPrereqs.Any())
            {
                eligibility.MissingPrerequisites.AddRange(missingPrereqs);
                _logger.LogWarning("Missing prerequisites: {Prerequisites}", string.Join(", ", missingPrereqs));
            }

            // Check capacity - use direct count without complex query
            var currentEnrollment = await _context.CourseEnrollments
                .CountAsync(ce => ce.CourseId == courseId &&
                                 ce.SemesterId == semesterId &&
                                 ce.IsActive);

            _logger.LogInformation("Checking capacity: Current={CurrentEnrollment}, Max={MaxStudents}",
                currentEnrollment, course.MaxStudents);

            eligibility.HasAvailableSeats = currentEnrollment < course.MaxStudents;

            // Check for existing enrollment
            var alreadyEnrolled = await _context.CourseEnrollments
                .AnyAsync(ce => ce.StudentId == studentId &&
                               ce.CourseId == courseId &&
                               ce.SemesterId == semesterId &&
                               ce.IsActive);

            _logger.LogInformation("Checking existing enrollment: AlreadyEnrolled={AlreadyEnrolled}", alreadyEnrolled);

            if (alreadyEnrolled)
            {
                eligibility.MissingRequirements.Add("Already enrolled in this course.");
                _logger.LogWarning("Already enrolled in this course");
            }

            eligibility.IsEligible = !eligibility.MissingRequirements.Any() &&
                                    !eligibility.MissingPrerequisites.Any() &&
                                    eligibility.HasAvailableSeats &&
                                    !alreadyEnrolled;

            _logger.LogInformation("=== END CheckEligibilityAsync ===");
            _logger.LogInformation("Final eligibility: IsEligible={IsEligible}", eligibility.IsEligible);

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

            try
            {
                // Get requested courses with their schedules
                var requestedCourses = await _context.Courses
                    .Where(c => courseIds.Contains(c.Id))
                    .Select(c => new CourseScheduleInfo
                    {
                        Id = c.Id,
                        CourseCode = c.CourseCode,
                        CourseName = c.CourseName,
                        ScheduleDays = c.ScheduleDays,
                        StartTime = c.StartTime,
                        EndTime = c.EndTime,
                        RoomNumber = c.RoomNumber,
                        ClassSchedule = c.ClassSchedule
                    })
                    .ToListAsync();

                // Get student's current enrollments with their schedules
                var currentEnrollments = await _context.CourseEnrollments
                    .Include(ce => ce.Course)
                    .Where(ce => ce.StudentId == studentId &&
                                ce.SemesterId == semesterId &&
                                ce.IsActive &&
                                ce.EnrollmentStatus == EnrollmentStatus.Active)
                    .Select(ce => new CourseScheduleInfo
                    {
                        Id = ce.CourseId,
                        CourseCode = ce.Course!.CourseCode,
                        CourseName = ce.Course!.CourseName,
                        ScheduleDays = ce.Course!.ScheduleDays,
                        StartTime = ce.Course!.StartTime,
                        EndTime = ce.Course!.EndTime,
                        RoomNumber = ce.Course!.RoomNumber,
                        ClassSchedule = ce.Course!.ClassSchedule
                    })
                    .ToListAsync();

                // Check for schedule conflicts
                foreach (var requestedCourse in requestedCourses)
                {
                    foreach (var currentCourse in currentEnrollments)
                    {
                        bool hasConflict = false;
                        string conflictDetails = "";

                        // Check using ScheduleDays and Times if available
                        if (!string.IsNullOrEmpty(requestedCourse.ScheduleDays) &&
                            !string.IsNullOrEmpty(currentCourse.ScheduleDays) &&
                            requestedCourse.StartTime.HasValue && requestedCourse.EndTime.HasValue &&
                            currentCourse.StartTime.HasValue && currentCourse.EndTime.HasValue)
                        {
                            // Check if days overlap
                            var requestedDays = requestedCourse.ScheduleDays.ToCharArray();
                            var currentDays = currentCourse.ScheduleDays.ToCharArray();
                            var overlappingDays = requestedDays.Intersect(currentDays);

                            if (overlappingDays.Any())
                            {
                                // Check if times overlap
                                var requestedStart = requestedCourse.StartTime.Value;
                                var requestedEnd = requestedCourse.EndTime.Value;
                                var currentStart = currentCourse.StartTime.Value;
                                var currentEnd = currentCourse.EndTime.Value;

                                hasConflict = (requestedStart < currentEnd && requestedEnd > currentStart);
                                if (hasConflict)
                                {
                                    conflictDetails = $"Time conflict on {string.Join(",", overlappingDays)}: " +
                                                     $"{requestedStart:hh\\:mm}-{requestedEnd:hh\\:mm} conflicts with " +
                                                     $"{currentStart:hh\\:mm}-{currentEnd:hh\\:mm}";
                                }
                            }
                        }
                        // Check using ClassSchedule string
                        else if (!string.IsNullOrEmpty(requestedCourse.ClassSchedule) &&
                                 !string.IsNullOrEmpty(currentCourse.ClassSchedule))
                        {
                            // Simple check: if both have schedules, assume potential conflict
                            // You can implement more sophisticated parsing based on your format
                            hasConflict = DoClassSchedulesConflict(requestedCourse.ClassSchedule, currentCourse.ClassSchedule);
                            if (hasConflict)
                            {
                                conflictDetails = $"Schedule conflict: " +
                                                 $"{requestedCourse.ClassSchedule} conflicts with " +
                                                 $"{currentCourse.ClassSchedule}";
                            }
                        }
                        // If no schedule data, check for duplicate enrollment
                        else if (requestedCourse.Id == currentCourse.Id)
                        {
                            hasConflict = true;
                            conflictDetails = $"Already enrolled in {requestedCourse.CourseCode}";
                        }

                        if (hasConflict && !string.IsNullOrEmpty(conflictDetails))
                        {
                            conflicts.Add(new EnrollmentConflict
                            {
                                Type = "Schedule Conflict",
                                Description = "Course times overlap",
                                Details = conflictDetails
                            });
                        }

                        // Check for same room conflicts
                        if (!string.IsNullOrEmpty(requestedCourse.RoomNumber) &&
                            !string.IsNullOrEmpty(currentCourse.RoomNumber) &&
                            requestedCourse.RoomNumber == currentCourse.RoomNumber &&
                            requestedCourse.Id != currentCourse.Id) // Different courses
                        {
                            conflicts.Add(new EnrollmentConflict
                            {
                                Type = "Room Conflict",
                                Description = "Same classroom assignment",
                                Details = $"Both {requestedCourse.CourseCode} and {currentCourse.CourseCode} are in Room {requestedCourse.RoomNumber}"
                            });
                        }
                    }
                }

                return conflicts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking schedule conflicts for student {StudentId}", studentId);
                return conflicts;
            }
        }

        // Helper class for schedule information
        private class CourseScheduleInfo
        {
            public int Id { get; set; }
            public string CourseCode { get; set; } = string.Empty;
            public string CourseName { get; set; } = string.Empty;
            public string? ScheduleDays { get; set; }
            public TimeSpan? StartTime { get; set; }
            public TimeSpan? EndTime { get; set; }
            public string? RoomNumber { get; set; }
            public string? ClassSchedule { get; set; }
        }

        // Helper method to check if two class schedules conflict
        private bool DoClassSchedulesConflict(string schedule1, string schedule2)
        {
            if (string.IsNullOrEmpty(schedule1) || string.IsNullOrEmpty(schedule2))
                return false;

            // Convert to uppercase for case-insensitive comparison
            schedule1 = schedule1.ToUpper();
            schedule2 = schedule2.ToUpper();

            // Check for overlapping days
            string[] daysOfWeek = { "M", "TU", "W", "TH", "F", "SA", "SU" };

            foreach (var day in daysOfWeek)
            {
                if (schedule1.Contains(day) && schedule2.Contains(day))
                {
                    // If both schedules have the same day, check for time overlap
                    // This is a simplified check - you might want to extract times
                    return true;
                }
            }

            return false;
        }

        private bool DoSchedulesOverlap(string schedule1, string schedule2)
        {
            // Implement your schedule overlap logic here
            return false; // Placeholder
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
                // Check if student is already on waitlist
                var existingEntry = await _context.WaitlistEntries
                    .FirstOrDefaultAsync(w => w.StudentId == request.StudentId &&
                                             w.CourseId == request.CourseId &&
                                             w.SemesterId == request.SemesterId &&
                                             w.IsActive);

                if (existingEntry != null)
                {
                    return new WaitlistResult
                    {
                        Success = false,
                        Message = "Student is already on the waitlist",
                        Position = existingEntry.Position
                    };
                }

                // Get current waitlist position
                var currentPosition = await _context.WaitlistEntries
                    .Where(w => w.CourseId == request.CourseId &&
                               w.SemesterId == request.SemesterId &&
                               w.IsActive)
                    .CountAsync();

                var waitlistEntry = new WaitlistEntry
                {
                    StudentId = request.StudentId,
                    CourseId = request.CourseId,
                    SemesterId = request.SemesterId,
                    Position = currentPosition + 1,
                    AddedDate = DateTime.Now,
                    IsActive = true,
                    RequestedBy = request.RequestedBy, // This will now work
                    ExpirationDate = DateTime.Now.AddDays(30) // 30-day expiration
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
            try
            {
                // Get active waitlist entries for this course
                var waitlistEntries = await _context.WaitlistEntries
                    .Where(w => w.CourseId == courseId &&
                               w.SemesterId == semesterId &&
                               w.IsActive)
                    .OrderBy(w => w.Position)
                    .Include(w => w.Student)
                    .ToListAsync();

                var course = await _context.Courses
                    .FirstOrDefaultAsync(c => c.Id == courseId);

                if (course == null)
                    return new WaitlistResult { Success = false, Message = "Course not found" };

                int enrolledCount = 0;

                foreach (var waitlistEntry in waitlistEntries)
                {
                    // Check if there are available seats
                    var currentEnrollment = await _context.CourseEnrollments
                        .CountAsync(ce => ce.CourseId == courseId &&
                                         ce.SemesterId == semesterId &&
                                         ce.IsActive);

                    if (currentEnrollment >= course.MaxStudents)
                        break; // No more seats available

                    // Enroll the student
                    var enrollment = new CourseEnrollment
                    {
                        StudentId = waitlistEntry.StudentId,
                        CourseId = courseId,
                        SemesterId = semesterId,
                        EnrollmentDate = DateTime.Now,
                        EnrollmentStatus = EnrollmentStatus.Active,
                        IsActive = true,
                        GradeStatus = GradeStatus.InProgress,
                        ApprovedBy = "Waitlist System",
                        ApprovalDate = DateTime.Now
                    };

                    _context.CourseEnrollments.Add(enrollment);

                    // Mark waitlist entry as processed
                    waitlistEntry.IsActive = false;
                    waitlistEntry.ProcessedDate = DateTime.Now; // This will now work

                    enrolledCount++;
                }

                await _context.SaveChangesAsync();

                return new WaitlistResult
                {
                    Success = true,
                    Message = $"Processed waitlist: {enrolledCount} students enrolled",
                    Position = 0 // Not applicable for bulk processing
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing waitlist for course {CourseId}", courseId);
                return new WaitlistResult { Success = false, Message = "Error processing waitlist" };
            }
        }


        public async Task<List<WaitlistEntry>> GetWaitlistAsync(int courseId, int semesterId)
        {
            return await _context.WaitlistEntries
                .Where(w => w.CourseId == courseId && w.SemesterId == semesterId && w.IsActive)
                .OrderBy(w => w.Position)
                .Include(w => w.Student)
                .ToListAsync();
        }
        //////123
        //public async Task<EnrollmentReport> GenerateEnrollmentReportAsync(int semesterId)
        //{
        //    var semester = await _context.Semesters.FindAsync(semesterId);

        //    var enrollments = await _context.CourseEnrollments
        //        .Where(e => (semesterId == 0 || e.SemesterId == semesterId) && e.IsActive)
        //        .ToListAsync();

        //    var courseStats = await _context.Courses
        //        .Where(c => c.IsActive && (semesterId == 0 || c.SemesterId == semesterId))
        //        .Select(c => new CourseEnrollmentStats
        //        {
        //            CourseId = c.Id,
        //            CourseCode = c.CourseCode,
        //            CourseName = c.CourseName, // NOW THIS WILL WORK
        //            CurrentEnrollment = c.CourseEnrollments.Count(ce => ce.IsActive && ce.EnrollmentStatus == EnrollmentStatus.Active),
        //            MaxCapacity = c.MaxStudents,
        //            WaitlistCount = _context.WaitlistEntries.Count(w => w.CourseId == c.Id && w.IsActive)
        //        })
        //        .ToListAsync();

        //    return new EnrollmentReport
        //    {
        //        SemesterId = semesterId,
        //        SemesterName = semester?.Name ?? "All Semesters",
        //        TotalEnrollments = enrollments.Count,
        //        ActiveEnrollments = enrollments.Count(e => e.EnrollmentStatus == EnrollmentStatus.Active),
        //        WaitlistedEnrollments = enrollments.Count(e => e.EnrollmentStatus == EnrollmentStatus.Waitlisted),
        //        CourseStats = courseStats
        //    };
        //}
        public async Task<EnrollmentReport> GenerateEnrollmentReportAsync(int semesterId)
        {
            var semester = await _context.Semesters.FindAsync(semesterId);

            var enrollments = await _context.CourseEnrollments
                .Where(e => (semesterId == 0 || e.SemesterId == semesterId) && e.IsActive)
                .ToListAsync();

            // Fix the LINQ query - avoid using navigation properties in Select
            var courseStats = await _context.Courses
                .Where(c => c.IsActive && (semesterId == 0 || c.SemesterId == semesterId))
                .Select(c => new CourseEnrollmentStats
                {
                    CourseId = c.Id,
                    CourseCode = c.CourseCode,
                    CourseName = c.CourseName,
                    MaxCapacity = c.MaxStudents,
                    // Get enrollment count separately
                    CurrentEnrollment = 0, // We'll update this separately
                    WaitlistCount = 0 // We'll update this separately
                })
                .ToListAsync();

            // Now update the counts
            foreach (var stat in courseStats)
            {
                stat.CurrentEnrollment = await _context.CourseEnrollments
                    .CountAsync(ce => ce.CourseId == stat.CourseId &&
                                     ce.IsActive &&
                                     ce.EnrollmentStatus == EnrollmentStatus.Active);

                stat.WaitlistCount = await _context.WaitlistEntries
                    .CountAsync(w => w.CourseId == stat.CourseId && w.IsActive);
            }

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
        //public async Task<List<Course>> GetAvailableCoursesAsync(int studentId, int semesterId)
        //{
        //    var student = await _context.Students
        //        .Include(s => s.CourseEnrollments)
        //        .FirstOrDefaultAsync(s => s.Id == studentId);

        //    if (student == null)
        //        return new List<Course>();

        //    return await _context.Courses
        //        .Where(c => c.SemesterId == semesterId &&
        //                   c.GradeLevel == student.GradeLevel &&
        //                   c.IsActive &&
        //                   c.HasAvailableSeats &&
        //                   !student.CourseEnrollments.Any(ce => ce.CourseId == c.Id && ce.IsActive))
        //        .Include(c => c.CourseDepartment)
        //        .Include(c => c.CourseSemester)
        //        .Select(c => new Course
        //        {
        //            Id = c.Id,
        //            CourseCode = c.CourseCode,
        //            CourseName = c.CourseName,
        //            Credits = c.Credits,
        //            Department = c.Department,
        //            MaxStudents = c.MaxStudents,
        //            CurrentEnrollment = c.CourseEnrollments.Count(ce => ce.IsActive),
        //            ScheduleDays = c.ScheduleDays,        // Add if needed
        //            StartTime = c.StartTime,              // Add if needed
        //            EndTime = c.EndTime,                  // Add if needed
        //            ClassSchedule = c.ClassSchedule       // Add if needed
        //        })
        //        .ToListAsync();
        //}
        //public async Task<List<Course>> GetAvailableCoursesAsync(int studentId, int semesterId)
        //{
        //    var student = await _context.Students
        //        .AsNoTracking()
        //        .FirstOrDefaultAsync(s => s.Id == studentId);

        //    if (student == null)
        //        return new List<Course>();

        //    return await _context.Courses
        //        .AsNoTracking()
        //        .Where(c =>
        //            c.SemesterId == semesterId &&
        //            c.GradeLevel == student.GradeLevel &&
        //            c.IsActive &&
        //            c.HasAvailableSeats &&
        //            !_context.CourseEnrollments.Any(ce =>
        //                ce.StudentId == studentId &&
        //                ce.CourseId == c.Id &&
        //                ce.IsActive
        //            )
        //        )
        //        .Include(c => c.CourseDepartment)
        //        .Include(c => c.CourseSemester)
        //        .Select(c => new Course
        //        {
        //            Id = c.Id,
        //            CourseCode = c.CourseCode,
        //            CourseName = c.CourseName,
        //            Credits = c.Credits,
        //            Department = c.Department,
        //            MaxStudents = c.MaxStudents,
        //            CurrentEnrollment = c.CourseEnrollments.Count(ce => ce.IsActive),
        //            ScheduleDays = c.ScheduleDays,
        //            StartTime = c.StartTime,
        //            EndTime = c.EndTime,
        //            ClassSchedule = c.ClassSchedule
        //        })
        //        .ToListAsync();
        //}
        public async Task<List<Course>> GetAvailableCoursesAsync(int studentId, int semesterId)
        {
            // Get student first
            var student = await _context.Students
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null)
                return new List<Course>();

            // Query courses that match semester, grade level, active, seats available, and not already enrolled
            var courses = await _context.Courses
                .AsNoTracking()
                .Where(c =>
                    c.SemesterId == semesterId &&
                    c.GradeLevel == student.GradeLevel &&
                    c.IsActive &&
                    c.MaxStudents > c.CourseEnrollments.Count(ce => ce.IsActive) && // replace HasAvailableSeats
                    !_context.CourseEnrollments.Any(ce =>
                        ce.StudentId == studentId &&
                        ce.CourseId == c.Id &&
                        ce.IsActive
                    )
                )
                .Include(c => c.CourseDepartment)
                .Include(c => c.CourseSemester)
                .Select(c => new Course
                {
                    Id = c.Id,
                    CourseCode = c.CourseCode,
                    CourseName = c.CourseName,
                    Credits = c.Credits,
                    Department = c.Department,
                    MaxStudents = c.MaxStudents,
                    CurrentEnrollment = c.CourseEnrollments.Count(ce => ce.IsActive),
                    ScheduleDays = c.ScheduleDays,
                    StartTime = c.StartTime,
                    EndTime = c.EndTime,
                    ClassSchedule = c.ClassSchedule
                })
                .ToListAsync();

            return courses;
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
        /////123
        //public async Task<BulkEnrollmentResult> ProcessBulkEnrollmentAsync(BulkEnrollmentRequest request)
        //{
        //    // Create result first to ensure it's initialized
        //    var result = new BulkEnrollmentResult
        //    {
        //        SemesterName = (await _context.Semesters.FindAsync(request.SemesterId))?.Name ?? "Unknown Semester",
        //        ProcessedAt = DateTime.Now,
        //        Results = new List<StudentEnrollmentResult>(),
        //        Message = "Processing completed"
        //    };

        //    // === NULL CHECKS ===
        //    if (request == null)
        //    {
        //        result.TotalStudents = 0;
        //        result.SuccessfullyEnrolled = 0;
        //        result.FailedEnrollments = 0;
        //        result.Message = "Request is null";
        //        return result;
        //    }

        //    if (request.StudentIds == null || !request.StudentIds.Any())
        //    {
        //        result.TotalStudents = 0;
        //        result.SuccessfullyEnrolled = 0;
        //        result.FailedEnrollments = 0;
        //        result.Message = "No students selected";
        //        return result;
        //    }

        //    if (request.CourseIds == null || !request.CourseIds.Any())
        //    {
        //        result.TotalStudents = 0;
        //        result.SuccessfullyEnrolled = 0;
        //        result.FailedEnrollments = 0;
        //        result.Message = "No courses selected";
        //        return result;
        //    }

        //    var studentResults = new List<StudentEnrollmentResult>();

        //    foreach (var studentId in request.StudentIds)
        //    {
        //        var student = await _context.Students.FindAsync(studentId);
        //        if (student == null) continue;

        //        var studentResult = new StudentEnrollmentResult
        //        {
        //            StudentId = studentId,
        //            StudentName = student.Name ?? "Unknown",
        //            StudentCode = student.StudentId ?? "Unknown",
        //            CourseResults = new List<CourseEnrollmentResult>(),
        //            Status = "Pending"
        //            // REMOVED: Summary assignment - it's computed automatically
        //        };

        //        int successfulEnrollments = 0;

        //        foreach (var courseId in request.CourseIds)
        //        {
        //            var course = await _context.Courses.FindAsync(courseId);
        //            if (course == null) continue;

        //            var courseResult = new CourseEnrollmentResult
        //            {
        //                CourseId = courseId,
        //                CourseCode = course.CourseCode ?? "Unknown",
        //                CourseName = course.CourseName ?? "Unknown",
        //                Success = false,
        //                Message = "Not processed"
        //            };

        //            try
        //            {
        //                var enrollmentRequest = new EnrollmentRequest
        //                {
        //                    StudentId = studentId,
        //                    CourseId = courseId,
        //                    SemesterId = request.SemesterId,
        //                    Type = request.Type,
        //                    RequestedBy = request.RequestedBy ?? "Bulk Enrollment System"
        //                };

        //                var enrollmentResult = await EnrollStudentAsync(enrollmentRequest);

        //                if (enrollmentResult.Success)
        //                {
        //                    courseResult.Success = true;
        //                    courseResult.Message = "Successfully enrolled";
        //                    successfulEnrollments++;
        //                }
        //                else
        //                {
        //                    courseResult.Success = false;
        //                    courseResult.Message = string.Join(", ", enrollmentResult.Errors);
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                courseResult.Success = false;
        //                courseResult.Message = $"Error: {ex.Message}";
        //            }

        //            studentResult.CourseResults.Add(courseResult);
        //        }

        //        // Update student result status (NO Summary assignment - it's computed)
        //        studentResult.Status = successfulEnrollments == request.CourseIds.Count ? "Success" :
        //                              successfulEnrollments > 0 ? "Partial" : "Failed";

        //        studentResults.Add(studentResult);
        //    }

        //    result.Results = studentResults;
        //    result.TotalStudents = request.StudentIds.Count;
        //    result.SuccessfullyEnrolled = studentResults.Count(r => r.Status != "Failed");
        //    result.FailedEnrollments = studentResults.Count(r => r.Status == "Failed");

        //    // Set final message
        //    result.Message = $"Bulk enrollment completed: {result.SuccessfullyEnrolled} students successfully enrolled, {result.FailedEnrollments} failed";

        //    return result;
        //}

        //public async Task<BulkEnrollmentResult> ProcessBulkEnrollmentAsync(BulkEnrollmentRequest request)
        //{
        //    // Create result properly - BulkEnrollmentResult inherits from BaseEntity
        //    var result = new BulkEnrollmentResult
        //    {
        //        SemesterName = (await _context.Semesters.FindAsync(request.SemesterId))?.Name ?? "Unknown Semester",
        //        ProcessedAt = DateTime.Now,
        //        Results = new List<StudentEnrollmentResult>(),
        //        Message = "Processing started",
        //        TotalStudents = 0,
        //        SuccessfullyEnrolled = 0,
        //        FailedEnrollments = 0
        //    };

        //    // BaseEntity properties will be set automatically (Id, CreatedDate, etc.)

        //    // === NULL CHECKS ===
        //    if (request == null)
        //    {
        //        result.Message = "Request is null";
        //        return result;
        //    }

        //    if (request.StudentIds == null || !request.StudentIds.Any())
        //    {
        //        result.Message = "No students selected";
        //        return result;
        //    }

        //    if (request.CourseIds == null || !request.CourseIds.Any())
        //    {
        //        result.Message = "No courses selected";
        //        return result;
        //    }

        //    // Start transaction for bulk operation
        //    using var transaction = await _context.Database.BeginTransactionAsync();

        //    try
        //    {
        //        var studentResults = new List<StudentEnrollmentResult>();

        //        // Pre-fetch all necessary data
        //        var allStudents = await _context.Students
        //            .Where(s => request.StudentIds.Contains(s.Id))
        //            .ToDictionaryAsync(s => s.Id);

        //        var allCourses = await _context.Courses
        //            .Where(c => request.CourseIds.Contains(c.Id))
        //            .ToDictionaryAsync(c => c.Id);

        //        // Get current enrollment counts
        //        var enrollmentCounts = await _context.CourseEnrollments
        //            .Where(ce => ce.SemesterId == request.SemesterId && ce.IsActive)
        //            .GroupBy(ce => ce.CourseId)
        //            .Select(g => new { CourseId = g.Key, Count = g.Count() })
        //            .ToDictionaryAsync(x => x.CourseId, x => x.Count);

        //        int totalSuccessfulEnrollments = 0;
        //        int totalFailedEnrollments = 0;

        //        foreach (var studentId in request.StudentIds)
        //        {
        //            if (!allStudents.TryGetValue(studentId, out var student))
        //            {
        //                // Student not found
        //                totalFailedEnrollments++;
        //                continue;
        //            }

        //            var studentResult = new StudentEnrollmentResult
        //            {
        //                StudentId = studentId,
        //                StudentName = student.Name ?? "Unknown",
        //                StudentCode = student.StudentId ?? "Unknown",
        //                CourseResults = new List<CourseEnrollmentResult>(),
        //                Status = "Pending"
        //                // Summary property will be computed automatically
        //            };

        //            int successfulEnrollments = 0;

        //            foreach (var courseId in request.CourseIds)
        //            {
        //                if (!allCourses.TryGetValue(courseId, out var course))
        //                {
        //                    // Course not found
        //                    studentResult.CourseResults.Add(new CourseEnrollmentResult
        //                    {
        //                        CourseId = courseId,
        //                        CourseCode = "Unknown",
        //                        CourseName = "Unknown",
        //                        Success = false,
        //                        Message = "Course not found"
        //                    });
        //                    continue;
        //                }

        //                var courseResult = new CourseEnrollmentResult
        //                {
        //                    CourseId = courseId,
        //                    CourseCode = course.CourseCode ?? "Unknown",
        //                    CourseName = course.CourseName ?? "Unknown",
        //                    Success = false,
        //                    Message = "Not processed"
        //                };

        //                try
        //                {
        //                    // Quick eligibility check
        //                    var isEligible = await QuickEligibilityCheckAsync(
        //                        studentId, courseId, request.SemesterId,
        //                        student, course, enrollmentCounts);

        //                    if (!isEligible)
        //                    {
        //                        courseResult.Message = "Not eligible for enrollment";
        //                        studentResult.CourseResults.Add(courseResult);
        //                        continue;
        //                    }

        //                    // Create enrollment
        //                    var enrollment = new CourseEnrollment
        //                    {
        //                        StudentId = studentId,
        //                        CourseId = courseId,
        //                        SemesterId = request.SemesterId,
        //                        EnrollmentDate = DateTime.Now,
        //                        EnrollmentType = request.Type,
        //                        EnrollmentMethod = EnrollmentMethod.Bulk,
        //                        EnrollmentStatus = EnrollmentStatus.Active,
        //                        IsActive = true,
        //                        GradeStatus = GradeStatus.InProgress,
        //                        ApprovedBy = request.RequestedBy ?? "Bulk Enrollment System",
        //                        ApprovalDate = DateTime.Now,
        //                        LastActivityDate = DateTime.Now
        //                    };

        //                    // Add notes if provided
        //                    if (!string.IsNullOrEmpty(request.Notes))
        //                    {
        //                        enrollment.LastActivityDate = DateTime.Now;
        //                        // You could add: enrollment.Notes = request.Notes; if your CourseEnrollment has a Notes property
        //                    }

        //                    _context.CourseEnrollments.Add(enrollment);

        //                    // Update enrollment count
        //                    if (enrollmentCounts.ContainsKey(courseId))
        //                    {
        //                        enrollmentCounts[courseId]++;
        //                    }
        //                    else
        //                    {
        //                        enrollmentCounts[courseId] = 1;
        //                    }

        //                    courseResult.Success = true;
        //                    courseResult.Message = "Successfully enrolled";
        //                    successfulEnrollments++;
        //                    totalSuccessfulEnrollments++;
        //                }
        //                catch (Exception ex)
        //                {
        //                    courseResult.Message = $"Error: {ex.Message}";
        //                    totalFailedEnrollments++;
        //                }

        //                studentResult.CourseResults.Add(courseResult);
        //            }

        //            // Update student result status
        //            studentResult.Status = successfulEnrollments == request.CourseIds.Count ? "Success" :
        //                                  successfulEnrollments > 0 ? "Partial" : "Failed";

        //            studentResults.Add(studentResult);
        //        }

        //        await _context.SaveChangesAsync();
        //        await transaction.CommitAsync();

        //        // Set final results
        //        result.Results = studentResults;
        //        result.TotalStudents = request.StudentIds.Count;
        //        result.SuccessfullyEnrolled = studentResults.Count(r => r.Status != "Failed");
        //        result.FailedEnrollments = studentResults.Count(r => r.Status == "Failed");

        //        // Set message with notes if provided
        //        if (!string.IsNullOrEmpty(request.Notes))
        //        {
        //            result.Message = $"Bulk enrollment completed with notes: '{request.Notes}'. " +
        //                           $"{result.SuccessfullyEnrolled} students successfully enrolled, " +
        //                           $"{result.FailedEnrollments} failed.";
        //        }
        //        else
        //        {
        //            result.Message = $"Bulk enrollment completed: {result.SuccessfullyEnrolled} students successfully enrolled, {result.FailedEnrollments} failed";
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        await transaction.RollbackAsync();
        //        _logger.LogError(ex, "Error in bulk enrollment");

        //        result.SuccessfullyEnrolled = 0;
        //        result.FailedEnrollments = request.StudentIds.Count;
        //        result.Message = $"Bulk enrollment failed: {ex.Message}";

        //        if (!string.IsNullOrEmpty(request.Notes))
        //        {
        //            result.Message += $" (Notes: {request.Notes})";
        //        }
        //    }

        //    return result;
        //}

        public async Task<BulkEnrollmentResult> ProcessBulkEnrollmentAsync(BulkEnrollmentRequest request)
        {
            _logger.LogInformation("=== START ProcessBulkEnrollmentAsync ===");
            _logger.LogInformation("Request: SemesterId={SemesterId}, StudentCount={StudentCount}, CourseCount={CourseCount}, Type={Type}",
                request?.SemesterId, request?.StudentIds?.Count, request?.CourseIds?.Count, request?.Type);

            // Add null check for request
            if (request == null)
            {
                _logger.LogError("Request is null in ProcessBulkEnrollmentAsync");
                return new BulkEnrollmentResult
                {
                    Message = "Request cannot be null",
                    SuccessfullyEnrolled = 0,
                    FailedEnrollments = 0,
                    TotalStudents = 0
                };
            }

            var result = InitializeBulkEnrollmentResult(request);

            if (!ValidateBulkRequest(request, result))
            {
                _logger.LogWarning("Bulk request validation failed: {Message}", result.Message);
                return result;
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var (allStudents, allCourses, enrollmentCounts) = await PrefetchDataAsync(request);

                _logger.LogInformation("Prefetched: {StudentCount} students, {CourseCount} courses",
                    allStudents.Count, allCourses.Count);

                // Add null check for StudentIds
                if (request.StudentIds != null)
                {
                    foreach (var studentId in request.StudentIds)
                    {
                        _logger.LogInformation("Processing student {StudentId}", studentId);

                        var studentResult = await ProcessStudentBulkEnrollmentAsync(
                            studentId, request, allStudents, allCourses, enrollmentCounts);

                        _logger.LogInformation("Student {StudentId} result: {Status}", studentId, studentResult.Status);

                        result.Results.Add(studentResult);
                    }
                }
                else
                {
                    _logger.LogWarning("StudentIds is null in request");
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                UpdateResultStatistics(result);

                _logger.LogInformation("=== COMPLETE ProcessBulkEnrollmentAsync ===");
                _logger.LogInformation("Result: Success={SuccessCount}, Failed={FailedCount}",
                    result.SuccessfullyEnrolled, result.FailedEnrollments);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                HandleBulkEnrollmentError(ex, request, result);

                _logger.LogError(ex, "=== ERROR ProcessBulkEnrollmentAsync ===");
            }

            return result;
        }

        private BulkEnrollmentResult InitializeBulkEnrollmentResult(BulkEnrollmentRequest request)
        {
            return new BulkEnrollmentResult
            {
                ProcessedAt = DateTime.Now,
                Results = new List<StudentEnrollmentResult>(),
                Message = "Processing started",
                TotalStudents = 0,
                SuccessfullyEnrolled = 0,
                FailedEnrollments = 0
            };
        }

        private bool ValidateBulkRequest(BulkEnrollmentRequest request, BulkEnrollmentResult result)
        {
            if (request == null)
            {
                result.Message = "Request is null";
                return false;
            }

            if (request.StudentIds == null || !request.StudentIds.Any())
            {
                result.Message = "No students selected";
                return false;
            }

            if (request.CourseIds == null || !request.CourseIds.Any())
            {
                result.Message = "No courses selected";
                return false;
            }

            return true;
        }

        private async Task<(Dictionary<int, Student>, Dictionary<int, Course>, Dictionary<int, int>)>
    PrefetchDataAsync(BulkEnrollmentRequest request)
        {
            // Add null checks
            if (request?.StudentIds == null || request?.CourseIds == null)
            {
                _logger.LogWarning("StudentIds or CourseIds is null in PrefetchDataAsync");
                return (new Dictionary<int, Student>(), new Dictionary<int, Course>(), new Dictionary<int, int>());
            }

            var allStudents = await _context.Students
                .Where(s => request.StudentIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id);

            var allCourses = await _context.Courses
                .Where(c => request.CourseIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id);

            var enrollmentCounts = await _context.CourseEnrollments
                .Where(ce => ce.SemesterId == request.SemesterId && ce.IsActive)
                .GroupBy(ce => ce.CourseId)
                .Select(g => new { CourseId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.CourseId, x => x.Count);

            return (allStudents, allCourses, enrollmentCounts);
        }

        private async Task<StudentEnrollmentResult> ProcessStudentBulkEnrollmentAsync(
            int studentId, BulkEnrollmentRequest request,
            Dictionary<int, Student> allStudents,
            Dictionary<int, Course> allCourses,
            Dictionary<int, int> enrollmentCounts)
        {
            if (!allStudents.TryGetValue(studentId, out var student))
            {
                return CreateFailedStudentResult(studentId, "Student not found");
            }

            var studentResult = InitializeStudentResult(student);
            int successfulEnrollments = 0;

            foreach (var courseId in request.CourseIds)
            {
                var courseResult = await ProcessCourseEnrollmentAsync(
                    studentId, courseId, request, student,
                    allCourses, enrollmentCounts);

                studentResult.CourseResults.Add(courseResult);

                if (courseResult.Success)
                    successfulEnrollments++;
            }

            studentResult.Status = DetermineStudentStatus(successfulEnrollments, request.CourseIds.Count);
            return studentResult;
        }

        private async Task<CourseEnrollmentResult> ProcessCourseEnrollmentAsync(
    int studentId, int courseId, BulkEnrollmentRequest request,
    Student student, Dictionary<int, Course> allCourses,
    Dictionary<int, int> enrollmentCounts)
        {
            _logger.LogInformation("Checking enrollment: Student={StudentId}, Course={CourseId}", studentId, courseId);

            var courseResult = InitializeCourseResult(courseId, allCourses);

            try
            {
                if (!allCourses.TryGetValue(courseId, out var course))
                {
                    courseResult.Message = "Course not found";
                    _logger.LogInformation("Course {CourseId} not found in prefetched data", courseId);
                    return courseResult;
                }

                // Check if student exists in dictionary
                if (student == null)
                {
                    courseResult.Message = "Student not found";
                    _logger.LogInformation("Student {StudentId} not found in prefetched data", studentId);
                    return courseResult;
                }

                // Log student and course details for debugging
                _logger.LogInformation("Student details - ID: {StudentId}, Name: {StudentName}, GradeLevel: {GradeLevel}, GPA: {GPA}, PassedHours: {PassedHours}",
                    student.Id, student.Name, student.GradeLevel, student.GPA, student.PassedHours);

                _logger.LogInformation("Course details - ID: {CourseId}, Name: {CourseName}, GradeLevel: {CourseGradeLevel}, MinGPA: {MinGPA}, MinPassedHours: {MinPassedHours}, MaxStudents: {MaxStudents}",
                    course.Id, course.CourseName, course.GradeLevel, course.MinGPA, course.MinPassedHours, course.MaxStudents);

                var isEligible = await QuickEligibilityCheckAsync(
                    studentId, courseId, request.SemesterId,
                    student, course, enrollmentCounts);

                if (!isEligible)
                {
                    courseResult.Message = "Not eligible for enrollment";
                    _logger.LogInformation("Student {StudentId} not eligible for course {CourseId}", studentId, courseId);
                    return courseResult;
                }

                await CreateEnrollmentAsync(studentId, courseId, request, courseResult);
                UpdateEnrollmentCount(courseId, enrollmentCounts);

                courseResult.Success = true;
                courseResult.Message = "Successfully enrolled";
                _logger.LogInformation("✓ Student {StudentId} enrolled in course {CourseId}", studentId, courseId);
            }
            catch (Exception ex)
            {
                courseResult.Message = $"Error: {ex.Message}";
                _logger.LogError(ex, "Error enrolling student {StudentId} in course {CourseId}", studentId, courseId);
            }

            return courseResult;
        }

        private async Task CreateEnrollmentAsync(int studentId, int courseId,
    BulkEnrollmentRequest request, CourseEnrollmentResult courseResult)
        {
            _logger.LogInformation("Creating enrollment for Student={StudentId}, Course={CourseId}", studentId, courseId);

            try
            {
                var enrollment = new CourseEnrollment
                {
                    StudentId = studentId,
                    CourseId = courseId,
                    SemesterId = request.SemesterId,
                    EnrollmentDate = DateTime.Now,
                    EnrollmentType = request.Type,
                    EnrollmentMethod = EnrollmentMethod.Bulk,
                    EnrollmentStatus = EnrollmentStatus.Active,
                    IsActive = true,
                    GradeStatus = GradeStatus.InProgress,
                    ApprovedBy = request.RequestedBy ?? "Bulk Enrollment System",
                    ApprovalDate = DateTime.Now,
                    LastActivityDate = DateTime.Now
                };

                _context.CourseEnrollments.Add(enrollment);

                // Try to save immediately to catch any errors
                var changes = await _context.SaveChangesAsync();
                _logger.LogInformation("Enrollment created successfully. Changes saved: {Changes}", changes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create enrollment for Student={StudentId}, Course={CourseId}", studentId, courseId);
                throw;
            }
        }



        private void UpdateResultStatistics(BulkEnrollmentResult result)
        {
            result.TotalStudents = result.Results.Count;
            result.SuccessfullyEnrolled = result.Results.Count(r => r.Status != "Failed");
            result.FailedEnrollments = result.Results.Count(r => r.Status == "Failed");

            var semesterName = result.Results.FirstOrDefault()?.CourseResults
                .FirstOrDefault()?.CourseName ?? "Unknown Semester";

            result.SemesterName = semesterName;
            result.Message = $"Bulk enrollment completed: {result.SuccessfullyEnrolled} students successfully enrolled, {result.FailedEnrollments} failed";
        }

        private void HandleBulkEnrollmentError(Exception ex, BulkEnrollmentRequest request, BulkEnrollmentResult result)
        {
            _logger.LogError(ex, "Error in bulk enrollment");

            result.SuccessfullyEnrolled = 0;
            result.FailedEnrollments = request.StudentIds?.Count ?? 0;
            result.Message = $"Bulk enrollment failed: {ex.Message}";
        }


        private StudentEnrollmentResult CreateFailedStudentResult(int studentId, string message)
        {
            return new StudentEnrollmentResult
            {
                StudentId = studentId,
                StudentName = "Unknown",
                StudentCode = "Unknown",
                Status = "Failed",
                CourseResults = new List<CourseEnrollmentResult>
        {
            new CourseEnrollmentResult
            {
                Success = false,
                Message = message
            }
        }
            };
        }

        private StudentEnrollmentResult InitializeStudentResult(Student student)
        {
            return new StudentEnrollmentResult
            {
                StudentId = student.Id,
                StudentName = student.Name ?? "Unknown",
                StudentCode = student.StudentId ?? "Unknown",
                CourseResults = new List<CourseEnrollmentResult>(),
                Status = "Pending"
            };
        }

        private CourseEnrollmentResult InitializeCourseResult(int courseId, Dictionary<int, Course> allCourses)
        {
            var course = allCourses.ContainsKey(courseId) ? allCourses[courseId] : null;

            return new CourseEnrollmentResult
            {
                CourseId = courseId,
                CourseCode = course?.CourseCode ?? "Unknown",
                CourseName = course?.CourseName ?? "Unknown",
                Success = false,
                Message = "Not processed"
            };
        }

        private string DetermineStudentStatus(int successfulEnrollments, int totalCourses)
        {
            return successfulEnrollments switch
            {
                var count when count == totalCourses => "Success",
                var count when count > 0 => "Partial",
                _ => "Failed"
            };
        }

        private void UpdateEnrollmentCount(int courseId, Dictionary<int, int> enrollmentCounts)
        {
            if (enrollmentCounts.ContainsKey(courseId))
            {
                enrollmentCounts[courseId]++;
            }
            else
            {
                enrollmentCounts[courseId] = 1;
            }
        }

        /**************************/

        private async Task<bool> QuickEligibilityCheckAsync(int studentId, int courseId, int semesterId,
    Student student, Course course, Dictionary<int, int> enrollmentCounts)
        {
            _logger.LogInformation("Checking eligibility: Student={StudentId}, Course={CourseId}", studentId, courseId);

            // 1. Check if already enrolled
            var alreadyEnrolled = await _context.CourseEnrollments
                .AnyAsync(ce => ce.StudentId == studentId &&
                               ce.CourseId == courseId &&
                               ce.SemesterId == semesterId &&
                               ce.IsActive);

            if (alreadyEnrolled)
            {
                _logger.LogInformation("❌ Already enrolled: Student={StudentId}, Course={CourseId}", studentId, courseId);
                return false;
            }

            // 2. Check grade level
            _logger.LogInformation("Checking grade level: Student={StudentGradeLevel}, Course={CourseGradeLevel}",
                student.GradeLevel, course.GradeLevel);

            if (student.GradeLevel != course.GradeLevel)
            {
                _logger.LogInformation("❌ Grade level mismatch: Student={StudentGradeLevel}, Course={CourseGradeLevel}",
                    student.GradeLevel, course.GradeLevel);
                return false;
            }

            // 3. Check GPA
            _logger.LogInformation("Checking GPA: Student={StudentGPA}, Required={CourseMinGPA}",
                student.GPA, course.MinGPA);

            if (student.GPA < course.MinGPA)
            {
                _logger.LogInformation("❌ GPA requirement: Student={StudentGPA}, Required={CourseMinGPA}",
                    student.GPA, course.MinGPA);
                return false;
            }

            // 4. Check passed hours
            _logger.LogInformation("Checking passed hours: Student={StudentHours}, Required={CourseHours}",
                student.PassedHours, course.MinPassedHours);

            if (student.PassedHours < course.MinPassedHours)
            {
                _logger.LogInformation("❌ Passed hours: Student={StudentHours}, Required={CourseHours}",
                    student.PassedHours, course.MinPassedHours);
                return false;
            }

            // 5. Check capacity
            var currentCount = enrollmentCounts.ContainsKey(courseId) ? enrollmentCounts[courseId] : 0;
            _logger.LogInformation("Checking capacity: Current={CurrentCount}, Max={MaxStudents}",
                currentCount, course.MaxStudents);

            if (currentCount >= course.MaxStudents)
            {
                _logger.LogInformation("❌ Course full: Current={CurrentCount}, Max={MaxStudents}",
                    currentCount, course.MaxStudents);
                return false;
            }

            // 6. Check prerequisites
            _logger.LogInformation("Checking prerequisites for course {CourseId}", courseId);

            var hasMissingPrerequisites = await _context.CoursePrerequisites
                .AnyAsync(cp => cp.CourseId == courseId &&
                               cp.IsRequired &&
                               !_context.CourseEnrollments.Any(ce =>
                                   ce.StudentId == studentId &&
                                   ce.CourseId == cp.PrerequisiteCourseId &&
                                   ce.Grade >= (cp.MinGrade ?? 60) &&
                                   ce.IsActive));

            if (hasMissingPrerequisites)
            {
                _logger.LogInformation("❌ Missing prerequisites for course {CourseId}", courseId);
                return false;
            }

            _logger.LogInformation("✓ Eligible: Student={StudentId}, Course={CourseId}", studentId, courseId);
            return true;
        }

        private static void AddSimpleAuditEntry(CourseEnrollment enrollment, string action, string performedBy, string notes)
        {
            // Simpler audit without circular dependencies
            enrollment.LastActivityDate = DateTime.Now;

            if (!string.IsNullOrEmpty(notes))
            {
                // Store notes in a simple way
                enrollment.LastActivityDate = DateTime.Now;
                // You can add audit logging to a separate table here if needed
            }
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

        public async Task<BulkEnrollmentDetailedResult> ProcessBulkEnrollmentWithDetailsAsync(BulkEnrollmentRequest request)
        {
            _logger.LogInformation("=== START ProcessBulkEnrollmentWithDetailsAsync ===");

            var result = new BulkEnrollmentDetailedResult
            {
                SemesterId = request.SemesterId,
                ProcessedAt = DateTime.Now,
                RequestType = request.Type.ToString(),
                RequestedBy = request.RequestedBy,
                IsEligibilityCheck = false
            };

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Prefetch all data
                var allStudents = await _context.Students
                    .Where(s => request.StudentIds.Contains(s.Id))
                    .ToDictionaryAsync(s => s.Id);

                var allCourses = await _context.Courses
                    .Where(c => request.CourseIds.Contains(c.Id))
                    .ToDictionaryAsync(c => c.Id);

                // Process each student
                int totalProcessed = 0;
                int totalEnrollmentsAttempted = 0;
                int totalEnrollmentsSuccessful = 0;

                foreach (var studentId in request.StudentIds)
                {
                    if (!allStudents.TryGetValue(studentId, out var student))
                        continue;

                    var studentDetail = new StudentEnrollmentDetail
                    {
                        StudentId = studentId,
                        StudentCode = student.StudentId ?? "Unknown",
                        StudentName = student.Name ?? "Unknown",
                        GPA = student.GPA,
                        PassedHours = student.PassedHours,
                        GradeLevel = student.GradeLevel.ToString()
                    };

                    int studentSuccessfulEnrollments = 0;

                    // Process each course for this student
                    foreach (var courseId in request.CourseIds)
                    {
                        if (!allCourses.TryGetValue(courseId, out var course))
                            continue;

                        totalEnrollmentsAttempted++;
                        var enrollmentDetail = new EnrollmentProcessDetail
                        {
                            CourseId = courseId,
                            CourseCode = course.CourseCode ?? "Unknown",
                            CourseName = course.CourseName ?? "Unknown"
                        };

                        // Step 1: Check eligibility with detailed requirements
                        var eligibilityCheck = await CheckDetailedEligibilityAsync(studentId, courseId, request.SemesterId);
                        enrollmentDetail.EligibilityChecks = eligibilityCheck;

                        if (eligibilityCheck.All(e => e.IsMet))
                        {
                            // Step 2: Check capacity
                            var currentEnrollment = await _context.CourseEnrollments
                                .CountAsync(ce => ce.CourseId == courseId && ce.SemesterId == request.SemesterId && ce.IsActive);

                            enrollmentDetail.CapacityCheck = new RequirementCheck
                            {
                                Name = "Course Capacity",
                                IsMet = currentEnrollment < course.MaxStudents,
                                Details = $"Current: {currentEnrollment}, Max: {course.MaxStudents}",
                                RequiredValue = course.MaxStudents.ToString(),
                                ActualValue = currentEnrollment.ToString()
                            };

                            // Step 3: Check for existing enrollment
                            var existingEnrollment = await _context.CourseEnrollments
                                .AnyAsync(ce => ce.StudentId == studentId && ce.CourseId == courseId &&
                                               ce.SemesterId == request.SemesterId && ce.IsActive);

                            enrollmentDetail.DuplicateCheck = new RequirementCheck
                            {
                                Name = "No Duplicate Enrollment",
                                IsMet = !existingEnrollment,
                                Details = existingEnrollment ? "Already enrolled in this course" : "Not enrolled yet"
                            };

                            // Step 4: Create enrollment if all checks pass
                            if (enrollmentDetail.EligibilityChecks.All(e => e.IsMet) &&
                                enrollmentDetail.CapacityCheck.IsMet &&
                                enrollmentDetail.DuplicateCheck.IsMet)
                            {
                                try
                                {
                                    var enrollment = new CourseEnrollment
                                    {
                                        StudentId = studentId,
                                        CourseId = courseId,
                                        SemesterId = request.SemesterId,
                                        EnrollmentDate = DateTime.Now,
                                        EnrollmentType = request.Type,
                                        EnrollmentMethod = EnrollmentMethod.Bulk,
                                        EnrollmentStatus = EnrollmentStatus.Active,
                                        IsActive = true,
                                        GradeStatus = GradeStatus.InProgress,
                                        ApprovedBy = request.RequestedBy ?? "Bulk Enrollment System",
                                        ApprovalDate = DateTime.Now,
                                        LastActivityDate = DateTime.Now
                                    };

                                    _context.CourseEnrollments.Add(enrollment);
                                    await _context.SaveChangesAsync();

                                    enrollmentDetail.Status = EnrollmentProcessStatus.Enrolled;
                                    enrollmentDetail.Message = "Successfully enrolled";
                                    enrollmentDetail.EnrollmentId = enrollment.Id;
                                    studentSuccessfulEnrollments++;
                                    totalEnrollmentsSuccessful++;
                                }
                                catch (Exception ex)
                                {
                                    enrollmentDetail.Status = EnrollmentProcessStatus.Failed;
                                    enrollmentDetail.Message = $"Database error: {ex.Message}";
                                }
                            }
                            else
                            {
                                enrollmentDetail.Status = EnrollmentProcessStatus.NotEligible;
                                enrollmentDetail.Message = "Failed one or more requirements";
                            }
                        }
                        else
                        {
                            enrollmentDetail.Status = EnrollmentProcessStatus.NotEligible;
                            enrollmentDetail.Message = "Failed eligibility requirements";
                        }

                        studentDetail.EnrollmentDetails.Add(enrollmentDetail);
                    }

                    // Determine student overall status
                    studentDetail.TotalCoursesAttempted = request.CourseIds.Count;
                    studentDetail.SuccessfulEnrollments = studentSuccessfulEnrollments;
                    studentDetail.Status = studentSuccessfulEnrollments switch
                    {
                        var count when count == request.CourseIds.Count => "Fully Enrolled",
                        var count when count > 0 => "Partially Enrolled",
                        _ => "Not Enrolled"
                    };

                    result.StudentDetails.Add(studentDetail);
                    totalProcessed++;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Set overall results
                result.TotalStudents = request.StudentIds.Count;
                result.TotalCourses = request.CourseIds.Count;
                result.TotalEnrollmentsAttempted = totalEnrollmentsAttempted;
                result.SuccessfulEnrollments = totalEnrollmentsSuccessful;
                result.FailedEnrollments = totalEnrollmentsAttempted - totalEnrollmentsSuccessful;
                result.ProcessedStudents = totalProcessed;

                // Calculate statistics
                result.SuccessRate = totalEnrollmentsAttempted > 0 ?
                    (decimal)totalEnrollmentsSuccessful / totalEnrollmentsAttempted * 100 : 0;
                result.StudentSuccessRate = result.TotalStudents > 0 ?
                    (decimal)result.StudentDetails.Count(s => s.Status != "Not Enrolled") / result.TotalStudents * 100 : 0;

                // Generate summary
                result.Summary = BulkEnrollmentHelpers.GenerateDetailedSummary(result);

                _logger.LogInformation("=== COMPLETE ProcessBulkEnrollmentWithDetailsAsync ===");
                return result;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error in detailed bulk enrollment");

                result.HasErrors = true;
                result.ErrorMessage = $"Processing error: {ex.Message}";
                return result;
            }
        }

        private async Task<List<RequirementCheck>> CheckDetailedEligibilityAsync(int studentId, int courseId, int semesterId)
        {
            var checks = new List<RequirementCheck>();

            var student = await _context.Students.FindAsync(studentId);
            var course = await _context.Courses.FindAsync(courseId);

            if (student == null || course == null)
            {
                checks.Add(new RequirementCheck
                {
                    Name = "Student/Course Exists",
                    IsMet = false,
                    Details = "Student or course not found"
                });
                return checks;
            }

            // Check grade level
            checks.Add(new RequirementCheck
            {
                Name = "Grade Level Match",
                IsMet = student.GradeLevel == course.GradeLevel,
                Details = student.GradeLevel == course.GradeLevel ?
                    $"Grade level matches ({student.GradeLevel})" :
                    $"Student grade: {student.GradeLevel}, Course requires: {course.GradeLevel}",
                RequiredValue = course.GradeLevel.ToString(),
                ActualValue = student.GradeLevel.ToString()
            });

            // Check GPA - FIXED: Use proper formatting
            checks.Add(new RequirementCheck
            {
                Name = "GPA Requirement",
                IsMet = student.GPA >= course.MinGPA,
                Details = student.GPA >= course.MinGPA ?
                    $"GPA requirement met ({student.GPA:0.00} >= {course.MinGPA:0.00})" :
                    $"GPA too low ({student.GPA:0.00} < {course.MinGPA:0.00})",
                RequiredValue = string.Format("{0:F2}", course.MinGPA),
                ActualValue = student.GPA.ToString("0.00")
            });

            // Check passed hours
            checks.Add(new RequirementCheck
            {
                Name = "Passed Hours",
                IsMet = student.PassedHours >= course.MinPassedHours,
                Details = student.PassedHours >= course.MinPassedHours ?
                    $"Passed hours requirement met ({student.PassedHours} >= {course.MinPassedHours})" :
                    $"Insufficient passed hours ({student.PassedHours} < {course.MinPassedHours})",
                RequiredValue = course.MinPassedHours.ToString(),
                ActualValue = student.PassedHours.ToString()
            });

            // Check prerequisites
            var missingPrereqs = await GetMissingPrerequisitesAsync(studentId, courseId);
            checks.Add(new RequirementCheck
            {
                Name = "Prerequisites",
                IsMet = !missingPrereqs.Any(),
                Details = !missingPrereqs.Any() ?
                    "All prerequisites satisfied" :
                    $"Missing prerequisites: {string.Join(", ", missingPrereqs)}",
                RequiredValue = "All prerequisites completed",
                ActualValue = missingPrereqs.Any() ? $"Missing: {string.Join(", ", missingPrereqs)}" : "All completed"
            });

            return checks;
        }

        public async Task<BulkEnrollmentDetailedResult> CheckBulkEligibilityWithDetailsAsync(BulkEnrollmentRequest request)
        {
            var result = new BulkEnrollmentDetailedResult
            {
                SemesterId = request.SemesterId,
                ProcessedAt = DateTime.Now,
                RequestType = request.Type.ToString(),
                RequestedBy = request.RequestedBy,
                IsEligibilityCheck = true
            };

            try
            {
                var allStudents = await _context.Students
                    .Where(s => request.StudentIds.Contains(s.Id))
                    .ToDictionaryAsync(s => s.Id);

                var allCourses = await _context.Courses
                    .Where(c => request.CourseIds.Contains(c.Id))
                    .ToDictionaryAsync(c => c.Id);

                foreach (var studentId in request.StudentIds)
                {
                    if (!allStudents.TryGetValue(studentId, out var student))
                        continue;

                    var studentDetail = new StudentEnrollmentDetail
                    {
                        StudentId = studentId,
                        StudentCode = student.StudentId ?? "Unknown",
                        StudentName = student.Name ?? "Unknown",
                        GPA = student.GPA,
                        PassedHours = student.PassedHours,
                        GradeLevel = student.GradeLevel.ToString()
                    };

                    foreach (var courseId in request.CourseIds)
                    {
                        if (!allCourses.TryGetValue(courseId, out var course))
                            continue;

                        var enrollmentDetail = new EnrollmentProcessDetail
                        {
                            CourseId = courseId,
                            CourseCode = course.CourseCode ?? "Unknown",
                            CourseName = course.CourseName ?? "Unknown"
                        };

                        // Check eligibility only
                        var eligibilityCheck = await CheckDetailedEligibilityAsync(studentId, courseId, request.SemesterId);
                        enrollmentDetail.EligibilityChecks = eligibilityCheck;

                        // Check capacity
                        var currentEnrollment = await _context.CourseEnrollments
                            .CountAsync(ce => ce.CourseId == courseId && ce.SemesterId == request.SemesterId && ce.IsActive);

                        enrollmentDetail.CapacityCheck = new RequirementCheck
                        {
                            Name = "Course Capacity",
                            IsMet = currentEnrollment < course.MaxStudents,
                            Details = $"Current: {currentEnrollment}, Max: {course.MaxStudents}",
                            RequiredValue = course.MaxStudents.ToString(),
                            ActualValue = currentEnrollment.ToString()
                        };

                        // Check existing enrollment
                        var existingEnrollment = await _context.CourseEnrollments
                            .AnyAsync(ce => ce.StudentId == studentId && ce.CourseId == courseId &&
                                           ce.SemesterId == request.SemesterId && ce.IsActive);

                        enrollmentDetail.DuplicateCheck = new RequirementCheck
                        {
                            Name = "No Duplicate Enrollment",
                            IsMet = !existingEnrollment,
                            Details = existingEnrollment ? "Already enrolled in this course" : "Not enrolled yet"
                        };

                        // Determine overall eligibility
                        enrollmentDetail.IsEligible = enrollmentDetail.EligibilityChecks.All(e => e.IsMet) &&
                                                    enrollmentDetail.CapacityCheck.IsMet &&
                                                    enrollmentDetail.DuplicateCheck.IsMet;

                        enrollmentDetail.Status = enrollmentDetail.IsEligible ?
                            EnrollmentProcessStatus.Eligible : EnrollmentProcessStatus.NotEligible;
                        enrollmentDetail.Message = enrollmentDetail.IsEligible ?
                            "Eligible for enrollment" : "Not eligible";

                        studentDetail.EnrollmentDetails.Add(enrollmentDetail);
                    }

                    // Calculate student eligibility
                    studentDetail.TotalCoursesAttempted = request.CourseIds.Count;
                    studentDetail.EligibleCourses = studentDetail.EnrollmentDetails.Count(e => e.IsEligible);
                    studentDetail.Status = studentDetail.EligibleCourses switch
                    {
                        var count when count == request.CourseIds.Count => "Fully Eligible",
                        var count when count > 0 => "Partially Eligible",
                        _ => "Not Eligible"
                    };

                    result.StudentDetails.Add(studentDetail);
                }

                result.TotalStudents = request.StudentIds.Count;
                result.TotalCourses = request.CourseIds.Count;
                result.SuccessfulEnrollments = result.StudentDetails.Sum(s => s.EligibleCourses);
                result.TotalEnrollmentsAttempted = result.TotalStudents * result.TotalCourses;
                result.FailedEnrollments = result.TotalEnrollmentsAttempted - result.SuccessfulEnrollments;

                // Generate summary for eligibility check
                result.Summary = BulkEnrollmentHelpers.GenerateEligibilitySummary(result);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in detailed eligibility check");
                result.HasErrors = true;
                result.ErrorMessage = $"Eligibility check error: {ex.Message}";
                return result;
            }
        }


        private string GenerateDetailedSummary(BulkEnrollmentDetailedResult result)
        {
            var summary = new StringBuilder();

            summary.AppendLine($"<strong>Bulk Enrollment Results</strong>");
            summary.AppendLine($"<div class='mt-2'>");
            summary.AppendLine($"<p><strong>Processed:</strong> {result.ProcessedAt:yyyy-MM-dd HH:mm:ss}</p>");
            summary.AppendLine($"<p><strong>Total Students:</strong> {result.TotalStudents}</p>");
            summary.AppendLine($"<p><strong>Total Courses:</strong> {result.TotalCourses}</p>");
            summary.AppendLine($"<p><strong>Total Enrollment Attempts:</strong> {result.TotalEnrollmentsAttempted}</p>");
            summary.AppendLine($"<p><strong>Successful Enrollments:</strong> {result.SuccessfulEnrollments}</p>");
            summary.AppendLine($"<p><strong>Failed Enrollments:</strong> {result.FailedEnrollments}</p>");
            summary.AppendLine($"<p><strong>Overall Success Rate:</strong> {result.SuccessRate:F1}%</p>");
            summary.AppendLine($"</div>");

            return summary.ToString();
        }

        private string GenerateEligibilitySummary(BulkEnrollmentDetailedResult result)
        {
            var summary = new StringBuilder();

            summary.AppendLine($"<strong>Eligibility Check Results</strong>");
            summary.AppendLine($"<div class='mt-2'>");
            summary.AppendLine($"<p><strong>Checked:</strong> {result.ProcessedAt:yyyy-MM-dd HH:mm:ss}</p>");
            summary.AppendLine($"<p><strong>Total Students:</strong> {result.TotalStudents}</p>");
            summary.AppendLine($"<p><strong>Total Courses:</strong> {result.TotalCourses}</p>");
            summary.AppendLine($"<p><strong>Potential Enrollments:</strong> {result.TotalEnrollmentsAttempted}</p>");
            summary.AppendLine($"<p><strong>Eligible Enrollments:</strong> {result.SuccessfulEnrollments}</p>");
            summary.AppendLine($"<p><strong>Ineligible Enrollments:</strong> {result.FailedEnrollments}</p>");

            var eligibleStudents = result.StudentDetails.Count(s => s.Status.Contains("Eligible"));
            summary.AppendLine($"<p><strong>Eligible Students:</strong> {eligibleStudents} ({result.StudentSuccessRate:F1}%)</p>");
            summary.AppendLine($"</div>");

            return summary.ToString();
        }

        //private string GenerateDetailedSummary(BulkEnrollmentDetailedResult result)
        //{
        //    return BulkEnrollmentHelpers.GenerateDetailedSummary(result);
        //}

        //private string GenerateEligibilitySummary(BulkEnrollmentDetailedResult result)
        //{
        //    return BulkEnrollmentHelpers.GenerateEligibilitySummary(result);
        //}



        /*
        public async Task<BulkEnrollmentDetailedResult> ProcessBulkEnrollmentWithDetailsAsync(BulkEnrollmentRequest request)
        {
            _logger.LogInformation("=== START ProcessBulkEnrollmentWithDetailsAsync ===");

            var result = new BulkEnrollmentDetailedResult
            {
                SemesterId = request.SemesterId,
                ProcessedAt = DateTime.Now,
                RequestType = request.Type.ToString(),
                RequestedBy = request.RequestedBy,
                IsEligibilityCheck = false
            };

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Prefetch all data
                var allStudents = await _context.Students
                    .Where(s => request.StudentIds.Contains(s.Id))
                    .ToDictionaryAsync(s => s.Id);

                var allCourses = await _context.Courses
                    .Where(c => request.CourseIds.Contains(c.Id))
                    .ToDictionaryAsync(c => c.Id);

                // Process each student
                int totalProcessed = 0;
                int totalEnrollmentsAttempted = 0;
                int totalEnrollmentsSuccessful = 0;

                foreach (var studentId in request.StudentIds)
                {
                    if (!allStudents.TryGetValue(studentId, out var student))
                        continue;

                    var studentDetail = new StudentEnrollmentDetail
                    {
                        StudentId = studentId,
                        StudentCode = student.StudentId ?? "Unknown",
                        StudentName = student.Name ?? "Unknown",
                        GPA = student.GPA,
                        PassedHours = student.PassedHours,
                        GradeLevel = student.GradeLevel.ToString()
                    };

                    int studentSuccessfulEnrollments = 0;

                    // Process each course for this student
                    foreach (var courseId in request.CourseIds)
                    {
                        if (!allCourses.TryGetValue(courseId, out var course))
                            continue;

                        totalEnrollmentsAttempted++;
                        var enrollmentDetail = new EnrollmentProcessDetail
                        {
                            CourseId = courseId,
                            CourseCode = course.CourseCode ?? "Unknown",
                            CourseName = course.CourseName ?? "Unknown"
                        };

                        // Step 1: Check eligibility with detailed requirements
                        var eligibilityCheck = await CheckDetailedEligibilityAsync(studentId, courseId, request.SemesterId);
                        enrollmentDetail.EligibilityChecks = eligibilityCheck;

                        if (eligibilityCheck.All(e => e.IsMet))
                        {
                            // Step 2: Check capacity
                            var currentEnrollment = await _context.CourseEnrollments
                                .CountAsync(ce => ce.CourseId == courseId && ce.SemesterId == request.SemesterId && ce.IsActive);

                            enrollmentDetail.CapacityCheck = new RequirementCheck
                            {
                                Name = "Course Capacity",
                                IsMet = currentEnrollment < course.MaxStudents,
                                Details = $"Current: {currentEnrollment}, Max: {course.MaxStudents}",
                                RequiredValue = course.MaxStudents.ToString(),
                                ActualValue = currentEnrollment.ToString()
                            };

                            // Step 3: Check for existing enrollment
                            var existingEnrollment = await _context.CourseEnrollments
                                .AnyAsync(ce => ce.StudentId == studentId && ce.CourseId == courseId &&
                                               ce.SemesterId == request.SemesterId && ce.IsActive);

                            enrollmentDetail.DuplicateCheck = new RequirementCheck
                            {
                                Name = "No Duplicate Enrollment",
                                IsMet = !existingEnrollment,
                                Details = existingEnrollment ? "Already enrolled in this course" : "Not enrolled yet"
                            };

                            // Step 4: Create enrollment if all checks pass
                            if (enrollmentDetail.EligibilityChecks.All(e => e.IsMet) &&
                                enrollmentDetail.CapacityCheck.IsMet &&
                                enrollmentDetail.DuplicateCheck.IsMet)
                            {
                                try
                                {
                                    var enrollment = new CourseEnrollment
                                    {
                                        StudentId = studentId,
                                        CourseId = courseId,
                                        SemesterId = request.SemesterId,
                                        EnrollmentDate = DateTime.Now,
                                        EnrollmentType = request.Type,
                                        EnrollmentMethod = EnrollmentMethod.Bulk,
                                        EnrollmentStatus = EnrollmentStatus.Active,
                                        IsActive = true,
                                        GradeStatus = GradeStatus.InProgress,
                                        ApprovedBy = request.RequestedBy ?? "Bulk Enrollment System",
                                        ApprovalDate = DateTime.Now,
                                        LastActivityDate = DateTime.Now
                                    };

                                    _context.CourseEnrollments.Add(enrollment);
                                    await _context.SaveChangesAsync();

                                    enrollmentDetail.Status = EnrollmentProcessStatus.Enrolled;
                                    enrollmentDetail.Message = "Successfully enrolled";
                                    enrollmentDetail.EnrollmentId = enrollment.Id;
                                    studentSuccessfulEnrollments++;
                                    totalEnrollmentsSuccessful++;
                                }
                                catch (Exception ex)
                                {
                                    enrollmentDetail.Status = EnrollmentProcessStatus.Failed;
                                    enrollmentDetail.Message = $"Database error: {ex.Message}";
                                }
                            }
                            else
                            {
                                enrollmentDetail.Status = EnrollmentProcessStatus.NotEligible;
                                enrollmentDetail.Message = "Failed one or more requirements";
                            }
                        }
                        else
                        {
                            enrollmentDetail.Status = EnrollmentProcessStatus.NotEligible;
                            enrollmentDetail.Message = "Failed eligibility requirements";
                        }

                        studentDetail.EnrollmentDetails.Add(enrollmentDetail);
                    }

                    // Determine student overall status
                    studentDetail.TotalCoursesAttempted = request.CourseIds.Count;
                    studentDetail.SuccessfulEnrollments = studentSuccessfulEnrollments;
                    studentDetail.Status = studentSuccessfulEnrollments switch
                    {
                        var count when count == request.CourseIds.Count => "Fully Enrolled",
                        var count when count > 0 => "Partially Enrolled",
                        _ => "Not Enrolled"
                    };

                    result.StudentDetails.Add(studentDetail);
                    totalProcessed++;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Set overall results
                result.TotalStudents = request.StudentIds.Count;
                result.TotalCourses = request.CourseIds.Count;
                result.TotalEnrollmentsAttempted = totalEnrollmentsAttempted;
                result.SuccessfulEnrollments = totalEnrollmentsSuccessful;
                result.FailedEnrollments = totalEnrollmentsAttempted - totalEnrollmentsSuccessful;
                result.ProcessedStudents = totalProcessed;

                // Calculate statistics
                result.SuccessRate = totalEnrollmentsAttempted > 0 ?
                    (decimal)totalEnrollmentsSuccessful / totalEnrollmentsAttempted * 100 : 0;
                result.StudentSuccessRate = result.TotalStudents > 0 ?
                    (decimal)result.StudentDetails.Count(s => s.Status != "Not Enrolled") / result.TotalStudents * 100 : 0;

                // Generate summary
                result.Summary = GenerateDetailedSummary(result);

                _logger.LogInformation("=== COMPLETE ProcessBulkEnrollmentWithDetailsAsync ===");
                return result;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error in detailed bulk enrollment");

                result.HasErrors = true;
                result.ErrorMessage = $"Processing error: {ex.Message}";
                return result;
            }
        }
        */


    }

}
