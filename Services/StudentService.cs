using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;

namespace StudentManagementSystem.Services
{
    public class StudentService : IStudentService
    {
        private readonly ApplicationDbContext _context;

        public StudentService(ApplicationDbContext context)
        {
            _context = context;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task<List<Student>> GetAllStudentsAsync()
        {
            return await _context.Students
                .Include(s => s.Account)
                .Include(s => s.Courses)
                .Include(s => s.FeePayments)
                .OrderBy(s => s.StudentId)
                .ToListAsync();
        }

        public async Task<Student?> GetStudentByIdAsync(int id)
        {
            return await _context.Students
                .Include(s => s.Account)
                .Include(s => s.Courses)
                .Include(s => s.FeePayments)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<Student?> GetStudentByStudentIdAsync(string studentId)
        {
            return await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == studentId);
        }

        public async Task<bool> StudentExistsAsync(string studentId)
        {
            return await _context.Students
                .AnyAsync(s => s.StudentId == studentId);
        }

        public async Task AddStudentAsync(Student student)
        {
            _context.Students.Add(student);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateStudentAsync(Student student)
        {
            _context.Students.Update(student);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteStudentAsync(int id)
        {
            var student = await GetStudentByIdAsync(id);
            if (student != null)
            {
                _context.Students.Remove(student);
                await _context.SaveChangesAsync();
            }
        }


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

                    // === DEBUG LOGGING ===
                    Console.WriteLine($"=== DEBUG: Excel Analysis ===");
                    Console.WriteLine($"Total rows in Excel: {rowCount}");
                    Console.WriteLine($"Total columns in Excel: {colCount}");
                    // === END DEBUG LOGGING ===

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

                    // === DEBUG LOGGING ===
                    Console.WriteLine($"Headers found: {string.Join(", ", headers)}");
                    // === END DEBUG LOGGING ===

                    result.TotalRows = rowCount - 1;
                    result.Headers = headers;

                    // Preview data and validate - PROCESS ALL ROWS
                    var validStudents = new List<Student>();
                    var invalidStudents = new List<InvalidStudent>(); // ADD THIS
                    var errors = new List<string>();
                    var previewData = new List<Dictionary<string, object>>();

                    // Process ALL rows from 2 to rowCount
                    for (int row = 2; row <= rowCount; row++)
                    {
                        try
                        {
                            // === DEBUG LOGGING ===
                            Console.WriteLine($"--- Processing row {row} ---");
                            // === END DEBUG LOGGING ===

                            // === FLEXIBLE COLUMN MAPPING ===
                            var student = new Student();
                            var rowData = new Dictionary<string, object>(); // For invalid student data

                            // Map columns by header name instead of fixed position
                            for (int col = 1; col <= headers.Count; col++)
                            {
                                var header = headers[col - 1].ToLower();
                                var value = worksheet.Cells[row, col].Text.Trim();

                                // === DEBUG LOGGING ===
                                Console.WriteLine($"  Column {col} ('{header}'): '{value}'");
                                // === END DEBUG LOGGING ===

                                // Store all row data for potential invalid student
                                rowData[headers[col - 1]] = value;

                                switch (header)
                                {
                                    case "studentid":
                                    case "رقم الطالب":
                                        student.StudentId = value;
                                        break;
                                    case "name":
                                    case "الاسم الكامل":
                                        student.Name = value;
                                        break;
                                    case "seatnumber":
                                    case "رقم المقعد":
                                        student.SeatNumber = value;
                                        break;
                                    case "nationalid":
                                    case "رقم الهوية":
                                        student.NationalId = value;
                                        break;
                                    case "department":
                                    case "القسم":
                                        student.Department = value;
                                        break;
                                    case "studylevel":
                                    case "المستوى الدراسي":
                                        student.StudyLevel = value;
                                        break;
                                    case "semester":
                                    case "الفصل الدراسي":
                                        student.Semester = value;
                                        break;
                                    case "grade":
                                    case "الصف":
                                        student.Grade = value;
                                        break;
                                    case "phone":
                                    case "الهاتف":
                                        student.Phone = value;
                                        break;
                                    case "email":
                                    case "البريد الإلكتروني":
                                        student.Email = value;
                                        break;
                                    case "gpa":
                                    case "المعدل التراكمي":
                                        if (decimal.TryParse(value, out decimal gpa))
                                            student.GPA = gpa;
                                        break;
                                    case "percentage":
                                    case "النسبة المئوية":
                                        if (decimal.TryParse(value, out decimal percentage))
                                            student.Percentage = percentage;
                                        break;
                                    case "passedhours":
                                    case "الساعات المنجزة":
                                        if (int.TryParse(value, out int passedHours))
                                            student.PassedHours = passedHours;
                                        break;
                                }
                            }
                            // === END FLEXIBLE COLUMN MAPPING ===

                            // === DEBUG LOGGING ===
                            Console.WriteLine($"  Parsed StudentId: '{student.StudentId}'");
                            Console.WriteLine($"  Parsed Name: '{student.Name}'");
                            Console.WriteLine($"  Is StudentId empty: {string.IsNullOrEmpty(student.StudentId)}");
                            Console.WriteLine($"  Is Name empty: {string.IsNullOrEmpty(student.Name)}");
                            // === END DEBUG LOGGING ===

                            string? validationError = null; // FIX: Make it nullable

                            // Validate required fields
                            if (string.IsNullOrEmpty(student.StudentId))
                            {
                                validationError = "Student ID is required";
                            }
                            else if (string.IsNullOrEmpty(student.Name))
                            {
                                validationError = "Name is required";
                            }

                            if (validationError != null)
                            {
                                // Create invalid student record
                                var invalidStudent = new InvalidStudent
                                {
                                    RowNumber = row,
                                    StudentId = student.StudentId,
                                    Name = student.Name,
                                    ErrorMessage = validationError,
                                    RowData = rowData
                                };

                                invalidStudents.Add(invalidStudent);
                                errors.Add($"Row {row}: {validationError}");

                                // === DEBUG LOGGING ===
                                Console.WriteLine($"  -> INVALID: {validationError}");
                                // === END DEBUG LOGGING ===
                                continue;
                            }

                            // If we get here, student is valid
                            validStudents.Add(student);

                            // Only add first 10 rows to preview data for display
                            if (previewData.Count < 10)
                            {
                                var previewRow = new Dictionary<string, object>
                                {
                                    ["StudentId"] = student.StudentId,
                                    ["Name"] = student.Name,
                                    ["SeatNumber"] = student.SeatNumber ?? "N/A",
                                    ["Phone"] = student.Phone ?? "N/A",
                                    ["Email"] = student.Email ?? "N/A",
                                    ["Department"] = student.Department ?? "N/A",
                                    ["GPA"] = student.GPA,
                                    ["Percentage"] = student.Percentage,
                                    ["PassedHours"] = student.PassedHours
                                };
                                previewData.Add(previewRow);
                            }

                            // === DEBUG LOGGING ===
                            Console.WriteLine($"  -> VALID student added");
                            // === END DEBUG LOGGING ===
                        }
                        catch (Exception ex)
                        {
                            var errorMsg = $"Row {row}: {ex.Message}";
                            errors.Add(errorMsg);

                            // Create invalid student for exception cases too
                            var invalidStudent = new InvalidStudent
                            {
                                RowNumber = row,
                                ErrorMessage = errorMsg
                            };
                            invalidStudents.Add(invalidStudent);

                            // === DEBUG LOGGING ===
                            Console.WriteLine($"  -> EXCEPTION: {errorMsg}");
                            // === END DEBUG LOGGING ===
                        }
                    }

                    // === DEBUG LOGGING ===
                    Console.WriteLine($"=== ANALYSIS COMPLETE ===");
                    Console.WriteLine($"Total rows processed: {rowCount - 1}");
                    Console.WriteLine($"Valid students found: {validStudents.Count}");
                    Console.WriteLine($"Invalid students found: {invalidStudents.Count}");
                    Console.WriteLine($"Errors found: {errors.Count}");
                    Console.WriteLine($"Preview data entries: {previewData.Count}");
                    // === END DEBUG LOGGING ===

                    result.ValidStudents = validStudents;
                    result.InvalidStudents = invalidStudents; // ADD THIS LINE
                    result.PreviewData = previewData;
                    result.Errors = errors;
                    result.ErrorCount = errors.Count; // Set ErrorCount
                    result.Success = true;
                    result.Message = $"File analyzed successfully. Found {result.TotalRows} rows, {validStudents.Count} valid students.";

                    if (invalidStudents.Any())
                    {
                        result.Message += $" {invalidStudents.Count} invalid students found.";
                    }
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.Message = $"Error analyzing file: {ex.Message}";

                    // === DEBUG LOGGING ===
                    Console.WriteLine($"=== ANALYSIS FAILED ===");
                    Console.WriteLine($"Error: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    // === END DEBUG LOGGING ===
                }

                return result;
            });
        }

