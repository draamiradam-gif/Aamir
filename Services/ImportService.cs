using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;
using System.Text;

namespace StudentManagementSystem.Services
{
    public interface IImportService
    {
        Task<ImportServiceResult> ImportRegistrations(IFormFile file, int semesterId, bool overwriteExisting = false);
        Task<ImportServiceResult> ImportStudents(IFormFile file);
        Task<ImportServiceResult> ImportCourses(IFormFile file);
    }

    public class ImportService : IImportService
    {
        private readonly ApplicationDbContext _context;

        public ImportService(ApplicationDbContext context)
        {
            _context = context;
            ExcelPackage.License.SetNonCommercialOrganization("Student Management System");
        }

        public async Task<ImportServiceResult> ImportRegistrations(IFormFile file, int semesterId, bool overwriteExisting = false)
        {
            var result = new ImportServiceResult(); // Changed here
            var records = new List<CourseRegistration>();

            try
            {
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);

                if (file.FileName.EndsWith(".xlsx") || file.FileName.EndsWith(".xls"))
                {
                    records = await ReadRegistrationsFromExcel(stream, semesterId);
                }
                else if (file.FileName.EndsWith(".csv"))
                {
                    records = await ReadRegistrationsFromCsv(stream, semesterId);
                }
                else
                {
                    result.ErrorMessage = "Unsupported file format. Please use Excel (.xlsx) or CSV (.csv) files.";
                    return result;
                }

                // ... rest of the method remains the same, just using ImportServiceResult
                foreach (var record in records)
                {
                    var validationResult = await ValidateRegistration(record, semesterId);
                    if (validationResult.IsValid)
                    {
                        if (overwriteExisting)
                        {
                            var existing = await _context.CourseRegistrations
                                .FirstOrDefaultAsync(r => r.StudentId == record.StudentId &&
                                                         r.CourseId == record.CourseId &&
                                                         r.SemesterId == semesterId);
                            if (existing != null)
                            {
                                _context.CourseRegistrations.Remove(existing);
                            }
                        }

                        var exists = await _context.CourseRegistrations
                            .AnyAsync(r => r.StudentId == record.StudentId &&
                                          r.CourseId == record.CourseId &&
                                          r.SemesterId == semesterId);

                        if (!exists)
                        {
                            _context.CourseRegistrations.Add(record);
                            result.ImportedCount++;
                        }
                        else
                        {
                            result.SkippedCount++;
                            result.Errors.Add($"Registration already exists - Student: {record.StudentId}, Course: {record.CourseId}");
                        }
                    }
                    else
                    {
                        result.SkippedCount++;
                        result.Errors.Add($"{validationResult.ErrorMessage} - Student: {record.StudentId}, Course: {record.CourseId}");
                    }
                }

                if (result.ImportedCount > 0)
                {
                    await _context.SaveChangesAsync();
                }

                result.Success = true;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Import failed: {ex.Message}";
                result.Success = false;
            }

            return result;
        }


        private async Task<List<CourseRegistration>> ReadRegistrationsFromExcel(Stream stream, int semesterId)
        {
            return await Task.Run(() =>
            {
                var registrations = new List<CourseRegistration>();

                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets[0];
                var rowCount = worksheet.Dimension?.Rows ?? 0;

                for (int row = 2; row <= rowCount; row++) // Start from row 2 (skip header)
                {
                    var studentId = worksheet.Cells[row, 1].GetValue<string>()?.Trim();
                    var courseCode = worksheet.Cells[row, 2].GetValue<string>()?.Trim();
                    var semesterName = worksheet.Cells[row, 3].GetValue<string>()?.Trim();
                    var registrationTypeStr = worksheet.Cells[row, 4].GetValue<string>()?.Trim();
                    var remarks = worksheet.Cells[row, 5].GetValue<string>()?.Trim();

                    if (string.IsNullOrEmpty(studentId) || string.IsNullOrEmpty(courseCode))
                        continue;

                    // Find student and course (synchronous for Task.Run)
                    var student = _context.Students.FirstOrDefault(s => s.StudentId == studentId);
                    var course = _context.Courses.FirstOrDefault(c => c.CourseCode == courseCode);

                    if (student != null && course != null)
                    {
                        var registrationType = RegistrationType.Regular;
                        if (!string.IsNullOrEmpty(registrationTypeStr) && Enum.TryParse<RegistrationType>(registrationTypeStr, out var parsedType))
                        {
                            registrationType = parsedType;
                        }

                        var registration = new CourseRegistration
                        {
                            StudentId = student.Id,
                            CourseId = course.Id,
                            SemesterId = semesterId,
                            RegistrationDate = DateTime.Now,
                            Status = RegistrationStatus.Pending,
                            RegistrationType = registrationType,
                            Remarks = remarks
                        };

                        registrations.Add(registration);
                    }
                }

                return registrations;
            });
        }

