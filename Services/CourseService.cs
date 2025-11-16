using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;
using StudentManagementSystem.ViewModels;
using StudentManagementSystem.Models.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Drawing;


namespace StudentManagementSystem.Services
{
    public class CourseService : ICourseService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CourseService> _logger;

        public CourseService(ApplicationDbContext context, ILogger<CourseService> logger)
        {
            _context = context;
            _logger = logger;
            QuestPDF.Settings.License = LicenseType.Community;

        }

        // BASIC COURSE CRUD
        //public async Task<List<Course>> GetAllCoursesAsync()
        //{
        //    return await _context.Courses
        //        .Include(c => c.CourseEnrollments)
        //        .Include(c => c.Prerequisites)
        //        .ThenInclude(p => p.PrerequisiteCourse)
        //        .OrderBy(c => c.CourseCode)
        //        .ToListAsync();
        //}
        public async Task<List<Course>> GetAllCoursesAsync()
        {
            return await _context.Courses
                .Include(c => c.CourseDepartment)
                .Include(c => c.CourseSemester)
                .Include(c => c.CourseEnrollments)
                .Include(c => c.Prerequisites)  // ✅ ADD THIS
                    .ThenInclude(p => p.PrerequisiteCourse)  // ✅ ADD THIS
                .Where(c => c.IsActive)
                .OrderBy(c => c.CourseCode)
                .ToListAsync();
        }
        public async Task<Course?> GetCourseByIdAsync(int id)
        {
            return await _context.Courses
                .Include(c => c.CourseEnrollments)
                    .ThenInclude(e => e.Student)
                .Include(c => c.Prerequisites)
                    .ThenInclude(p => p.PrerequisiteCourse)
                .Include(c => c.RequiredFor)
                    .ThenInclude(r => r.Course)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Course?> GetCourseByCodeAsync(string courseCode)
        {
            return await _context.Courses
                .FirstOrDefaultAsync(c => c.CourseCode == courseCode);
        }

        public async Task<bool> CourseExistsAsync(string courseCode)
        {
            return await _context.Courses
                .AnyAsync(c => c.CourseCode == courseCode);
        }

        public async Task AddCourseAsync(Course course)
        {
            _context.Courses.Add(course);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateCourseAsync(Course course)
        {
            _context.Courses.Update(course);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteCourseAsync(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Delete course enrollments
                var enrollments = await _context.CourseEnrollments
                    .Where(ce => ce.CourseId == id)
                    .ToListAsync();
                _context.CourseEnrollments.RemoveRange(enrollments);

                // 2. Delete prerequisites where this course is the main course
                var prerequisitesAsCourse = await _context.CoursePrerequisites
                    .Where(cp => cp.CourseId == id)
                    .ToListAsync();
                _context.CoursePrerequisites.RemoveRange(prerequisitesAsCourse);

                // 3. Delete prerequisites where this course is the prerequisite
                var prerequisitesAsPrereq = await _context.CoursePrerequisites
                    .Where(cp => cp.PrerequisiteCourseId == id)
                    .ToListAsync();
                _context.CoursePrerequisites.RemoveRange(prerequisitesAsPrereq);

                // 4. Delete the course itself
                var course = await _context.Courses.FindAsync(id);
                if (course != null)
                {
                    _context.Courses.Remove(course);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }


        // ENROLLMENT MANAGEMENT
        public async Task<List<CourseEnrollment>> GetCourseEnrollmentsAsync(int courseId)
        {
            return await _context.CourseEnrollments
                .Include(e => e.Student)
                .Where(e => e.CourseId == courseId)
                .OrderBy(e => e.Student!.Name)
                .ToListAsync();
        }

        public async Task<List<CourseEnrollment>> GetStudentEnrollmentsAsync(int studentId)
        {
            return await _context.CourseEnrollments
                .Include(e => e.Course)
                .Where(e => e.StudentId == studentId)
                .OrderBy(e => e.Course!.CourseCode)
                .ToListAsync();
        }

        public async Task EnrollStudentAsync(int courseId, int studentId)
        {
            var enrollment = new CourseEnrollment
            {
                CourseId = courseId,
                StudentId = studentId,
                EnrollmentDate = DateTime.Now,
                IsActive = true
            };

            _context.CourseEnrollments.Add(enrollment);
            await _context.SaveChangesAsync();
        }

        public async Task UnenrollStudentAsync(int enrollmentId)
        {
            var enrollment = await _context.CourseEnrollments.FindAsync(enrollmentId);
            if (enrollment != null)
            {
                _context.CourseEnrollments.Remove(enrollment);
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateGradeAsync(int enrollmentId, decimal grade, string gradeLetter)
        {
            var enrollment = await _context.CourseEnrollments.FindAsync(enrollmentId);
            if (enrollment != null)
            {
                enrollment.Grade = grade;
                enrollment.GradeLetter = gradeLetter;
                await _context.SaveChangesAsync();
            }
        }

        // PREREQUISITE MANAGEMENT
        public async Task AddPrerequisiteAsync(int courseId, int prerequisiteCourseId, decimal? minGrade)
        {
            var existing = await _context.CoursePrerequisites
                .FirstOrDefaultAsync(cp => cp.CourseId == courseId && cp.PrerequisiteCourseId == prerequisiteCourseId);

            if (existing == null)
            {
                var prerequisite = new CoursePrerequisite
                {
                    CourseId = courseId,
                    PrerequisiteCourseId = prerequisiteCourseId,
                    MinGrade = minGrade,
                    IsRequired = true
                };

                _context.CoursePrerequisites.Add(prerequisite);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<CoursePrerequisite>> GetCoursePrerequisitesAsync(int courseId)
        {
            return await _context.CoursePrerequisites
                .Include(cp => cp.PrerequisiteCourse)
                .Where(cp => cp.CourseId == courseId)
                .ToListAsync();
        }

        public async Task RemovePrerequisiteAsync(int prerequisiteId)
        {
            var prerequisite = await _context.CoursePrerequisites.FindAsync(prerequisiteId);
            if (prerequisite != null)
            {
                _context.CoursePrerequisites.Remove(prerequisite);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> CanStudentEnrollAsync(int studentId, int courseId)
        {
            var missingPrerequisites = await GetMissingPrerequisitesAsync(studentId, courseId);
            return !missingPrerequisites.Any();
        }

        public async Task<List<string>> GetMissingPrerequisitesAsync(int studentId, int courseId)
        {
            var missing = new List<string>();

            var prerequisites = await GetCoursePrerequisitesAsync(courseId);
            var studentCompletedCourses = await _context.CourseEnrollments
                .Where(e => e.StudentId == studentId && e.Grade.HasValue && e.Grade >= 60)
                .Select(e => e.CourseId)
                .ToListAsync();

            foreach (var prereq in prerequisites)
            {
                if (!studentCompletedCourses.Contains(prereq.PrerequisiteCourseId))
                {
                    var prereqCourse = await _context.Courses.FindAsync(prereq.PrerequisiteCourseId);
                    if (prereqCourse != null)
                    {
                        missing.Add($"{prereqCourse.CourseCode} - {prereqCourse.CourseName}");
                    }
                }
            }

            return missing;
        }

        // STATISTICS
        public async Task<int> GetTotalCoursesAsync()
        {
            return await _context.Courses.CountAsync();
        }

        public async Task<int> GetActiveEnrollmentsCountAsync()
        {
            return await _context.CourseEnrollments.CountAsync(e => e.IsActive);
        }

        public async Task<List<Course>> GetCoursesByDepartmentAsync(string department)
        {
            return await _context.Courses
                .Where(c => c.Department == department)
                .OrderBy(c => c.CourseCode)
                .ToListAsync();
        }

        // IMPORT/EXPORT
        public async Task<ImportResult> ImportCoursesFromExcelAsync(Stream stream)
        {
            var result = new ImportResult();

            try
            {
                ExcelPackage.License.SetNonCommercialOrganization("Student Management System");
                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets[0];

                if (worksheet == null)
                {
                    result.Message = "No worksheet found in the Excel file.";
                    return result;
                }

                var rowCount = worksheet.Dimension?.Rows ?? 0;
                if (rowCount <= 1)
                {
                    result.Message = "No data found in the Excel file.";
                    return result;
                }

                var validCourses = new List<Course>();
                var errors = new List<string>();

                for (int row = 2; row <= rowCount; row++)
                {
                    try
                    {
                        var courseCode = worksheet.Cells[row, 1]?.Value?.ToString()?.Trim();
                        var courseName = worksheet.Cells[row, 2]?.Value?.ToString()?.Trim();

                        // ✅ DEBUG: Check what's in each column
                        Console.WriteLine($"=== ROW {row} DEBUG ===");
                        Console.WriteLine($"CourseCode: '{courseCode}'");
                        Console.WriteLine($"CourseName: '{courseName}'");

                        for (int col = 1; col <= 12; col++) // Check first 12 columns
                        {
                            var value = worksheet.Cells[row, col]?.Value?.ToString()?.Trim();
                            Console.WriteLine($"Column {col}: '{value}'");
                        }

                        var prerequisiteCodes = worksheet.Cells[row, 11]?.Value?.ToString()?.Trim();
                        Console.WriteLine($"Prerequisites from col 11: '{prerequisiteCodes}'");

                        if (string.IsNullOrEmpty(prerequisiteCodes))
                        {
                            prerequisiteCodes = worksheet.Cells[row, 10]?.Value?.ToString()?.Trim();
                            Console.WriteLine($"Prerequisites from col 10: '{prerequisiteCodes}'");
                        }

                        if (string.IsNullOrEmpty(courseCode))
                        {
                            errors.Add($"Row {row}: CourseCode is required");
                            continue;
                        }

                        if (string.IsNullOrEmpty(courseName))
                        {
                            errors.Add($"Row {row}: CourseName is required");
                            continue;
                        }

                        var course = new Course
                        {
                            CourseCode = courseCode,
                            CourseName = courseName,
                            Description = worksheet.Cells[row, 3]?.Value?.ToString()?.Trim(),
                            Credits = TryParseInt(worksheet.Cells[row, 4]?.Value?.ToString(), 3),
                            Department = worksheet.Cells[row, 5]?.Value?.ToString()?.Trim() ?? "General",
                            Semester = TryParseInt(worksheet.Cells[row, 6]?.Value?.ToString(), 1),
                            MaxStudents = TryParseInt(worksheet.Cells[row, 7]?.Value?.ToString(), 1000), // ✅ Updated default
                            MinGPA = TryParseDecimal(worksheet.Cells[row, 8]?.Value?.ToString(), 2.0m),
                            MinPassedHours = TryParseInt(worksheet.Cells[row, 9]?.Value?.ToString(), 0),
                            IsActive = IsActive(worksheet.Cells[row, 10]?.Value?.ToString()),
                            PrerequisitesString = prerequisiteCodes // ✅ SAVE TO PrerequisitesString
                        };

                        validCourses.Add(course);
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Row {row}: Error processing row - {ex.Message}");
                    }
                }

                // Save valid courses
                foreach (var course in validCourses)
                {
                    try
                    {
                        var existingCourse = await GetCourseByCodeAsync(course.CourseCode);

                        if (existingCourse != null)
                        {
                            existingCourse.CourseName = course.CourseName;
                            existingCourse.Description = course.Description;
                            existingCourse.Credits = course.Credits;
                            existingCourse.Department = course.Department;
                            existingCourse.Semester = course.Semester;
                            existingCourse.MaxStudents = course.MaxStudents;
                            existingCourse.MinGPA = course.MinGPA;
                            existingCourse.MinPassedHours = course.MinPassedHours;
                            existingCourse.IsActive = course.IsActive;
                            existingCourse.PrerequisitesString = course.PrerequisitesString; // ✅ UPDATE PREREQUISITES
                        }
                        else
                        {
                            _context.Courses.Add(course);
                        }

                        await _context.SaveChangesAsync();

                        // ✅ OPTIONAL: Keep the prerequisite relationship processing if you want
                        if (!string.IsNullOrEmpty(course.PrerequisitesString))
                        {
                            var courseId = existingCourse?.Id ?? course.Id;
                            var prereqCodes = course.PrerequisitesString.Split(',')
                                .Select(p => p.Trim())
                                .Where(p => !string.IsNullOrEmpty(p))
                                .ToList();

                            foreach (var prereqCode in prereqCodes)
                            {
                                var prereqCourse = await GetCourseByCodeAsync(prereqCode);
                                if (prereqCourse != null && prereqCourse.Id != courseId)
                                {
                                    await AddPrerequisiteAsync(courseId, prereqCourse.Id, null);
                                }
                            }
                        }

                        result.ImportedCount++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Error saving course {course.CourseCode}: {ex.Message}");
                    }
                }

                result.Success = true;
                result.Message = $"Successfully processed {result.ImportedCount} courses. {errors.Count} errors found.";
                result.Errors = errors;
            }
            catch (Exception ex)
            {
                result.Message = $"Import failed: {ex.Message}";
                _logger.LogError(ex, "Error importing courses from Excel");
            }

            return result;
        }

        /*
         
        public async Task<ImportResult> ImportCoursesFromExcelAsync(Stream stream)
{
    var result = new ImportResult();

    try
    {
        ExcelPackage.License.SetNonCommercialOrganization("Student Management System");
        using var package = new ExcelPackage(stream);
        var worksheet = package.Workbook.Worksheets[0];

        if (worksheet == null || worksheet.Dimension == null)
        {
            result.Message = "No worksheet found in the Excel file.";
            return result;
        }

        var rowCount = worksheet.Dimension.Rows;
        var colCount = worksheet.Dimension.Columns;

        // ✅ GET HEADERS FROM EXCEL (SAME AS ANALYSIS METHOD)
        var headers = new List<string>();
        for (int col = 1; col <= colCount; col++)
        {
            var headerValue = worksheet.Cells[1, col].Text.Trim();
            if (!string.IsNullOrEmpty(headerValue))
            {
                headers.Add(headerValue);
            }
        }

        var validCourses = new List<Course>();
        var errors = new List<string>();

        // ✅ USE HEADER MAPPING (SAME AS ANALYSIS METHOD)
        for (int row = 2; row <= rowCount; row++)
        {
            try
            {
                var course = new Course();
                string? prerequisitesValue = null;
                string? courseSpecValue = null;
                string? iconValue = null;

                // Map columns by header name
                for (int col = 1; col <= headers.Count; col++)
                {
                    var header = headers[col - 1].ToLower();
                    var value = worksheet.Cells[row, col].Text.Trim();

                    switch (header)
                    {
                        case "coursecode (required)":
                        case "coursecode":
                        case "كود المادة":
                            course.CourseCode = value;
                            break;
                        case "coursename (required)":
                        case "coursename":
                        case "اسم المادة":
                            course.CourseName = value;
                            break;
                        case "description":
                        case "الوصف":
                            course.Description = value;
                            break;
                        case "credits":
                        case "الساعات المعتمدة":
                            if (int.TryParse(value, out int credits))
                                course.Credits = credits;
                            else if (string.IsNullOrEmpty(value))
                                course.Credits = 3;
                            break;
                        case "department":
                        case "القسم":
                            course.Department = value;
                            break;
                        case "semester":
                        case "الفصل الدراسي":
                            if (int.TryParse(value, out int semester))
                                course.Semester = semester;
                            else if (string.IsNullOrEmpty(value))
                                course.Semester = 1;
                            break;
                        case "maxstudents":
                        case "الحد الأقصى للطلاب":
                            if (int.TryParse(value, out int maxStudents))
                                course.MaxStudents = maxStudents;
                            else if (string.IsNullOrEmpty(value))
                                course.MaxStudents = 1000;
                            break;
                        case "mingpa":
                        case "الحد الأدنى للمعدل التراكمي":
                            if (decimal.TryParse(value, out decimal minGPA))
                                course.MinGPA = minGPA;
                            else if (string.IsNullOrEmpty(value))
                                course.MinGPA = 2.0m;
                            break;
                        case "minpassedhours":
                        case "الحد الأدنى للساعات المنجزة":
                            if (int.TryParse(value, out int minPassedHours))
                                course.MinPassedHours = minPassedHours;
                            else if (string.IsNullOrEmpty(value))
                                course.MinPassedHours = 0;
                            break;
                        case "prerequisites":
                        case "المتطلبات السابقة":
                            prerequisitesValue = value;
                            break;
                        case "coursespecification":
                            courseSpecValue = value;
                            break;
                        case "icon":
                            iconValue = value;
                            break;
                        case "isactive":
                        case "نشط":
                            course.IsActive = value.ToLower() switch
                            {
                                "yes" => true,
                                "نعم" => true,
                                "true" => true,
                                "1" => true,
                                "y" => true,
                                _ => false
                            };
                            break;
                    }
                }

                // ✅ STORE PREREQUISITES (SAME AS ANALYSIS METHOD)
                if (!string.IsNullOrEmpty(prerequisitesValue))
                {
                    course.PrerequisitesString = prerequisitesValue;
                }
                if (!string.IsNullOrEmpty(courseSpecValue))
                {
                    course.CourseSpecification = courseSpecValue;
                }
                if (!string.IsNullOrEmpty(iconValue))
                {
                    course.Icon = iconValue;
                }

                // Validation
                if (string.IsNullOrEmpty(course.CourseCode))
                {
                    errors.Add($"Row {row}: CourseCode is required");
                    continue;
                }

                if (string.IsNullOrEmpty(course.CourseName))
                {
                    errors.Add($"Row {row}: CourseName is required");
                    continue;
                }

                validCourses.Add(course);
            }
            catch (Exception ex)
            {
                errors.Add($"Row {row}: Error processing row - {ex.Message}");
            }
        }

        // Save valid courses
        foreach (var course in validCourses)
        {
            try
            {
                var existingCourse = await _context.Courses
                    .FirstOrDefaultAsync(c => c.CourseCode == course.CourseCode);

                if (existingCourse != null)
                {
                    // Update existing course
                    existingCourse.CourseName = course.CourseName;
                    existingCourse.Description = course.Description;
                    existingCourse.Credits = course.Credits;
                    existingCourse.Department = course.Department;
                    existingCourse.Semester = course.Semester;
                    existingCourse.MaxStudents = course.MaxStudents;
                    existingCourse.MinGPA = course.MinGPA;
                    existingCourse.MinPassedHours = course.MinPassedHours;
                    existingCourse.PrerequisitesString = course.PrerequisitesString; // ✅ SAVE PREREQUISITES
                    existingCourse.CourseSpecification = course.CourseSpecification;
                    existingCourse.Icon = course.Icon;
                    existingCourse.IsActive = course.IsActive;
                }
                else
                {
                    // Add new course
                    _context.Courses.Add(course);
                }

                await _context.SaveChangesAsync();
                result.ImportedCount++;
            }
            catch (Exception ex)
            {
                errors.Add($"Error saving course {course.CourseCode}: {ex.Message}");
            }
        }

        result.Success = true;
        result.Message = $"Successfully imported {result.ImportedCount} courses. {errors.Count} errors found.";
        result.Errors = errors;
    }
    catch (Exception ex)
    {
        result.Message = $"Import failed: {ex.Message}";
        _logger.LogError(ex, "Error importing courses from Excel");
    }

    return result;
}
         
         */


        public async Task<byte[]> ExportCoursesToExcelAsync()
        {
            return await Task.Run(async () =>
            {
                var courses = await GetAllCoursesAsync();

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Courses");

                // Headers - ADD PREREQUISITES
                string[] headers = {
            "CourseCode", "CourseName", "Description", "Credits",
            "Department", "Semester", "MaxStudents", "MinGPA",
            "MinPassedHours", "Prerequisites", "IsActive"  // ✅ ADDED PREREQUISITES
        };

                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[1, i + 1].Value = headers[i];
                }

                // Data
                for (int i = 0; i < courses.Count; i++)
                {
                    var course = courses[i];
                    worksheet.Cells[i + 2, 1].Value = course.CourseCode;
                    worksheet.Cells[i + 2, 2].Value = course.CourseName;
                    worksheet.Cells[i + 2, 3].Value = course.Description;
                    worksheet.Cells[i + 2, 4].Value = course.Credits;
                    worksheet.Cells[i + 2, 5].Value = course.Department;
                    worksheet.Cells[i + 2, 6].Value = course.Semester;
                    worksheet.Cells[i + 2, 7].Value = course.MaxStudents;
                    worksheet.Cells[i + 2, 8].Value = course.MinGPA;
                    worksheet.Cells[i + 2, 9].Value = course.MinPassedHours;
                    worksheet.Cells[i + 2, 10].Value = course.PrerequisitesString ?? ""; // ✅ USE PrerequisitesString
                    worksheet.Cells[i + 2, 11].Value = course.IsActive ? "Yes" : "No";
                }

                // Format headers
                using (var range = worksheet.Cells[1, 1, 1, headers.Length])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                }

                worksheet.Cells.AutoFitColumns();
                return package.GetAsByteArray();
            });
        }
        /*

                public async Task<byte[]> ExportCoursesToExcelAsync()
                {
                return await Task.Run(async () =>
                {
                    var courses = await GetAllCoursesAsync();

                    using var package = new ExcelPackage();
                    var worksheet = package.Workbook.Worksheets.Add("Courses");

                    // ✅ UPDATED HEADERS WITH NEW FIELDS
                    string[] headers = {
                        "CourseCode", "CourseName", "Description", "Credits",
                        "Department", "Semester", "MaxStudents", "MinGPA",
                        "MinPassedHours", "Prerequisites", "CourseSpecification", "Icon", "IsActive"
                    };

                    for (int i = 0; i < headers.Length; i++)
                    {
                        worksheet.Cells[1, i + 1].Value = headers[i];
                    }

                    // ✅ UPDATED DATA WITH NEW FIELDS
                    for (int i = 0; i < courses.Count; i++)
                    {
                        var course = courses[i];
                        worksheet.Cells[i + 2, 1].Value = course.CourseCode;
                        worksheet.Cells[i + 2, 2].Value = course.CourseName;
                        worksheet.Cells[i + 2, 3].Value = course.Description;
                        worksheet.Cells[i + 2, 4].Value = course.Credits;
                        worksheet.Cells[i + 2, 5].Value = course.Department;
                        worksheet.Cells[i + 2, 6].Value = course.Semester;
                        worksheet.Cells[i + 2, 7].Value = course.MaxStudents;
                        worksheet.Cells[i + 2, 8].Value = course.MinGPA;
                        worksheet.Cells[i + 2, 9].Value = course.MinPassedHours;
                        worksheet.Cells[i + 2, 10].Value = course.PrerequisitesString; // ✅ SIMPLER
                        worksheet.Cells[i + 2, 11].Value = course.CourseSpecification;
                        worksheet.Cells[i + 2, 12].Value = course.Icon;
                        worksheet.Cells[i + 2, 13].Value = course.IsActive ? "Yes" : "No";
                    }

                    // Format headers
                    using (var range = worksheet.Cells[1, 1, 1, headers.Length])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                    }

                    worksheet.Cells.AutoFitColumns();
                    return package.GetAsByteArray();
                    });
                } 
          */


        public async Task<byte[]> ExportCoursesToPdfAsync()
        {
            var courses = await GetAllCoursesAsync();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(2f, Unit.Centimetre);

                    // Header - SIMPLE
                    page.Header().AlignCenter().Text("Courses Report").SemiBold().FontSize(16);

                    // Content - SIMPLE TABLE (no complex nesting)
                    page.Content().Table(table =>
                    {
                        // Define columns
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(3f, Unit.Centimetre);
                            columns.RelativeColumn(3);
                            columns.ConstantColumn(2f, Unit.Centimetre);
                            columns.RelativeColumn(2);
                            columns.ConstantColumn(2f, Unit.Centimetre);
                            columns.ConstantColumn(2f, Unit.Centimetre);
                        });

                        // Header
                        table.Header(header =>
                        {
                            header.Cell().Padding(5).Text("Code").SemiBold();
                            header.Cell().Padding(5).Text("Name").SemiBold();
                            header.Cell().Padding(5).Text("Credits").SemiBold();
                            header.Cell().Padding(5).Text("Department").SemiBold();
                            header.Cell().Padding(5).Text("Semester").SemiBold();
                            header.Cell().Padding(5).Text("Status").SemiBold();
                        });

                        // Rows - SIMPLE
                        foreach (var course in courses)
                        {
                            table.Cell().BorderBottom(1).Padding(5).Text(course.CourseCode);
                            table.Cell().BorderBottom(1).Padding(5).Text(course.CourseName);
                            table.Cell().BorderBottom(1).Padding(5).Text(course.Credits.ToString());
                            table.Cell().BorderBottom(1).Padding(5).Text(course.Department ?? "General");
                            table.Cell().BorderBottom(1).Padding(5).Text(course.Semester.ToString());
                            table.Cell().BorderBottom(1).Padding(5).Text(course.IsActive ? "Active" : "Inactive");
                        }
                    });

                    // Footer - SIMPLE
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span($"Generated on {DateTime.Now:yyyy-MM-dd} | {courses.Count} courses");
                    });
                });
            });

            return document.GeneratePdf();
        }