        public async Task<ImportResult> ExecuteImportAsync(ImportResult analysisResult, ImportSettings settings)
        {
            var result = new ImportResult();

            try
            {
                int importedCount = 0;
                int errorCount = 0;

                foreach (var student in analysisResult.ValidStudents)
                {
                    try
                    {
                        var existingStudent = await GetStudentByStudentIdAsync(student.StudentId);

                        if (existingStudent != null && settings.UpdateExisting) // Use UpdateExisting instead of OverrideExisting
                        {
                            // Update existing student
                            existingStudent.Name = student.Name;
                            existingStudent.SeatNumber = student.SeatNumber;
                            existingStudent.NationalId = student.NationalId;
                            existingStudent.Department = student.Department;
                            existingStudent.StudyLevel = student.StudyLevel;
                            existingStudent.Semester = student.Semester;
                            existingStudent.Grade = student.Grade;
                            existingStudent.Phone = student.Phone;
                            existingStudent.Email = student.Email;
                            existingStudent.GPA = student.GPA;
                            existingStudent.Percentage = student.Percentage;
                            existingStudent.PassedHours = student.PassedHours;

                            _context.Students.Update(existingStudent);
                        }
                        else if (existingStudent == null)
                        {
                            // Add new student
                            _context.Students.Add(student);
                        }

                        await _context.SaveChangesAsync();
                        importedCount++;
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        result.Errors.Add($"Error importing student {student.StudentId}: {ex.Message}");
                    }
                }

                result.Success = true;
                result.ImportedCount = importedCount;
                result.ErrorCount = errorCount;
                result.Message = $"Import completed. {importedCount} students imported, {errorCount} errors.";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Import failed: {ex.Message}";
            }

            return result;
        }