        private async Task<List<CourseRegistration>> ReadRegistrationsFromCsv(Stream stream, int semesterId)
        {
            return await Task.Run(() =>
            {
                var registrations = new List<CourseRegistration>();

                stream.Position = 0;
                using var reader = new StreamReader(stream);

                // Skip header
                reader.ReadLine();

                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    var values = ParseCsvLine(line);
                    if (values.Length < 2) continue;

                    var studentId = values[0]?.Trim();
                    var courseCode = values[1]?.Trim();
                    var registrationTypeStr = values.Length > 3 ? values[3]?.Trim() : "Regular";
                    var remarks = values.Length > 4 ? values[4]?.Trim() : null;

                    if (string.IsNullOrEmpty(studentId) || string.IsNullOrEmpty(courseCode))
                        continue;

                    var student = _context.Students.FirstOrDefault(s => s.StudentId == studentId);
                    var course = _context.Courses.FirstOrDefault(c => c.CourseCode == courseCode);

                    if (student != null && course != null)
                    {
                        var registrationType = RegistrationType.Regular;
                        if (!string.IsNullOrEmpty(registrationTypeStr) && Enum.TryParse<RegistrationType>(registrationTypeStr, out var parsedType))
                        {
                            registrationType = parsedType;
                        }

                        var registration = new CourseRegistration
                        {
                            StudentId = student.Id,
                            CourseId = course.Id,
                            SemesterId = semesterId,
                            RegistrationDate = DateTime.Now,
                            Status = RegistrationStatus.Pending,
                            RegistrationType = registrationType,
                            Remarks = remarks
                        };

                        registrations.Add(registration);
                    }
                }

                return registrations;
            });
        }

        private string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            var inQuotes = false;
            var currentValue = new StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(currentValue.ToString());
                    currentValue.Clear();
                }
                else
                {
                    currentValue.Append(c);
                }
            }

            result.Add(currentValue.ToString());
            return result.ToArray();
        }

        private async Task<ValidationResult> ValidateRegistration(CourseRegistration registration, int semesterId)
        {
            // Check if student exists and is active
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Id == registration.StudentId && s.IsActive);
            if (student == null)
                return ValidationResult.Fail("Student not found or inactive");

            // Check if course exists and is active
            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.Id == registration.CourseId && c.IsActive);
            if (course == null)
                return ValidationResult.Fail("Course not found or inactive");

            // Check if semester exists and is active
            var semester = await _context.Semesters
                .FirstOrDefaultAsync(s => s.Id == semesterId && s.IsActive);
            if (semester == null)
                return ValidationResult.Fail("Semester not found or inactive");

            return ValidationResult.Success();
        }

        public async Task<ImportServiceResult> ImportStudents(IFormFile file) // Changed here
        {
            var result = new ImportServiceResult(); // Changed here
            // TODO: Implement student import logic
            await Task.CompletedTask;
            return result;
        }

        public async Task<ImportServiceResult> ImportCourses(IFormFile file) // Changed here
        {
            var result = new ImportServiceResult(); // Changed here
            // TODO: Implement course import logic
            await Task.CompletedTask;
            return result;
        }
    }

    public class ImportServiceResult
    {
        public bool Success { get; set; }
        public int ImportedCount { get; set; }
        public int SkippedCount { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new List<string>();
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;

        public static ValidationResult Success() => new ValidationResult { IsValid = true };
        public static ValidationResult Fail(string message) => new ValidationResult { IsValid = false, ErrorMessage = message };
    }
}