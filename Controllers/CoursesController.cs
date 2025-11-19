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
    //[Route("Courses")]
    //[Route("[controller]")]
    //[Route("[controller]/[action]")]
    //[Route("Courses/[action]")]
    //[ApiController]
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
        //[Route("")]
        //[Route("Index")]
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
        //[HttpGet("Create")]
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



        // GET: Courses/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var course = await _courseService.GetCourseByIdAsync(id);
                if (course == null)
                {
                    TempData["Error"] = "Course not found.";
                    return RedirectToAction(nameof(Index));
                }
                return View(course);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading course: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Courses/Delete/5
        [HttpPost]
        [ActionName("Delete")] // ✅ CRITICAL: This makes POST use the same URL as GET
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var course = await _courseService.GetCourseByIdAsync(id);
                if (course == null)
                {
                    TempData["Error"] = "Course not found.";
                    return RedirectToAction(nameof(Index));
                }

                await _courseService.DeleteCourseAsync(id);
                TempData["Success"] = $"Course '{course.CourseName}' deleted successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to delete course: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Courses/Prerequisites/5
        [HttpGet]
        //[HttpGet("Prerequisites/{id}")]
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
        //[HttpPost("AddPrerequisite")]
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
        //[HttpPost("RemovePrerequisite/{prerequisiteId}")]
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
        //[Route("Enroll/{id}")]
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

        [HttpPost]
        //[HttpPost("EnrollSingleStudent")]
        public async Task<IActionResult> EnrollStudent(int courseId, int studentId)
        {
            try
            {
                var canEnroll = await _courseService.CanStudentEnrollAsync(studentId, courseId);
                if (!canEnroll)
                {
                    var missingPrerequisites = await _courseService.GetMissingPrerequisitesAsync(studentId, courseId);
                    TempData["Error"] = $"Cannot enroll student. Missing prerequisites: {string.Join(", ", missingPrerequisites)}";
                    return RedirectToAction(nameof(Enroll), new { id = courseId });
                }

                await _courseService.EnrollStudentAsync(courseId, studentId);
                TempData["Success"] = "Student enrolled successfully!";
                return RedirectToAction(nameof(Details), new { id = courseId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enrolling student");
                TempData["Error"] = $"Error enrolling student: {ex.Message}";
                return RedirectToAction(nameof(Enroll), new { id = courseId });
            }
        }

        // POST: Courses/EnrollStudent
        [HttpPost]
        //[HttpPost("EnrollMultipleStudents")]
        public async Task<IActionResult> EnrollMultipleStudents(int courseId, int[] studentIds)
        {
            try
            {
                if (studentIds == null || !studentIds.Any())
                {
                    TempData["Error"] = "Please select at least one student to enroll.";
                    return RedirectToAction(nameof(Enroll), new { id = courseId });
                }

                int successCount = 0;
                foreach (var studentId in studentIds)
                {
                    try
                    {
                        var canEnroll = await _courseService.CanStudentEnrollAsync(studentId, courseId);
                        if (canEnroll)
                        {
                            await _courseService.EnrollStudentAsync(courseId, studentId);
                            successCount++;
                        }
                        else
                        {
                            var missingPrerequisites = await _courseService.GetMissingPrerequisitesAsync(studentId, courseId);
                            _logger.LogWarning($"Student {studentId} cannot enroll due to missing prerequisites: {string.Join(", ", missingPrerequisites)}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error enrolling student {studentId} in course {courseId}");
                    }
                }

                if (successCount > 0)
                {
                    TempData["Success"] = $"Successfully enrolled {successCount} student(s)!";
                }
                else
                {
                    TempData["Error"] = "No students were enrolled. Check if students meet prerequisites.";
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
        //[HttpPost("UnenrollStudent")]
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
        //[HttpGet("UpdateGrade/{id}")]
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
        //[HttpPost("UpdateGrade")]
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

        // GET: Courses/Export
        [HttpGet]
        //[HttpGet("Export")]
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
        //[Route("ExportPdf")]
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
        //[Route("ExportCoursePdf")]
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

        //// GET: Courses/DownloadTemplate
        //public IActionResult DownloadTemplate()
        //{
        //    try
        //    {
        //        ExcelPackage.License.SetNonCommercialOrganization("Student Management System");

        //        using var package = new ExcelPackage();
        //        var worksheet = package.Workbook.Worksheets.Add("Courses Template");

        //        // English Headers
        //        worksheet.Cells[1, 1].Value = "CourseCode";
        //        worksheet.Cells[1, 2].Value = "CourseName";
        //        worksheet.Cells[1, 3].Value = "Description";
        //        worksheet.Cells[1, 4].Value = "Credits";
        //        worksheet.Cells[1, 5].Value = "Department";
        //        worksheet.Cells[1, 6].Value = "Semester";
        //        worksheet.Cells[1, 7].Value = "MaxStudents";
        //        worksheet.Cells[1, 8].Value = "MinGPA";
        //        worksheet.Cells[1, 9].Value = "MinPassedHours";
        //        worksheet.Cells[1, 10].Value = "IsActive";
        //        worksheet.Cells[1, 11].Value = "Prerequisites";

        //        // Arabic Headers
        //        worksheet.Cells[1, 12].Value = "كود المادة";
        //        worksheet.Cells[1, 13].Value = "اسم المادة";
        //        worksheet.Cells[1, 14].Value = "الوصف";
        //        worksheet.Cells[1, 15].Value = "الساعات المعتمدة";
        //        worksheet.Cells[1, 16].Value = "القسم";
        //        worksheet.Cells[1, 17].Value = "الفصل الدراسي";
        //        worksheet.Cells[1, 18].Value = "الحد الأقصى للطلاب";
        //        worksheet.Cells[1, 19].Value = "الحد الأدنى للمعدل التراكمي";
        //        worksheet.Cells[1, 20].Value = "الحد الأدنى للساعات المنجزة";
        //        worksheet.Cells[1, 21].Value = "نشط";
        //        worksheet.Cells[1, 22].Value = "المتطلبات السابقة";

        //        // Style headers
        //        using (var range = worksheet.Cells[1, 1, 1, 22])
        //        {
        //            range.Style.Font.Bold = true;
        //            range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
        //            range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGreen);
        //            range.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
        //        }

        //        // Add sample data
        //        worksheet.Cells[2, 1].Value = "CS101";
        //        worksheet.Cells[2, 2].Value = "Introduction to Computer Science";
        //        worksheet.Cells[2, 3].Value = "Basic computer science concepts";
        //        worksheet.Cells[2, 4].Value = 3;
        //        worksheet.Cells[2, 5].Value = "Computer Science";
        //        worksheet.Cells[2, 6].Value = 1;
        //        worksheet.Cells[2, 7].Value = 40;
        //        worksheet.Cells[2, 8].Value = 2.0;
        //        worksheet.Cells[2, 9].Value = 0;
        //        worksheet.Cells[2, 10].Value = "Yes";
        //        worksheet.Cells[2, 11].Value = "MATH101";

        //        // Arabic sample data
        //        worksheet.Cells[2, 12].Value = "CS101";
        //        worksheet.Cells[2, 13].Value = "مقدمة في علوم الحاسب";
        //        worksheet.Cells[2, 14].Value = "مفاهيم أساسية في علوم الحاسب";
        //        worksheet.Cells[2, 15].Value = 3;
        //        worksheet.Cells[2, 16].Value = "علوم الحاسب";
        //        worksheet.Cells[2, 17].Value = 1;
        //        worksheet.Cells[2, 18].Value = 40;
        //        worksheet.Cells[2, 19].Value = 2.0;
        //        worksheet.Cells[2, 20].Value = 0;
        //        worksheet.Cells[2, 21].Value = "نعم";
        //        worksheet.Cells[2, 22].Value = "MATH101";

        //        // Auto-fit columns
        //        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

        //        // Add instructions sheet
        //        var instructionsSheet = package.Workbook.Worksheets.Add("Instructions");
        //        instructionsSheet.Cells[1, 1].Value = "Courses Import Template Instructions";
        //        instructionsSheet.Cells[1, 1].Style.Font.Bold = true;
        //        instructionsSheet.Cells[1, 1].Style.Font.Size = 14;

        //        instructionsSheet.Cells[3, 1].Value = "Required Fields:";
        //        instructionsSheet.Cells[3, 1].Style.Font.Bold = true;
        //        instructionsSheet.Cells[4, 1].Value = "- CourseCode (كود المادة)";
        //        instructionsSheet.Cells[5, 1].Value = "- CourseName (اسم المادة)";

        //        instructionsSheet.Cells[7, 1].Value = "Optional Fields:";
        //        instructionsSheet.Cells[7, 1].Style.Font.Bold = true;
        //        instructionsSheet.Cells[8, 1].Value = "- Description (الوصف)";
        //        instructionsSheet.Cells[9, 1].Value = "- Credits (الساعات المعتمدة) - Default: 3";
        //        instructionsSheet.Cells[10, 1].Value = "- Department (القسم)";
        //        instructionsSheet.Cells[11, 1].Value = "- Semester (الفصل الدراسي) - Default: 1";
        //        instructionsSheet.Cells[12, 1].Value = "- MaxStudents (الحد الأقصى للطلاب) - Default: 30";
        //        instructionsSheet.Cells[13, 1].Value = "- MinGPA (الحد الأدنى للمعدل التراكمي) - Default: 2.0";
        //        instructionsSheet.Cells[14, 1].Value = "- MinPassedHours (الحد الأدنى للساعات المنجزة) - Default: 0";
        //        instructionsSheet.Cells[15, 1].Value = "- IsActive (نشط) - Yes/No or نعم/لا - Default: Yes";
        //        instructionsSheet.Cells[16, 1].Value = "- Prerequisites (المتطلبات السابقة) - Comma-separated CourseCodes (e.g., 'MATH101,CS100')";

        //        instructionsSheet.Cells[18, 1].Value = "Notes:";
        //        instructionsSheet.Cells[18, 1].Style.Font.Bold = true;
        //        instructionsSheet.Cells[19, 1].Value = "- You can use either English or Arabic column headers";
        //        instructionsSheet.Cells[20, 1].Value = "- Existing courses with same CourseCode will be updated";
        //        instructionsSheet.Cells[21, 1].Value = "- New courses will be created";
        //        instructionsSheet.Cells[22, 1].Value = "- Remove sample rows before uploading your data";

        //        instructionsSheet.Cells[instructionsSheet.Dimension.Address].AutoFitColumns();

        //        var fileBytes = package.GetAsByteArray();
        //        return File(fileBytes,
        //            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        //            "Courses_Import_Template.xlsx");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error generating template");
        //        TempData["Error"] = $"Error generating template file: {ex.Message}";
        //        return RedirectToAction(nameof(Import));
        //    }
        //}

        // GET: Courses/ExportEnrollments/5
        [HttpGet]
        //[Route("ExportEnrollments")]
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
        //[Route("SelectFromExisting")]
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

        /// POST: Courses/AddToSemester
        [HttpPost]
        [ValidateAntiForgeryToken]
        //[Route("AddToSemester")]
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
        [HttpGet]
        //[Route("PopulateDropdowns")]
        private async Task PopulateDropdowns()
        {
            // Populate departments
            var departments = await _context.Departments
                .Include(d => d.College!)
                    .ThenInclude(c => c.University!)
                .Where(d => d.IsActive)
                .OrderBy(d => d.College!.University!.Name)
                .ThenBy(d => d.College!.Name)
                .ThenBy(d => d.Name)
                .Select(d => new SelectListItem
                {
                    Value = d.Id.ToString(),
                    Text = $"{d.College!.University!.Name} → {d.College!.Name} → {d.Name}"
                })
                .ToListAsync();

            ViewBag.DepartmentId = new SelectList(departments, "Value", "Text");

            // Populate semesters
            var semesters = await _context.Semesters
                .Where(s => s.IsActive)
                .OrderByDescending(s => s.AcademicYear)
                .ThenBy(s => s.SemesterType)
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = $"{s.Name} ({s.SemesterType} {s.AcademicYear})"
                })
                .ToListAsync();

            ViewBag.SemesterId = new SelectList(semesters, "Value", "Text");
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
        //[Route("LoadAvailablePrerequisites")]
        private async Task LoadAvailablePrerequisites(Course course)
        {
            var allCourses = await _courseService.GetAllCoursesAsync();
            course.AvailablePrerequisites = allCourses.Where(c => c.Id != course.Id).ToList();
            ViewBag.AvailableCourses = course.AvailablePrerequisites;
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
        [HttpGet]
        public IActionResult ImportReview()
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
                if (analysisResult == null)
                {
                    TempData["Error"] = "Invalid import data.";
                    return RedirectToAction(nameof(Import));
                }

                Console.WriteLine($"ImportReview - Valid: {analysisResult.ValidCourses?.Count}, Invalid: {analysisResult.InvalidCourses?.Count}");

                // Add serial numbers
                if (analysisResult.ValidCourses != null)
                {
                    int serial = 1;
                    foreach (var course in analysisResult.ValidCourses)
                    {
                        course.SerialNumber = serial++;
                    }
                }

                var viewModel = new ImportReviewViewModel
                {
                    ImportResult = analysisResult,
                    ImportSettings = new ImportSettings()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"IMPORT REVIEW ERROR: {ex}");
                TempData["Error"] = "Error loading import preview.";
                return RedirectToAction(nameof(Import));
            }
        }

        // POST: Courses/ImportExecute
        [HttpPost]
        //[HttpPost("ImportExecute")]
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

                var importResult = await _courseService.ExecuteImportAsync(analysisResult, settings);

                // Clean up session data
                HttpContext.Session.Remove("ImportAnalysis");
                HttpContext.Session.Remove("FileName");

                // Store import results for display on Index page
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

            // Headers with instructions - UPDATED WITH NEW FIELDS
            string[] headers = {
        "CourseCode (Required)",
        "CourseName (Required)",
        "Description",
        "Credits",
        "Department",
        "Semester",
        "MaxStudents",
        "MinGPA",
        "MinPassedHours",
        "Prerequisites", // ✅ NEW FIELD
        "CourseSpecification", // ✅ NEW FIELD
        "Icon", // ✅ NEW FIELD
        "IsActive"
    };

            string[] descriptions = {
        "Unique course identifier (e.g., CS101)",
        "Course name (e.g., Introduction to Computer Science)",
        "Course description (max 5000 characters)",
        "Credit hours (1-6)",
        "Department name (e.g., Computer Science)",
        "Semester number (1-8)",
        "Maximum students (1-1000)",
        "Minimum GPA required (0.00-4.00)",
        "Minimum passed hours required",
        "Prerequisite course codes separated by commas (e.g., CS101,MATH201)", // ✅ NEW
        "Course specification document link or text", // ✅ NEW
        "Icon name or URL (e.g., fas fa-code)", // ✅ NEW
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

            // Add sample data - UPDATED WITH NEW FIELDS
            var sampleCourses = new[]
            {
        new {
            CourseCode = "CS101",
            CourseName = "Introduction to Computer Science",
            Description = "Basic concepts of computer science and programming",
            Credits = 3,
            Department = "Computer Science",
            Semester = 1,
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
            Semester = 2,
            MaxStudents = 1000,
            MinGPA = 2.5m,
            MinPassedHours = 30,
            Prerequisites = "MATH101",
            CourseSpecification = "https://example.com/math201-spec.pdf",
            Icon = "fas fa-calculator",
            IsActive = "Yes"
        },
        new {
            CourseCode = "CS301",
            CourseName = "Data Structures",
            Description = "Advanced data structures and algorithms analysis",
            Credits = 3,
            Department = "Computer Science",
            Semester = 3,
            MaxStudents = 1000,
            MinGPA = 3.0m,
            MinPassedHours = 60,
            Prerequisites = "CS101,CS201",
            CourseSpecification = "https://example.com/cs301-spec.pdf",
            Icon = "fas fa-diagram-project",
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
                worksheet.Cells[i + 3, 6].Value = course.Semester;
                worksheet.Cells[i + 3, 7].Value = course.MaxStudents;
                worksheet.Cells[i + 3, 8].Value = course.MinGPA;
                worksheet.Cells[i + 3, 9].Value = course.MinPassedHours;
                worksheet.Cells[i + 3, 10].Value = course.Prerequisites; // ✅ NEW
                worksheet.Cells[i + 3, 11].Value = course.CourseSpecification; // ✅ NEW
                worksheet.Cells[i + 3, 12].Value = course.Icon; // ✅ NEW
                worksheet.Cells[i + 3, 13].Value = course.IsActive;
            }

            // Add data validation for numeric fields
            AddExcelDataValidation(worksheet);

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

        // POST: Courses/DeleteMultiple
        [HttpPost]
        //[HttpPost("DeleteMultiple")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMultiple(int[] selectedCourses)
        {
            try
            {
                if (selectedCourses == null || selectedCourses.Length == 0)
                {
                    TempData["Error"] = "No courses selected for deletion.";
                    return RedirectToAction(nameof(Index));
                }

                await _courseService.DeleteMultipleCoursesAsync(selectedCourses);
                TempData["Success"] = $"Successfully deleted {selectedCourses.Length} courses.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to delete courses: {ex.Message}";
                _logger.LogError(ex, "Error during bulk course deletion");
            }

            return RedirectToAction(nameof(Index));
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


        // In your CoursesController.cs

        [HttpGet]
        //[Route("GetCoursesBySemester")]
        public async Task<IActionResult> GetCoursesBySemester(int semesterId)
        {
            try
            {
                var courses = await _context.Courses
                    .Where(c => c.SemesterId == semesterId && c.IsActive)
                    .Include(c => c.Prerequisites) // Include prerequisites
                        .ThenInclude(p => p.PrerequisiteCourse) // Include prerequisite course details
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
                            .Select(p => p.PrerequisiteCourse!.CourseCode!)) // Get prerequisite codes
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
        /*
                [HttpGet]
                [Route("GetCoursesBySemester")]
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
                                description = c.Description ?? string.Empty, // Handle null description
                                isActive = c.IsActive,
                                prerequisites = c.Prerequisites != null ?
                                    string.Join(", ", c.Prerequisites
                                        .Where(p => p.PrerequisiteCourse != null && !string.IsNullOrEmpty(p.PrerequisiteCourse.CourseCode))
                                        .Select(p => p.PrerequisiteCourse!.CourseCode!))
                                    : string.Empty
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
        */
        [HttpGet]
        //[Route("GetAvailableCoursesForSemester")]
        public async Task<IActionResult> GetAvailableCoursesForSemester(int semesterId)
        {
            try
            {
                var availableCourses = await _context.Courses
                    .Where(c => c.SemesterId != semesterId && c.IsActive) // ✅ FIXED: Removed null check
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
        //[Route("VerifyExists/{id}")]
        public async Task<IActionResult> VerifyExists(int id)
        {
            var exists = await _context.Courses.AnyAsync(c => c.Id == id);
            return Json(exists);
        }





        // GET: Courses/Details/5
        [HttpGet]
        //[HttpGet("Details/{id}")]
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
        //[HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var course = await _courseService.GetCourseByIdAsync(id);
                if (course == null)
                {
                    return NotFound();
                }

                var prerequisites = await _courseService.GetCoursePrerequisitesAsync(id);
                course.SelectedPrerequisiteIds = prerequisites.Select(p => p.PrerequisiteCourseId).ToList();

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
        //[HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Course course)
        {
            if (id != course.Id)
            {
                return NotFound();
            }

            try
            {
                if (ModelState.IsValid)
                {
                    await _courseService.UpdateCourseAsync(course);

                    var existingPrerequisites = await _courseService.GetCoursePrerequisitesAsync(id);
                    var existingPrereqIds = existingPrerequisites.Select(p => p.PrerequisiteCourseId).ToList();

                    // FIX: Check for null and use empty list if null
                    var selectedPrereqIds = course.SelectedPrerequisiteIds ?? new List<int>();

                    foreach (var prereqId in selectedPrereqIds)
                    {
                        if (!existingPrereqIds.Contains(prereqId))
                        {
                            await _courseService.AddPrerequisiteAsync(course.Id, prereqId, null);
                        }
                    }

                    foreach (var existingPrereq in existingPrerequisites)
                    {
                        if (!selectedPrereqIds.Contains(existingPrereq.PrerequisiteCourseId))
                        {
                            await _courseService.RemovePrerequisiteAsync(existingPrereq.Id);
                        }
                    }

                    TempData["Success"] = $"Course {course.CourseCode} updated successfully!";
                    return RedirectToAction(nameof(Index));
                }

                await LoadAvailablePrerequisites(course);
                await PopulateDropdowns();
                return View(course);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating course");
                TempData["Error"] = $"Error updating course: {ex.Message}";
                await LoadAvailablePrerequisites(course);
                await PopulateDropdowns();
                return View(course);
            }
        }

        [HttpPost("CreateDefaultSemester")]
        public async Task<IActionResult> CreateDefaultSemester()
        {
            try
            {
                if (!await _context.Semesters.AnyAsync())
                {
                    var defaultSemester = new Semester
                    {
                        Name = "Fall " + DateTime.Now.Year,
                        SemesterType = "Fall",
                        AcademicYear = DateTime.Now.Year,
                        StartDate = new DateTime(DateTime.Now.Year, 9, 1),
                        EndDate = new DateTime(DateTime.Now.Year, 12, 31),
                        RegistrationStartDate = DateTime.Now.AddDays(-30),
                        RegistrationEndDate = DateTime.Now.AddDays(30),
                        IsActive = true,
                        IsCurrent = true,
                        IsRegistrationOpen = true
                    };

                    _context.Semesters.Add(defaultSemester);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Default semester created successfully!";
                }
                else
                {
                    TempData["Info"] = "Semesters already exist in the database.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error creating default semester: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        //[HttpGet("Debug")]
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