        public async Task<byte[]> ExportStudentsToExcelAsync()
        {
            return await Task.Run(async () =>
            {
                var students = await GetAllStudentsAsync();

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("الطلاب");

                // Arabic headers
                string[] headers = {
                    "رقم الطالب", "رقم المقعد", "الاسم الكامل", "رقم الهوية",
                    "القسم", "المستوى الدراسي", "الفصل الدراسي", "الصف",
                    "الهاتف", "البريد الإلكتروني", "الساعات المنجزة", "المعدل التراكمي"
                };

                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[1, i + 1].Value = headers[i];
                }

                // Data
                for (int i = 0; i < students.Count; i++)
                {
                    var student = students[i];
                    worksheet.Cells[i + 2, 1].Value = student.StudentId;
                    worksheet.Cells[i + 2, 2].Value = student.SeatNumber;
                    worksheet.Cells[i + 2, 3].Value = student.Name;
                    worksheet.Cells[i + 2, 4].Value = student.NationalId;
                    worksheet.Cells[i + 2, 5].Value = student.Department;
                    worksheet.Cells[i + 2, 6].Value = student.StudyLevel;
                    worksheet.Cells[i + 2, 7].Value = student.Semester;
                    worksheet.Cells[i + 2, 8].Value = student.Grade;
                    worksheet.Cells[i + 2, 9].Value = student.Phone;
                    worksheet.Cells[i + 2, 10].Value = student.Email;
                    worksheet.Cells[i + 2, 11].Value = student.PassedHours;
                    worksheet.Cells[i + 2, 12].Value = student.GPA;
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

        public async Task<byte[]> ExportStudentToPdfAsync(int studentId)
        {
            var student = await GetStudentByIdAsync(studentId);
            if (student == null)
                throw new Exception("Student not found");

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
                        .Text("Student Information")
                        .SemiBold().FontSize(24).FontColor(Colors.Blue.Darken3);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(column =>
                        {
                            column.Spacing(20);

                            // Student Information
                            column.Item().Text($"Student ID: {student.StudentId}");
                            column.Item().Text($"Name: {student.Name}");
                            column.Item().Text($"Seat Number: {student.SeatNumber}");
                            column.Item().Text($"National ID: {student.NationalId}");
                            column.Item().Text($"Phone: {student.Phone}");
                            column.Item().Text($"Email: {student.Email}");
                            column.Item().Text($"Department: {student.Department}");
                            column.Item().Text($"Study Level: {student.StudyLevel}");
                            column.Item().Text($"Semester: {student.Semester}");
                            column.Item().Text($"Grade: {student.Grade}");

                            // Academic Information - REMOVED AvailableHours
                            column.Item().Text($"GPA: {student.GPA:F2}");
                            column.Item().Text($"Percentage: {student.Percentage:F2}%");
                            column.Item().Text($"Passed Hours: {student.PassedHours}");
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

        

        public async Task<byte[]> ExportAllStudentsToPdfAsync()
        {
            var students = await GetAllStudentsAsync();

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
                        .Text("All Students Report")
                        .SemiBold().FontSize(20).FontColor(Colors.Blue.Darken3);

                    page.Content()
                        .Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(); // Student ID
                                columns.RelativeColumn(); // Name
                                columns.RelativeColumn(); // Phone
                                columns.RelativeColumn(); // Email
                                columns.RelativeColumn(); // Department
                                columns.RelativeColumn(); // GPA
                                columns.RelativeColumn(); // Percentage
                                columns.RelativeColumn(); // Passed Hours
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Student ID");
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Name");
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Phone");
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Email");
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Department");
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("GPA");
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Percentage");
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Passed Hours");
                            });

                            foreach (var student in students)
                            {
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(student.StudentId);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(student.Name);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(student.Phone ?? "N/A");
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(student.Email ?? "N/A");
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(student.Department ?? "N/A");
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(student.GPA.ToString("F2"));
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(student.Percentage.ToString("F2") + "%");
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(student.PassedHours.ToString());
                            }
                        });
                });
            });

