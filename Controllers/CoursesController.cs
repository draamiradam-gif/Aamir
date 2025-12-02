// Controllers/CoursesController.cs (UPDATED - Import Methods Removed)
using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;
using StudentManagementSystem.Services;
using StudentManagementSystem.ViewModels;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace StudentManagementSystem.Controllers
{
    [Authorize]
    public class CoursesController : BaseController
    {
        private readonly ICourseService _courseService;
        private readonly IStudentService _studentService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CoursesController> _logger;

        public CoursesController(ApplicationDbContext context, ICourseService courseService, IStudentService studentService, ILogger<CoursesController> logger)
        {
            _courseService = courseService;
            _studentService = studentService;
            _context = context;
            _logger = logger;
        }

        // GET: Courses
        [HttpGet]
        public async Task<IActionResult> Index(string searchString, string department, int? semester, string sortBy = "CourseCode", string sortOrder = "asc")
        {
            // ✅ Simple admin check
            //var redirectResult = RedirectIfNoPermission("Courses.View");
            //if (redirectResult != null) return redirectResult;

            try
            {
                // ✅ USE DIRECT DB CONTEXT TO INCLUDE PREREQUISITES
                var courses = _context.Courses
                    .Include(c => c.CourseDepartment)
                    .Include(c => c.CourseSemester)
                    .Include(c => c.Prerequisites)
                        .ThenInclude(p => p.PrerequisiteCourse)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(searchString))
                {
                    courses = courses.Where(c =>
                        c.CourseCode.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                        c.CourseName.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                        (c.Description != null && c.Description.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                    );
                }

                if (!string.IsNullOrEmpty(department))
                {
                    courses = courses.Where(c => c.Department == department);
                }

                if (semester.HasValue)
                {
                    courses = courses.Where(c => c.SemesterId == semester.Value);
                }

                // ✅ ADD PREREQUISITES SORTING
                courses = sortBy?.ToLower() switch
                {
                    "prerequisites" => sortOrder == "desc"
                        ? courses.OrderByDescending(c => c.Prerequisites.Count)
                        : courses.OrderBy(c => c.Prerequisites.Count),
                    "coursecode" => sortOrder == "desc"
                        ? courses.OrderByDescending(c => c.CourseCode)
                        : courses.OrderBy(c => c.CourseCode),
                    "coursename" => sortOrder == "desc"
                        ? courses.OrderByDescending(c => c.CourseName)
                        : courses.OrderBy(c => c.CourseName),
                    "description" => sortOrder == "desc"
                        ? courses.OrderByDescending(c => c.Description)
                        : courses.OrderBy(c => c.Description),
                    "credits" => sortOrder == "desc"
                        ? courses.OrderByDescending(c => c.Credits)
                        : courses.OrderBy(c => c.Credits),
                    "department" => sortOrder == "desc"
                        ? courses.OrderByDescending(c => c.Department)
                        : courses.OrderBy(c => c.Department),
                    "semester" => sortOrder == "desc"
                        ? courses.OrderByDescending(c => c.SemesterId)
                        : courses.OrderBy(c => c.SemesterId),
                    "enrollment" => sortOrder == "desc"
                        ? courses.OrderByDescending(c => c.CurrentEnrollment)
                        : courses.OrderBy(c => c.CurrentEnrollment),
                    "status" => sortOrder == "desc"
                        ? courses.OrderByDescending(c => c.IsActive)
                        : courses.OrderBy(c => c.IsActive),
                    _ => courses.OrderBy(c => c.CourseCode)
                };

                var courseList = await courses.ToListAsync();

                ViewData["CurrentFilter"] = searchString;
                ViewData["CurrentDepartment"] = department;
                ViewData["CurrentSemester"] = semester;
                ViewData["CurrentSort"] = sortBy;
                ViewData["CurrentOrder"] = sortOrder;

                return View(courseList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading courses");
                TempData["Error"] = "Error loading courses.";
                return View(new List<Course>());
            }
        }

        // GET: Courses/Create
        [HttpGet]
        public async Task<IActionResult> Create(int? semesterId, int? copyFromCourseId)
        {
            if (!HasPermission("Courses.Create"))
            {
                SetErrorMessage("You don't have permission to create courses.");
                return RedirectToAction("Index");
            }

            var course = new Course();

            if (semesterId.HasValue)
            {
                var semester = await _context.Semesters
                    .Include(s => s.Department)
                    .Include(s => s.Branch)
                    .Include(s => s.SubBranch)
                    .FirstOrDefaultAsync(s => s.Id == semesterId);
                ViewBag.ParentSemester = semester;
            }

            // If copying from existing course
            if (copyFromCourseId.HasValue)
            {
                var sourceCourse = await _context.Courses
                    .Include(c => c.Prerequisites)
                        .ThenInclude(p => p.PrerequisiteCourse)
                    .FirstOrDefaultAsync(c => c.Id == copyFromCourseId);

                if (sourceCourse != null)
                {
                    course.CourseCode = $"{sourceCourse.CourseCode}-COPY";
                    course.CourseName = $"{sourceCourse.CourseName} - Copy";
                    course.Description = sourceCourse.Description;
                    course.Credits = sourceCourse.Credits;
                    course.Department = sourceCourse.Department;
                    course.Semester = sourceCourse.Semester;
                    course.MaxStudents = sourceCourse.MaxStudents;
                    course.MinGPA = sourceCourse.MinGPA;
                    course.MinPassedHours = sourceCourse.MinPassedHours;
                    course.DepartmentId = sourceCourse.DepartmentId;
                    course.SemesterId = sourceCourse.SemesterId;

                    // FIX: Add null safety for prerequisites
                    course.SelectedPrerequisiteIds = sourceCourse.Prerequisites
                        .Where(p => p.PrerequisiteCourse != null)
                        .Select(p => p.PrerequisiteCourse!.Id)
                        .ToList();
                }
            }

            // ✅ NEW: Get all available courses for the selection dropdown
            var availableCourses = await _context.Courses
                .Where(c => c.IsActive)
                .OrderBy(c => c.CourseCode)
                .ToListAsync();

            ViewBag.AvailableCourses = availableCourses;

            await PopulateDropdowns();
            await PopulatePrerequisites(course);
            return View(course);
        }

        // POST: Courses/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Course course)
        {
            if (!HasPermission("Courses.Create"))
            {
                SetErrorMessage("You don't have permission to create courses.");
                return RedirectToAction("Index");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Check if course code already exists
                    var existingCourse = await _context.Courses
                        .AnyAsync(c => c.CourseCode == course.CourseCode && c.IsActive);

                    if (existingCourse)
                    {
                        ModelState.AddModelError("CourseCode", "A course with this code already exists.");
                        await PopulateDropdowns();
                        await PopulatePrerequisites(course);
                        return View(course);
                    }

                    // Add the course first
                    _context.Add(course);
                    await _context.SaveChangesAsync();

                    // Add prerequisites if any selected
                    if (course.SelectedPrerequisiteIds != null && course.SelectedPrerequisiteIds.Any())
                    {
                        foreach (var prerequisiteId in course.SelectedPrerequisiteIds)
                        {
                            var prerequisite = new CoursePrerequisite
                            {
                                CourseId = course.Id,
                                PrerequisiteCourseId = prerequisiteId,
                                IsRequired = true
                            };
                            _context.CoursePrerequisites.Add(prerequisite);
                        }
                        await _context.SaveChangesAsync();
                    }

                    TempData["SuccessMessage"] = $"Course '{course.CourseName}' created successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating course");
                    ModelState.AddModelError("", "An error occurred while creating the course.");
                }
            }

            await PopulateDropdowns();
            await PopulatePrerequisites(course);
            return View(course);
        }

        // GET: Courses/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {            
            try
            {
                var course = await _courseService.GetCourseByIdAsync(id);
                if (course == null)
                {
                    return NotFound();
                }

                var enrollments = await _courseService.GetCourseEnrollmentsAsync(id);
                ViewBag.Enrollments = enrollments;

                return View(course);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading course details");
                TempData["Error"] = "Error loading course details.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Courses/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (!HasPermission("Courses.Edit"))
            {
                SetErrorMessage("You don't have permission to edit courses.");
                return RedirectToAction("Index");
            }

            try
            {
                var course = await _context.Courses
                    .Include(c => c.Prerequisites)
                        .ThenInclude(p => p.PrerequisiteCourse)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (course == null)
                {
                    return NotFound();
                }

                // Load selected prerequisites
                course.SelectedPrerequisiteIds = course.Prerequisites
                    .Select(p => p.PrerequisiteCourseId)
                    .ToList();

                await LoadAvailablePrerequisites(course);
                await PopulateDropdowns();
                return View(course);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading course for edit");
                TempData["Error"] = "Error loading course.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Courses/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Course course)
        {
            if (!HasPermission("Courses.Edit"))
            {
                SetErrorMessage("You don't have permission to edit courses.");
                return RedirectToAction("Index");
            }

            if (id != course.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Get existing course with prerequisites
                    var existingCourse = await _context.Courses
                        .Include(c => c.Prerequisites)
                        .FirstOrDefaultAsync(c => c.Id == id);

                    if (existingCourse == null)
                    {
                        return NotFound();
                    }

                    // Update course properties
                    existingCourse.CourseCode = course.CourseCode;
                    existingCourse.CourseName = course.CourseName;
                    existingCourse.Description = course.Description;
                    existingCourse.Credits = course.Credits;
                    existingCourse.Department = course.Department;
                    existingCourse.DepartmentId = course.DepartmentId;
                    existingCourse.SemesterId = course.SemesterId;
                    existingCourse.GradeLevel = course.GradeLevel;
                    existingCourse.IsActive = course.IsActive;
                    existingCourse.MaxStudents = course.MaxStudents;
                    existingCourse.MinGPA = course.MinGPA;
                    existingCourse.MinPassedHours = course.MinPassedHours;
                    existingCourse.PrerequisitesString = course.PrerequisitesString;

                    // Update prerequisites
                    await UpdateCoursePrerequisites(existingCourse, course.SelectedPrerequisiteIds);

                    _context.Update(existingCourse);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = $"Course '{course.CourseName}' updated successfully!";
                    return RedirectToAction(nameof(Details), new { id = existingCourse.Id });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CourseExists(course.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating course {CourseId}", id);
                    TempData["ErrorMessage"] = $"An error occurred while updating the course: {ex.Message}";
                }
            }

            await PopulateDropdowns();
            await LoadAvailablePrerequisites(course);
            return View(course);
        }

        // GET: Courses/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (!HasPermission("Courses.Delete"))
            {
                SetErrorMessage("You don't have permission to delete courses.");
                return RedirectToAction("Index");
            }

            if (id == null)
            {
                return NotFound();
            }

            var course = await _context.Courses
                .Include(c => c.CourseDepartment)
                .Include(c => c.CourseSemester)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (course == null)
            {
                return NotFound();
            }

            // Check if course has enrollments
            var hasEnrollments = await _context.CourseEnrollments
                .AnyAsync(ce => ce.CourseId == id && ce.IsActive);

            ViewBag.HasEnrollments = hasEnrollments;

            return View(course);
        }

        // POST: Courses/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!HasPermission("Courses.Delete"))
            {
                SetErrorMessage("You don't have permission to delete courses.");
                return RedirectToAction("Index");
            }

            try
            {
                var course = await _context.Courses
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (course == null)
                {
                    TempData["ErrorMessage"] = "Course not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Check if course has enrollments
                var hasEnrollments = await _context.CourseEnrollments
                    .AnyAsync(ce => ce.CourseId == id && ce.IsActive);

                if (hasEnrollments)
                {
                    TempData["ErrorMessage"] = $"Cannot delete course '{course.CourseName}' because it has active enrollments. Please deactivate the course instead.";
                    return RedirectToAction(nameof(Index));
                }

                _context.Courses.Remove(course);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Course '{course.CourseName}' deleted successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting course {CourseId}", id);
                TempData["ErrorMessage"] = $"An error occurred while deleting the course: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Courses/Prerequisites/5
        [HttpGet]
        public async Task<IActionResult> Prerequisites(int id)
        {
            try
            {
                var course = await _courseService.GetCourseByIdAsync(id);
                if (course == null)
                {
                    return NotFound();
                }

                var prerequisites = await _courseService.GetCoursePrerequisitesAsync(id);
                var allCourses = await _courseService.GetAllCoursesAsync();
                var availablePrerequisites = allCourses.Where(c => c.Id != id).ToList();

                ViewBag.Course = course;
                ViewBag.Prerequisites = prerequisites;
                ViewBag.AvailableCourses = availablePrerequisites;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading prerequisites");
                TempData["Error"] = "Error loading prerequisites.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // POST: Courses/AddPrerequisite
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPrerequisite(int courseId, int prerequisiteCourseId, decimal? minGrade)
        {
            try
            {
                await _courseService.AddPrerequisiteAsync(courseId, prerequisiteCourseId, minGrade);
                TempData["Success"] = "Prerequisite added successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding prerequisite");
                TempData["Error"] = $"Error adding prerequisite: {ex.Message}";
            }

            return RedirectToAction(nameof(Prerequisites), new { id = courseId });
        }

        // POST: Courses/RemovePrerequisite
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemovePrerequisite(int prerequisiteId, int courseId)
        {
            try
            {
                await _courseService.RemovePrerequisiteAsync(prerequisiteId);
                TempData["Success"] = "Prerequisite removed successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing prerequisite");
                TempData["Error"] = $"Error removing prerequisite: {ex.Message}";
            }

            return RedirectToAction(nameof(Prerequisites), new { id = courseId });
        }

        // GET: Courses/Enroll/5
        [HttpGet]
        public async Task<IActionResult> Enroll(int id)
        {
            if (!HasPermission("Courses.Enroll"))
            {
                SetErrorMessage("You don't have permission to enroll students.");
                return RedirectToAction("Index");
            }

            try
            {
                var course = await _courseService.GetCourseByIdAsync(id);
                if (course == null)
                {
                    return NotFound();
                }

                var students = await _studentService.GetAllStudentsAsync();
                var enrolledStudentIds = (await _courseService.GetCourseEnrollmentsAsync(id))
                    .Where(e => e.IsActive)
                    .Select(e => e.StudentId)
                    .ToList();

                var availableStudents = students.Where(s => !enrolledStudentIds.Contains(s.Id)).ToList();

                ViewBag.AvailableStudents = availableStudents;
                return View(course);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading enrollment page");
                TempData["Error"] = "Error loading enrollment page.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Courses/EnrollStudent
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnrollStudent(int courseId, int[] studentIds)
        {
            if (!HasPermission("Courses.Enroll"))
            {
                SetErrorMessage("You don't have permission to enroll students.");
                return RedirectToAction("Index");
            }

            try
            {
                if (studentIds == null || !studentIds.Any())
                {
                    TempData["Error"] = "Please select at least one student to enroll.";
                    return RedirectToAction(nameof(Enroll), new { id = courseId });
                }

                var course = await _courseService.GetCourseByIdAsync(courseId);
                if (course == null)
                {
                    TempData["Error"] = "Course not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Check course capacity
                if (course.CurrentEnrollment + studentIds.Length > course.MaxStudents)
                {
                    TempData["Error"] = $"Cannot enroll {studentIds.Length} students. Course would exceed maximum capacity.";
                    return RedirectToAction(nameof(Enroll), new { id = courseId });
                }

                int successCount = 0;
                var failedEnrollments = new List<string>();

                foreach (var studentId in studentIds)
                {
                    try
                    {
                        // Check if student is already enrolled
                        var existingEnrollment = await _context.CourseEnrollments
                            .FirstOrDefaultAsync(ce => ce.CourseId == courseId && ce.StudentId == studentId && ce.IsActive);

                        if (existingEnrollment != null)
                        {
                            failedEnrollments.Add($"Student {studentId} is already enrolled");
                            continue;
                        }

                        // Check prerequisites
                        var canEnroll = await _courseService.CanStudentEnrollAsync(studentId, courseId);
                        if (!canEnroll)
                        {
                            var missingPrerequisites = await _courseService.GetMissingPrerequisitesAsync(studentId, courseId);
                            failedEnrollments.Add($"Student {studentId} missing prerequisites: {string.Join(", ", missingPrerequisites)}");
                            continue;
                        }

                        // Enroll student
                        await _courseService.EnrollStudentAsync(courseId, studentId);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error enrolling student {studentId} in course {courseId}");
                        failedEnrollments.Add($"Student {studentId}: {ex.Message}");
                    }
                }

                if (successCount > 0)
                {
                    TempData["Success"] = $"Successfully enrolled {successCount} student(s)!";
                }

                if (failedEnrollments.Any())
                {
                    TempData["Warning"] = $"{failedEnrollments.Count} enrollment(s) failed: {string.Join("; ", failedEnrollments.Take(5))}";
                }

                return RedirectToAction(nameof(Details), new { id = courseId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enrolling students");
                TempData["Error"] = $"Error enrolling students: {ex.Message}";
                return RedirectToAction(nameof(Enroll), new { id = courseId });
            }
        }

        // POST: Courses/UnenrollStudent
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnenrollStudent(int enrollmentId, int courseId)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            try
            {
                await _courseService.UnenrollStudentAsync(enrollmentId);
                TempData["Success"] = "Student unenrolled successfully!";
                return RedirectToAction(nameof(Details), new { id = courseId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unenrolling student");
                TempData["Error"] = $"Error unenrolling student: {ex.Message}";
                return RedirectToAction(nameof(Details), new { id = courseId });
            }
        }

        // GET: Courses/UpdateGrade/5
        [HttpGet]
        public async Task<IActionResult> UpdateGrade(int id)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            try
            {
                var enrollment = await _courseService.GetCourseEnrollmentsAsync(id);
                var specificEnrollment = enrollment.FirstOrDefault(e => e.Id == id);

                if (specificEnrollment == null)
                {
                    return NotFound();
                }

                return View(specificEnrollment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading grade update page");
                TempData["Error"] = "Error loading grade update page.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Courses/UpdateGrade
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateGrade(int enrollmentId, decimal grade, string gradeLetter)
        {
            if (!HasPermission("Grading.UpdateGrade"))
            {
                SetErrorMessage("You don't have permission to UpdateGrades.");
                return RedirectToAction("Index");
            }

            try
            {
                await _courseService.UpdateGradeAsync(enrollmentId, grade, gradeLetter);
                TempData["Success"] = "Grade updated successfully!";

                var enrollment = await _courseService.GetCourseEnrollmentsAsync(enrollmentId);
                var courseId = enrollment.FirstOrDefault()?.CourseId;

                return RedirectToAction(nameof(Details), new { id = courseId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating grade");
                TempData["Error"] = $"Error updating grade: {ex.Message}";
                return RedirectToAction(nameof(UpdateGrade), new { id = enrollmentId });
            }
        }

        // GET: Courses/Export
        [HttpGet]
        public async Task<IActionResult> Export()
        {
            if (!HasPermission("Courses.Export"))
            {
                SetErrorMessage("You don't have permission to export courses.");
                return RedirectToAction("Index");
            }

            try
            {
                var fileBytes = await _courseService.ExportCoursesToExcelAsync();
                return File(fileBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Courses_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting courses");
                TempData["Error"] = $"Export failed: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Courses/ExportPdf
        [HttpGet]
        public async Task<IActionResult> ExportPdf()
        {
            if (!HasPermission("Courses.Export"))
            {
                SetErrorMessage("You don't have permission to export courses.");
                return RedirectToAction("Index");
            }

            try
            {
                var fileBytes = await _courseService.ExportCoursesToPdfAsync();
                return File(fileBytes,
                    "application/pdf",
                    $"Courses_Report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating PDF report");
                TempData["Error"] = $"PDF generation failed: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Courses/ExportCoursePdf/5
        [HttpGet]
        public async Task<IActionResult> ExportCoursePdf(int id)
        {
            if (!HasPermission("Courses.Export"))
            {
                SetErrorMessage("You don't have permission to export courses.");
                return RedirectToAction("Index");
            }

            try
            {
                var fileBytes = await _courseService.ExportCourseDetailsToPdfAsync(id);
                var course = await _courseService.GetCourseByIdAsync(id);
                var fileName = $"Course_{course?.CourseCode}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

                return File(fileBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating course PDF");
                TempData["Error"] = $"PDF generation failed: {ex.Message}";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // GET: Courses/ExportEnrollments/5
        [HttpGet]
        public async Task<IActionResult> ExportEnrollments(int id)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            try
            {
                var fileBytes = await _courseService.ExportCourseEnrollmentsToExcelAsync(id);
                var course = await _courseService.GetCourseByIdAsync(id);
                return File(fileBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Enrollments_{course?.CourseCode}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting enrollments");
                TempData["Error"] = $"Export failed: {ex.Message}";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // GET: Courses/SelectFromExisting
        [HttpGet]
        public async Task<IActionResult> SelectFromExisting(int? semesterId)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            var courses = await _context.Courses
                .Include(c => c.CourseDepartment)
                .Include(c => c.CourseSemester)
                .Where(c => c.IsActive)
                .OrderBy(c => c.CourseCode)
                .ToListAsync();

            ViewBag.SemesterId = semesterId;
            return View(courses);
        }

        // POST: Courses/AddToSemester
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToSemester(int courseId, int semesterId)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            try
            {
                var course = await _context.Courses
                    .Include(c => c.Prerequisites)
                    .ThenInclude(p => p.PrerequisiteCourse)
                    .FirstOrDefaultAsync(c => c.Id == courseId);

                var semester = await _context.Semesters.FindAsync(semesterId);

                if (course == null || semester == null)
                {
                    TempData["ErrorMessage"] = "Course or semester not found.";
                    return RedirectToAction(nameof(SelectFromExisting), new { semesterId });
                }

                // Generate a unique course code for the copy
                var baseCourseCode = course.CourseCode;
                var newCourseCode = $"{baseCourseCode}-{semester.Id}";
                var counter = 1;

                // Check if the course code already exists and find a unique one
                while (await _context.Courses.AnyAsync(c => c.CourseCode == newCourseCode && c.IsActive))
                {
                    newCourseCode = $"{baseCourseCode}-{semester.Id}-{counter}";
                    counter++;
                }

                // Create a copy of the course for the semester with unique course code
                var newCourse = new Course
                {
                    CourseCode = newCourseCode,
                    CourseName = $"{course.CourseName} - {semester.Name}",
                    Description = course.Description,
                    Credits = course.Credits,
                    Department = course.Department,
                    Semester = course.Semester,
                    IsActive = true,
                    MaxStudents = course.MaxStudents,
                    MinGPA = course.MinGPA,
                    MinPassedHours = course.MinPassedHours,
                    DepartmentId = course.DepartmentId,
                    SemesterId = semesterId,
                    CreatedDate = DateTime.Now
                };

                _context.Courses.Add(newCourse);
                await _context.SaveChangesAsync();

                // Copy prerequisites if they exist
                if (course.Prerequisites != null && course.Prerequisites.Any())
                {
                    foreach (var prerequisite in course.Prerequisites.Where(p => p.PrerequisiteCourse != null))
                    {
                        var newPrerequisite = new CoursePrerequisite
                        {
                            CourseId = newCourse.Id,
                            PrerequisiteCourseId = prerequisite.PrerequisiteCourseId,
                            IsRequired = prerequisite.IsRequired,
                            MinGrade = prerequisite.MinGrade
                        };
                        _context.CoursePrerequisites.Add(newPrerequisite);
                    }
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = $"Course '{course.CourseName}' successfully added to semester '{semester.Name}' with course code '{newCourseCode}'.";
                return RedirectToAction("Details", "Semesters", new { id = semesterId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding course to semester");
                TempData["ErrorMessage"] = "An error occurred while adding the course to the semester.";
                return RedirectToAction(nameof(SelectFromExisting), new { semesterId });
            }
        }

        // POST: Courses/BulkAddToSemester
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkAddToSemester(int semesterId, string courseIds)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            try
            {
                if (string.IsNullOrEmpty(courseIds))
                {
                    TempData["ErrorMessage"] = "No courses selected.";
                    return RedirectToAction("SelectFromExisting", new { semesterId });
                }

                var courseIdArray = courseIds.Split(',').Select(int.Parse).ToArray();
                var semester = await _context.Semesters.FindAsync(semesterId);

                if (semester == null)
                {
                    TempData["ErrorMessage"] = "Semester not found.";
                    return RedirectToAction("SelectFromExisting", new { semesterId });
                }

                int successCount = 0;
                var addedCourses = new List<string>();

                foreach (var courseId in courseIdArray)
                {
                    var course = await _context.Courses
                        .Include(c => c.Prerequisites)
                        .ThenInclude(p => p.PrerequisiteCourse)
                        .FirstOrDefaultAsync(c => c.Id == courseId);

                    if (course != null)
                    {
                        // Generate unique course code
                        var baseCourseCode = course.CourseCode;
                        var newCourseCode = $"{baseCourseCode}-{semester.Id}";
                        var counter = 1;

                        while (await _context.Courses.AnyAsync(c => c.CourseCode == newCourseCode && c.IsActive))
                        {
                            newCourseCode = $"{baseCourseCode}-{semester.Id}-{counter}";
                            counter++;
                        }

                        // Create course copy
                        var newCourse = new Course
                        {
                            CourseCode = newCourseCode,
                            CourseName = $"{course.CourseName} - {semester.Name}",
                            Description = course.Description,
                            Credits = course.Credits,
                            Department = course.Department,
                            Semester = course.Semester,
                            IsActive = true,
                            MaxStudents = course.MaxStudents,
                            MinGPA = course.MinGPA,
                            MinPassedHours = course.MinPassedHours,
                            DepartmentId = course.DepartmentId,
                            SemesterId = semesterId,
                            CreatedDate = DateTime.Now
                        };

                        _context.Courses.Add(newCourse);
                        await _context.SaveChangesAsync();

                        // Copy prerequisites
                        if (course.Prerequisites != null && course.Prerequisites.Any())
                        {
                            foreach (var prerequisite in course.Prerequisites.Where(p => p.PrerequisiteCourse != null))
                            {
                                var newPrerequisite = new CoursePrerequisite
                                {
                                    CourseId = newCourse.Id,
                                    PrerequisiteCourseId = prerequisite.PrerequisiteCourseId,
                                    IsRequired = prerequisite.IsRequired,
                                    MinGrade = prerequisite.MinGrade
                                };
                                _context.CoursePrerequisites.Add(newPrerequisite);
                            }
                            await _context.SaveChangesAsync();
                        }

                        successCount++;
                        addedCourses.Add($"{course.CourseCode} → {newCourseCode}");
                    }
                }

                if (successCount > 0)
                {
                    TempData["SuccessMessage"] = $"Successfully added {successCount} courses to semester '{semester.Name}'.";
                    if (addedCourses.Any())
                    {
                        TempData["AddedCourses"] = string.Join("<br>", addedCourses);
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "No courses were added to the semester.";
                }

                return RedirectToAction("Details", "Semesters", new { id = semesterId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk adding courses to semester");
                TempData["ErrorMessage"] = "An error occurred while adding courses to the semester.";
                return RedirectToAction("SelectFromExisting", new { semesterId });
            }
        }

        // POST: Courses/ExportSelected
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportSelected(int[] selectedCourses)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            try
            {
                if (selectedCourses == null || selectedCourses.Length == 0)
                {
                    TempData["Error"] = "No courses selected for export.";
                    return RedirectToAction(nameof(Index));
                }

                var fileBytes = await _courseService.ExportSelectedCoursesAsync(selectedCourses);

                if (fileBytes == null || fileBytes.Length == 0)
                {
                    TempData["Error"] = "Export failed: No data generated";
                    return RedirectToAction(nameof(Index));
                }

                return File(fileBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Selected_Courses_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Export failed: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Courses/BulkDeleteConfirmation
        [HttpGet]
        public async Task<IActionResult> BulkDeleteConfirmation([FromQuery] List<int> selectedCourses)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            try
            {
                if (selectedCourses == null || !selectedCourses.Any())
                {
                    TempData["ErrorMessage"] = "No courses selected for deletion.";
                    return RedirectToAction(nameof(Index));
                }

                var courses = await _context.Courses
                    .Where(c => selectedCourses.Contains(c.Id))
                    .ToListAsync();

                // Check for courses with enrollments
                var coursesWithEnrollments = new List<Course>();
                foreach (var course in courses)
                {
                    var hasEnrollments = await _context.CourseEnrollments
                        .AnyAsync(ce => ce.CourseId == course.Id && ce.IsActive);

                    if (hasEnrollments)
                    {
                        coursesWithEnrollments.Add(course);
                    }
                }

                ViewBag.CoursesWithEnrollments = coursesWithEnrollments;
                ViewBag.SelectedCourseIds = selectedCourses;

                return View(courses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading bulk delete confirmation");
                TempData["ErrorMessage"] = "Error loading delete confirmation.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Courses/BulkDeleteConfirmed
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkDeleteConfirmed(List<int> selectedCourses)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            try
            {
                if (selectedCourses == null || !selectedCourses.Any())
                {
                    TempData["ErrorMessage"] = "No courses selected for deletion.";
                    return RedirectToAction(nameof(Index));
                }

                var courses = await _context.Courses
                    .Where(c => selectedCourses.Contains(c.Id))
                    .ToListAsync();

                // Check for courses with enrollments
                var coursesWithEnrollments = courses.Where(c =>
                    _context.CourseEnrollments.Any(ce => ce.CourseId == c.Id && ce.IsActive)
                ).ToList();

                if (coursesWithEnrollments.Any())
                {
                    TempData["ErrorMessage"] = $"Cannot delete {coursesWithEnrollments.Count} course(s) with active enrollments.";
                    return RedirectToAction(nameof(BulkDeleteConfirmation), new { selectedCourses });
                }

                int deletedCount = 0;
                foreach (var course in courses)
                {
                    try
                    {
                        await _courseService.DeleteCourseAsync(course.Id);
                        deletedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error deleting course {CourseId}", course.Id);
                    }
                }

                TempData["SuccessMessage"] = $"Successfully deleted {deletedCount} course(s).";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk course deletion");
                TempData["ErrorMessage"] = $"An error occurred while deleting courses: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Courses/DeleteAllConfirmation
        [HttpGet]
        public IActionResult DeleteAllConfirmation()
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            var totalCourses = _context.Courses.Count();
            ViewBag.TotalCourses = totalCourses;
            return View();
        }

        // POST: Courses/DeleteAllConfirmed
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAllConfirmed()
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            try
            {
                var totalCourses = await _context.Courses.CountAsync();
                await _courseService.DeleteAllCoursesAsync();

                TempData["SuccessMessage"] = $"Successfully deleted all {totalCourses} courses.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting all courses");
                TempData["ErrorMessage"] = $"Failed to delete all courses: {ex.Message}";
                return RedirectToAction(nameof(DeleteAllConfirmation));
            }
        }

        // POST: Courses/DeleteMultiple
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMultiple([FromBody] List<int> selectedCourses)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            try
            {
                if (selectedCourses == null || !selectedCourses.Any())
                {
                    return Json(new { success = false, message = "No courses selected for deletion." });
                }

                var courses = await _context.Courses
                    .Where(c => selectedCourses.Contains(c.Id))
                    .ToListAsync();

                // Check for courses with enrollments
                var coursesWithEnrollments = new List<string>();
                foreach (var course in courses)
                {
                    var hasEnrollments = await _context.CourseEnrollments
                        .AnyAsync(ce => ce.CourseId == course.Id && ce.IsActive);

                    if (hasEnrollments)
                    {
                        coursesWithEnrollments.Add(course.CourseName);
                    }
                }

                if (coursesWithEnrollments.Any())
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Cannot delete {coursesWithEnrollments.Count} course(s) with active enrollments."
                    });
                }

                _context.Courses.RemoveRange(courses);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"{courses.Count} course(s) deleted successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting multiple courses");
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

        // GET: Semesters/Dashboard
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var semesters = await _context.Semesters
                .Include(s => s.Department)
                .Include(s => s.Branch)
                .Include(s => s.SubBranch)
                .Where(s => s.IsActive)
                .OrderByDescending(s => s.AcademicYear)
                .ThenBy(s => s.StartDate)
                .ToListAsync();

            return View(semesters);
        }

        [HttpGet]
        public async Task<IActionResult> GetCoursesBySemester(int semesterId)
        {
            try
            {
                var courses = await _context.Courses
                    .Where(c => c.SemesterId == semesterId && c.IsActive)
                    .Include(c => c.Prerequisites)
                        .ThenInclude(p => p.PrerequisiteCourse)
                    .Select(c => new
                    {
                        id = c.Id,
                        courseName = c.CourseName,
                        courseCode = c.CourseCode,
                        credits = c.Credits,
                        description = c.Description,
                        isActive = c.IsActive,
                        prerequisites = string.Join(", ", c.Prerequisites
                            .Where(p => p.PrerequisiteCourse != null && !string.IsNullOrEmpty(p.PrerequisiteCourse.CourseCode))
                            .Select(p => p.PrerequisiteCourse!.CourseCode!))
                    })
                    .ToListAsync();

                return Json(courses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading courses for semester {SemesterId}", semesterId);
                return Json(new List<object>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailableCoursesForSemester(int semesterId)
        {
            try
            {
                var availableCourses = await _context.Courses
                    .Where(c => c.SemesterId != semesterId && c.IsActive)
                    .Select(c => new
                    {
                        id = c.Id,
                        courseCode = c.CourseCode,
                        courseName = c.CourseName,
                        credits = c.Credits,
                        description = c.Description
                    })
                    .ToListAsync();

                return Json(availableCourses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading available courses for semester {SemesterId}", semesterId);
                return Json(new List<object>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCourseEnrollments(int courseId, int semesterId)
        {
            try
            {
                var enrollments = await _context.CourseEnrollments
                    .Include(ce => ce.Student)
                    .Where(ce => ce.CourseId == courseId && ce.SemesterId == semesterId && ce.IsActive)
                    .Select(ce => new
                    {
                        id = ce.Id,
                        studentId = ce.StudentId,
                        studentCode = ce.Student != null ? ce.Student.StudentId : "N/A",
                        studentName = ce.Student != null ? ce.Student.Name : "Unknown Student",
                        status = ce.EnrollmentStatus.ToString(),
                        enrollmentDate = ce.CreatedDate
                    })
                    .ToListAsync();

                return Json(enrollments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting enrollments for course {CourseId}", courseId);
                return Json(new List<object>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> VerifyExists(int id)
        {
            var exists = await _context.Courses.AnyAsync(c => c.Id == id);
            return Json(exists);
        }

        [HttpGet]
        public async Task<IActionResult> GetCourseData(int id)
        {            
            var course = await _context.Courses
                .Include(c => c.CourseSemester)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null)
            {
                return NotFound();
            }

            var courseData = new Dictionary<string, object>
            {
                ["Id"] = course.Id,
                ["CourseCode"] = course.CourseCode,
                ["CourseName"] = course.CourseName,
                ["SemesterId"] = course.SemesterId?.ToString() ?? "null",
                ["SemesterName"] = course.CourseSemester?.Name ?? "Unassigned",
                ["Department"] = course.Department ?? string.Empty,
                ["Credits"] = course.Credits
            };

            return Json(courseData);
        }

        // PRIVATE HELPER METHODS

        [HttpGet]
        private async Task PopulateDropdowns()
        {
            ViewBag.Departments = await _context.Departments
                .Where(d => d.IsActive)
                .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name })
                .ToListAsync();

            ViewBag.Semesters = await _context.Semesters
                .Where(s => s.IsActive)
                .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name })
                .ToListAsync();
        }

        [HttpGet]
        private async Task PopulatePrerequisites(Course course)
        {
            // Get all active courses except the current one (if it exists)
            var allCourses = await _context.Courses
                .Where(c => c.IsActive && c.Id != course.Id)
                .OrderBy(c => c.CourseCode)
                .ToListAsync();

            course.AvailablePrerequisites = allCourses;

            // If we have selected prerequisites, get their codes for display
            if (course.SelectedPrerequisiteIds != null && course.SelectedPrerequisiteIds.Any())
            {
                var prerequisiteCourses = await _context.Courses
                    .Where(c => course.SelectedPrerequisiteIds.Contains(c.Id))
                    .Select(c => c.CourseCode)
                    .ToListAsync();

                course.PrerequisiteCodes = string.Join(", ", prerequisiteCourses);
            }
        }

        [HttpGet]
        private async Task LoadAvailablePrerequisites(Course course)
        {
            var allCourses = await _context.Courses
                .Where(c => c.IsActive && c.Id != course.Id)
                .OrderBy(c => c.CourseCode)
                .ToListAsync();

            ViewBag.AvailableCourses = allCourses;
        }

        private async Task UpdateCoursePrerequisites(Course existingCourse, List<int> selectedPrerequisiteIds)
        {
            // Clear existing prerequisites
            var existingPrerequisites = await _context.CoursePrerequisites
                .Where(cp => cp.CourseId == existingCourse.Id)
                .ToListAsync();

            _context.CoursePrerequisites.RemoveRange(existingPrerequisites);
            await _context.SaveChangesAsync();

            // Add new prerequisites
            if (selectedPrerequisiteIds != null && selectedPrerequisiteIds.Any())
            {
                foreach (var prerequisiteId in selectedPrerequisiteIds)
                {
                    // Prevent self-referencing
                    if (prerequisiteId != existingCourse.Id)
                    {
                        var prerequisite = new CoursePrerequisite
                        {
                            CourseId = existingCourse.Id,
                            PrerequisiteCourseId = prerequisiteId,
                            IsRequired = true
                        };
                        _context.CoursePrerequisites.Add(prerequisite);
                    }
                }
                await _context.SaveChangesAsync();
            }
        }

        private bool CourseExists(int id)
        {
            return _context.Courses.Any(e => e.Id == id);
        }

        // Helper method to check for unique constraint violations
        private bool IsUniqueConstraintViolation(DbUpdateException ex)
        {
            return ex.InnerException is SqlException sqlException &&
                   (sqlException.Number == 2601 || sqlException.Number == 2627);
        }

        // Helper method to delete course using raw SQL
        [HttpPost]
        private async Task DeleteCourseWithRawSql(int courseId)
        {
            var sql = @"
        BEGIN TRANSACTION;
        TRY
            -- Delete from CoursePrerequisites where this course is the prerequisite
            DELETE FROM CoursePrerequisites WHERE PrerequisiteCourseId = @courseId;
            
            -- Delete prerequisites for this course
            DELETE FROM CoursePrerequisites WHERE CourseId = @courseId;
            
            -- Delete course enrollments
            DELETE FROM CourseEnrollments WHERE CourseId = @courseId;
            
            -- Finally delete the course
            DELETE FROM Courses WHERE Id = @courseId;
            
            COMMIT TRANSACTION;
        CATCH
            ROLLBACK TRANSACTION;
            THROW;
        END CATCH";

            await _context.Database.ExecuteSqlRawAsync(sql, new SqlParameter("@courseId", courseId));
        }

        // Helper method to delete all courses using raw SQL
        [HttpPost]
        private async Task DeleteAllCoursesWithRawSql()
        {
            var sql = @"
        BEGIN TRANSACTION;
        TRY
            -- Delete all course prerequisites
            DELETE FROM CoursePrerequisites;
            
            -- Delete all course enrollments
            DELETE FROM CourseEnrollments;
            
            -- Delete all courses
            DELETE FROM Courses;
            
            COMMIT TRANSACTION;
        CATCH
            ROLLBACK TRANSACTION;
            THROW;
        END CATCH";

            await _context.Database.ExecuteSqlRawAsync(sql);
        }

        [HttpPost]
        public async Task<IActionResult> CreateDefaultSemester()
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            try
            {
                // Check if any semester exists
                var anySemester = await _context.Semesters.AnyAsync();

                if (!anySemester)
                {
                    var defaultSemester = new Semester
                    {
                        Name = "Default Semester",
                        SemesterType = "Fall",
                        AcademicYear = DateTime.Now.Year,
                        StartDate = DateTime.Now,
                        EndDate = DateTime.Now.AddMonths(4),
                        RegistrationStartDate = DateTime.Now.AddDays(-7),
                        RegistrationEndDate = DateTime.Now.AddDays(30),
                        IsActive = true,
                        IsCurrent = true,
                        IsRegistrationOpen = true
                    };

                    _context.Semesters.Add(defaultSemester);
                    await _context.SaveChangesAsync();

                    return Json(new
                    {
                        success = true,
                        message = "Created default semester",
                        semester = new { defaultSemester.Id, defaultSemester.Name }
                    });
                }
                else
                {
                    var existingSemester = await _context.Semesters.FirstAsync();
                    return Json(new
                    {
                        success = true,
                        message = "Semester already exists",
                        semester = new { existingSemester.Id, existingSemester.Name }
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message,
                    inner = ex.InnerException?.Message
                });
            }
        }

        [HttpGet]
        public IActionResult Debug()
        {
            var actions = new List<string>();

            // List all public methods that might be causing conflicts
            var methods = typeof(CoursesController).GetMethods()
                .Where(m => m.IsPublic && !m.IsSpecialName)
                .Select(m => m.Name)
                .Distinct()
                .ToList();

            return Json(new
            {
                PublicMethods = methods,
                Message = "Check for duplicate method names above"
            });
        }

        [HttpGet("DebugRoutes")]
        public IActionResult DebugRoutes()
        {
            var routes = new
            {
                Delete_GET = Url.Action("Delete", new { id = 1 }),
                DeleteConfirmed_POST = Url.Action("DeleteConfirmed", new { id = 1 }),
                Current_URL = $"{Request.Scheme}://{Request.Host}{Request.Path}{Request.QueryString}"
            };

            return Json(routes);
        }
    }
}