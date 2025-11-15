using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using StudentManagementSystem.Models;
using StudentManagementSystem.Services;
using StudentManagementSystem.ViewModels;
using StudentManagementSystem.Models.ViewModels;


namespace StudentManagementSystem.Controllers
{
    [Authorize]
    public class StudentsController : Controller
    {
        private readonly IStudentService _studentService;

        private readonly ILogger<StudentsController> _logger;

        public StudentsController(IStudentService studentService, ILogger<StudentsController> logger)
        {
            _studentService = studentService;
            _logger = logger;
        }

        // GET: Students
        public async Task<IActionResult> Index(string searchString, string sortBy, string sortOrder, int? pageNumber)
        {
            try
            {
                // Get all students
                var allStudents = await _studentService.GetAllStudentsAsync();

                // Apply search filter with null checks
                if (!string.IsNullOrEmpty(searchString))
                {
                    allStudents = allStudents.Where(s =>
                        (s.Name != null && s.Name.Contains(searchString, StringComparison.OrdinalIgnoreCase)) ||
                        (s.StudentId != null && s.StudentId.Contains(searchString, StringComparison.OrdinalIgnoreCase)) ||
                        (s.SeatNumber != null && s.SeatNumber.Contains(searchString, StringComparison.OrdinalIgnoreCase)) ||
                        (s.Department != null && s.Department.Contains(searchString, StringComparison.OrdinalIgnoreCase)))
                        .ToList();
                }

                // Apply sorting
                var sortedStudents = SortStudents(allStudents, sortBy, sortOrder);

                // Add serial numbers
                int serial = 1;
                foreach (var student in sortedStudents)
                {
                    student.SerialNumber = serial++;
                }

                // Setup pagination - use the synchronous Create method for List<T>
                int pageSize = 10;
                var paginatedStudents = PaginatedList<Student>.Create(sortedStudents, pageNumber ?? 1, pageSize);

                // Store current filters in ViewData for the view
                ViewData["CurrentFilter"] = searchString;
                ViewData["CurrentSort"] = sortBy;
                ViewData["CurrentOrder"] = sortOrder;

                return View(paginatedStudents);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error loading students: {ex.Message}";
                return View(new PaginatedList<Student>(new List<Student>(), 0, 1, 10));
            }
        }

        private List<Student> SortStudents(List<Student> students, string sortBy, string sortOrder)
        {
            IEnumerable<Student> query = students;

            // Set default sort if none specified
            if (string.IsNullOrEmpty(sortBy))
            {
                sortBy = "StudentId";
                sortOrder = "asc";
            }

            // Apply sorting with null checks
            switch (sortBy.ToLower())
            {
                case "studentid":
                    query = sortOrder == "desc" ?
                        query.OrderByDescending(s => s.StudentId ?? "") :
                        query.OrderBy(s => s.StudentId ?? "");
                    break;
                case "name":
                    query = sortOrder == "desc" ?
                        query.OrderByDescending(s => s.Name ?? "") :
                        query.OrderBy(s => s.Name ?? "");
                    break;
                case "seatnumber":
                    query = sortOrder == "desc" ?
                        query.OrderByDescending(s => s.SeatNumber ?? "") :
                        query.OrderBy(s => s.SeatNumber ?? "");
                    break;
                case "department":
                    query = sortOrder == "desc" ?
                        query.OrderByDescending(s => s.Department ?? "") :
                        query.OrderBy(s => s.Department ?? "");
                    break;
                case "gpa":
                    query = sortOrder == "desc" ?
                        query.OrderByDescending(s => s.GPA) :
                        query.OrderBy(s => s.GPA);
                    break;
                case "percentage":
                    query = sortOrder == "desc" ?
                        query.OrderByDescending(s => s.Percentage) :
                        query.OrderBy(s => s.Percentage);
                    break;
                default:
                    query = query.OrderBy(s => s.StudentId ?? "");
                    break;
            }

            return query.ToList();
        }

        // GET: Students/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var student = await _studentService.GetStudentByIdAsync(id);
            if (student == null)
            {
                return NotFound();
            }
            return View(student);
        }

        // GET: Students/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Students/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Student student)
        {
            if (!ModelState.IsValid)
                return View(student);

            if (await _studentService.StudentExistsAsync(student.StudentId))
            {
                ModelState.AddModelError("StudentId", "Student ID already exists.");
                return View(student);
            }

            await _studentService.AddStudentAsync(student);
            return RedirectToAction(nameof(Index));
        }

