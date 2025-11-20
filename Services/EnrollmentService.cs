using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;

namespace StudentManagementSystem.Services{
    
    public class EnrollmentService : IEnrollmentService
    {
        private readonly ApplicationDbContext _context;

        public EnrollmentService(ApplicationDbContext context)
        {
            _context = context;
        }

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

    }

}

    
    




