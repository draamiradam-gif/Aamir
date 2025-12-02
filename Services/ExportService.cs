using OfficeOpenXml;
using OfficeOpenXml.Style;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using StudentManagementSystem.Models;
using System.Text;

namespace StudentManagementSystem.Services
{
    public interface IExportService
    {
        Task<byte[]> ExportRegistrationsToExcel(List<CourseRegistration> registrations);
        Task<byte[]> ExportRegistrationsToCsv(List<CourseRegistration> registrations);
        Task<byte[]> ExportRegistrationsToPdf(List<CourseRegistration> registrations);
        Task<byte[]> GenerateImportTemplate(string type);
        Task<byte[]> ExportAnalyticsReport(RegistrationAnalytics analytics);
    }

    public class ExportService : IExportService
    {
        public ExportService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
            ExcelPackage.License.SetNonCommercialOrganization("Student Management System");
        }

        public async Task<byte[]> ExportRegistrationsToExcel(List<CourseRegistration> registrations)
        {
            return await Task.Run(() =>
            {
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Registrations");

                // Headers
                var headers = new[] { "Student ID", "Student Name", "Course Code", "Course Name", "Semester", "Registration Date", "Status", "Type", "Approved By", "Approval Date" };
                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[1, i + 1].Value = headers[i];
                    worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                    worksheet.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                }

                // Data
                int row = 2;
                foreach (var reg in registrations)
                {
                    worksheet.Cells[row, 1].Value = reg.Student?.StudentId;
                    worksheet.Cells[row, 2].Value = reg.Student?.Name;
                    worksheet.Cells[row, 3].Value = reg.Course?.CourseCode;
                    worksheet.Cells[row, 4].Value = reg.Course?.CourseName;
                    worksheet.Cells[row, 5].Value = reg.Semester?.Name;
                    worksheet.Cells[row, 6].Value = reg.RegistrationDate;
                    worksheet.Cells[row, 6].Style.Numberformat.Format = "yyyy-mm-dd hh:mm";
                    worksheet.Cells[row, 7].Value = reg.Status.ToString();
                    worksheet.Cells[row, 8].Value = reg.RegistrationType.ToString();
                    worksheet.Cells[row, 9].Value = reg.ApprovedBy;
                    worksheet.Cells[row, 10].Value = reg.ApprovalDate?.ToString("yyyy-MM-dd");

                    row++;
                }

                // Auto-fit columns
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                return package.GetAsByteArray();
            });
        }

        public async Task<byte[]> ExportRegistrationsToCsv(List<CourseRegistration> registrations)
        {
            return await Task.Run(() =>
            {
                var csv = new StringBuilder();

                // Headers
                csv.AppendLine("Student ID,Student Name,Course Code,Course Name,Semester,Registration Date,Status,Type,Approved By,Approval Date");

                // Data
                foreach (var reg in registrations)
                {
                    csv.AppendLine($"\"{reg.Student?.StudentId}\",\"{reg.Student?.Name}\",\"{reg.Course?.CourseCode}\",\"{reg.Course?.CourseName}\",\"{reg.Semester?.Name}\",\"{reg.RegistrationDate:yyyy-MM-dd HH:mm}\",\"{reg.Status}\",\"{reg.RegistrationType}\",\"{reg.ApprovedBy}\",\"{reg.ApprovalDate?.ToString("yyyy-MM-dd")}\"");
                }

                return Encoding.UTF8.GetBytes(csv.ToString());
            });
        }

        public async Task<byte[]> ExportRegistrationsToPdf(List<CourseRegistration> registrations)
        {
            return await Task.Run(() =>
            {
                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(10));

                        page.Header()
                            .AlignCenter()
                            .Text("Course Registrations Report")
                            .SemiBold().FontSize(16).FontColor(Colors.Blue.Medium);

                        page.Content()
                            .PaddingVertical(1, Unit.Centimetre)
                            .Column(x =>
                            {
                                x.Spacing(10);

                                // Summary
                                x.Item().Text($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm}");
                                x.Item().Text($"Total Registrations: {registrations.Count}");

                                // Table
                                x.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(2); // Student
                                        columns.RelativeColumn(2); // Course
                                        columns.RelativeColumn(1.5f); // Semester
                                        columns.RelativeColumn(1.5f); // Date
                                        columns.RelativeColumn(1); // Status
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Student");
                                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Course");
                                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Semester");
                                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Date");
                                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Status");
                                    });

                                    foreach (var reg in registrations.Take(50)) // Limit for PDF
                                    {
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{reg.Student?.Name}\n{reg.Student?.StudentId}");
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{reg.Course?.CourseCode}\n{reg.Course?.CourseName}");
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(reg.Semester?.Name ?? "N/A");
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(reg.RegistrationDate.ToString("yyyy-MM-dd"));
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(reg.Status.ToString());
                                    }
                                });
                            });

                        page.Footer()
                            .AlignCenter()
                            .Text(x =>
                            {
                                x.Span("Page ");
                                x.CurrentPageNumber();
                                x.Span(" of ");
                                x.TotalPages();
                            });
                    });
                });

                return document.GeneratePdf();
            });
        }

        public async Task<byte[]> GenerateImportTemplate(string type)
        {
            return await Task.Run(() =>
            {
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Template");

                if (type == "registrations")
                {
                    // Headers
                    var headers = new[] { "StudentID", "CourseCode", "Semester", "RegistrationType", "Remarks" };
                    for (int i = 0; i < headers.Length; i++)
                    {
                        worksheet.Cells[1, i + 1].Value = headers[i];
                        worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                        worksheet.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGreen);
                    }

                    // Sample data
                    worksheet.Cells[2, 1].Value = "S12345";
                    worksheet.Cells[2, 2].Value = "CS101";
                    worksheet.Cells[2, 3].Value = "Fall 2024";
                    worksheet.Cells[2, 4].Value = "Regular";
                    worksheet.Cells[2, 5].Value = "Sample registration";

                    // Data validation for RegistrationType
                    var registrationTypeRange = worksheet.Cells["D2:D100"];
                    var validation = registrationTypeRange.DataValidation.AddListDataValidation();
                    foreach (var typeName in Enum.GetNames(typeof(RegistrationType)))
                    {
                        validation.Formula.Values.Add(typeName);
                    }

                    // Instructions
                    worksheet.Cells[4, 1].Value = "Instructions:";
                    worksheet.Cells[4, 1].Style.Font.Bold = true;
                    worksheet.Cells[5, 1].Value = "1. StudentID must exist in the system";
                    worksheet.Cells[6, 1].Value = "2. CourseCode must exist in the system";
                    worksheet.Cells[7, 1].Value = "3. Semester must match existing semesters";
                    worksheet.Cells[8, 1].Value = "4. RegistrationType must be one of: " + string.Join(", ", Enum.GetNames(typeof(RegistrationType)));

                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
                }

                return package.GetAsByteArray();
            });
        }

        public async Task<byte[]> ExportAnalyticsReport(RegistrationAnalytics analytics)
        {
            return await Task.Run(() =>
            {
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Analytics");

                // Title
                worksheet.Cells[1, 1].Value = "Registration Analytics Report";
                worksheet.Cells[1, 1].Style.Font.Bold = true;
                worksheet.Cells[1, 1].Style.Font.Size = 16;
                worksheet.Cells[1, 1, 1, 3].Merge = true;

                // Summary
                worksheet.Cells[3, 1].Value = "Total Registrations:";
                worksheet.Cells[3, 2].Value = analytics.TotalRegistrations;

                worksheet.Cells[4, 1].Value = "Successful Registrations:";
                worksheet.Cells[4, 2].Value = analytics.SuccessfulRegistrations;

                worksheet.Cells[5, 1].Value = "Success Rate:";
                worksheet.Cells[5, 2].Value = analytics.SuccessRate / 100;
                worksheet.Cells[5, 2].Style.Numberformat.Format = "0.00%";

                // Department Distribution
                int row = 7;
                worksheet.Cells[row, 1].Value = "Department Distribution";
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                row++;

                foreach (var dept in analytics.RegistrationByDepartment)
                {
                    worksheet.Cells[row, 1].Value = dept.Key;
                    worksheet.Cells[row, 2].Value = dept.Value;
                    row++;
                }

                // Top Courses
                row += 2;
                worksheet.Cells[row, 1].Value = "Top Courses";
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                row++;

                foreach (var course in analytics.TopCourses)
                {
                    worksheet.Cells[row, 1].Value = course.Key;
                    worksheet.Cells[row, 2].Value = course.Value;
                    row++;
                }

                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                return package.GetAsByteArray();
            });
        }
    }
}