        public async Task<byte[]> ExportCourseDetailsToPdfAsync(int courseId)
        {
            var course = await GetCourseByIdAsync(courseId);
            if (course == null)
                return new byte[0];

            var enrollments = await GetCourseEnrollmentsAsync(courseId);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2f, Unit.Centimetre);

                    // Header
                    page.Header().AlignCenter().Text($"Course: {course.CourseCode} - {course.CourseName}").SemiBold().FontSize(16);

                    // Content - SIMPLE (no complex nesting)
                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(column =>
                    {
                        column.Spacing(10);

                        // Course Info - SIMPLE LIST
                        column.Item().Text($"Course Code: {course.CourseCode}").SemiBold();
                        column.Item().Text($"Course Name: {course.CourseName}").SemiBold();
                        column.Item().Text($"Description: {course.Description ?? "N/A"}");
                        column.Item().Text($"Credits: {course.Credits}");
                        column.Item().Text($"Department: {course.Department ?? "General"}");
                        column.Item().Text($"Semester: {course.Semester}");
                        column.Item().Text($"Max Students: {course.MaxStudents}");
                        column.Item().Text($"Current Enrollment: {enrollments.Count}");
                        column.Item().Text($"Status: {(course.IsActive ? "Active" : "Inactive")}");

                        // Enrollments section - SIMPLE
                        if (enrollments.Any())
                        {
                            column.Item().PaddingTop(10).Text("Enrollments:").SemiBold();

                            foreach (var enrollment in enrollments.Take(15)) // Limit to prevent overflow
                            {
                                column.Item().PaddingLeft(10).Text(
                                    $"{enrollment.Student?.Name ?? "N/A"} " +
                                    $"(ID: {enrollment.Student?.StudentId ?? "N/A"}) - " +
                                    $"Grade: {enrollment.Grade?.ToString() ?? "N/A"}");
                            }

                            if (enrollments.Count > 15)
                            {
                                column.Item().Text($"... and {enrollments.Count - 15} more students").Italic();
                            }
                        }
                    });