        // GET: Students/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var student = await _studentService.GetStudentByIdAsync(id);
            if (student == null)
            {
                return NotFound();
            }
            return View(student);
        }

        // POST: Students/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Student student)
        {
            if (id != student.Id)
                return NotFound();

            if (!ModelState.IsValid)
                return View(student);

            try
            {
                await _studentService.UpdateStudentAsync(student);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                if (!await StudentExists(student.Id))
                    return NotFound();

                throw;
            }
        }

        // GET: Students/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var student = await _studentService.GetStudentByIdAsync(id);
            if (student == null)
            {
                return NotFound();
            }
            return View(student);
        }

        // POST: Students/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _studentService.DeleteStudentAsync(id);
            return RedirectToAction(nameof(Index));
        }

        // GET: Students/Import
        public IActionResult Import()
        {
            return View();
        }

        // POST: Students/Import
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(100 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 100 * 1024 * 1024)]
        public async Task<IActionResult> Import(IFormFile file)
        {
            try
            {
                _logger.LogInformation("Starting file upload process");

                if (file == null || file.Length == 0)
                {
                    ViewBag.Error = "Please select a file.";
                    return View();
                }

                _logger.LogInformation($"File received: {file.FileName}, Size: {file.Length} bytes");

                var extension = Path.GetExtension(file.FileName).ToLower();
                if (extension != ".xlsx" && extension != ".xls")
                {
                    ViewBag.Error = "Please select an Excel file (.xlsx or .xls).";
                    return View();
                }

                if (file.Length > 100 * 1024 * 1024)
                {
                    ViewBag.Error = "File size must be less than 100MB.";
                    return View();
                }

                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                stream.Position = 0;

                _logger.LogInformation("Calling AnalyzeExcelImportAsync service");
                var analysisResult = await _studentService.AnalyzeExcelImportAsync(stream);

                if (analysisResult.Success)
                {
                    _logger.LogInformation($"Import analysis successful. Found {analysisResult.ValidStudents?.Count ?? 0} valid students");

                    // STORE IN SESSION INSTEAD OF TEMPDATA
                    HttpContext.Session.SetString("ImportAnalysis", System.Text.Json.JsonSerializer.Serialize(analysisResult));
                    HttpContext.Session.SetString("FileName", file.FileName);

                    _logger.LogInformation("Redirecting to ImportReview page");
                    return RedirectToAction("ImportReview"); // Use RedirectToAction instead of View()
                }

                _logger.LogWarning($"Import analysis failed: {analysisResult.Message}");
                ViewBag.Error = analysisResult.Message;
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during file import");
                ViewBag.Error = $"Import failed: {ex.Message}";
                return View();
            }
        }

        // GET: Students/ImportReview
        public IActionResult ImportReview()
        {
            try
            {
                _logger.LogInformation("Loading ImportReview page");

                // CHECK SESSION INSTEAD OF TEMPDATA
                var analysisJson = HttpContext.Session.GetString("ImportAnalysis");
                if (string.IsNullOrEmpty(analysisJson))
                {
                    _logger.LogWarning("No import analysis data found in Session");
                    TempData["Error"] = "Import session expired or no data available. Please upload the file again.";
                    return RedirectToAction(nameof(Import));
                }

                var analysisResult = System.Text.Json.JsonSerializer.Deserialize<ImportResult>(analysisJson);

                if (analysisResult == null)
                {
                    _logger.LogWarning("Failed to deserialize import analysis data");
                    TempData["Error"] = "Failed to load import data. Please upload the file again.";
                    return RedirectToAction(nameof(Import));
                }

                // Create view model with serial numbers
                var viewModel = new ImportReviewViewModel
                {
                    ImportResult = analysisResult,
                    ImportSettings = new ImportSettings()
                };

                // Add serial numbers to valid students
                if (analysisResult.ValidStudents != null)
                {
                    int serial = 1;
                    foreach (var student in analysisResult.ValidStudents)
                    {
                        student.SerialNumber = serial++;
                    }
                }

                _logger.LogInformation($"Successfully loaded import review with {analysisResult.ValidStudents?.Count ?? 0} students");
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading ImportReview page");
                TempData["Error"] = "Error loading preview. Please try uploading again.";
                return RedirectToAction(nameof(Import));
            }
        }

        //// POST: Students/ImportExecute
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> ImportExecute(StudentManagementSystem.Models.ImportSettings settings)
        //{
        //    try
        //    {
        //        _logger.LogInformation("Starting import execution");

        //        // CHECK SESSION INSTEAD OF TEMPDATA
        //        var analysisJson = HttpContext.Session.GetString("ImportAnalysis");
        //        if (string.IsNullOrEmpty(analysisJson))
        //        {
        //            _logger.LogWarning("Import session expired - no ImportAnalysis in Session");
        //            TempData["Error"] = "Import session expired. Please upload the file again.";
        //            return RedirectToAction(nameof(Import));
        //        }

        //        var analysisResult = System.Text.Json.JsonSerializer.Deserialize<ImportResult>(analysisJson);

        //        if (analysisResult == null || !analysisResult.Success)
        //        {
        //            _logger.LogWarning("Invalid import analysis result");
        //            TempData["Error"] = "Invalid import data.";
        //            return RedirectToAction(nameof(Import));
        //        }

        //        _logger.LogInformation($"Executing import with {analysisResult.ValidStudents?.Count ?? 0} students, Duplicate Handling: {settings.DuplicateHandling}");

        //        // Pass the settings to your service
        //        var importResult = await _studentService.ExecuteImportAsync(analysisResult, settings);

        //        // CLEAN UP SESSION DATA
        //        HttpContext.Session.Remove("ImportAnalysis");
        //        HttpContext.Session.Remove("FileName");

        //        _logger.LogInformation($"Import execution completed: {importResult.Success}, Message: {importResult.Message}");
        //        TempData[importResult.Success ? "Success" : "Error"] = importResult.Message;
        //        return RedirectToAction(nameof(Index));
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error during import execution");
        //        TempData["Error"] = $"Import execution failed: {ex.Message}";
        //        return RedirectToAction(nameof(Import));
        //    }
        //}

        // GET: Students/Export
        public async Task<IActionResult> Export()
        {
            try
            {
                Console.WriteLine("Export method called");
                var fileBytes = await _studentService.ExportStudentsToExcelAsync();
                Console.WriteLine($"Export generated {fileBytes?.Length ?? 0} bytes");

                if (fileBytes == null || fileBytes.Length == 0)
                {
                    TempData["Error"] = "Export failed: No data generated";
                    return RedirectToAction(nameof(Index));
                }

                return File(fileBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Students_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Export error: {ex}");
                TempData["Error"] = $"Export failed: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Students/AddTestData
        public async Task<IActionResult> AddTestData()
        {
            try
            {
                var result = await _studentService.AddTestStudents();
                TempData["Success"] = result;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to add test data: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Students/Debug
        public IActionResult Debug()
        {
            return View();
        }

        //// POST: Students/DeleteMultiple
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> DeleteMultiple(int[] selectedStudents)
        //{
        //    try
        //    {
        //        if (selectedStudents == null || selectedStudents.Length == 0)
        //        {
        //            TempData["Error"] = "No students selected for deletion.";
        //            return RedirectToAction(nameof(Index));
        //        }

        //        foreach (var id in selectedStudents)
        //        {
        //            await _studentService.DeleteStudentAsync(id);
        //        }

        //        TempData["Success"] = $"Successfully deleted {selectedStudents.Length} students.";
        //        return RedirectToAction(nameof(Index));
        //    }
        //    catch (Exception ex)
        //    {
        //        TempData["Error"] = $"Failed to delete students: {ex.Message}";
        //        return RedirectToAction(nameof(Index));
        //    }
        //}

        // POST: Students/ExportSelected
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExportSelected(int[] selectedStudents)
        {
            try
            {
                if (selectedStudents == null || selectedStudents.Length == 0)
                {
                    TempData["Error"] = "No students selected for export.";
                    return RedirectToAction(nameof(Index));
                }

                Console.WriteLine($"Exporting {selectedStudents.Length} selected students");

                var fileBytes = await _studentService.ExportSelectedStudentsAsync(selectedStudents);

                if (fileBytes == null || fileBytes.Length == 0)
                {
                    TempData["Error"] = "Export failed: No data generated";
                    return RedirectToAction(nameof(Index));
                }

                Console.WriteLine($"Successfully generated export file with {fileBytes.Length} bytes");

                return File(fileBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Selected_Students_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Export selected error: {ex}");
                TempData["Error"] = $"Export failed: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        private async Task<bool> StudentExists(int id)
        {
            var student = await _studentService.GetStudentByIdAsync(id);
            return student != null;
        }


        // GET: Students/ImportReview with sorting and filtering
        [HttpGet]
        public IActionResult ImportReview(string? sortBy, string? sortOrder, string? searchString)
        {
            try
            {
                _logger.LogInformation("Loading ImportReview page with filters");

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

                // Apply search filter - fixed null reference
                if (!string.IsNullOrEmpty(searchString) && analysisResult.ValidStudents != null)
                {
                    analysisResult.ValidStudents = analysisResult.ValidStudents
                        .Where(s => (s.Name != null && s.Name.Contains(searchString, StringComparison.OrdinalIgnoreCase)) ||
                                   (s.StudentId != null && s.StudentId.Contains(searchString, StringComparison.OrdinalIgnoreCase)) ||
                                   (s.Department != null && s.Department.Contains(searchString, StringComparison.OrdinalIgnoreCase)))
                        .ToList();
                }

                // Apply sorting - provide default values for null parameters
                if (analysisResult.ValidStudents != null)
                {
                    analysisResult.ValidStudents = SortImportStudents(
                        analysisResult.ValidStudents,
                        sortBy ?? "SerialNumber",
                        sortOrder ?? "asc"
                    );
                }

                // Add serial numbers
                if (analysisResult.ValidStudents != null)
                {
                    int serial = 1;
                    foreach (var student in analysisResult.ValidStudents)
                    {
                        student.SerialNumber = serial++;
                    }
                }

                var viewModel = new ImportReviewViewModel
                {
                    ImportResult = analysisResult,
                    ImportSettings = new ImportSettings(), // This should not be null
                    SortBy = sortBy ?? "SerialNumber",
                    SortOrder = sortOrder ?? "asc",
                    SearchString = searchString ?? "" // FIX: Add null coalescing for searchString
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading ImportReview page");
                TempData["Error"] = "Error loading preview.";
                return RedirectToAction(nameof(Import));
            }
        }


        /*
        // GET: Students/ImportReview with sorting and filtering
        [HttpGet]
        public IActionResult ImportReview(string sortBy, string sortOrder, string searchString)
        {
            try
            {
                _logger.LogInformation("Loading ImportReview page with filters");

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

                // Apply search filter
                if (!string.IsNullOrEmpty(searchString))
                {
                    analysisResult.ValidStudents = analysisResult.ValidStudents?
                        .Where(s => (s.Name != null && s.Name.Contains(searchString, StringComparison.OrdinalIgnoreCase)) ||
                                   (s.StudentId != null && s.StudentId.Contains(searchString, StringComparison.OrdinalIgnoreCase)) ||
                                   (s.Department != null && s.Department.Contains(searchString, StringComparison.OrdinalIgnoreCase)))
                        .ToList();
                }

                // Apply sorting
                if (analysisResult.ValidStudents != null)
                {
                    analysisResult.ValidStudents = SortImportStudents(analysisResult.ValidStudents, sortBy, sortOrder);
                }

                // Add serial numbers
                if (analysisResult.ValidStudents != null)
                {
                    int serial = 1;
                    foreach (var student in analysisResult.ValidStudents)
                    {
                        student.SerialNumber = serial++;
                    }
                }

                var viewModel = new ImportReviewViewModel
                {
                    ImportResult = analysisResult,
                    ImportSettings = new ImportSettings(),
                    SortBy = sortBy ?? "SerialNumber",
                    SortOrder = sortOrder ?? "asc",
                    SearchString = searchString
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading ImportReview page");
                TempData["Error"] = "Error loading preview.";
                return RedirectToAction(nameof(Import));
            }
        }
        */
        private List<Student> SortImportStudents(List<Student> students, string sortBy, string sortOrder)
        {
            if (students == null) return new List<Student>();

            IEnumerable<Student> query = students;

            switch (sortBy?.ToLower())
            {
                case "studentid":
                    query = sortOrder == "desc" ?
                        query.OrderByDescending(s => s.StudentId ?? "") :
                        query.OrderBy(s => s.StudentId ?? "");
                    break;
                case "name":
                    query = sortOrder == "desc" ?
                        query.OrderByDescending(s => s.Name ?? "") :
                        query.OrderBy(s => s.Name ?? "");
                    break;
                case "seatnumber":
                    query = sortOrder == "desc" ?
                        query.OrderByDescending(s => s.SeatNumber ?? "") :
                        query.OrderBy(s => s.SeatNumber ?? "");
                    break;
                case "department":
                    query = sortOrder == "desc" ?
                        query.OrderByDescending(s => s.Department ?? "") :
                        query.OrderBy(s => s.Department ?? "");
                    break;
                case "gpa":
                    query = sortOrder == "desc" ?
                        query.OrderByDescending(s => s.GPA) :
                        query.OrderBy(s => s.GPA);
                    break;
                case "percentage":
                    query = sortOrder == "desc" ?
                        query.OrderByDescending(s => s.Percentage) :
                        query.OrderBy(s => s.Percentage);
                    break;
                default: // SerialNumber
                    query = sortOrder == "desc" ?
                        query.OrderByDescending(s => s.SerialNumber) :
                        query.OrderBy(s => s.SerialNumber);
                    break;
            }

            return query.ToList();
        }

        // GET: Students/DownloadTemplate
        public IActionResult DownloadTemplate()
        {
            try
            {
                var templateBytes = GenerateExcelTemplate();
                return File(templateBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "Student_Import_Template.xlsx");
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
            var worksheet = package.Workbook.Worksheets.Add("Students Template");

            // Headers with instructions
            string[] headers = {
        "StudentId (Required)",
        "Name (Required)",
        "SeatNumber",
        "NationalId",
        "Department",
        "StudyLevel",
        "Semester",
        "Grade",
        "Phone",
        "Email",
        "GPA",
        "Percentage",
        "PassedHours"
    };

            string[] descriptions = {
        "Unique student identifier",
        "Full name of student",
        "Classroom seat number",
        "National identification number",
        "Department name",
        "Study level/year",
        "Academic semester",
        "Grade/Class",
        "Phone number",
        "Email address",
        "Grade Point Average (0.00-4.00)",
        "Percentage score (0-100)",
        "Completed credit hours"
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

            // Add sample data
            var sampleStudents = new[]
            {
        new { StudentId = "20240001", Name = "أحمد محمد", SeatNumber = "CS-001", Department = "Computer Science", GPA = 3.8m },
        new { StudentId = "20240002", Name = "فاطمة علي", SeatNumber = "CS-002", Department = "Computer Science", GPA = 3.6m }
    };

            for (int i = 0; i < sampleStudents.Length; i++)
            {
                var student = sampleStudents[i];
                worksheet.Cells[i + 3, 1].Value = student.StudentId;
                worksheet.Cells[i + 3, 2].Value = student.Name;
                worksheet.Cells[i + 3, 3].Value = student.SeatNumber;
                worksheet.Cells[i + 3, 5].Value = student.Department;
                worksheet.Cells[i + 3, 11].Value = student.GPA;
            }

            // Auto-fit columns
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            return package.GetAsByteArray();
        }

        // POST: Students/DeleteMultiple
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMultiple(int[] selectedStudents)
        {
            try
            {
                if (selectedStudents == null || selectedStudents.Length == 0)
                {
                    TempData["Error"] = "No students selected for deletion.";
                    return RedirectToAction(nameof(Index));
                }

                int deletedCount = 0;
                foreach (var id in selectedStudents)
                {
                    var student = await _studentService.GetStudentByIdAsync(id);
                    if (student != null)
                    {
                        await _studentService.DeleteStudentAsync(id);
                        deletedCount++;
                    }
                }

                TempData["Success"] = $"Successfully deleted {deletedCount} students.";
                _logger.LogInformation($"Deleted {deletedCount} students via bulk delete");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to delete students: {ex.Message}";
                _logger.LogError(ex, "Error during bulk student deletion");
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Students/DeleteAll
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAll()
        {
            try
            {
                var allStudents = await _studentService.GetAllStudentsAsync();
                int totalCount = allStudents.Count;

                if (totalCount == 0)
                {
                    TempData["Warning"] = "No students found to delete.";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var student in allStudents)
                {
                    await _studentService.DeleteStudentAsync(student.Id);
                }

                TempData["Success"] = $"Successfully deleted all {totalCount} students.";
                _logger.LogInformation($"Deleted all {totalCount} students from the system");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to delete all students: {ex.Message}";
                _logger.LogError(ex, "Error during mass student deletion");
            }

            return RedirectToAction(nameof(Index));
        }

        // Update the ImportExecute action to show import results
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportExecute(ImportSettings settings)
        {
            try
            {
                _logger.LogInformation("Starting import execution");

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

                var importResult = await _studentService.ExecuteImportAsync(analysisResult, settings);

                // Store import results for display on Index page
                if (importResult.Success)
                {
                    TempData["ImportResult"] = importResult.Message;
                    TempData["ImportedCount"] = importResult.ImportedCount;
                    TempData["ErrorCount"] = importResult.ErrorCount;
                }

                // Clean up session data
                HttpContext.Session.Remove("ImportAnalysis");
                HttpContext.Session.Remove("FileName");

                _logger.LogInformation($"Import execution completed: {importResult.Success}");

                if (!importResult.Success)
                {
                    TempData["Error"] = importResult.Message;
                    return RedirectToAction(nameof(Import));
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during import execution");
                TempData["Error"] = $"Import execution failed: {ex.Message}";
                return RedirectToAction(nameof(Import));
            }
        }
    }
}