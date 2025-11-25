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
    public class CoursesController : Controller
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

<<<<<<< HEAD
=======
        // GET: Courses/Import
        [HttpGet]
        //[Route("Import")]
        public IActionResult Import()
        {
            return View();
        }

        // POST: Courses/Import
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile file)
        {
            try
            {
                Console.WriteLine($"=== IMPORT STARTED ===");
                Console.WriteLine($"File: {file?.FileName}, Size: {file?.Length}");

                if (file == null || file.Length == 0)
                {
                    ViewBag.Error = "Please select a file.";
                    return View();
                }

                var extension = Path.GetExtension(file.FileName).ToLower();
                if (extension != ".xlsx" && extension != ".xls")
                {
                    ViewBag.Error = "Please select an Excel file (.xlsx or .xls).";
                    return View();
                }

                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                stream.Position = 0;

                Console.WriteLine($"Calling AnalyzeExcelImportAsync...");

                // Use await since AnalyzeExcelImportAsync now returns Task<ImportResult>
                var analysisResult = await _courseService.AnalyzeExcelImportAsync(stream);

                Console.WriteLine($"Analysis Result - Success: {analysisResult.Success}, Message: {analysisResult.Message}");
                Console.WriteLine($"Valid Courses: {analysisResult.ValidCourses?.Count}, Invalid Courses: {analysisResult.InvalidCourses?.Count}");

                if (analysisResult.Success)
                {
                    // Store in session for review
                    HttpContext.Session.SetString("ImportAnalysis", System.Text.Json.JsonSerializer.Serialize(analysisResult));
                    Console.WriteLine($"Redirecting to ImportReview...");
                    return RedirectToAction("ImportReview");
                }

                ViewBag.Error = analysisResult.Message;
                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"IMPORT ERROR: {ex}");
                ViewBag.Error = $"Import failed: {ex.Message}";
                return View();
            }
        }

