using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;
using StudentManagementSystem.Services;
using System.ComponentModel.DataAnnotations;

namespace StudentManagementSystem.Controllers
{
    public class GradingController : BaseController
    {
        private readonly IGradingService _gradingService;
        private readonly ApplicationDbContext _context;

        public GradingController(IGradingService gradingService, ApplicationDbContext context)
        {
            _gradingService = gradingService;
            _context = context;
        }

        // Grading Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var courses = await _context.Courses
                .Include(c => c.CourseDepartment)
                .Where(c => c.IsActive)
                .ToListAsync();

            var semesters = await _context.Semesters
                .Where(s => s.IsActive)
                .ToListAsync();

            var viewModel = new GradingDashboardViewModel
            {
                Courses = courses,
                Semesters = semesters,
                RecentGradingActivities = await GetRecentGradingActivities()
            };

            return View(viewModel);
        }

        // Manage Grading Components for a Course
        public async Task<IActionResult> ManageComponents(int? courseId)
        {
            // Get courses list for dropdown
            var allCourses = await _context.Courses
                .Include(c => c.CourseDepartment)
                .Where(c => c.IsActive)
                .ToListAsync();

            // If no courseId provided OR courseId is 0, show course selection
            if (!courseId.HasValue || courseId.Value == 0)
            {
                var selectionModel = new ManageComponentsViewModel
                {
                    Courses = allCourses,
                    Components = new List<GradingComponent>(),
                    AvailableTemplates = await _context.GradingTemplates.ToListAsync(),
                    TotalWeight = 0
                };

                return View(selectionModel);
            }

            var course = await _context.Courses
                .Include(c => c.CourseDepartment)
                .FirstOrDefaultAsync(c => c.Id == courseId.Value);

            if (course == null)
            {
                TempData["Error"] = "Course not found!";

                // Return to selection view if course doesn't exist
                var errorModel = new ManageComponentsViewModel
                {
                    Courses = allCourses,
                    Components = new List<GradingComponent>(),
                    AvailableTemplates = await _context.GradingTemplates.ToListAsync(),
                    TotalWeight = 0
                };

                return View(errorModel);
            }

            var components = await _context.GradingComponents
                .Where(gc => gc.CourseId == courseId.Value)
                .OrderBy(gc => gc.ComponentType)
                .ToListAsync();

            var templates = await _context.GradingTemplates.ToListAsync();

            var componentsModel = new ManageComponentsViewModel
            {
                Course = course,
                Courses = allCourses,
                Components = components,
                AvailableTemplates = templates,
                TotalWeight = components.Sum(c => c.WeightPercentage)
            };

            return View(componentsModel);
        }

        // Add/Edit Grading Component
        [HttpPost]
        public async Task<IActionResult> SaveComponent(GradingComponent component)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            // Clear ModelState for ComponentType since it's an enum, not a string
            ModelState.Remove("ComponentType");

            if (ModelState.IsValid)
            {
                try
                {
                    if (component.Id == 0)
                    {
                        // Create new component
                        component.IsActive = true;
                        _context.GradingComponents.Add(component);
                    }
                    else
                    {
                        // Update existing component
                        var existingComponent = await _context.GradingComponents
                            .FirstOrDefaultAsync(gc => gc.Id == component.Id);

                        if (existingComponent == null)
                        {
                            TempData["Error"] = "Grading component not found!";
                            return RedirectToAction(nameof(ManageComponents), new { courseId = component.CourseId });
                        }

                        existingComponent.Name = component.Name;
                        existingComponent.ComponentType = component.ComponentType;
                        existingComponent.WeightPercentage = component.WeightPercentage;
                        existingComponent.MaximumMarks = component.MaximumMarks;
                        existingComponent.Description = component.Description;
                        existingComponent.IncludeInFinalGrade = component.IncludeInFinalGrade;
                    }

                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Grading component saved successfully!";

                    // Validate total weight after saving
                    var isValid = await _gradingService.ValidateGradingComponents(component.CourseId);
                    if (!isValid)
                    {
                        TempData["Warning"] = "Warning: Total weight percentage does not equal 100%. Please review your grading components.";
                    }

                    return RedirectToAction(nameof(ManageComponents), new { courseId = component.CourseId });
                }
                catch (DbUpdateException ex)
                {
                    TempData["Error"] = $"Error saving grading component: {ex.Message}";
                    return RedirectToAction(nameof(ManageComponents), new { courseId = component.CourseId });
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"An unexpected error occurred: {ex.Message}";
                    return RedirectToAction(nameof(ManageComponents), new { courseId = component.CourseId });
                }
            }

