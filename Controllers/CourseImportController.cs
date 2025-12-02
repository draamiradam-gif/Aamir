// Controllers/CourseImportController.cs
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
    public class CourseImportController : BaseController
    {
        private readonly ICourseService _courseService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CourseImportController> _logger;

        public CourseImportController(ApplicationDbContext context, ICourseService courseService, ILogger<CourseImportController> logger)
        {
            _courseService = courseService;
            _context = context;
            _logger = logger;
        }

        // GET: CourseImport/Import
        [HttpGet]
        public IActionResult Import()
        {
            //return View();
            return View("~/Views/Courses/Import.cshtml");
        }

        // POST: CourseImport/Import
        // POST: CourseImport/Import (Enhanced with prerequisites support)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

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

                Console.WriteLine($"Calling AnalyzeExcelImportWithPrerequisitesAsync...");

                // Use the enhanced analysis that includes prerequisites
                var analysisResult = await AnalyzeExcelImportWithPrerequisitesAsync(stream);

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

        // GET: CourseImport/ImportReview
        [HttpGet]
        public async Task<IActionResult> ImportReview()
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

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

                //return View(viewModel);
                return View("~/Views/Courses/ImportReview.cshtml", viewModel);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading import review: {ex.Message}";
                return RedirectToAction(nameof(Import));
            }
        }

        // POST: CourseImport/ImportExecuteWithOptions
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportExecuteWithOptions(ImportSettings settings, ImportOptions options)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

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
                var importResult = await ExecuteImportWithPrerequisitesAsync(analysisResult, settings);

                // Clean up session data
                HttpContext.Session.Remove("ImportAnalysis");

                if (importResult.Success)
                {
                    TempData["ImportResult"] = importResult.Message;
                    TempData["ImportedCount"] = importResult.ImportedCount;
                    TempData["ErrorCount"] = importResult.ErrorCount;
                }

                TempData[importResult.Success ? "Success" : "Error"] = importResult.Message;
                return RedirectToAction("Index", "Courses"); // Redirect to Courses controller
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

        // POST: CourseImport/ImportExecute
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportExecute(ImportSettings settings)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

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
                var importResult = await ExecuteImportWithPrerequisitesAsync(analysisResult, settings);

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
                return RedirectToAction("Index", "Courses"); // Redirect to Courses controller
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Import execution failed: {ex.Message}";
                return RedirectToAction(nameof(Import));
            }
        }

        // GET: CourseImport/DownloadTemplate
        [HttpGet]
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

        // PRIVATE METHODS (copied from CoursesController)

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
                                    await CreateCourseCopyWithSemesterAsync(importedCourse, newCourseCode);
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

        private byte[] GenerateExcelTemplate()
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Courses Template");

            // Updated headers with prerequisites
            string[] headers = {
        "CourseCode (Required)",
        "CourseName (Required)",
        "Description",
        "Credits",
        "Department",
        "Semester (Optional)",
        "MaxStudents",
        "MinGPA",
        "MinPassedHours",
        "Prerequisites (Optional - comma separated course codes)",
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
        "Semester ID number (optional)",
        "Maximum students (1-1000)",
        "Minimum GPA required (0.00-4.00)",
        "Minimum passed hours required",
        "Prerequisite course codes separated by commas (e.g., CS101,MATH201,ENG101)",
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

            // Updated sample data with prerequisites examples
            var sampleCourses = new[]
            {
        new {
            CourseCode = "CS101",
            CourseName = "Introduction to Computer Science",
            Description = "Basic concepts of computer science and programming",
            Credits = 3,
            Department = "Computer Science",
            Semester = (object)1,
            MaxStudents = 1000,
            MinGPA = 2.0m,
            MinPassedHours = 0,
            Prerequisites = "", // No prerequisites
            CourseSpecification = "https://example.com/cs101-spec.pdf",
            Icon = "fas fa-laptop-code",
            IsActive = "Yes"
        },
        new {
            CourseCode = "CS201",
            CourseName = "Data Structures",
            Description = "Advanced data structures and algorithms",
            Credits = 4,
            Department = "Computer Science",
            Semester = (object)2,
            MaxStudents = 1000,
            MinGPA = 2.5m,
            MinPassedHours = 30,
            Prerequisites = "CS101,MATH101", // Multiple prerequisites
            CourseSpecification = "https://example.com/cs201-spec.pdf",
            Icon = "fas fa-diagram-project",
            IsActive = "Yes"
        },
        new {
            CourseCode = "MATH301",
            CourseName = "Advanced Calculus",
            Description = "Multivariable calculus and vector analysis",
            Credits = 4,
            Department = "Mathematics",
            Semester = (object)3,
            MaxStudents = 1000,
            MinGPA = 3.0m,
            MinPassedHours = 60,
            Prerequisites = "MATH201", // Single prerequisite
            CourseSpecification = "https://example.com/math301-spec.pdf",
            Icon = "fas fa-calculator",
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
                worksheet.Cells[i + 3, 10].Value = course.Prerequisites; // Prerequisites column
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

        private async Task CreateMissingSemesters(List<Course> coursesWithInvalidSemesters)
        {
            // Get unique semester IDs that need to be created (with proper nullable handling)
            var missingSemesterIds = coursesWithInvalidSemesters
                .Where(c => c.SemesterId.HasValue && c.SemesterId > 0)
                .Select(c => c.SemesterId!.Value)
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
                        Name = $"Auto-Created Semester {semesterId}",
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

        private async Task<Course> CreateCourseCopyWithSemesterAsync(Course sourceCourse, string newCourseCode)
        {
            var course = new Course
            {
                CourseCode = newCourseCode ?? string.Empty,
                CourseName = sourceCourse.CourseName ?? string.Empty,
                Description = sourceCourse.Description,
                Credits = sourceCourse.Credits,
                Department = sourceCourse.Department ?? "General",
                SemesterId = sourceCourse.SemesterId,
                IsActive = true,
                MaxStudents = sourceCourse.MaxStudents,
                MinGPA = sourceCourse.MinGPA,
                MinPassedHours = sourceCourse.MinPassedHours,
                PrerequisitesString = sourceCourse.PrerequisitesString,
                CreatedDate = DateTime.Now
            };

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();
            return course;
        }
        ////////////////
        ///
        // Add these methods to CourseImportController.cs

        /// <summary>
        /// Enhanced import analysis that includes prerequisites parsing
        /// </summary>
        private Task<ImportResult> AnalyzeExcelImportWithPrerequisitesAsync(Stream stream)
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
                
                        // Semester ID (nullable)
                        var semesterIdValue = worksheet.Cells[row, 6].Value?.ToString()?.Trim();
                        int? semesterId = null;
                        if (!string.IsNullOrEmpty(semesterIdValue) && int.TryParse(semesterIdValue, out int parsedSemesterId))
                        {
                            semesterId = parsedSemesterId;
                        }
                
                        var maxStudents = int.TryParse(worksheet.Cells[row, 7].Value?.ToString(), out int ms) ? ms : 30;
                        var minGPA = decimal.TryParse(worksheet.Cells[row, 8].Value?.ToString(), out decimal gpa) ? gpa : 2.0m;
                        var minPassedHours = int.TryParse(worksheet.Cells[row, 9].Value?.ToString(), out int mph) ? mph : 0;
                
                        // NEW: Read prerequisites
                        var prerequisites = worksheet.Cells[row, 10].Value?.ToString()?.Trim() ?? string.Empty;
                
                        var isActive = GetBoolCellValue(worksheet, row, 13, true);

                        // Validation
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

                        // Validate prerequisites format (if provided)
                        if (!string.IsNullOrEmpty(prerequisites))
                        {
                            var prerequisiteErrors = ValidatePrerequisitesFormat(prerequisites);
                            if (prerequisiteErrors.Any())
                            {
                                invalidCourses.Add(new InvalidCourse
                                {
                                    RowNumber = row,
                                    ErrorMessage = $"Invalid prerequisites format: {string.Join(", ", prerequisiteErrors)}",
                                    CourseCode = courseCode ?? string.Empty,
                                    CourseName = courseName ?? string.Empty
                                });
                                continue;
                            }
                        }

                        // Create Course entity
                        var course = new Course
                        {
                            SerialNumber = row - 1,
                            CourseCode = courseCode ?? string.Empty,
                            CourseName = courseName ?? string.Empty,
                            Description = description,
                            Credits = credits,
                            Department = department ?? "General",
                            SemesterId = semesterId,
                            MaxStudents = maxStudents,
                            MinGPA = minGPA,
                            MinPassedHours = minPassedHours,
                            IsActive = isActive,
                            CreatedDate = DateTime.Now,
                            PrerequisitesString = prerequisites
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

        /// <summary>
        /// Validate prerequisites format
        /// </summary>
        private List<string> ValidatePrerequisitesFormat(string prerequisites)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(prerequisites))
                return errors;

            var prerequisiteCodes = prerequisites
                .Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList();

            foreach (var code in prerequisiteCodes)
            {
                if (code.Length > 20)
                {
                    errors.Add($"Prerequisite code '{code}' is too long (max 20 characters)");
                }

                if (!System.Text.RegularExpressions.Regex.IsMatch(code, @"^[a-zA-Z0-9\-_\s]+$"))
                {
                    errors.Add($"Prerequisite code '{code}' contains invalid characters");
                }
            }

            return errors;
        }

        /// <summary>
        /// Process prerequisites for a course after it's created/updated
        /// </summary>
        private async Task ProcessCoursePrerequisitesAsync(Course course, string prerequisitesString)
        {
            if (course?.Id == null)
            {
                _logger.LogWarning("Cannot process prerequisites for null course or course without ID");
                return;
            }

            if (string.IsNullOrWhiteSpace(prerequisitesString))
                return;

            try
            {
                var prerequisiteCodes = prerequisitesString
                    .Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Trim())
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .Distinct()
                    .ToList();

                if (!prerequisiteCodes.Any())
                    return;

                // Get all existing course codes and their IDs
                var existingCourses = await _context.Courses
                    .Where(c => c.IsActive)
                    .Select(c => new { c.Id, c.CourseCode })
                    .ToDictionaryAsync(c => c.CourseCode.ToUpper(), c => c.Id);

                var prerequisitesAdded = 0;
                var prerequisiteErrors = new List<string>();

                // First, remove existing prerequisites for this course (for OVERRIDE case)
                var existingPrerequisites = await _context.CoursePrerequisites
                    .Where(cp => cp.CourseId == course.Id)
                    .ToListAsync();

                if (existingPrerequisites.Any())
                {
                    _context.CoursePrerequisites.RemoveRange(existingPrerequisites);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Removed {Count} existing prerequisites for course {CourseCode}",
                        existingPrerequisites.Count, course.CourseCode);
                }

                foreach (var prerequisiteCode in prerequisiteCodes)
                {
                    try
                    {
                        var normalizedCode = prerequisiteCode.ToUpper();

                        // Check if prerequisite course exists
                        if (existingCourses.TryGetValue(normalizedCode, out int prerequisiteCourseId))
                        {
                            // Prevent self-referencing
                            if (prerequisiteCourseId == course.Id)
                            {
                                prerequisiteErrors.Add($"Cannot add self as prerequisite: {prerequisiteCode}");
                                continue;
                            }

                            // Check if prerequisite already exists (shouldn't after removal, but just in case)
                            var existingPrerequisite = await _context.CoursePrerequisites
                                .FirstOrDefaultAsync(cp =>
                                    cp.CourseId == course.Id &&
                                    cp.PrerequisiteCourseId == prerequisiteCourseId);

                            if (existingPrerequisite == null)
                            {
                                var prerequisite = new CoursePrerequisite
                                {
                                    CourseId = course.Id,
                                    PrerequisiteCourseId = prerequisiteCourseId,
                                    IsRequired = true,
                                    MinGrade = null
                                };

                                _context.CoursePrerequisites.Add(prerequisite);
                                prerequisitesAdded++;
                                _logger.LogInformation("Added prerequisite: {PrerequisiteCode} for course {CourseCode}",
                                    prerequisiteCode, course.CourseCode);
                            }
                        }
                        else
                        {
                            prerequisiteErrors.Add($"Prerequisite course not found: {prerequisiteCode}");
                            _logger.LogWarning("Prerequisite course not found: {PrerequisiteCode} for course {CourseCode}",
                                prerequisiteCode, course.CourseCode);
                        }
                    }
                    catch (Exception ex)
                    {
                        prerequisiteErrors.Add($"Error processing prerequisite '{prerequisiteCode}': {ex.Message}");
                        _logger.LogError(ex, "Error processing prerequisite {PrerequisiteCode} for course {CourseCode}",
                            prerequisiteCode, course.CourseCode);
                    }
                }

                if (prerequisitesAdded > 0)
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Successfully added {Count} prerequisites for course {CourseCode}",
                        prerequisitesAdded, course.CourseCode);
                }

                if (prerequisiteErrors.Any())
                {
                    _logger.LogWarning("Prerequisite errors for course {CourseCode}: {Errors}",
                        course.CourseCode, string.Join("; ", prerequisiteErrors));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing prerequisites for course {CourseCode}", course.CourseCode);
                throw new Exception($"Failed to process prerequisites: {ex.Message}");
            }
        }

        /// <summary>
        /// Enhanced import execution with prerequisites support
        /// </summary>
        private async Task<ImportResult> ExecuteImportWithPrerequisitesAsync(ImportResult analysisResult, ImportSettings settings)
        {
            try
            {
                int importedCount = 0;
                int errorCount = 0;
                int prerequisitesProcessed = 0;
                var errors = new List<string>();
                var prerequisiteErrors = new List<string>();

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

                // Store courses that need prerequisite processing
                var coursesToProcessPrerequisites = new List<(Course Course, string PrerequisitesString)>();

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

                        Course? course = null;
                        bool isNewCourse = false;

                        if (courseExists)
                        {
                            switch (settings.DuplicateHandling)
                            {
                                case "Skip":
                                    errors.Add($"Skipped: Course '{importedCourse.CourseCode}' already exists");
                                    continue;
                                case "Override":
                                    // Find and update existing course
                                    course = await _context.Courses
                                        .FirstOrDefaultAsync(c => c.CourseCode.ToLower() == normalizedCode);

                                    if (course != null)
                                    {
                                        UpdateCourseFromImport(course, importedCourse);
                                        importedCount++;

                                        // Store for prerequisite processing (OVERRIDE case)
                                        if (!string.IsNullOrWhiteSpace(importedCourse.PrerequisitesString))
                                        {
                                            coursesToProcessPrerequisites.Add((course, importedCourse.PrerequisitesString));
                                        }
                                    }
                                    break;
                                case "CreateNew":
                                    // Create new course with unique code
                                    var newCourseCode = await GenerateUniqueCourseCode(importedCourse.CourseCode);
                                    course = await CreateCourseCopyWithSemesterAsync(importedCourse, newCourseCode);
                                    isNewCourse = true;
                                    importedCount++;
                                    break;
                            }
                        }
                        else
                        {
                            // Create new course with validated semester ID
                            course = await CreateNewCourseWithSemesterAsync(importedCourse);
                            isNewCourse = true;
                            importedCount++;
                            // Add to existing codes to prevent duplicates in same import
                            existingCourseCodes.Add(normalizedCode);
                        }

                        // Store for prerequisite processing (NEW courses)
                        if (isNewCourse && course != null && !string.IsNullOrWhiteSpace(importedCourse.PrerequisitesString))
                        {
                            coursesToProcessPrerequisites.Add((course, importedCourse.PrerequisitesString));
                        }
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        errors.Add($"Error importing '{importedCourse.CourseCode}': {ex.Message}");
                        _logger.LogError(ex, "Error importing course {CourseCode}", importedCourse.CourseCode);
                    }
                }

                // Save all course changes first
                await _context.SaveChangesAsync();

                // NOW process prerequisites for all stored courses
                foreach (var (course, prerequisitesString) in coursesToProcessPrerequisites)
                {
                    try
                    {
                        await ProcessCoursePrerequisitesAsync(course, prerequisitesString);
                        prerequisitesProcessed++;
                    }
                    catch (Exception ex)
                    {
                        prerequisiteErrors.Add($"Course '{course.CourseCode}': {ex.Message}");
                        _logger.LogError(ex, "Error processing prerequisites for course {CourseCode}", course.CourseCode);
                    }
                }

                return new ImportResult
                {
                    Success = importedCount > 0,
                    Message = $"Imported {importedCount} courses successfully. {errorCount} errors. Processed prerequisites for {prerequisitesProcessed} courses.",
                    ImportedCount = importedCount,
                    ErrorCount = errorCount,
                    PrerequisitesProcessed = prerequisitesProcessed,
                    Errors = errors,
                    PrerequisiteErrors = prerequisiteErrors
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in import execution with prerequisites");
                return new ImportResult
                {
                    Success = false,
                    Message = $"Import failed: {ex.Message}",
                    ImportedCount = 0,
                    ErrorCount = analysisResult.ValidCourses.Count,
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        // Helper methods for the enhanced import

        private void UpdateCourseFromImport(Course existingCourse, Course importedCourse)
        {
            existingCourse.CourseName = importedCourse.CourseName ?? string.Empty;
            existingCourse.Description = importedCourse.Description;
            existingCourse.Credits = importedCourse.Credits;
            existingCourse.Department = importedCourse.Department ?? "General";
            existingCourse.SemesterId = importedCourse.SemesterId;
            existingCourse.MaxStudents = importedCourse.MaxStudents;
            existingCourse.MinGPA = importedCourse.MinGPA;
            existingCourse.MinPassedHours = importedCourse.MinPassedHours;
            existingCourse.IsActive = true;
            existingCourse.ModifiedDate = DateTime.Now;

            // Update prerequisites string (will be processed separately if needed)
            if (!string.IsNullOrWhiteSpace(importedCourse.PrerequisitesString))
            {
                existingCourse.PrerequisitesString = importedCourse.PrerequisitesString;
            }
        }

        private async Task<Course> CreateNewCourseWithSemesterAsync(Course importedCourse)
        {
            var course = new Course
            {
                CourseCode = importedCourse.CourseCode ?? string.Empty,
                CourseName = importedCourse.CourseName ?? string.Empty,
                Description = importedCourse.Description,
                Credits = importedCourse.Credits,
                Department = importedCourse.Department ?? "General",
                SemesterId = importedCourse.SemesterId,
                IsActive = true,
                MaxStudents = importedCourse.MaxStudents,
                MinGPA = importedCourse.MinGPA,
                MinPassedHours = importedCourse.MinPassedHours,
                PrerequisitesString = importedCourse.PrerequisitesString,
                CreatedDate = DateTime.Now
            };

            _context.Courses.Add(course);
            await _context.SaveChangesAsync(); // This makes it truly async
            return course;
        }

        //private async Task<Course> CreateCourseCopyWithSemester(Course sourceCourse, string newCourseCode)
        //{
        //    var course = new Course
        //    {
        //        CourseCode = newCourseCode ?? string.Empty,
        //        CourseName = sourceCourse.CourseName ?? string.Empty,
        //        Description = sourceCourse.Description,
        //        Credits = sourceCourse.Credits,
        //        Department = sourceCourse.Department ?? "General",
        //        SemesterId = sourceCourse.SemesterId,
        //        IsActive = true,
        //        MaxStudents = sourceCourse.MaxStudents,
        //        MinGPA = sourceCourse.MinGPA,
        //        MinPassedHours = sourceCourse.MinPassedHours,
        //        PrerequisitesString = sourceCourse.PrerequisitesString, // Store for processing
        //        CreatedDate = DateTime.Now
        //    };

        //    _context.Courses.Add(course);
        //    return course;
        //}

        private bool GetBoolCellValue(ExcelWorksheet worksheet, int row, int column, bool defaultValue)
        {
            try
            {
                var value = worksheet.Cells[row, column].Value?.ToString()?.Trim().ToLower();
                return value switch
                {
                    "yes" or "true" or "1" or "y" => true,
                    "no" or "false" or "0" or "n" => false,
                    _ => defaultValue
                };
            }
            catch
            {
                return defaultValue;
            }
        }


    }
}