            return document.GeneratePdf();
        }

        public async Task<string> AddTestStudents()
        {
            try
            {
                var testStudents = new List<Student>
                {
                    new Student
                    {
                        StudentId = "20240001",
                        SeatNumber = "CS-001",
                        Name = "أحمد محمد علي",
                        NationalId = "30112250101234",
                        Department = "علوم الحاسب",
                        StudyLevel = "المستوى الأول",
                        Semester = "الفصل الدراسي الأول",
                        Grade = "الأولى",
                        Phone = "05501234567",
                        Email = "ahmed.mohamed@student.edu.sa",
                        GPA = 3.8m,
                        PassedHours = 36
                    },
                    new Student
                    {
                        StudentId = "20240002",
                        SeatNumber = "CS-002",
                        Name = "فاطمة عبد الله حسن",
                        NationalId = "30805220112345",
                        Department = "علوم الحاسب",
                        StudyLevel = "المستوى الأول",
                        Semester = "الفصل الدراسي الأول",
                        Grade = "الأولى",
                        Phone = "05501234568",
                        Email = "fatima.abdullah@student.edu.sa",
                        GPA = 3.6m,
                        PassedHours = 36
                    },
                    new Student
                    {
                        StudentId = "20240003",
                        SeatNumber = "ME-001",
                        Name = "خالد إبراهيم سالم",
                        NationalId = "30108110109876",
                        Department = "الهندسة الميكانيكية",
                        StudyLevel = "المستوى الثاني",
                        Semester = "الفصل الدراسي الثاني",
                        Grade = "الثانية",
                        Phone = "05501234569",
                        Email = "khaled.ibrahim@student.edu.sa",
                        GPA = 3.5m,
                        PassedHours = 45
                    }
                };

                int addedCount = 0;
                foreach (var student in testStudents)
                {
                    if (!await StudentExistsAsync(student.StudentId))
                    {
                        _context.Students.Add(student);
                        addedCount++;
                    }
                }

                await _context.SaveChangesAsync();
                return $"Added {addedCount} test students successfully.";
            }
            catch (Exception ex)
            {
                return $"Error adding test students: {ex.Message}";
            }
        }

        public async Task<byte[]> ExportSelectedStudentsAsync(int[] studentIds)
        {
            try
            {
                // Get all selected students in one database query (more efficient)
                var selectedStudents = new List<Student>();

                // This assumes you have a method to get multiple students by IDs
                // If not, you might need to add: Task<List<Student>> GetStudentsByIdsAsync(int[] ids)
                foreach (var id in studentIds)
                {
                    var student = await GetStudentByIdAsync(id);
                    if (student != null)
                    {
                        selectedStudents.Add(student);
                    }
                }

                if (!selectedStudents.Any())
                {
                    throw new Exception("No valid students found for export.");
                }

                return await GenerateExcelFileAsync(selectedStudents);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to export selected students: {ex.Message}", ex);
            }
        }

        private async Task<byte[]> GenerateExcelFileAsync(List<Student> students)
        {
            return await Task.Run(() =>
            {
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Students");

                // Add headers with correct field names
                string[] headers = {
            "StudentId", "Name", "SeatNumber", "NationalId", "Department",
            "StudyLevel", "Semester", "Grade", "Phone", "Email",
            "GPA", "Percentage", "PassedHours"
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
                foreach (var student in students)
                {
                    worksheet.Cells[row, 1].Value = student.StudentId;
                    worksheet.Cells[row, 2].Value = student.Name;
                    worksheet.Cells[row, 3].Value = student.SeatNumber;
                    worksheet.Cells[row, 4].Value = student.NationalId;
                    worksheet.Cells[row, 5].Value = student.Department;
                    worksheet.Cells[row, 6].Value = student.StudyLevel;
                    worksheet.Cells[row, 7].Value = student.Semester;
                    worksheet.Cells[row, 8].Value = student.Grade;
                    worksheet.Cells[row, 9].Value = student.Phone;
                    worksheet.Cells[row, 10].Value = student.Email;
                    worksheet.Cells[row, 11].Value = Math.Round(student.GPA, 2);
                    worksheet.Cells[row, 12].Value = Math.Round(student.Percentage, 2);
                    worksheet.Cells[row, 13].Value = student.PassedHours;
                    row++;
                }

                // Auto-fit columns
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                return package.GetAsByteArray();
            });
        }
    }
}