>>>>>>> 2510eef0503cd1f13788ed842e48bf72c263f9b3
        // GET: Courses/Export
        [HttpGet]
        public async Task<IActionResult> Export()
        {
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
<<<<<<< HEAD
=======
        [HttpGet]
        //[Route("PopulateDropdowns")]
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
        //[Route("PopulatePrerequisites")]
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

        // Helper method to check for unique constraint violations
        [HttpGet]
        //[Route("IsUniqueConstraintViolation")]
        private bool IsUniqueConstraintViolation(DbUpdateException ex)
        {
            return ex.InnerException is SqlException sqlException &&
                   (sqlException.Number == 2601 || sqlException.Number == 2627);
        }

        ////////////
        ///

        // GET: Courses/ImportReview
        //[HttpGet]
        //public IActionResult ImportReview()
        //{
        //    try
        //    {
        //        var analysisJson = HttpContext.Session.GetString("ImportAnalysis");
        //        if (string.IsNullOrEmpty(analysisJson))
        //        {
        //            TempData["Error"] = "Import session expired. Please upload the file again.";
        //            return RedirectToAction(nameof(Import));
        //        }

        //        var analysisResult = System.Text.Json.JsonSerializer.Deserialize<ImportResult>(analysisJson);
        //        if (analysisResult == null)
        //        {
        //            TempData["Error"] = "Invalid import data.";
        //            return RedirectToAction(nameof(Import));
        //        }

        //        Console.WriteLine($"ImportReview - Valid: {analysisResult.ValidCourses?.Count}, Invalid: {analysisResult.InvalidCourses?.Count}");

        //        // Add serial numbers
        //        if (analysisResult.ValidCourses != null)
        //        {
        //            int serial = 1;
        //            foreach (var course in analysisResult.ValidCourses)
        //            {
        //                course.SerialNumber = serial++;
        //            }
        //        }

        //        var viewModel = new ImportReviewViewModel
        //        {
        //            ImportResult = analysisResult,
        //            ImportSettings = new ImportSettings()
        //        };

        //        return View(viewModel);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"IMPORT REVIEW ERROR: {ex}");
        //        TempData["Error"] = "Error loading import preview.";
        //        return RedirectToAction(nameof(Import));
        //    }
        //}

        [HttpGet]
        public async Task<IActionResult> ImportReview()
        {
            try
            {
                var analysisJson = HttpContext.Session.GetString("ImportAnalysis");
                if (string.IsNullOrEmpty(analysisJson))
                {
                    TempData["Error"] = "Import session expired. Please upload the file again.";
                    return RedirectToAction(nameof(Import));
                }

                var analysisResult = System.Text.Json.JsonSerializer.Deserialize<ImportResult>(analysisJson);
                if (analysisResult == null || !analysisResult.Success)
                {
                    TempData["Error"] = "Invalid import data.";
                    return RedirectToAction(nameof(Import));
                }

                // Get existing semester IDs for validation
                var existingSemesterIds = await _context.Semesters
                    .Select(s => s.Id)
                    .ToListAsync();

                // Get semester IDs from import (with proper nullable handling)
                var semesterIdsInImport = GetSemesterIdsFromImport(analysisResult);

                // Find missing semester IDs
                var missingSemesterIds = semesterIdsInImport
                    .Where(id => !existingSemesterIds.Contains(id))
                    .ToList();

                var viewModel = new ImportReviewViewModel
                {
                    ImportResult = analysisResult,
                    ImportSettings = new ImportSettings(),
                    MissingSemesterIds = missingSemesterIds,
                    ExistingSemesterIds = existingSemesterIds
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading import review: {ex.Message}";
                return RedirectToAction(nameof(Import));
            }
        }


        //// POST: Courses/ImportExecute
        //[HttpPost]
        ////[HttpPost("ImportExecute")]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> ImportExecute(ImportSettings settings)
        //{
        //    try
        //    {
        //        var analysisJson = HttpContext.Session.GetString("ImportAnalysis");
        //        if (string.IsNullOrEmpty(analysisJson))
        //        {
        //            TempData["Error"] = "Import session expired. Please upload the file again.";
        //            return RedirectToAction(nameof(Import));
        //        }

        //        var analysisResult = System.Text.Json.JsonSerializer.Deserialize<ImportResult>(analysisJson);
        //        if (analysisResult == null || !analysisResult.Success)
        //        {
        //            TempData["Error"] = "Invalid import data.";
        //            return RedirectToAction(nameof(Import));
        //        }

        //        var importResult = await _courseService.ExecuteImportAsync(analysisResult, settings);

        //        // Clean up session data
        //        HttpContext.Session.Remove("ImportAnalysis");
        //        HttpContext.Session.Remove("FileName");

        //        // Store import results for display on Index page
        //        if (importResult.Success)
        //        {
        //            TempData["ImportResult"] = importResult.Message;
        //            TempData["ImportedCount"] = importResult.ImportedCount;
        //            TempData["ErrorCount"] = importResult.ErrorCount;
        //        }

        //        TempData[importResult.Success ? "Success" : "Error"] = importResult.Message;
        //        return RedirectToAction(nameof(Index));
        //    }
        //    catch (Exception ex)
        //    {
        //        TempData["Error"] = $"Import execution failed: {ex.Message}";
        //        return RedirectToAction(nameof(Import));
        //    }
        //}




        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> ImportExecute(ImportSettings settings, ImportOptions options)
        //{
        //    try
        //    {
        //        var analysisJson = HttpContext.Session.GetString("ImportAnalysis");
        //        if (string.IsNullOrEmpty(analysisJson))
        //        {
        //            TempData["Error"] = "Import session expired. Please upload the file again.";
        //            return RedirectToAction(nameof(Import));
        //        }

        //        var analysisResult = System.Text.Json.JsonSerializer.Deserialize<ImportResult>(analysisJson);
        //        if (analysisResult == null || !analysisResult.Success)
        //        {
        //            TempData["Error"] = "Invalid import data.";
        //            return RedirectToAction(nameof(Import));
        //        }

        //        // Handle semester creation based on options
        //        if (options.SemesterCreationMode == "auto")
        //        {
        //            await CreateMissingSemesters(analysisResult.ValidCourses);
        //        }
        //        else if (options.SemesterCreationMode == "ask" && options.SemestersToCreate.Any())
        //        {
        //            await CreateSpecificSemesters(options.SemestersToCreate);
        //        }
        //        else if (options.SemesterCreationMode == "ignore")
        //        {
        //            // Remove semester assignments for non-existent semesters
        //            await RemoveInvalidSemesterAssignments(analysisResult.ValidCourses);
        //        }

        //        var importResult = await _courseService.ExecuteImportAsync(analysisResult, settings);

        //        // Clean up session data
        //        HttpContext.Session.Remove("ImportAnalysis");

        //        if (importResult.Success)
        //        {
        //            TempData["ImportResult"] = importResult.Message;
        //            TempData["ImportedCount"] = importResult.ImportedCount;
        //            TempData["ErrorCount"] = importResult.ErrorCount;
        //        }

        //        TempData[importResult.Success ? "Success" : "Error"] = importResult.Message;
        //        return RedirectToAction(nameof(Index));
        //    }
        //    catch (Exception ex)
        //    {
        //        TempData["Error"] = $"Import execution failed: {ex.Message}";
        //        return RedirectToAction(nameof(Import));
        //    }
        //}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportExecuteWithOptions(ImportSettings settings, ImportOptions options)
        {
            try
            {
                var analysisJson = HttpContext.Session.GetString("ImportAnalysis");
                if (string.IsNullOrEmpty(analysisJson))
                {
                    TempData["Error"] = "Import session expired. Please upload the file again.";
                    return RedirectToAction(nameof(Import));
                }

                var analysisResult = System.Text.Json.JsonSerializer.Deserialize<ImportResult>(analysisJson);
                if (analysisResult == null || !analysisResult.Success)
                {
                    TempData["Error"] = "Invalid import data.";
                    return RedirectToAction(nameof(Import));
                }

                // Handle semester creation based on options
                if (options.SemesterCreationMode == "auto")
                {
                    await CreateMissingSemesters(analysisResult.ValidCourses);
                }
                else if (options.SemesterCreationMode == "ask" && options.SemestersToCreate.Any())
                {
                    await CreateSpecificSemesters(options.SemestersToCreate);
                }
                else if (options.SemesterCreationMode == "ignore")
                {
                    await RemoveInvalidSemesterAssignments(analysisResult.ValidCourses);
                }

                // Use the semester-preserving import logic
                var importResult = await ExecuteImportWithSemesterPreservation(analysisResult, settings);

                // Clean up session data
                HttpContext.Session.Remove("ImportAnalysis");

                if (importResult.Success)
                {
                    TempData["ImportResult"] = importResult.Message;
                    TempData["ImportedCount"] = importResult.ImportedCount;
                    TempData["ErrorCount"] = importResult.ErrorCount;
                }

                TempData[importResult.Success ? "Success" : "Error"] = importResult.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException dbEx)
            {
                var innerMessage = dbEx.InnerException?.Message ?? "No inner exception";
                var fullError = $"Database constraint violation: {dbEx.Message}. Inner: {innerMessage}";

                _logger.LogError(dbEx, "Database constraint violation during import");
                TempData["Error"] = $"Import failed: {fullError}";
                return RedirectToAction(nameof(Import));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during import execution");
                TempData["Error"] = $"Import execution failed: {ex.Message}";
                return RedirectToAction(nameof(Import));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportExecute(ImportSettings settings)
        {
            try
            {
                var analysisJson = HttpContext.Session.GetString("ImportAnalysis");
                if (string.IsNullOrEmpty(analysisJson))
                {
                    TempData["Error"] = "Import session expired. Please upload the file again.";
                    return RedirectToAction(nameof(Import));
                }

                var analysisResult = System.Text.Json.JsonSerializer.Deserialize<ImportResult>(analysisJson);
                if (analysisResult == null || !analysisResult.Success)
                {
                    TempData["Error"] = "Invalid import data.";
                    return RedirectToAction(nameof(Import));
                }

                // Use the semester-preserving import logic (no semester options)
                var importResult = await ExecuteImportWithSemesterPreservation(analysisResult, settings);

                // Clean up session data
                HttpContext.Session.Remove("ImportAnalysis");
                HttpContext.Session.Remove("FileName");

                if (importResult.Success)
                {
                    TempData["ImportResult"] = importResult.Message;
                    TempData["ImportedCount"] = importResult.ImportedCount;
                    TempData["ErrorCount"] = importResult.ErrorCount;
                }

                TempData[importResult.Success ? "Success" : "Error"] = importResult.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Import execution failed: {ex.Message}";
                return RedirectToAction(nameof(Import));
            }
        }

        private async Task HandleSemesterCreation(ImportOptions options, List<Course> validCourses)
        {
            // Get all existing semester IDs
            var existingSemesterIds = await _context.Semesters
                .Select(s => s.Id)
                .ToListAsync();

            // Find courses with non-existent semester IDs (safe nullable handling)
            var coursesWithInvalidSemesters = validCourses
                .Where(c => c.SemesterId.HasValue && c.SemesterId > 0 && !existingSemesterIds.Contains(c.SemesterId.Value)) // ✅ Fixed line 998
                .ToList();

            if (options.SemesterCreationMode == "auto" && coursesWithInvalidSemesters.Any())
            {
                await CreateMissingSemesters(coursesWithInvalidSemesters);
            }
            else if (options.SemesterCreationMode == "ask" && options.SemestersToCreate.Any())
            {
                await CreateSpecificSemesters(options.SemestersToCreate);
            }
            else if (options.SemesterCreationMode == "ignore" && coursesWithInvalidSemesters.Any())
            {
                await RemoveInvalidSemesterAssignments(coursesWithInvalidSemesters);
            }
        }



        // GET: Courses/DownloadTemplate
        [HttpGet]
        //[Route("DownloadTemplate")]
        public IActionResult DownloadTemplate()
        {
            try
            {
                var templateBytes = GenerateExcelTemplate();
                return File(templateBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "Course_Import_Template.xlsx");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to download template: {ex.Message}";
                return RedirectToAction(nameof(Import));
            }
        }

        private byte[] GenerateExcelTemplate()
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Courses Template");

            // Headers with instructions - UPDATED FOR NULLABLE SEMESTER
            string[] headers = {
        "CourseCode (Required)",
        "CourseName (Required)",
        "Description",
        "Credits",
        "Department",
        "Semester (Optional - leave empty for unassigned)", // ✅ Updated description
        "MaxStudents",
        "MinGPA",
        "MinPassedHours",
        "Prerequisites",
        "CourseSpecification",
        "Icon",
        "IsActive"
    };

            string[] descriptions = {
        "Unique course identifier (e.g., CS101)",
        "Course name (e.g., Introduction to Computer Science)",
        "Course description (max 5000 characters)",
        "Credit hours (1-6)",
        "Department name (e.g., Computer Science)",
        "Semester ID number (optional - leave blank if not assigned to semester)", // ✅ Updated
        "Maximum students (1-1000)",
        "Minimum GPA required (0.00-4.00)",
        "Minimum passed hours required",
        "Prerequisite course codes separated by commas (e.g., CS101,MATH201)",
        "Course specification document link or text",
        "Icon name or URL (e.g., fas fa-code)",
        "Yes/No or true/false or 1/0"
    };

            // Add headers and descriptions
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
                worksheet.Cells[2, i + 1].Value = descriptions[i];

                // Style headers
                worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                worksheet.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);

                // Style descriptions
                worksheet.Cells[2, i + 1].Style.Font.Italic = true;
                worksheet.Cells[2, i + 1].Style.Font.Color.SetColor(System.Drawing.Color.Gray);
            }

            // Add sample data - UPDATED WITH NULLABLE SEMESTER
            var sampleCourses = new[]
{
    new {
        CourseCode = "CS101",
        CourseName = "Introduction to Computer Science",
        Description = "Basic concepts of computer science and programming",
        Credits = 3,
        Department = "Computer Science",
        Semester = (object)1, // ✅ Cast to object - assigned to semester 1
        MaxStudents = 1000,
        MinGPA = 2.0m,
        MinPassedHours = 0,
        Prerequisites = "",
        CourseSpecification = "https://example.com/cs101-spec.pdf",
        Icon = "fas fa-laptop-code",
        IsActive = "Yes"
    },
    new {
        CourseCode = "MATH201",
        CourseName = "Calculus I",
        Description = "Differential and integral calculus of single variable functions",
        Credits = 4,
        Department = "Mathematics",
        Semester = (object)"", // ✅ Cast to object - empty = unassigned (null)
        MaxStudents = 1000,
        MinGPA = 2.5m,
        MinPassedHours = 30,
        Prerequisites = "MATH101",
        CourseSpecification = "https://example.com/math201-spec.pdf",
        Icon = "fas fa-calculator",
        IsActive = "Yes"
    },
    new {
        CourseCode = "PHY101",
        CourseName = "Physics I",
        Description = "Fundamental principles of physics",
        Credits = 4,
        Department = "Physics",
        Semester = (object)2, // ✅ Cast to object - assigned to semester 2
        MaxStudents = 1000,
        MinGPA = 2.0m,
        MinPassedHours = 0,
        Prerequisites = "",
        CourseSpecification = "https://example.com/phy101-spec.pdf",
        Icon = "fas fa-atom",
        IsActive = "Yes"
    }
};
            // Add sample data to worksheet
            for (int i = 0; i < sampleCourses.Length; i++)
            {
                var course = sampleCourses[i];
                worksheet.Cells[i + 3, 1].Value = course.CourseCode;
                worksheet.Cells[i + 3, 2].Value = course.CourseName;
                worksheet.Cells[i + 3, 3].Value = course.Description;
                worksheet.Cells[i + 3, 4].Value = course.Credits;
                worksheet.Cells[i + 3, 5].Value = course.Department;
                worksheet.Cells[i + 3, 6].Value = course.Semester; // Can be number or empty string
                worksheet.Cells[i + 3, 7].Value = course.MaxStudents;
                worksheet.Cells[i + 3, 8].Value = course.MinGPA;
                worksheet.Cells[i + 3, 9].Value = course.MinPassedHours;
                worksheet.Cells[i + 3, 10].Value = course.Prerequisites;
                worksheet.Cells[i + 3, 11].Value = course.CourseSpecification;
                worksheet.Cells[i + 3, 12].Value = course.Icon;
                worksheet.Cells[i + 3, 13].Value = course.IsActive;
            }

            // Auto-fit columns
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            return package.GetAsByteArray();
        }

        private void AddExcelDataValidation(ExcelWorksheet worksheet)
        {
            // Credits validation (1-6)
            var creditsValidation = worksheet.DataValidations.AddIntegerValidation("D4:D100");
            creditsValidation.Formula.Value = 1;
            creditsValidation.Formula2.Value = 6;
            creditsValidation.ShowErrorMessage = true;
            creditsValidation.ErrorTitle = "Invalid Credits";
            creditsValidation.Error = "Credits must be between 1 and 6";

            // Semester validation (1-8)
            var semesterValidation = worksheet.DataValidations.AddIntegerValidation("F4:F100");
            semesterValidation.Formula.Value = 1;
            semesterValidation.Formula2.Value = 8;
            semesterValidation.ShowErrorMessage = true;
            semesterValidation.ErrorTitle = "Invalid Semester";
            semesterValidation.Error = "Semester must be between 1 and 8";

            // MaxStudents validation (1-1000)
            var maxStudentsValidation = worksheet.DataValidations.AddIntegerValidation("G4:G100");
            maxStudentsValidation.Formula.Value = 1;
            maxStudentsValidation.Formula2.Value = 1000;
            maxStudentsValidation.ShowErrorMessage = true;
            maxStudentsValidation.ErrorTitle = "Invalid Max Students";
            maxStudentsValidation.Error = "Max Students must be between 1 and 1000";

            // MinGPA validation (0.00-4.00)
            var minGPAValidation = worksheet.DataValidations.AddDecimalValidation("H4:H100");
            minGPAValidation.Formula.Value = 0;
            minGPAValidation.Formula2.Value = 4.00;
            minGPAValidation.ShowErrorMessage = true;
            minGPAValidation.ErrorTitle = "Invalid Min GPA";
            minGPAValidation.Error = "Min GPA must be between 0.00 and 4.00";
        }

        

        // POST: Courses/DeleteAll
        [HttpPost]
        //[HttpPost("DeleteAll")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAll()
        {
            try
            {
                var courseCount = await _context.Courses.CountAsync();
                await _courseService.DeleteAllCoursesAsync();
                TempData["Success"] = $"Successfully deleted all {courseCount} courses.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to delete all courses: {ex.Message}";
                _logger.LogError(ex, "Error during mass course deletion");
            }

            return RedirectToAction(nameof(Index));
        }

        // Helper method to delete course using raw SQL
        [HttpPost]
        //[HttpPost("DeleteCourseWithRawSql")]
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
        //[HttpPost("DeleteAllCoursesWithRawSql")]
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


        // POST: Courses/ExportSelected
        [HttpPost]
        //[HttpPost("ExportSelected")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportSelected(int[] selectedCourses)
        {
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
>>>>>>> 2510eef0503cd1f13788ed842e48bf72c263f9b3

        // POST: Courses/BulkAddToSemester
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkAddToSemester(int semesterId, string courseIds)
        {
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
<<<<<<< HEAD
                TempData["Error"] = $"Export failed: {ex.Message}";
                return RedirectToAction(nameof(Index));
=======
                _logger.LogError(ex, "Error deleting multiple courses");
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateDefaultSemester()
        {
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
>>>>>>> 2510eef0503cd1f13788ed842e48bf72c263f9b3
            }
        }

        // GET: Courses/BulkDeleteConfirmation
        [HttpGet]
        public async Task<IActionResult> BulkDeleteConfirmation([FromQuery] List<int> selectedCourses)
        {
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
            var totalCourses = _context.Courses.Count();
            ViewBag.TotalCourses = totalCourses;
            return View();
        }

        // POST: Courses/DeleteAllConfirmed
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAllConfirmed()
        {
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
<<<<<<< HEAD

        // POST: Courses/DeleteMultiple
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMultiple([FromBody] List<int> selectedCourses)
        {
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
=======

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> ImportExecute(ImportSettings settings)
        //{
        //    try
        //    {
        //        var analysisJson = HttpContext.Session.GetString("ImportAnalysis");
        //        if (string.IsNullOrEmpty(analysisJson))
        //        {
        //            TempData["Error"] = "Import session expired. Please upload the file again.";
        //            return RedirectToAction(nameof(Import));
        //        }

        //        var analysisResult = System.Text.Json.JsonSerializer.Deserialize<ImportResult>(analysisJson);
        //        if (analysisResult == null || !analysisResult.Success)
        //        {
        //            TempData["Error"] = "Invalid import data.";
        //            return RedirectToAction(nameof(Import));
        //        }

        //        // FIX: Use the semester IDs from the imported courses
        //        var importResult = await ExecuteImportWithSemesterPreservation(analysisResult, settings);

        //        // Clean up session data
        //        HttpContext.Session.Remove("ImportAnalysis");
        //        HttpContext.Session.Remove("FileName");

        //        if (importResult.Success)
        //        {
        //            TempData["ImportResult"] = importResult.Message;
        //            TempData["ImportedCount"] = importResult.ImportedCount;
        //            TempData["ErrorCount"] = importResult.ErrorCount;
        //        }

        //        TempData[importResult.Success ? "Success" : "Error"] = importResult.Message;
        //        return RedirectToAction(nameof(Index));
        //    }
        //    catch (Exception ex)
        //    {
        //        TempData["Error"] = $"Import execution failed: {ex.Message}";
        //        return RedirectToAction(nameof(Import));
        //    }
        //}

        private async Task<ImportResult> ExecuteImportWithSemesterPreservation(ImportResult analysisResult, ImportSettings settings)
        {
            try
            {
                int importedCount = 0;
                int errorCount = 0;
                var errors = new List<string>();

                // Get all existing course codes for duplicate checking
                var existingCourseCodes = await _context.Courses
                    .Where(c => c.IsActive)
                    .Select(c => c.CourseCode.ToLower())
                    .ToListAsync();

                // Get valid semester IDs once
                var validSemesterIds = await _context.Semesters
                    .Select(s => s.Id)
                    .ToListAsync();

                if (!validSemesterIds.Any())
                {
                    return new ImportResult
                    {
                        Success = false,
                        Message = "No semesters exist in the database. Please create at least one semester before importing courses.",
                        ImportedCount = 0,
                        ErrorCount = analysisResult.ValidCourses.Count,
                        Errors = new List<string> { "No semesters available" }
                    };
                }

                foreach (var importedCourse in analysisResult.ValidCourses)
                {
                    try
                    {
                        // Validate and fix semester ID first
                        if (!importedCourse.SemesterId.HasValue || importedCourse.SemesterId <= 0 || !validSemesterIds.Contains(importedCourse.SemesterId.Value))
                        {
                            importedCourse.SemesterId = validSemesterIds.First();
                        }

                        // Check if course code already exists (case-insensitive)
                        var normalizedCode = importedCourse.CourseCode?.ToLower() ?? string.Empty;
                        var courseExists = existingCourseCodes.Contains(normalizedCode);

                        if (courseExists)
                        {
                            switch (settings.DuplicateHandling)
                            {
                                case "Skip":
                                    errors.Add($"Skipped: Course '{importedCourse.CourseCode}' already exists");
                                    continue;
                                case "Override":
                                    // Find and update existing course
                                    var existingCourse = await _context.Courses
                                        .FirstOrDefaultAsync(c => c.CourseCode.ToLower() == normalizedCode);

                                    if (existingCourse != null)
                                    {
                                        existingCourse.CourseName = importedCourse.CourseName ?? string.Empty;
                                        existingCourse.Description = importedCourse.Description;
                                        existingCourse.Credits = importedCourse.Credits;
                                        existingCourse.Department = importedCourse.Department ?? "General";
                                        existingCourse.SemesterId = importedCourse.SemesterId; // Use validated semester ID
                                        existingCourse.MaxStudents = importedCourse.MaxStudents;
                                        existingCourse.MinGPA = importedCourse.MinGPA;
                                        existingCourse.MinPassedHours = importedCourse.MinPassedHours;
                                        existingCourse.IsActive = true;
                                        existingCourse.ModifiedDate = DateTime.Now;
                                        importedCount++;
                                    }
                                    break;
                                case "CreateNew":
                                    // Create new course with unique code
                                    var newCourseCode = await GenerateUniqueCourseCode(importedCourse.CourseCode);
                                    await CreateCourseCopyWithSemester(importedCourse, newCourseCode);
                                    importedCount++;
                                    break;
                            }
                        }
                        else
                        {
                            // Create new course with validated semester ID
                            await CreateNewCourseWithSemester(importedCourse);
                            importedCount++;
                            // Add to existing codes to prevent duplicates in same import
                            existingCourseCodes.Add(normalizedCode);
                        }
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        errors.Add($"Error importing '{importedCourse.CourseCode}': {ex.Message}");
                        _logger.LogError(ex, "Error importing course {CourseCode}", importedCourse.CourseCode);
                    }
                }

                await _context.SaveChangesAsync();

                return new ImportResult
                {
                    Success = importedCount > 0,
                    Message = $"Imported {importedCount} courses successfully. {errorCount} errors.",
                    ImportedCount = importedCount,
                    ErrorCount = errorCount,
                    Errors = errors
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in import execution");
                return new ImportResult
                {
                    Success = false,
                    Message = $"Import failed: {ex.Message}",
                    ImportedCount = 0,
                    ErrorCount = analysisResult.ValidCourses.Count,
                    Errors = new List<string> { ex.Message, ex.InnerException?.Message ?? "No inner exception" }
                };
            }
        }

        [HttpPost]
        public async Task<IActionResult> TestFixedImport()
        {
            try
            {
                // Create test courses with various semester IDs
                var testCourses = new List<Course>
        {
            new Course
            {
                CourseCode = "FIXED-TEST-1",
                CourseName = "Fixed Test Course 1",
                Credits = 3,
                Department = "Testing",
                SemesterId = 0, // This should be fixed to semester 1
                IsActive = true,
                MaxStudents = 1000,
                CreatedDate = DateTime.Now
            },
            new Course
            {
                CourseCode = "FIXED-TEST-2",
                CourseName = "Fixed Test Course 2",
                Credits = 4,
                Department = "Testing",
                SemesterId = 999, // This should be fixed to semester 1
                IsActive = true,
                MaxStudents = 1000,
                CreatedDate = DateTime.Now
            },
            new Course
            {
                CourseCode = "FIXED-TEST-3",
                CourseName = "Fixed Test Course 3",
                Credits = 3,
                Department = "Testing",
                SemesterId = 1, // This is valid
                IsActive = true,
                MaxStudents = 1000,
                CreatedDate = DateTime.Now
            }
        };

                // Test the validation and creation
                foreach (var course in testCourses)
                {
                    await ValidateAndFixSingleSemesterId(course);
                }

                _context.Courses.AddRange(testCourses);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Fixed import test completed successfully",
                    courses = testCourses.Select(c => new { c.CourseCode, c.SemesterId })
                });
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

        private Task CreateNewCourseWithSemester(Course importedCourse)
        {
            var course = new Course
            {
                CourseCode = importedCourse.CourseCode ?? string.Empty,
                CourseName = importedCourse.CourseName ?? string.Empty,
                Description = importedCourse.Description,
                Credits = importedCourse.Credits,
                Department = importedCourse.Department ?? "General",
                SemesterId = importedCourse.SemesterId, // Can be null now
                IsActive = true,
                MaxStudents = importedCourse.MaxStudents,
                MinGPA = importedCourse.MinGPA,
                MinPassedHours = importedCourse.MinPassedHours,
                CreatedDate = DateTime.Now
            };

            _context.Courses.Add(course);
            return Task.CompletedTask;
        }

        private async Task ValidateAndFixSingleSemesterId(Course course)
        {
            // Get all valid semester IDs
            var validSemesterIds = await _context.Semesters
                .Select(s => s.Id)
                .ToListAsync();

            // If semester ID is invalid or 0, assign to first available semester
            if (!course.SemesterId.HasValue || course.SemesterId <= 0 || !validSemesterIds.Contains(course.SemesterId.Value))
            {
                if (validSemesterIds.Any())
                {
                    var originalSemesterId = course.SemesterId;
                    course.SemesterId = validSemesterIds.First();

                    _logger.LogWarning("Course {CourseCode} had invalid SemesterId {OriginalId}, assigned to SemesterId {NewId}",
                        course.CourseCode, originalSemesterId, course.SemesterId);
                }
                else
                {
                    throw new InvalidOperationException("No semesters exist in the database. Please create at least one semester.");
                }
            }
        }

        private async Task<string> GenerateUniqueCourseCode(string? baseCourseCode)
        {
            if (string.IsNullOrWhiteSpace(baseCourseCode))
            {
                baseCourseCode = "COURSE";
            }

            var counter = 1;
            string newCourseCode;

            // First try without counter
            newCourseCode = $"{baseCourseCode}-COPY";

            // If that exists, start with counter
            if (await _context.Courses.AnyAsync(c => c.CourseCode == newCourseCode))
            {
                newCourseCode = $"{baseCourseCode}-COPY1";
                counter = 2;
            }

            // Increment counter until we find a unique code
            while (await _context.Courses.AnyAsync(c => c.CourseCode == newCourseCode))
            {
                newCourseCode = $"{baseCourseCode}-COPY{counter}";
                counter++;

                // Safety check to prevent infinite loop
                if (counter > 1000)
                {
                    newCourseCode = $"{baseCourseCode}-{Guid.NewGuid().ToString("N").Substring(0, 8)}";
                    break;
                }
            }

            return newCourseCode;
        }

        private async Task CreateCourseCopyWithSemester(Course sourceCourse, string newCourseCode)
        {
            // Create copy with new course code but same semester ID (handle nullable)
            var course = new Course
            {
                CourseCode = newCourseCode ?? string.Empty,
                CourseName = sourceCourse.CourseName ?? string.Empty,
                Description = sourceCourse.Description,
                Credits = sourceCourse.Credits,
                Department = sourceCourse.Department ?? "General",
                SemesterId = sourceCourse.SemesterId, // Can be null now
                IsActive = true,
                MaxStudents = sourceCourse.MaxStudents,
                MinGPA = sourceCourse.MinGPA,
                MinPassedHours = sourceCourse.MinPassedHours,
                CreatedDate = DateTime.Now
            };

            _context.Courses.Add(course);
            await _context.SaveChangesAsync(); // Add await since this is now async
        }



        [HttpPost]
        public async Task<IActionResult> TestImportWithSemesterPreservation()
        {
            try
            {
                // Create test courses with different semester IDs
                var testCourses = new List<Course>
        {
            new Course { CourseCode = "TEST101", CourseName = "Test Course 1", Credits = 3, Department = "Testing", SemesterId = 2, IsActive = true },
            new Course { CourseCode = "TEST102", CourseName = "Test Course 2", Credits = 4, Department = "Testing", SemesterId = 3, IsActive = true },
            new Course { CourseCode = "TEST103", CourseName = "Test Course 3", Credits = 3, Department = "Testing", SemesterId = 0, IsActive = true },
            new Course { CourseCode = "TEST104", CourseName = "Test Course 4", Credits = 4, Department = "Testing", SemesterId = 4, IsActive = true }
        };

                _context.Courses.AddRange(testCourses);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Created test courses with different semester IDs",
                    courses = testCourses.Select(c => new { c.CourseCode, c.SemesterId })
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> TestImportWithSemesters()
        {
            try
            {
                // Test data with different semester IDs
                var testCourses = new List<Course>
        {
            new Course {
                CourseCode = "IMPORT101",
                CourseName = "Import Test 1",
                Credits = 3,
                Department = "Testing",
                SemesterId = 2,
                IsActive = true,
                CreatedDate = DateTime.Now
            },
            new Course {
                CourseCode = "IMPORT102",
                CourseName = "Import Test 2",
                Credits = 4,
                Department = "Testing",
                SemesterId = 3,
                IsActive = true,
                CreatedDate = DateTime.Now
            },
            new Course {
                CourseCode = "IMPORT103",
                CourseName = "Import Test 3",
                Credits = 3,
                Department = "Testing",
                SemesterId = 0,
                IsActive = true,
                CreatedDate = DateTime.Now
            },
            new Course {
                CourseCode = "IMPORT104",
                CourseName = "Import Test 4",
                Credits = 4,
                Department = "Testing",
                SemesterId = 4,
                IsActive = true,
                CreatedDate = DateTime.Now
            }
        };

                _context.Courses.AddRange(testCourses);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Created test import courses with different semester IDs",
                    courses = testCourses.Select(c => new { c.CourseCode, c.SemesterId })
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public Task<ImportResult> AnalyzeExcelImportAsync(Stream stream)
        {
            try
            {
                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets[0];

                if (worksheet?.Dimension == null)
                {
                    return Task.FromResult(new ImportResult
                    {
                        Success = false,
                        Message = "Excel file is empty or invalid",
                        ValidCourses = new List<Course>(),
                        InvalidCourses = new List<InvalidCourse>(),
                        TotalRows = 0,
                        Errors = new List<string> { "Worksheet or dimension is null" }
                    });
                }

                var validCourses = new List<Course>();
                var invalidCourses = new List<InvalidCourse>();
                var errors = new List<string>();

                var rowCount = worksheet.Dimension.Rows;

                for (int row = 2; row <= rowCount; row++)
                {
                    try
                    {
                        var courseCode = worksheet.Cells[row, 1].Value?.ToString()?.Trim();
                        var courseName = worksheet.Cells[row, 2].Value?.ToString()?.Trim();
                        var description = worksheet.Cells[row, 3].Value?.ToString()?.Trim();
                        var credits = int.TryParse(worksheet.Cells[row, 4].Value?.ToString(), out int c) ? c : 3;
                        var department = worksheet.Cells[row, 5].Value?.ToString()?.Trim();

                        // ✅ UPDATED: Read semester ID as nullable int
                        var semesterIdValue = worksheet.Cells[row, 6].Value?.ToString()?.Trim();
                        int? semesterId = null;

                        if (!string.IsNullOrEmpty(semesterIdValue) && int.TryParse(semesterIdValue, out int parsedSemesterId))
                        {
                            semesterId = parsedSemesterId;
                        }
                        // If semesterIdValue is empty or invalid, semesterId remains null

                        // Validation with null safety
                        if (string.IsNullOrEmpty(courseCode))
                        {
                            invalidCourses.Add(new InvalidCourse
                            {
                                RowNumber = row,
                                ErrorMessage = "Course code is required",
                                CourseCode = courseCode ?? string.Empty,
                                CourseName = courseName ?? string.Empty
                            });
                            continue;
                        }

                        if (string.IsNullOrEmpty(courseName))
                        {
                            invalidCourses.Add(new InvalidCourse
                            {
                                RowNumber = row,
                                ErrorMessage = "Course name is required",
                                CourseCode = courseCode ?? string.Empty,
                                CourseName = courseName ?? string.Empty
                            });
                            continue;
                        }

                        // Create Course entity with null safety
                        var course = new Course
                        {
                            SerialNumber = row - 1,
                            CourseCode = courseCode ?? string.Empty,
                            CourseName = courseName ?? string.Empty,
                            Description = description,
                            Credits = credits,
                            Department = department ?? "General",
                            SemesterId = semesterId, // ✅ Now accepts null values
                            MaxStudents = 1000, // Default value or read from Excel if available
                            IsActive = true,
                            CreatedDate = DateTime.Now
                        };

                        validCourses.Add(course);
                    }
                    catch (Exception ex)
                    {
                        invalidCourses.Add(new InvalidCourse
                        {
                            RowNumber = row,
                            ErrorMessage = $"Error parsing row: {ex.Message}"
                        });
                    }
                }

                return Task.FromResult(new ImportResult
                {
                    Success = true,
                    Message = $"Analysis complete: {validCourses.Count} valid, {invalidCourses.Count} invalid",
                    ValidCourses = validCourses,
                    InvalidCourses = invalidCourses,
                    TotalRows = rowCount - 1,
                    Errors = errors
                });
            }
            catch (Exception ex)
            {
                return Task.FromResult(new ImportResult
                {
                    Success = false,
                    Message = $"Error analyzing Excel file: {ex.Message}",
                    ValidCourses = new List<Course>(),
                    InvalidCourses = new List<InvalidCourse>(),
                    TotalRows = 0,
                    Errors = new List<string> { ex.Message }
                });
            }
        }



        [HttpGet]
        public async Task<IActionResult> DebugImportBehavior()
        {
            try
            {
                // Check recent imports to see semester assignments
                var recentImports = await _context.Courses
                    .OrderByDescending(c => c.CreatedDate)
                    .Take(10)
                    .Select(c => new {
                        c.CourseCode,
                        c.CourseName,
                        c.SemesterId,
                        c.CreatedDate,
                        SemesterName = c.CourseSemester != null ? c.CourseSemester.Name : "Unassigned"
                    })
                    .ToListAsync();

                // Safe semester distribution query
                var allCourses = await _context.Courses
                    .Include(c => c.CourseSemester)
                    .ToListAsync();

                var distribution = allCourses
                    .GroupBy(c => c.SemesterId)
                    .Select(g => new
                    {
                        SemesterId = g.Key,
                        Count = g.Count(),
                        SemesterName = g.FirstOrDefault()?.CourseSemester?.Name ?? "Unassigned"
                    })
                    .ToList();

                return Json(new
                {
                    RecentImports = recentImports,
                    SemesterDistribution = distribution,
                    TotalCourses = allCourses.Count,
                    Message = "Check if recently imported courses have correct semester assignments"
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }




        

        private string CleanCourseCode(string courseCode)
        {
            if (string.IsNullOrWhiteSpace(courseCode))
                return "COURSE";

            // Remove or replace invalid characters
            var cleaned = System.Text.RegularExpressions.Regex.Replace(courseCode, @"[^a-zA-Z0-9\-_]", "");

            // Trim to reasonable length
            if (cleaned.Length > 15)
                cleaned = cleaned.Substring(0, 15);

            // Ensure it's not empty after cleaning
            if (string.IsNullOrWhiteSpace(cleaned))
                cleaned = "COURSE";

            return cleaned.Trim();
        }

        private async Task CreateMissingSemesters(List<Course> coursesWithInvalidSemesters)
        {
            // Get unique semester IDs that need to be created (with proper nullable handling)
            var missingSemesterIds = coursesWithInvalidSemesters
                .Where(c => c.SemesterId.HasValue && c.SemesterId > 0) // ✅ Check HasValue first
                .Select(c => c.SemesterId!.Value) // ✅ Use null-forgiving operator since we filtered nulls
                .Distinct()
                .ToList();

            // Get existing semester IDs to avoid duplicates
            var existingSemesterIds = await _context.Semesters
                .Select(s => s.Id)
                .ToListAsync();

            // Filter out semester IDs that already exist
            var semestersToCreate = missingSemesterIds
                .Where(id => !existingSemesterIds.Contains(id))
                .ToList();

            foreach (var semesterId in semestersToCreate)
            {
                // Double-check it doesn't exist (concurrent operation safety)
                var existingSemester = await _context.Semesters.FindAsync(semesterId);
                if (existingSemester == null)
                {
                    var semester = new Semester
                    {
                        Name = $"Auto-Created Semester {semesterId}",
                        SemesterType = "Fall",
                        AcademicYear = DateTime.Now.Year,
                        StartDate = DateTime.Now,
                        EndDate = DateTime.Now.AddMonths(4),
                        RegistrationStartDate = DateTime.Now.AddDays(-7),
                        RegistrationEndDate = DateTime.Now.AddDays(30),
                        IsActive = true,
                        IsCurrent = false,
                        IsRegistrationOpen = true
                    };

                    _context.Semesters.Add(semester);
                }
            }

            if (semestersToCreate.Any())
            {
                await _context.SaveChangesAsync();
            }
        }

        private async Task CreateSpecificSemesters(List<int> semesterIdsToCreate)
        {
            if (!semesterIdsToCreate.Any()) return;

            var currentYear = DateTime.Now.Year;
            foreach (var semesterId in semesterIdsToCreate)
            {
                // Check if semester already exists
                var exists = await _context.Semesters.AnyAsync(s => s.Id == semesterId);
                if (!exists)
                {
                    var newSemester = new Semester
                    {
                        Name = $"Imported Semester {semesterId}",
                        SemesterType = "Fall",
                        AcademicYear = currentYear,
                        StartDate = DateTime.Now,
                        EndDate = DateTime.Now.AddMonths(4),
                        RegistrationStartDate = DateTime.Now.AddDays(-7),
                        RegistrationEndDate = DateTime.Now.AddDays(30),
                        IsActive = true,
                        IsCurrent = false,
                        IsRegistrationOpen = true
                    };

                    _context.Semesters.Add(newSemester);
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task RemoveInvalidSemesterAssignments(List<Course> coursesWithInvalidSemesters)
        {
            // Set SemesterId to null for courses with invalid semester assignments
            foreach (var course in coursesWithInvalidSemesters)
            {
                course.SemesterId = null;
            }
            await _context.SaveChangesAsync();
        }

        private List<int> GetSemesterIdsFromImport(ImportResult analysisResult)
        {
            if (analysisResult?.ValidCourses == null)
            {
                return new List<int>();
            }

            var semesterIdsInImport = new List<int>();

            foreach (var course in analysisResult.ValidCourses)
            {
                if (course.SemesterId.HasValue && course.SemesterId.Value > 0)
                {
                    semesterIdsInImport.Add(course.SemesterId.Value);
                }
            }

            return semesterIdsInImport.Distinct().ToList();
        }

        private async Task ValidateAndFixSemesterIds(List<Course> courses)
        {
            // Get all valid semester IDs (including 0 if it exists)
            var validSemesterIds = await _context.Semesters
                .Select(s => s.Id)
                .ToListAsync();

            // Check if semester 0 exists, if not we need to handle it differently
            var hasUnassignedSemester = validSemesterIds.Contains(0);

            // Fix courses with invalid semester IDs
            foreach (var course in courses)
            {
                if (course.SemesterId.HasValue && course.SemesterId > 0 && !validSemesterIds.Contains(course.SemesterId.Value))
                {
                    _logger.LogWarning("Course {CourseCode} has invalid SemesterId {SemesterId}",
                        course.CourseCode, course.SemesterId);

                    // If we have an unassigned semester, use it. Otherwise, use the first available semester.
                    if (hasUnassignedSemester)
                    {
                        course.SemesterId = 0;
                    }
                    else if (validSemesterIds.Any())
                    {
                        course.SemesterId = validSemesterIds.First();
                        _logger.LogInformation("Assigned course {CourseCode} to semester {SemesterId}",
                            course.CourseCode, course.SemesterId);
                    }
                    else
                    {
                        // No semesters exist at all - this is a problem
                        _logger.LogError("No semesters exist in database for course {CourseCode}", course.CourseCode);
                        throw new InvalidOperationException("No semesters exist in the database. Please create at least one semester.");
                    }
                }
            }
        }

        private void ValidateCourseData(Course course)
        {
            if (string.IsNullOrEmpty(course.CourseCode))
                throw new ArgumentException("CourseCode is required");

            if (string.IsNullOrEmpty(course.CourseName))
                throw new ArgumentException("CourseName is required");

            if (string.IsNullOrEmpty(course.Department))
                course.Department = "General";

            // Truncate if too long
            if (course.CourseCode.Length > 20)
                course.CourseCode = course.CourseCode.Substring(0, 20);

            if (course.CourseName.Length > 100)
                course.CourseName = course.CourseName.Substring(0, 100);

            if (course.Department.Length > 50)
                course.Department = course.Department.Substring(0, 50);

            // Validate credits
            if (course.Credits < 1) course.Credits = 1;
            if (course.Credits > 6) course.Credits = 6;

            // Validate max students
            if (course.MaxStudents < 1) course.MaxStudents = 1000;
            if (course.MaxStudents > 1000) course.MaxStudents = 1000;
        }

        [HttpPost]
        public async Task<IActionResult> TestCompleteImportFlow()
        {
            try
            {
                // Create a test import result
                var testCourses = new List<Course>
        {
            new Course
            {
                CourseCode = "TEST001",
                CourseName = "Test Course 1",
                Credits = 3,
                Department = "Testing",
                SemesterId = 999, // This should trigger semester creation
                IsActive = true,
                MaxStudents = 1000,
                CreatedDate = DateTime.Now
            },
            new Course
            {
                CourseCode = "TEST002",
                CourseName = "Test Course 2",
                Credits = 4,
                Department = "Testing",
                SemesterId = 0, // Unassigned
                IsActive = true,
                MaxStudents = 1000,
                CreatedDate = DateTime.Now
            }
        };

                var importResult = new ImportResult
                {
                    Success = true,
                    ValidCourses = testCourses,
                    InvalidCourses = new List<InvalidCourse>(),
                    TotalRows = 2
                };

                var settings = new ImportSettings { DuplicateHandling = "Skip" };
                var options = new ImportOptions { SemesterCreationMode = "auto" };

                // Test the import flow
                using var transaction = await _context.Database.BeginTransactionAsync();

                var result = await ExecuteImportWithSemesterPreservation(importResult, settings);

                await transaction.CommitAsync();

                return Json(new
                {
                    success = true,
                    message = "Test import flow completed",
                    importResult = result
                });
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

        [HttpPost]
        public async Task<IActionResult> CreateUnassignedSemester()
        {
            try
            {
                // Check if semester with ID 0 already exists
                var existingSemester = await _context.Semesters.FindAsync(0);

                if (existingSemester == null)
                {
                    var unassignedSemester = new Semester
                    {
                        Id = 0, // Explicitly set ID to 0
                        Name = "Unassigned Courses",
                        SemesterType = "Unassigned",
                        AcademicYear = DateTime.Now.Year,
                        StartDate = DateTime.Now,
                        EndDate = DateTime.Now.AddYears(1),
                        RegistrationStartDate = DateTime.Now,
                        RegistrationEndDate = DateTime.Now.AddYears(1),
                        IsActive = true,
                        IsCurrent = false,
                        IsRegistrationOpen = false
                    };

                    _context.Semesters.Add(unassignedSemester);
                    await _context.SaveChangesAsync();

                    return Json(new
                    {
                        success = true,
                        message = "Created 'Unassigned' semester with ID = 0",
                        semester = new { unassignedSemester.Id, unassignedSemester.Name }
                    });
                }
                else
                {
                    return Json(new
                    {
                        success = true,
                        message = "Unassigned semester already exists",
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
        [Route("Courses/DebugImportConstraints")]
        public async Task<IActionResult> DebugImportConstraints()
        {
            try
            {
                var results = new List<object>();

                // Test 1: Basic course creation
                try
                {
                    var testCourse = new Course
                    {
                        CourseCode = $"TEST-{DateTime.Now:HHmmss}",
                        CourseName = "Test Course",
                        Credits = 3,
                        Department = "Testing",
                        SemesterId = 0,
                        IsActive = true,
                        MaxStudents = 1000,
                        CreatedDate = DateTime.Now
                    };

                    _context.Courses.Add(testCourse);
                    await _context.SaveChangesAsync();

                    // Clean up
                    _context.Courses.Remove(testCourse);
                    await _context.SaveChangesAsync();

                    results.Add(new
                    {
                        test = "Basic Course Creation",
                        success = true,
                        message = "Can create and delete courses successfully"
                    });
                }
                catch (Exception ex)
                {
                    results.Add(new
                    {
                        test = "Basic Course Creation",
                        success = false,
                        error = ex.Message,
                        innerError = ex.InnerException?.Message,
                        details = "This tests if basic course operations work"
                    });
                }

                // Test 2: Check for duplicate course codes
                try
                {
                    var duplicateCodes = await _context.Courses
                        .GroupBy(c => c.CourseCode)
                        .Where(g => g.Count() > 1)
                        .Select(g => new { CourseCode = g.Key, Count = g.Count() })
                        .ToListAsync();

                    results.Add(new
                    {
                        test = "Duplicate Course Codes Check",
                        success = true,
                        duplicates = duplicateCodes
                    });
                }
                catch (Exception ex)
                {
                    results.Add(new
                    {
                        test = "Duplicate Course Codes Check",
                        success = false,
                        error = ex.Message
                    });
                }

                return Json(new
                {
                    success = true,
                    timestamp = DateTime.Now,
                    tests = results
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    error = ex.Message,
                    innerError = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }
        [HttpPost]
        public async Task<IActionResult> DebugDatabaseConstraints()
        {
            try
            {
                var results = new List<object>();

                // Test 1: Basic course creation with valid semester
                try
                {
                    // Get first available semester
                    var availableSemester = await _context.Semesters.FirstOrDefaultAsync();
                    if (availableSemester == null)
                    {
                        results.Add(new
                        {
                            test = "Basic Course Creation",
                            success = false,
                            error = "No semesters exist in database. Please create a semester first."
                        });
                    }
                    else
                    {
                        var testCourse1 = new Course
                        {
                            CourseCode = $"TEST-{DateTime.Now:HHmmss}",
                            CourseName = "Test Course",
                            Credits = 3,
                            Department = "Testing",
                            SemesterId = availableSemester.Id, // Use existing semester
                            IsActive = true,
                            MaxStudents = 1000,
                            CreatedDate = DateTime.Now
                        };
                        _context.Courses.Add(testCourse1);
                        await _context.SaveChangesAsync();
                        results.Add(new
                        {
                            test = "Basic Course Creation",
                            success = true,
                            courseCode = testCourse1.CourseCode,
                            semesterId = availableSemester.Id
                        });
                    }
                }
                catch (Exception ex)
                {
                    results.Add(new
                    {
                        test = "Basic Course Creation",
                        success = false,
                        error = ex.Message,
                        inner = ex.InnerException?.Message
                    });
                }

                return Json(new { tests = results });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    error = ex.Message,
                    inner = ex.InnerException?.Message
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> DebugSemesters()
        {
            try
            {
                var semesters = await _context.Semesters
                    .OrderBy(s => s.Id)
                    .Select(s => new { s.Id, s.Name, s.SemesterType, s.IsActive })
                    .ToListAsync();

                return Json(new
                {
                    success = true,
                    semesters = semesters,
                    message = $"Found {semesters.Count} semesters in database"
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    error = ex.Message,
                    inner = ex.InnerException?.Message
                });
            }
        }
        [HttpPost]
        public async Task<IActionResult> TestSafeImport()
        {
            try
            {
                // Get first available semester
                var availableSemester = await _context.Semesters.FirstOrDefaultAsync();
                if (availableSemester == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "No semesters exist. Please create a semester first using /Courses/CreateDefaultSemester"
                    });
                }

                // Create a simple test course
                var testCourse = new Course
                {
                    CourseCode = $"SAFETEST-{DateTime.Now:HHmmss}",
                    CourseName = "Safe Test Course",
                    Credits = 3,
                    Department = "Testing",
                    SemesterId = availableSemester.Id, // Use existing semester
                    IsActive = true,
                    MaxStudents = 1000,
                    CreatedDate = DateTime.Now
                };

                _context.Courses.Add(testCourse);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Safe test course created successfully",
                    course = new { testCourse.CourseCode, testCourse.SemesterId },
                    semester = new { availableSemester.Id, availableSemester.Name }
                });
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

        [HttpPost]
        public async Task<IActionResult> TestNullableSemester()
        {
            try
            {
                // Test 1: Course with null SemesterId
                var course1 = new Course
                {
                    CourseCode = "TEST-NULL",
                    CourseName = "Test Course with Null Semester",
                    Credits = 3,
                    Department = "Testing",
                    SemesterId = null, // ✅ This should work now
                    IsActive = true,
                    CreatedDate = DateTime.Now
                };

                // Test 2: Course with SemesterId = 0
                var course2 = new Course
                {
                    CourseCode = "TEST-ZERO",
                    CourseName = "Test Course with Zero Semester",
                    Credits = 3,
                    Department = "Testing",
                    SemesterId = 0, // ✅ This should also work
                    IsActive = true,
                    CreatedDate = DateTime.Now
                };

                _context.Courses.AddRange(course1, course2);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Nullable semester test passed!",
                    courses = new[] {
                new { course1.CourseCode, course1.SemesterId },
                new { course2.CourseCode, course2.SemesterId }
            }
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message,
                    innerException = ex.InnerException?.Message
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> DebugCourseSemesters()
        {
            var courses = await _context.Courses
                .Include(c => c.CourseSemester)
                .Select(c => new
                {
                    c.CourseCode,
                    c.CourseName,
                    SemesterId = c.SemesterId, // This can be null
                    SemesterName = c.CourseSemester != null ? c.CourseSemester.Name : "Unassigned"
                })
                .Take(10)
                .ToListAsync();

            // Safe JSON serialization
            return Json(new
            {
                Courses = courses,
                Message = "Sample courses with semester assignments (null = unassigned)"
            });
        }

        [HttpPost]
        public async Task<IActionResult> TestNullableSemesterHandling()
        {
            try
            {
                // Test creating courses with different SemesterId values
                var testCourses = new List<Course>
        {
            new Course { CourseCode = "TEST1", CourseName = "Test 1", SemesterId = null, IsActive = true },
            new Course { CourseCode = "TEST2", CourseName = "Test 2", SemesterId = 0, IsActive = true },
            new Course { CourseCode = "TEST3", CourseName = "Test 3", SemesterId = 1, IsActive = true }
        };

                _context.Courses.AddRange(testCourses);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Nullable semester handling test passed!",
                    courses = testCourses.Select(c => new { c.CourseCode, c.SemesterId })
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message,
                    innerException = ex.InnerException?.Message
                });
            }
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
                ["SemesterId"] = course.SemesterId?.ToString() ?? "null", // ✅ Fixed
                ["SemesterName"] = course.CourseSemester?.Name ?? "Unassigned",
                ["Department"] = course.Department ?? string.Empty,
                ["Credits"] = course.Credits
            };

            return Json(courseData);
        }

        private static List<int> GetValidSemesterIds(IEnumerable<Course> courses)
        {
            return courses?
                .Select(c => c.SemesterId)
                .Where(id => id.HasValue && id > 0)
                .Select(id => id!.Value)
                .Distinct()
                .ToList() ?? new List<int>();
        }
<<<<<<< HEAD

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
                        studentCode = ce.Student != null ? ce.Student.StudentId : "N/A", // Explicit null check
                        studentName = ce.Student != null ? ce.Student.Name : "Unknown Student",  // Add null check
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


=======
>>>>>>> b719770a87085740b760d958104cdbb206173fc7
    }



}

>>>>>>> 2510eef0503cd1f13788ed842e48bf72c263f9b3