            // If we got this far, something failed; redisplay form
            var course = await _context.Courses
                .Include(c => c.CourseDepartment)
                .FirstOrDefaultAsync(c => c.Id == component.CourseId);

            if (course == null)
            {
                return NotFound();
            }

            var components = await _context.GradingComponents
                .Where(gc => gc.CourseId == component.CourseId)
                .OrderBy(gc => gc.ComponentType)
                .ToListAsync();

            var templates = await _context.GradingTemplates.ToListAsync();

            var viewModel = new ManageComponentsViewModel
            {
                Course = course,
                Components = components,
                AvailableTemplates = templates,
                TotalWeight = components.Sum(c => c.WeightPercentage)
            };

            return View("ManageComponents", viewModel);
        }

        // Apply Template to Course
        [HttpPost]
        public async Task<IActionResult> ApplyTemplate(int courseId, int templateId)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            try
            {
                var result = await _gradingService.ApplyGradingTemplateToCourse(courseId, templateId);
                if (result)
                {
                    TempData["Success"] = "Grading template applied successfully!";
                }
                else
                {
                    TempData["Error"] = "Failed to apply grading template. Course or template not found.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error applying template: {ex.Message}";
            }

            return RedirectToAction(nameof(ManageComponents), new { courseId });
        }

        // Delete Grading Component
        [HttpPost]
        public async Task<IActionResult> DeleteComponent(int id, int courseId)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            try
            {
                var component = await _context.GradingComponents.FindAsync(id);
                if (component != null)
                {
                    // Check if there are any grades associated with this component
                    var hasGrades = await _context.StudentGrades.AnyAsync(sg => sg.GradingComponentId == id);

                    if (hasGrades)
                    {
                        TempData["Error"] = "Cannot delete component that has grades assigned. Please remove grades first or deactivate the component.";
                        return RedirectToAction(nameof(ManageComponents), new { courseId });
                    }

                    _context.GradingComponents.Remove(component);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Grading component deleted successfully!";
                }
                else
                {
                    TempData["Error"] = "Grading component not found!";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting component: {ex.Message}";
            }

            return RedirectToAction(nameof(ManageComponents), new { courseId });
        }

        // Enter Grades for Students
        // Enter Grades for Students - MAKE PARAMETERS OPTIONAL
        public async Task<IActionResult> EnterGrades(int? courseId, int? semesterId, int? componentId)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            // Get courses and semesters for dropdowns
            var allCourses = await _context.Courses
                .Include(c => c.CourseDepartment)
                .Where(c => c.IsActive)
                .ToListAsync();

            var allSemesters = await _context.Semesters
                .Where(s => s.IsActive)
                .ToListAsync();

            // If no course or semester selected, show selection form
            if (!courseId.HasValue || !semesterId.HasValue)
            {
                var selectionModel = new EnterGradesViewModel
                {
                    Courses = allCourses,
                    Semesters = allSemesters
                };

                return View(selectionModel);
            }

            var course = await _context.Courses
                .Include(c => c.CourseDepartment)
                .FirstOrDefaultAsync(c => c.Id == courseId.Value);

            var semester = await _context.Semesters.FindAsync(semesterId.Value);

            if (course == null || semester == null)
            {
                return NotFound();
            }

            var components = await _context.GradingComponents
                .Where(gc => gc.CourseId == courseId.Value)
                .ToListAsync();

            if (!components.Any())
            {
                TempData["Warning"] = "No grading components defined for this course. Please set up grading components first.";
                return RedirectToAction(nameof(ManageComponents), new { courseId = courseId.Value });
            }

            var selectedComponent = componentId.HasValue
                ? components.FirstOrDefault(c => c.Id == componentId.Value)
                : components.First();

            var enrollments = await _context.CourseEnrollments
                .Include(ce => ce.Student)
                .Where(ce => ce.CourseId == courseId.Value && ce.SemesterId == semesterId.Value && ce.IsActive)
                .ToListAsync();

            var gradesModel = new EnterGradesViewModel
            {
                Course = course,
                Semester = semester,
                Courses = allCourses, // Include for dropdown
                Semesters = allSemesters, // Include for dropdown
                Components = components,
                SelectedComponent = selectedComponent,
                Enrollments = enrollments,
                StudentGrades = await GetStudentGradesForComponent(enrollments, selectedComponent?.Id ?? 0)
            };

            return View(gradesModel);
        }

        [HttpPost]
        public async Task<IActionResult> SaveGrades(List<StudentGrade> grades)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            if (grades != null && grades.Any())
            {
                foreach (var grade in grades)
                {
                    await _gradingService.UpdateStudentGrade(grade);
                }
                TempData["Success"] = "Grades saved successfully!";

                // Safe way to get redirect parameters
                if (grades.Count > 0 && grades[0] != null)
                {
                    return RedirectToAction(nameof(EnterGrades), new
                    {
                        courseId = grades[0].CourseId,
                        semesterId = grades[0].SemesterId,
                        componentId = grades[0].GradingComponentId
                    });
                }
            }

            return RedirectToAction(nameof(Dashboard));
        }

        // View Final Grades
        public async Task<IActionResult> FinalGrades(int courseId, int semesterId)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            var finalGrades = await _context.FinalGrades
                .Include(fg => fg.Student)
                .Where(fg => fg.CourseId == courseId && fg.SemesterId == semesterId)
                .ToListAsync();

            var course = await _context.Courses
                .Include(c => c.CourseDepartment)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            var semester = await _context.Semesters.FindAsync(semesterId);

            var viewModel = new FinalGradesViewModel
            {
                Course = course,
                Semester = semester,
                FinalGrades = finalGrades,
                Summary = await _gradingService.GetCourseGradingSummary(courseId, semesterId)
            };

            return View(viewModel);
        }

        // Grade Reports
        public async Task<IActionResult> Reports()
        {
            var courses = await _context.Courses
                .Include(c => c.CourseDepartment)
                .Where(c => c.IsActive)
                .ToListAsync();

            var semesters = await _context.Semesters
                .Where(s => s.IsActive)
                .ToListAsync();

            var viewModel = new GradeReportsViewModel
            {
                Courses = courses,
                Semesters = semesters
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> GenerateReport(int courseId, int semesterId, string reportType)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            var report = await _gradingService.GenerateGradeReport(courseId, semesterId);
            // Implement report generation logic (PDF, Excel, etc.)
            return View("ReportView", report);
        }

        // Import/Export Grades
        public async Task<IActionResult> ImportExport()
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            var courses = await _context.Courses.Where(c => c.IsActive).ToListAsync();
            var semesters = await _context.Semesters.Where(s => s.IsActive).ToListAsync();

            var viewModel = new ImportExportViewModel
            {
                Courses = courses,
                Semesters = semesters
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> ImportGrades(IFormFile file, int courseId, int semesterId)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please select a file to import.";
                return RedirectToAction(nameof(ImportExport));
            }

            using var stream = file.OpenReadStream();
            var result = await _gradingService.ImportGradesFromExcel(stream, courseId, semesterId);

            if (result.Success)
            {
                TempData["Success"] = $"Successfully imported {result.ProcessedRecords} grades.";
            }
            else
            {
                TempData["Error"] = $"Import failed: {result.Message}";
            }

            return RedirectToAction(nameof(ImportExport));
        }

        public async Task<IActionResult> ExportGrades(int courseId, int semesterId)
        {
            var stream = await _gradingService.ExportGradesToExcel(courseId, semesterId);
            var course = await _context.Courses.FindAsync(courseId);
            var semester = await _context.Semesters.FindAsync(semesterId);

            var fileName = $"{course?.CourseCode}_{semester?.Name}_Grades_{DateTime.Now:yyyyMMdd}.xlsx";
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        // Helper Methods
        private Task<List<GradingActivity>> GetRecentGradingActivities()
        {
            // Return empty list for now - can implement actual logic later
            return Task.FromResult(new List<GradingActivity>());
        }

        private async Task<List<StudentGrade>> GetStudentGradesForComponent(List<CourseEnrollment> enrollments, int componentId)
        {
            var studentIds = enrollments.Select(e => e.StudentId).ToList();
            return await _context.StudentGrades
                .Where(sg => studentIds.Contains(sg.StudentId) && sg.GradingComponentId == componentId)
                .ToListAsync();
        }

       
        // NEW ACTIONS - TEMPLATES MANAGEMENT
        public async Task<IActionResult> Templates()
        {
            var templates = await _context.GradingTemplates
                .Include(t => t.Components)
                .ToListAsync();

            return View(templates);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTemplate(GradingTemplate template)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // If this is set as default, unset any existing default
                    if (template.IsDefault)
                    {
                        var existingDefault = await _context.GradingTemplates
                            .Where(t => t.IsDefault)
                            .FirstOrDefaultAsync();

                        if (existingDefault != null)
                        {
                            existingDefault.IsDefault = false;
                        }
                    }

                    _context.GradingTemplates.Add(template);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Grading template created successfully!";
                    return RedirectToAction(nameof(Templates));
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Error creating template: {ex.Message}";
                }
            }

            // If we got here, something went wrong
            return View("Templates", await _context.GradingTemplates.Include(t => t.Components).ToListAsync());
        }

        public async Task<IActionResult> TemplateDetails(int id)
        {
            var template = await _context.GradingTemplates
                .Include(t => t.Components)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (template == null)
            {
                return NotFound();
            }

            return View(template);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteTemplate(int id)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            try
            {
                var template = await _context.GradingTemplates
                    .Include(t => t.Components)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (template == null)
                {
                    TempData["Error"] = "Template not found!";
                    return RedirectToAction(nameof(Templates));
                }

                // Check if template is in use
                var coursesUsingTemplate = await _context.Courses
                    .Where(c => _context.GradingComponents
                        .Where(gc => gc.CourseId == c.Id)
                        .Any(gc => gc.Name.Contains(template.TemplateName)))
                    .CountAsync();

                if (coursesUsingTemplate > 0)
                {
                    TempData["Error"] = $"Cannot delete template. It is being used by {coursesUsingTemplate} course(s).";
                    return RedirectToAction(nameof(Templates));
                }

                _context.GradingTemplates.Remove(template);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Template deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting template: {ex.Message}";
            }

            return RedirectToAction(nameof(Templates));
        }

        // NEW ACTIONS - GRADE SCALES MANAGEMENT
        public async Task<IActionResult> GradeScales()
        {
            var gradeScales = await _context.GradeScales
                .OrderBy(g => g.MinPercentage)
                .ToListAsync();

            return View(gradeScales);
        }

        [HttpPost]
        public async Task<IActionResult> SaveGradeScale(GradeScale scale)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Validate percentage ranges
                    var overlappingScales = await _context.GradeScales
                        .Where(g => g.Id != scale.Id &&
                                   ((g.MinPercentage <= scale.MinPercentage && g.MaxPercentage >= scale.MinPercentage) ||
                                    (g.MinPercentage <= scale.MaxPercentage && g.MaxPercentage >= scale.MaxPercentage) ||
                                    (scale.MinPercentage <= g.MinPercentage && scale.MaxPercentage >= g.MinPercentage)))
                        .ToListAsync();

                    if (overlappingScales.Any())
                    {
                        TempData["Error"] = "Grade scale percentage range overlaps with existing scales.";
                        return RedirectToAction(nameof(GradeScales));
                    }

                    if (scale.Id == 0)
                    {
                        _context.GradeScales.Add(scale);
                    }
                    else
                    {
                        _context.GradeScales.Update(scale);
                    }

                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Grade scale saved successfully!";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Error saving grade scale: {ex.Message}";
                }
            }
            else
            {
                TempData["Error"] = "Please correct the validation errors.";
            }

            return RedirectToAction(nameof(GradeScales));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteGradeScale(int id)
        {
            if (!IsAdminUser())
            {
                return RedirectUnauthorized("Admin access required.");
            }

            try
            {
                var scale = await _context.GradeScales.FindAsync(id);
                if (scale != null)
                {
                    _context.GradeScales.Remove(scale);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Grade scale deleted successfully!";
                }
                else
                {
                    TempData["Error"] = "Grade scale not found!";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting grade scale: {ex.Message}";
            }

            return RedirectToAction(nameof(GradeScales));
        }

        // NEW ACTIONS - STUDENT TRANSCRIPT
        public async Task<IActionResult> StudentTranscript(int studentId)
        {
            var student = await _context.Students
                .Include(s => s.CourseEnrollments)
                    .ThenInclude(ce => ce.Course)
                .Include(s => s.CourseEnrollments)
                    .ThenInclude(ce => ce.Semester)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null)
            {
                return NotFound();
            }

            // Calculate GPA and create transcript
            var completedEnrollments = student.CourseEnrollments
                .Where(e => e.GradeStatus == GradeStatus.Completed && e.GradePoints.HasValue)
                .ToList();

            var totalGradePoints = completedEnrollments
                .Sum(e => e.GradePoints!.Value * (e.Course?.Credits ?? 0));

            var totalCredits = completedEnrollments
                .Sum(e => e.Course?.Credits ?? 0);

            var gpa = totalCredits > 0 ? totalGradePoints / totalCredits : 0;

            var transcript = new StudentTranscript
            {
                Student = student,
                Enrollments = student.CourseEnrollments.ToList(),
                GPA = gpa,
                GeneratedDate = DateTime.Now
            };

            return View(transcript);
        }

        public async Task<IActionResult> StudentSearch()
        {
            var students = await _context.Students
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();

            return View(students);
        }

        // NEW ACTIONS - BULK OPERATIONS
        public async Task<IActionResult> BulkOperations()
        {
            var courses = await _context.Courses
                .Where(c => c.IsActive)
                .ToListAsync();

            var semesters = await _context.Semesters
                .Where(s => s.IsActive)
                .ToListAsync();

            var viewModel = new BulkOperationsViewModel
            {
                Courses = courses,
                Semesters = semesters
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> BulkUploadGrades(IFormFile file, int courseId, int semesterId)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please select a file to upload.";
                return RedirectToAction(nameof(BulkOperations));
            }

            try
            {
                using var stream = file.OpenReadStream();
                var result = await _gradingService.ImportGradesFromExcel(stream, courseId, semesterId);

                if (result.Success)
                {
                    TempData["Success"] = $"Successfully processed {result.ProcessedRecords} grades. {result.FailedRecords} failed.";
                }
                else
                {
                    TempData["Error"] = $"Upload failed: {result.Message}";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error during bulk upload: {ex.Message}";
            }

            return RedirectToAction(nameof(BulkOperations));
        }

        [HttpPost]
        public async Task<IActionResult> BulkCalculateFinalGrades(int courseId, int semesterId)
        {
            try
            {
                var enrollments = await _context.CourseEnrollments
                    .Where(ce => ce.CourseId == courseId && ce.SemesterId == semesterId && ce.IsActive)
                    .Include(ce => ce.Student)
                    .ToListAsync();

                int processed = 0;
                int errors = 0;

                foreach (var enrollment in enrollments)
                {
                    try
                    {
                        await _gradingService.CalculateFinalGrade(enrollment.StudentId, courseId, semesterId);
                        processed++;
                    }
                    catch
                    {
                        errors++;
                    }
                }

                if (errors == 0)
                {
                    TempData["Success"] = $"Successfully calculated final grades for {processed} students.";
                }
                else
                {
                    TempData["Warning"] = $"Calculated grades for {processed} students. {errors} calculations failed.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error during bulk calculation: {ex.Message}";
            }

            return RedirectToAction(nameof(BulkOperations));
        }

        public async Task<IActionResult> DownloadTemplate(string type)
        {
            try
            {
                using var memoryStream = new MemoryStream();

                // Use EPPlus to create Excel templates (make sure you have EPPlus package installed)
                using (var package = new OfficeOpenXml.ExcelPackage(memoryStream))
                {
                    var worksheet = package.Workbook.Worksheets.Add("Template");

                    switch (type.ToLower())
                    {
                        case "grades":
                            await CreateGradesTemplate(worksheet);
                            break;
                        case "roster":
                            await CreateRosterTemplate(worksheet);
                            break;
                        case "export":
                            await CreateExportTemplate(worksheet);
                            break;
                        default:
                            TempData["Error"] = "Invalid template type.";
                            return RedirectToAction(nameof(BulkOperations));
                    }

                    // Save the package to the memory stream
                    await package.SaveAsync();
                }

                memoryStream.Position = 0;
                var fileName = $"{type}_template_{DateTime.Now:yyyyMMdd}.xlsx";

                return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error generating template: {ex.Message}";
                return RedirectToAction(nameof(BulkOperations));
            }
        }

        // Helper methods for creating templates
        private async Task CreateGradesTemplate(OfficeOpenXml.ExcelWorksheet worksheet)
        {
            // Add headers
            worksheet.Cells[1, 1].Value = "StudentID";
            worksheet.Cells[1, 2].Value = "StudentName";
            worksheet.Cells[1, 3].Value = "CourseCode";
            worksheet.Cells[1, 4].Value = "Component";
            worksheet.Cells[1, 5].Value = "MarksObtained";
            worksheet.Cells[1, 6].Value = "IsAbsent";
            worksheet.Cells[1, 7].Value = "IsExempted";
            worksheet.Cells[1, 8].Value = "Remarks";

            // Style headers
            using (var range = worksheet.Cells[1, 1, 1, 8])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
            }

            // Add sample data
            worksheet.Cells[2, 1].Value = "STU001";
            worksheet.Cells[2, 2].Value = "John Doe";
            worksheet.Cells[2, 3].Value = "CS101";
            worksheet.Cells[2, 4].Value = "Final Exam";
            worksheet.Cells[2, 5].Value = 85;
            worksheet.Cells[2, 6].Value = "FALSE";
            worksheet.Cells[2, 7].Value = "FALSE";
            worksheet.Cells[2, 8].Value = "Excellent work";

            worksheet.Cells[3, 1].Value = "STU002";
            worksheet.Cells[3, 2].Value = "Jane Smith";
            worksheet.Cells[3, 3].Value = "CS101";
            worksheet.Cells[3, 4].Value = "Final Exam";
            worksheet.Cells[3, 5].Value = 92;
            worksheet.Cells[3, 6].Value = "FALSE";
            worksheet.Cells[3, 7].Value = "FALSE";
            worksheet.Cells[3, 8].Value = "Outstanding performance";

            // Auto-fit columns
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            await Task.CompletedTask;
        }

        private async Task CreateRosterTemplate(OfficeOpenXml.ExcelWorksheet worksheet)
        {           
            // Add headers for student roster
            worksheet.Cells[1, 1].Value = "StudentID";
            worksheet.Cells[1, 2].Value = "FullName";
            worksheet.Cells[1, 3].Value = "Email";
            worksheet.Cells[1, 4].Value = "Phone";
            worksheet.Cells[1, 5].Value = "Department";
            worksheet.Cells[1, 6].Value = "GradeLevel";
            worksheet.Cells[1, 7].Value = "GPA";

            // Style headers
            using (var range = worksheet.Cells[1, 1, 1, 7])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGreen);
            }

            // Add sample data
            worksheet.Cells[2, 1].Value = "STU001";
            worksheet.Cells[2, 2].Value = "John Doe";
            worksheet.Cells[2, 3].Value = "john.doe@university.edu";
            worksheet.Cells[2, 4].Value = "555-0101";
            worksheet.Cells[2, 5].Value = "Computer Science";
            worksheet.Cells[2, 6].Value = "3";
            worksheet.Cells[2, 7].Value = "3.8";

            // Auto-fit columns
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            await Task.CompletedTask;
        }

        private async Task CreateExportTemplate(OfficeOpenXml.ExcelWorksheet worksheet)
        {            
            // Add headers for data export
            worksheet.Cells[1, 1].Value = "ExportType";
            worksheet.Cells[1, 2].Value = "CourseCode";
            worksheet.Cells[1, 3].Value = "Semester";
            worksheet.Cells[1, 4].Value = "IncludeComponents";
            worksheet.Cells[1, 5].Value = "IncludeStudentDetails";
            worksheet.Cells[1, 6].Value = "Format";

            // Style headers
            using (var range = worksheet.Cells[1, 1, 1, 6])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightYellow);
            }

            // Add sample data
            worksheet.Cells[2, 1].Value = "Grades";
            worksheet.Cells[2, 2].Value = "CS101";
            worksheet.Cells[2, 3].Value = "Fall 2024";
            worksheet.Cells[2, 4].Value = "TRUE";
            worksheet.Cells[2, 5].Value = "TRUE";
            worksheet.Cells[2, 6].Value = "Excel";

            // Auto-fit columns
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            await Task.CompletedTask;
        }

        // NEW ACTION - ANALYTICS
        public async Task<IActionResult> Analytics()
        {
            var analytics = new GradingAnalyticsViewModel
            {
                TotalCourses = await _context.Courses.CountAsync(c => c.IsActive),
                TotalStudents = await _context.Students.CountAsync(s => s.IsActive),
                TotalGradedCourses = await _context.CourseEnrollments
                    .Where(ce => ce.GradeStatus == GradeStatus.Completed)
                    .Select(ce => ce.CourseId)
                    .Distinct()
                    .CountAsync(),
                PendingGrades = await _context.CourseEnrollments
                    .CountAsync(ce => ce.GradeStatus == GradeStatus.InProgress && ce.IsActive)
            };

            // Calculate average GPA
            var completedEnrollments = await _context.CourseEnrollments
                .Where(ce => ce.GradeStatus == GradeStatus.Completed && ce.GradePoints.HasValue)
                .Include(ce => ce.Course)
                .ToListAsync();

            if (completedEnrollments.Any())
            {
                var totalGradePoints = completedEnrollments
                    .Sum(e => e.GradePoints!.Value * (e.Course?.Credits ?? 0));

                var totalCredits = completedEnrollments
                    .Sum(e => e.Course?.Credits ?? 0);

                analytics.AverageGPA = totalCredits > 0 ? totalGradePoints / totalCredits : 0;
            }

            // Grade distribution
            analytics.GradeDistribution = await _context.CourseEnrollments
                .Where(ce => ce.GradeStatus == GradeStatus.Completed && ce.GradeLetter != null)
                .GroupBy(ce => ce.GradeLetter)
                .Select(g => new GradeDistribution
                {
                    Grade = g.Key!,
                    Count = g.Count(),
                    Percentage = (decimal)g.Count() / completedEnrollments.Count * 100
                })
                .ToListAsync();

            return View(analytics);
        }

        // Helper method for student search in transcript
        [HttpPost]
        public async Task<IActionResult> SearchStudentForTranscript(string searchTerm)
        {
            var students = await _context.Students
                .Where(s => s.IsActive &&
                           (s.Name.Contains(searchTerm) || s.StudentId.Contains(searchTerm)))
                .OrderBy(s => s.Name)
                .Take(10)
                .ToListAsync();

            return Json(students.Select(s => new {
                id = s.Id,
                name = s.Name,
                studentId = s.StudentId
            }));
        }

    }

    // View Models
    public class GradingDashboardViewModel
    {
        public List<Course> Courses { get; set; } = new List<Course>();
        public List<Semester> Semesters { get; set; } = new List<Semester>();
        public List<GradingActivity> RecentGradingActivities { get; set; } = new List<GradingActivity>();
    }

    public class ManageComponentsViewModel
    {
        public Course? Course { get; set; }
        public List<GradingComponent> Components { get; set; } = new List<GradingComponent>();
        public List<GradingTemplate> AvailableTemplates { get; set; } = new List<GradingTemplate>();
        public decimal TotalWeight { get; set; }

        // Add these computed properties
        public bool IsValidWeight => Math.Abs(TotalWeight - 100) < 0.01m;
        public string WeightStatus => IsValidWeight ? "Valid" : "Invalid - Total must equal 100%";
        public string WeightStatusClass => IsValidWeight ? "alert-success" : "alert-warning";

        // ADD THIS MISSING PROPERTY
        public List<Course> Courses { get; set; } = new List<Course>();
    }

    public class EnterGradesViewModel
    {
        public Course? Course { get; set; }
        public Semester? Semester { get; set; }
        public List<GradingComponent> Components { get; set; } = new List<GradingComponent>();
        public GradingComponent? SelectedComponent { get; set; }
        public List<CourseEnrollment> Enrollments { get; set; } = new List<CourseEnrollment>();
        public List<StudentGrade> StudentGrades { get; set; } = new List<StudentGrade>();

        public List<Course> Courses { get; set; } = new List<Course>();
        public List<Semester> Semesters { get; set; } = new List<Semester>();
    }

    public class FinalGradesViewModel
    {
        public Course? Course { get; set; }
        public Semester? Semester { get; set; }
        public List<FinalGrade> FinalGrades { get; set; } = new List<FinalGrade>();
        public GradingSummary Summary { get; set; } = new GradingSummary();
    }

    public class GradeReportsViewModel
    {
        public List<Course> Courses { get; set; } = new List<Course>();
        public List<Semester> Semesters { get; set; } = new List<Semester>();
    }

    public class ImportExportViewModel
    {
        public List<Course> Courses { get; set; } = new List<Course>();
        public List<Semester> Semesters { get; set; } = new List<Semester>();
    }

    public class GradingActivity
    {
        public string CourseName { get; set; } = string.Empty;
        public string Activity { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Instructor { get; set; } = string.Empty;
    }

    // VIEW MODELS FOR NEW ACTIONS
    public class BulkOperationsViewModel
    {
        public List<Course> Courses { get; set; } = new List<Course>();
        public List<Semester> Semesters { get; set; } = new List<Semester>();
    }

    public class GradingAnalyticsViewModel
    {
        public int TotalCourses { get; set; }
        public int TotalStudents { get; set; }
        public int TotalGradedCourses { get; set; }
        public int PendingGrades { get; set; }
        public decimal AverageGPA { get; set; }
        public List<GradeDistribution> GradeDistribution { get; set; } = new List<GradeDistribution>();
    }

    public class GradeDistribution
    {
        public string Grade { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Percentage { get; set; }
    }
}