                    // Footer
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span($"Generated on {DateTime.Now:yyyy-MM-dd HH:mm}");
                    });
                });
            });

            return document.GeneratePdf();
        }

        public async Task<byte[]> ExportCourseEnrollmentsToExcelAsync(int courseId)
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Enrollments");

            var enrollments = await GetCourseEnrollmentsAsync(courseId);
            var course = await GetCourseByIdAsync(courseId);

            worksheet.Cells[1, 1].Value = $"Enrollments for {course?.CourseCode} - {course?.CourseName}";
            worksheet.Cells[1, 1].Style.Font.Bold = true;

            // Headers
            worksheet.Cells[3, 1].Value = "Student ID";
            worksheet.Cells[3, 2].Value = "Student Name";
            worksheet.Cells[3, 3].Value = "Enrollment Date";
            worksheet.Cells[3, 4].Value = "Grade";
            worksheet.Cells[3, 5].Value = "Grade Letter";
            worksheet.Cells[3, 6].Value = "Status";

            int row = 4;
            foreach (var enrollment in enrollments)
            {
                worksheet.Cells[row, 1].Value = enrollment.Student?.StudentId;
                worksheet.Cells[row, 2].Value = enrollment.Student?.Name;
                worksheet.Cells[row, 3].Value = enrollment.EnrollmentDate.ToString("yyyy-MM-dd");
                worksheet.Cells[row, 4].Value = enrollment.Grade;
                worksheet.Cells[row, 5].Value = enrollment.GradeLetter;
                worksheet.Cells[row, 6].Value = enrollment.IsActive ? "Active" : "Inactive";
                row++;
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            return package.GetAsByteArray();
        }

        // GRADE MANAGEMENT METHODS
        public async Task UpdateGradeWithCalculationAsync(int enrollmentId, decimal grade, string gradeLetter)
        {
            var enrollment = await _context.CourseEnrollments.FindAsync(enrollmentId);
            if (enrollment != null)
            {
                enrollment.Grade = grade;
                enrollment.GradeLetter = gradeLetter;
                enrollment.GradePoints = await CalculateGradePointsAsync(gradeLetter);
                enrollment.GradeStatus = await DetermineGradeStatusAsync(grade, gradeLetter);

                if (enrollment.GradeStatus == GradeStatus.Completed)
                {
                    enrollment.CompletionDate = DateTime.Now;
                }

                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<GradeScale>> GetGradeScalesAsync()
        {
            return await _context.GradeScales
                .Where(gs => gs.IsActive)
                .OrderByDescending(gs => gs.MinPercentage)
                .ToListAsync();
        }

        public async Task<decimal> CalculateStudentGPAAsync(int studentId)
        {
            var completedEnrollments = await _context.CourseEnrollments
                .Include(e => e.Course)
                .Where(e => e.StudentId == studentId
                         && e.GradePoints.HasValue
                         && e.GradeStatus == GradeStatus.Completed)
                .ToListAsync();

            if (!completedEnrollments.Any())
                return 0;

            var totalGradePoints = completedEnrollments.Sum(e => e.GradePoints!.Value * e.Course!.Credits);
            var totalCredits = completedEnrollments.Sum(e => e.Course!.Credits);

            return totalCredits > 0 ? totalGradePoints / totalCredits : 0;
        }

        // GRADE SCALE MANAGEMENT
        public async Task AddGradeScaleAsync(GradeScale gradeScale)
        {
            _context.GradeScales.Add(gradeScale);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateGradeScaleAsync(GradeScale gradeScale)
        {
            _context.GradeScales.Update(gradeScale);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteGradeScaleAsync(int id)
        {
            var gradeScale = await _context.GradeScales.FindAsync(id);
            if (gradeScale != null)
            {
                _context.GradeScales.Remove(gradeScale);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<GradeScale?> GetGradeScaleByIdAsync(int id)
        {
            return await _context.GradeScales.FindAsync(id);
        }

        public async Task<List<GradeScale>> GetAllGradeScalesAsync()
        {
            return await _context.GradeScales
                .OrderByDescending(gs => gs.MinPercentage)
                .ToListAsync();
        }

        // STUDENT GRADE REPORTS
        public async Task<List<CourseEnrollment>> GetStudentGradesAsync(int studentId)
        {
            return await _context.CourseEnrollments
                .Include(e => e.Course)
                .Where(e => e.StudentId == studentId)
                .OrderBy(e => e.Course!.CourseCode)
                .ToListAsync();
        }

        public async Task<StudentTranscript> GenerateStudentTranscriptAsync(int studentId)
        {
            var student = await _context.Students.FindAsync(studentId);
            var enrollments = await GetStudentGradesAsync(studentId);
            var gpa = await CalculateStudentGPAAsync(studentId);

            return new StudentTranscript
            {
                Student = student,
                Enrollments = enrollments,
                GPA = gpa,
                GeneratedDate = DateTime.Now
            };
        }

        public async Task<(bool CanDelete, string Message)> CanDeleteCourseAsync(int courseId)
        {
            try
            {
                var hasEnrollments = await _context.CourseEnrollments
                    .AnyAsync(ce => ce.CourseId == courseId);

                if (hasEnrollments)
                {
                    return (false, "Cannot delete course because it has student enrollments.");
                }

                var isPrerequisite = await _context.CoursePrerequisites
                    .AnyAsync(cp => cp.PrerequisiteCourseId == courseId);

                if (isPrerequisite)
                {
                    return (false, "Cannot delete course because it is used as a prerequisite for other courses.");
                }

                return (true, "Course can be deleted.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if course can be deleted");
                return (false, $"Error checking course deletion: {ex.Message}");
            }
        }

        // HELPER METHODS
        private async Task<decimal> CalculateGradePointsAsync(string gradeLetter)
        {
            var gradeScale = await _context.GradeScales
                .FirstOrDefaultAsync(gs => gs.GradeLetter == gradeLetter);
            return gradeScale?.GradePoints ?? 0;
        }

        private async Task<GradeStatus> DetermineGradeStatusAsync(decimal grade, string gradeLetter)
        {
            var gradeScale = await _context.GradeScales
                .FirstOrDefaultAsync(gs => gs.GradeLetter == gradeLetter);

            if (gradeScale == null)
                return GradeStatus.InProgress;

            return gradeScale.IsPassingGrade ? GradeStatus.Completed : GradeStatus.Failed;
        }

        private int TryParseInt(string? value, int defaultValue)
        {
            return int.TryParse(value, out int result) ? result : defaultValue;
        }

        private decimal TryParseDecimal(string? value, decimal defaultValue)
        {
            return decimal.TryParse(value, out decimal result) ? result : defaultValue;
        }

        private bool IsActive(string? value)
        {
            if (string.IsNullOrEmpty(value)) return true;
            var lowerValue = value.ToLower();
            return lowerValue == "yes" || lowerValue == "true" || lowerValue == "1" || lowerValue == "نعم";
        }

        public async Task<byte[]> ExportSelectedCoursesToExcelAsync(int[] courseIds)
        {
            ExcelPackage.License.SetNonCommercialOrganization("Student Management System");
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Selected Courses");

            // Headers
            worksheet.Cells[1, 1].Value = "CourseCode";
            worksheet.Cells[1, 2].Value = "CourseName";
            worksheet.Cells[1, 3].Value = "Description";
            worksheet.Cells[1, 4].Value = "Credits";
            worksheet.Cells[1, 5].Value = "Department";
            worksheet.Cells[1, 6].Value = "Semester";
            worksheet.Cells[1, 7].Value = "MaxStudents";
            worksheet.Cells[1, 8].Value = "MinGPA";
            worksheet.Cells[1, 9].Value = "MinPassedHours";
            worksheet.Cells[1, 10].Value = "IsActive";
            worksheet.Cells[1, 11].Value = "CurrentEnrollment";
            worksheet.Cells[1, 12].Value = "Prerequisites";

            // Style headers
            using (var range = worksheet.Cells[1, 1, 1, 12])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            int row = 2;
            foreach (var courseId in courseIds)
            {
                var course = await GetCourseByIdAsync(courseId);
                if (course != null)
                {
                    var prerequisites = await GetCoursePrerequisitesAsync(courseId);
                    var prereqCodes = prerequisites.Select(p => p.PrerequisiteCourse?.CourseCode)
                                                  .Where(code => !string.IsNullOrEmpty(code));

                    worksheet.Cells[row, 1].Value = course.CourseCode;
                    worksheet.Cells[row, 2].Value = course.CourseName;
                    worksheet.Cells[row, 3].Value = course.Description;
                    worksheet.Cells[row, 4].Value = course.Credits;
                    worksheet.Cells[row, 5].Value = course.Department;
                    worksheet.Cells[row, 6].Value = course.Semester;
                    worksheet.Cells[row, 7].Value = course.MaxStudents;
                    worksheet.Cells[row, 8].Value = course.MinGPA;
                    worksheet.Cells[row, 9].Value = course.MinPassedHours;
                    worksheet.Cells[row, 10].Value = course.IsActive ? "Yes" : "No";
                    worksheet.Cells[row, 11].Value = course.CurrentEnrollment;
                    worksheet.Cells[row, 12].Value = string.Join(", ", prereqCodes);

                    row++;
                }
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            return package.GetAsByteArray();
        }

        ///////////////////////
        ///
        //public async Task<ImportResult> AnalyzeExcelImportAsync(Stream stream, ImportSettings? settings = null)
        //{
        //    return await Task.Run(() =>
        //    {
        //        var result = new ImportResult();

        //        try
        //        {
        //            using var package = new ExcelPackage(stream);
        //            var worksheet = package.Workbook.Worksheets[0];

        //            if (worksheet == null || worksheet.Dimension == null)
        //            {
        //                result.Success = false;
        //                result.Message = "Excel file is empty or invalid.";
        //                return result;
        //            }

        //            var rowCount = worksheet.Dimension.Rows;
        //            var colCount = worksheet.Dimension.Columns;

        //            // Analyze headers
        //            var headers = new List<string>();
        //            for (int col = 1; col <= colCount; col++)
        //            {
        //                var headerValue = worksheet.Cells[1, col].Text.Trim();
        //                if (!string.IsNullOrEmpty(headerValue))
        //                {
        //                    headers.Add(headerValue);
        //                }
        //            }

        //            result.TotalRows = rowCount - 1;
        //            result.Headers = headers;

        //            // Preview data and validate
        //            var validCourses = new List<Course>();
        //            var invalidCourses = new List<InvalidCourse>();
        //            var errors = new List<string>();
        //            var previewData = new List<Dictionary<string, object>>();

        //            // Process ALL rows from 2 to rowCount
        //            for (int row = 2; row <= rowCount; row++)
        //            {
        //                try
        //                {
        //                    // Flexible column mapping
        //                    var course = new Course();
        //                    var rowData = new Dictionary<string, object>();
        //                    string? prerequisitesValue = null;

        //                    // Map columns by header name
        //                    for (int col = 1; col <= headers.Count; col++)
        //                    {
        //                        var header = headers[col - 1].ToLower();
        //                        var value = worksheet.Cells[row, col].Text.Trim();

        //                        // Store all row data
        //                        rowData[headers[col - 1]] = value;

        //                        switch (header)
        //                        {
        //                            case "coursecode":
        //                            case "كود المادة":
        //                                course.CourseCode = value;
        //                                break;
        //                            case "coursename":
        //                            case "اسم المادة":
        //                                course.CourseName = value;
        //                                break;
        //                            case "description":
        //                            case "الوصف":
        //                                course.Description = value;
        //                                break;
        //                            case "credits":
        //                            case "الساعات المعتمدة":
        //                                if (int.TryParse(value, out int credits))
        //                                    course.Credits = credits;
        //                                break;
        //                            case "department":
        //                            case "القسم":
        //                                course.Department = value;
        //                                break;
        //                            case "semester":
        //                            case "الفصل الدراسي":
        //                                if (int.TryParse(value, out int semester))
        //                                    course.Semester = semester;
        //                                break;
        //                            case "maxstudents":
        //                            case "الحد الأقصى للطلاب":
        //                                if (int.TryParse(value, out int maxStudents))
        //                                    course.MaxStudents = maxStudents;
        //                                break;
        //                            case "mingpa":
        //                            case "الحد الأدنى للمعدل التراكمي":
        //                                if (decimal.TryParse(value, out decimal minGPA))
        //                                    course.MinGPA = minGPA;
        //                                break;
        //                            case "minpassedhours":
        //                            case "الحد الأدنى للساعات المنجزة":
        //                                if (int.TryParse(value, out int minPassedHours))
        //                                    course.MinPassedHours = minPassedHours;
        //                                break;
        //                            case "prerequisites":
        //                            case "المتطلبات السابقة":
        //                                prerequisitesValue = value;
        //                                break;
        //                            case "isactive":
        //                            case "نشط":
        //                                course.IsActive = value.ToLower() switch
        //                                {
        //                                    "yes" => true,
        //                                    "نعم" => true,
        //                                    "true" => true,
        //                                    "1" => true,
        //                                    _ => false
        //                                };
        //                                break;
        //                        }
        //                    }

        //                    // ✅ STORE PREREQUISITES DATA FOR LATER PROCESSING
        //                    if (!string.IsNullOrEmpty(prerequisitesValue))
        //                    {
        //                        // Store prerequisites in a property that can be used later
        //                        // You can add a temporary property to Course or store in rowData
        //                        rowData["PrerequisitesRaw"] = prerequisitesValue;
        //                    }

        //                    string? validationError = null;

        //                    // Validate required fields
        //                    if (string.IsNullOrEmpty(course.CourseCode))
        //                    {
        //                        validationError = "Course Code is required";
        //                    }
        //                    else if (string.IsNullOrEmpty(course.CourseName))
        //                    {
        //                        validationError = "Course Name is required";
        //                    }
        //                    else if (course.Credits < 1 || course.Credits > 6)
        //                    {
        //                        validationError = "Credits must be between 1 and 6";
        //                    }
        //                    else if (course.Semester < 1 || course.Semester > 8)
        //                    {
        //                        validationError = "Semester must be between 1 and 8";
        //                    }

        //                    if (validationError != null)
        //                    {
        //                        var invalidCourse = new InvalidCourse
        //                        {
        //                            RowNumber = row,
        //                            CourseCode = course.CourseCode,
        //                            CourseName = course.CourseName,
        //                            ErrorMessage = validationError,
        //                            RowData = rowData
        //                        };
        //                        invalidCourses.Add(invalidCourse);
        //                        errors.Add($"Row {row}: {validationError}");
        //                        continue;
        //                    }

        //                    // If valid, add to courses
        //                    validCourses.Add(course);

        //                    // Add to preview data
        //                    if (previewData.Count < 10)
        //                    {
        //                        var previewRow = new Dictionary<string, object>
        //                        {
        //                            ["CourseCode"] = course.CourseCode,
        //                            ["CourseName"] = course.CourseName,
        //                            ["Description"] = course.Description ?? "N/A",
        //                            ["Credits"] = course.Credits,
        //                            ["Department"] = course.Department ?? "N/A",
        //                            ["Semester"] = course.Semester,
        //                            ["MaxStudents"] = course.MaxStudents,
        //                            ["MinGPA"] = course.MinGPA,
        //                            ["Prerequisites"] = prerequisitesValue ?? "None", // ✅ ADD PREREQUISITES TO PREVIEW
        //                            ["IsActive"] = course.IsActive ? "Yes" : "No"
        //                        };
        //                        previewData.Add(previewRow);
        //                    }
        //                }
        //                catch (Exception ex)
        //                {
        //                    var errorMsg = $"Row {row}: {ex.Message}";
        //                    errors.Add(errorMsg);
        //                    var invalidCourse = new InvalidCourse
        //                    {
        //                        RowNumber = row,
        //                        ErrorMessage = errorMsg
        //                    };
        //                    invalidCourses.Add(invalidCourse);
        //                }
        //            }

        //            result.ValidCourses = validCourses;
        //            result.InvalidCourses = invalidCourses;
        //            result.PreviewData = previewData;
        //            result.Errors = errors;
        //            result.ErrorCount = errors.Count;
        //            result.Success = true;
        //            result.Message = $"File analyzed successfully. Found {result.TotalRows} rows, {validCourses.Count} valid courses.";

        //            if (invalidCourses.Any())
        //            {
        //                result.Message += $" {invalidCourses.Count} invalid courses found.";
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            result.Success = false;
        //            result.Message = $"Error analyzing file: {ex.Message}";
        //        }

        //        return result;
        //    });
        //}

        public async Task<ImportResult> AnalyzeExcelImportAsync(Stream stream, ImportSettings? settings = null)
        {
            return await Task.Run(() =>
            {
                var result = new ImportResult();

                try
                {
                    using var package = new ExcelPackage(stream);
                    var worksheet = package.Workbook.Worksheets[0];

                    if (worksheet == null || worksheet.Dimension == null)
                    {
                        result.Success = false;
                        result.Message = "Excel file is empty or invalid.";
                        return result;
                    }

                    var rowCount = worksheet.Dimension.Rows;
                    var colCount = worksheet.Dimension.Columns;

                    // Analyze headers
                    var headers = new List<string>();
                    for (int col = 1; col <= colCount; col++)
                    {
                        var headerValue = worksheet.Cells[1, col].Text.Trim();
                        if (!string.IsNullOrEmpty(headerValue))
                        {
                            headers.Add(headerValue);
                        }
                    }

                    result.TotalRows = rowCount - 1;
                    result.Headers = headers;

                    // Preview data and validate
                    var validCourses = new List<Course>();
                    var invalidCourses = new List<InvalidCourse>();
                    var errors = new List<string>();
                    var previewData = new List<Dictionary<string, object>>();

                    // Process ALL rows from 2 to rowCount
                    for (int row = 2; row <= rowCount; row++)
                    {
                        try
                        {
                            // Flexible column mapping
                            var course = new Course();
                            var rowData = new Dictionary<string, object>();
                            string? prerequisitesValue = null;
                            string? courseSpecValue = null;
                            string? iconValue = null;

                            // Map columns by header name
                            for (int col = 1; col <= headers.Count; col++)
                            {
                                var header = headers[col - 1].ToLower();
                                var value = worksheet.Cells[row, col].Text.Trim();

                                // Store all row data
                                rowData[headers[col - 1]] = value;

                                switch (header)
                                {
                                    case "coursecode (required)":
                                    case "coursecode":
                                    case "كود المادة":
                                        course.CourseCode = value;
                                        break;
                                    case "coursename (required)":
                                    case "coursename":
                                    case "اسم المادة":
                                        course.CourseName = value;
                                        break;
                                    case "description":
                                    case "الوصف":
                                        course.Description = value;
                                        break;
                                    case "credits":
                                    case "الساعات المعتمدة":
                                        if (int.TryParse(value, out int credits))
                                            course.Credits = credits;
                                        else if (string.IsNullOrEmpty(value))
                                            course.Credits = 3; // Default value
                                        break;
                                    case "department":
                                    case "القسم":
                                        course.Department = value;
                                        break;
                                    case "semester":
                                    case "الفصل الدراسي":
                                        if (int.TryParse(value, out int semester))
                                            course.Semester = semester;
                                        else if (string.IsNullOrEmpty(value))
                                            course.Semester = 1; // Default value
                                        break;
                                    case "maxstudents":
                                    case "الحد الأقصى للطلاب":
                                        if (int.TryParse(value, out int maxStudents))
                                            course.MaxStudents = maxStudents;
                                        else if (string.IsNullOrEmpty(value))
                                            course.MaxStudents = 1000; // Default value
                                        break;
                                    case "mingpa":
                                    case "الحد الأدنى للمعدل التراكمي":
                                        if (decimal.TryParse(value, out decimal minGPA))
                                            course.MinGPA = minGPA;
                                        else if (string.IsNullOrEmpty(value))
                                            course.MinGPA = 2.0m; // Default value
                                        break;
                                    case "minpassedhours":
                                    case "الحد الأدنى للساعات المنجزة":
                                        if (int.TryParse(value, out int minPassedHours))
                                            course.MinPassedHours = minPassedHours;
                                        else if (string.IsNullOrEmpty(value))
                                            course.MinPassedHours = 0; // Default value
                                        break;
                                    case "prerequisites":
                                    case "المتطلبات السابقة":
                                        prerequisitesValue = value;
                                        break;
                                    case "coursespecification":
                                        courseSpecValue = value;
                                        break;
                                    case "icon":
                                        iconValue = value;
                                        break;
                                    case "isactive":
                                    case "نشط":
                                        course.IsActive = value.ToLower() switch
                                        {
                                            "yes" => true,
                                            "نعم" => true,
                                            "true" => true,
                                            "1" => true,
                                            "y" => true,
                                            _ => false
                                        };
                                        break;
                                }
                            }

                            // ✅ STORE NEW FIELDS
                            // ✅ STORE NEW FIELDS
                            if (!string.IsNullOrEmpty(prerequisitesValue))
                            {
                                course.PrerequisitesString = prerequisitesValue;  // Use the new property
                            }
                            if (!string.IsNullOrEmpty(courseSpecValue))
                            {
                                course.CourseSpecification = courseSpecValue;
                            }
                            if (!string.IsNullOrEmpty(iconValue))
                            {
                                course.Icon = iconValue;
                            }

                            string? validationError = null;

                            // ✅ FIXED VALIDATION: More flexible validation
                            if (string.IsNullOrEmpty(course.CourseCode))
                            {
                                validationError = "Course Code is required";
                            }
                            else if (string.IsNullOrEmpty(course.CourseName))
                            {
                                validationError = "Course Name is required";
                            }
                            else if (course.Credits < 1 || course.Credits > 6)
                            {
                                validationError = "Credits must be between 1 and 6";
                            }
                            else if (course.Semester < 1 || course.Semester > 8)
                            {
                                validationError = "Semester must be between 1 and 8";
                            }
                            else if (course.MaxStudents < 1 || course.MaxStudents > 1000)
                            {
                                validationError = "Max Students must be between 1 and 1000";
                            }

                            if (validationError != null)
                            {
                                var invalidCourse = new InvalidCourse
                                {
                                    RowNumber = row,
                                    CourseCode = course.CourseCode,
                                    CourseName = course.CourseName,
                                    ErrorMessage = validationError,
                                    RowData = rowData
                                };
                                invalidCourses.Add(invalidCourse);
                                errors.Add($"Row {row}: {validationError}");
                                continue;
                            }

                            // If valid, add to courses
                            validCourses.Add(course);

                            // Add to preview data
                            if (previewData.Count < 10)
                            {
                                var previewRow = new Dictionary<string, object>
                                {
                                    ["CourseCode"] = course.CourseCode,
                                    ["CourseName"] = course.CourseName,
                                    ["Description"] = course.Description ?? "N/A",
                                    ["Credits"] = course.Credits,
                                    ["Department"] = course.Department ?? "N/A",
                                    ["Semester"] = course.Semester,
                                    ["MaxStudents"] = course.MaxStudents,
                                    ["MinGPA"] = course.MinGPA,
                                    ["MinPassedHours"] = course.MinPassedHours,
                                    ["Prerequisites"] = prerequisitesValue ?? "None",
                                    ["CourseSpecification"] = courseSpecValue ?? "None",
                                    ["Icon"] = iconValue ?? "None",
                                    ["IsActive"] = course.IsActive ? "Yes" : "No"
                                };
                                previewData.Add(previewRow);
                            }
                        }
                        catch (Exception ex)
                        {
                            var errorMsg = $"Row {row}: {ex.Message}";
                            errors.Add(errorMsg);
                            var invalidCourse = new InvalidCourse
                            {
                                RowNumber = row,
                                ErrorMessage = errorMsg
                            };
                            invalidCourses.Add(invalidCourse);
                        }
                    }

                    result.ValidCourses = validCourses;
                    result.InvalidCourses = invalidCourses;
                    result.PreviewData = previewData;
                    result.Errors = errors;
                    result.ErrorCount = errors.Count;
                    result.Success = true;
                    result.Message = $"File analyzed successfully. Found {result.TotalRows} rows, {validCourses.Count} valid courses.";

                    if (invalidCourses.Any())
                    {
                        result.Message += $" {invalidCourses.Count} invalid courses found.";
                    }
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.Message = $"Error analyzing file: {ex.Message}";
                }

                return result;
            });
        }

        // Add this method to process prerequisites after courses are imported
        private async Task ProcessPrerequisitesAsync(List<Course> importedCourses, Dictionary<string, string> prerequisitesMapping)
        {
            foreach (var course in importedCourses)
            {
                if (prerequisitesMapping.ContainsKey(course.CourseCode))
                {
                    var prerequisiteCodes = prerequisitesMapping[course.CourseCode]
                        .Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (var prereqCode in prerequisiteCodes)
                    {
                        var prerequisiteCourse = await _context.Courses
                            .FirstOrDefaultAsync(c => c.CourseCode == prereqCode.Trim());

                        if (prerequisiteCourse != null)
                        {
                            var coursePrerequisite = new CoursePrerequisite
                            {
                                CourseId = course.Id,
                                PrerequisiteCourseId = prerequisiteCourse.Id,
                                IsRequired = true
                            };
                            _context.CoursePrerequisites.Add(coursePrerequisite);
                        }
                    }
                }
            }
            await _context.SaveChangesAsync();
        }

        public async Task<ImportResult> ExecuteImportAsync(ImportResult analysisResult, ImportSettings settings)
        {
            var result = new ImportResult();

            try
            {
                int importedCount = 0;
                int errorCount = 0;

                foreach (var course in analysisResult.ValidCourses)
                {
                    try
                    {
                        var existingCourse = await GetCourseByCodeAsync(course.CourseCode);

                        if (existingCourse != null && settings.UpdateExisting)
                        {
                            // Update existing course
                            existingCourse.CourseName = course.CourseName;
                            existingCourse.Description = course.Description;
                            existingCourse.Credits = course.Credits;
                            existingCourse.Department = course.Department;
                            existingCourse.Semester = course.Semester;
                            existingCourse.MaxStudents = course.MaxStudents;
                            existingCourse.MinGPA = course.MinGPA;
                            existingCourse.MinPassedHours = course.MinPassedHours;
                            existingCourse.IsActive = course.IsActive;

                            _context.Courses.Update(existingCourse);
                        }
                        else if (existingCourse == null)
                        {
                            // Add new course
                            _context.Courses.Add(course);
                        }

                        await _context.SaveChangesAsync();
                        importedCount++;
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        result.Errors.Add($"Error importing course {course.CourseCode}: {ex.Message}");
                    }
                }

                result.Success = true;
                result.ImportedCount = importedCount;
                result.ErrorCount = errorCount;
                result.Message = $"Import completed. {importedCount} courses imported, {errorCount} errors.";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Import failed: {ex.Message}";
            }

            return result;
        }

        public async Task<byte[]> ExportSelectedCoursesAsync(int[] courseIds)
        {
            try
            {
                // Get all selected courses in one database query
                var selectedCourses = new List<Course>();

                foreach (var id in courseIds)
                {
                    var course = await GetCourseByIdAsync(id);
                    if (course != null)
                    {
                        selectedCourses.Add(course);
                    }
                }

                if (!selectedCourses.Any())
                {
                    throw new Exception("No valid courses found for export.");
                }

                return await GenerateExcelFileAsync(selectedCourses);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to export selected courses: {ex.Message}", ex);
            }
        }

        private async Task<byte[]> GenerateExcelFileAsync(List<Course> courses)
        {
            return await Task.Run(() =>
            {
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Courses");

                // Add headers with correct field names
                string[] headers = {
                    "CourseCode", "CourseName", "Description", "Credits",
                    "Department", "Semester", "MaxStudents", "MinGPA",
                    "MinPassedHours", "IsActive"
                };

                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[1, i + 1].Value = headers[i];
                }

                // Style headers
                using (var range = worksheet.Cells[1, 1, 1, headers.Length])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }

                // Add data
                int row = 2;
                foreach (var course in courses)
                {
                    worksheet.Cells[row, 1].Value = course.CourseCode;
                    worksheet.Cells[row, 2].Value = course.CourseName;
                    worksheet.Cells[row, 3].Value = course.Description;
                    worksheet.Cells[row, 4].Value = course.Credits;
                    worksheet.Cells[row, 5].Value = course.Department;
                    worksheet.Cells[row, 6].Value = course.Semester;
                    worksheet.Cells[row, 7].Value = course.MaxStudents;
                    worksheet.Cells[row, 8].Value = Math.Round(course.MinGPA, 2);
                    worksheet.Cells[row, 9].Value = course.MinPassedHours;
                    worksheet.Cells[row, 10].Value = course.IsActive ? "Yes" : "No";
                    row++;
                }

                // Auto-fit columns
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                return package.GetAsByteArray();
            });
        }

        public async Task<byte[]> ExportCourseToPdfAsync(int courseId)
        {
            var course = await GetCourseByIdAsync(courseId);
            if (course == null)
                throw new Exception("Course not found");

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header()
                        .AlignCenter()
                        .Text("Course Information")
                        .SemiBold().FontSize(24).FontColor(Colors.Blue.Darken3);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(column =>
                        {
                            column.Spacing(20);

                            // Course Information
                            column.Item().Text($"Course Code: {course.CourseCode}");
                            column.Item().Text($"Course Name: {course.CourseName}");
                            column.Item().Text($"Description: {course.Description ?? "N/A"}");
                            column.Item().Text($"Credits: {course.Credits}");
                            column.Item().Text($"Department: {course.Department}");
                            column.Item().Text($"Semester: {course.Semester}");
                            column.Item().Text($"Max Students: {course.MaxStudents}");
                            column.Item().Text($"Min GPA: {course.MinGPA:F2}");
                            column.Item().Text($"Min Passed Hours: {course.MinPassedHours}");
                            column.Item().Text($"Active: {(course.IsActive ? "Yes" : "No")}");

                            // Enrollment Information
                            column.Item().Text($"Current Enrollment: {course.CurrentEnrollment}");
                            column.Item().Text($"Available Seats: {course.MaxStudents - course.CurrentEnrollment}");
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Generated on: ");
                            x.Span(DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                        });
                });
            });

            return document.GeneratePdf();
        }

        public async Task<byte[]> ExportAllCoursesToPdfAsync()
        {
            var courses = await GetAllCoursesAsync();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header()
                        .AlignCenter()
                        .Text("All Courses Report")
                        .SemiBold().FontSize(20).FontColor(Colors.Blue.Darken3);

                    page.Content()
                        .Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(); // Course Code
                                columns.RelativeColumn(); // Course Name
                                columns.RelativeColumn(); // Department
                                columns.RelativeColumn(); // Semester
                                columns.RelativeColumn(); // Credits
                                columns.RelativeColumn(); // Max Students
                                columns.RelativeColumn(); // Current Enrollment
                                columns.RelativeColumn(); // Status
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Course Code");
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Course Name");
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Department");
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Semester");
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Credits");
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Max Students");
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Current Enrollment");
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Status");
                            });

                            foreach (var course in courses)
                            {
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(course.CourseCode);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(course.CourseName);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(course.Department ?? "N/A");
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(course.Semester.ToString());
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(course.Credits.ToString());
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(course.MaxStudents.ToString());
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(course.CurrentEnrollment.ToString());
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(course.IsActive ? "Active" : "Inactive");
                            }
                        });
                });
            });

            return document.GeneratePdf();
        }

        public async Task<string> AddTestCourses()
        {
            try
            {
                var testCourses = new List<Course>
                {
                    new Course
                    {
                        CourseCode = "CS101",
                        CourseName = "Introduction to Computer Science",
                        Description = "Fundamental concepts of computer science and programming",
                        Credits = 3,
                        Department = "Computer Science",
                        Semester = 1,
                        MaxStudents = 40,
                        MinGPA = 2.0m,
                        MinPassedHours = 0,
                        IsActive = true
                    },
                    new Course
                    {
                        CourseCode = "MATH201",
                        CourseName = "Calculus I",
                        Description = "Differential and integral calculus",
                        Credits = 4,
                        Department = "Mathematics",
                        Semester = 2,
                        MaxStudents = 35,
                        MinGPA = 2.5m,
                        MinPassedHours = 15,
                        IsActive = true
                    },
                    new Course
                    {
                        CourseCode = "PHY101",
                        CourseName = "General Physics",
                        Description = "Fundamental principles of physics",
                        Credits = 4,
                        Department = "Physics",
                        Semester = 1,
                        MaxStudents = 30,
                        MinGPA = 2.0m,
                        MinPassedHours = 0,
                        IsActive = true
                    }
                };

                int addedCount = 0;
                foreach (var course in testCourses)
                {
                    if (!await CourseExistsAsync(course.CourseCode))
                    {
                        _context.Courses.Add(course);
                        addedCount++;
                    }
                }

                await _context.SaveChangesAsync();
                return $"Added {addedCount} test courses successfully.";
            }
            catch (Exception ex)
            {
                return $"Error adding test courses: {ex.Message}";
            }
        }

        public async Task DeleteMultipleCoursesAsync(int[] courseIds)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                foreach (var courseId in courseIds)
                {
                    // Delete enrollments
                    var enrollments = await _context.CourseEnrollments
                        .Where(ce => ce.CourseId == courseId)
                        .ToListAsync();
                    _context.CourseEnrollments.RemoveRange(enrollments);

                    // Delete prerequisites
                    var prerequisitesAsCourse = await _context.CoursePrerequisites
                        .Where(cp => cp.CourseId == courseId)
                        .ToListAsync();
                    _context.CoursePrerequisites.RemoveRange(prerequisitesAsCourse);

                    var prerequisitesAsPrereq = await _context.CoursePrerequisites
                        .Where(cp => cp.PrerequisiteCourseId == courseId)
                        .ToListAsync();
                    _context.CoursePrerequisites.RemoveRange(prerequisitesAsPrereq);

                    // Delete course
                    var course = await _context.Courses.FindAsync(courseId);
                    if (course != null)
                    {
                        _context.Courses.Remove(course);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        public async Task DeleteAllCoursesAsync()
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Delete all records in correct order
                _context.CourseEnrollments.RemoveRange(_context.CourseEnrollments);
                _context.CoursePrerequisites.RemoveRange(_context.CoursePrerequisites);
                _context.Courses.RemoveRange(_context.Courses);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<Course>> GetAllCoursesWithPrerequisitesAsync()
        {
            return await _context.Courses
                .Include(c => c.CourseDepartment)
                .Include(c => c.CourseSemester)
                .Include(c => c.CourseEnrollments)
                .Include(c => c.Prerequisites)
                    .ThenInclude(p => p.PrerequisiteCourse)
                .Where(c => c.IsActive)
                .OrderBy(c => c.CourseCode)
                .ToListAsync();
        }
/*
        private async Task<Course?> GetCourseByCodeAsync(string courseCode)
        {
            return await _context.Courses
                .FirstOrDefaultAsync(c => c.CourseCode == courseCode);
        }

        private async Task AddPrerequisiteAsync(int courseId, int prerequisiteCourseId, decimal? minGrade)
        {
            var existing = await _context.CoursePrerequisites
                .FirstOrDefaultAsync(cp => cp.CourseId == courseId && cp.PrerequisiteCourseId == prerequisiteCourseId);

            if (existing == null)
            {
                var prerequisite = new CoursePrerequisite
                {
                    CourseId = courseId,
                    PrerequisiteCourseId = prerequisiteCourseId,
                    MinGrade = minGrade,
                    IsRequired = true
                };
                _context.CoursePrerequisites.Add(prerequisite);
                await _context.SaveChangesAsync();
            }
        }

        private int TryParseInt(string? value, int defaultValue)
        {
            return int.TryParse(value, out int result) ? result : defaultValue;
        }

        private decimal TryParseDecimal(string? value, decimal defaultValue)
        {
            return decimal.TryParse(value, out decimal result) ? result : defaultValue;
        }

        private bool IsActive(string? value)
        {
            return value?.ToLower() switch
            {
                "yes" => true,
                "نعم" => true,
                "true" => true,
                "1" => true,
                "y" => true,
                _ => false
            };
        }
*/
    }
}