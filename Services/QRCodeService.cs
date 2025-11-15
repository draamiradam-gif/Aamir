using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Drawing;


namespace StudentManagementSystem.Services
{
    public class QRCodeService : IQRCodeService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<QRCodeService> _logger;

        public QRCodeService(ApplicationDbContext context, ILogger<QRCodeService> logger)
        {
            _context = context;
            _logger = logger;

            ExcelPackage.License.SetNonCommercialOrganization("Student Management System");
            QuestPDF.Settings.License = LicenseType.Community;
        }

        // ========== SESSION MANAGEMENT ==========

        public async Task<QRCodeSession> CreateSessionAsync(QRCodeSession session)
        {
            _context.QRCodeSessions.Add(session);
            await _context.SaveChangesAsync();
            return session;
        }

        public async Task<QRAttendance> ScanQRCodeAsync(string token, int studentId, string? deviceInfo = null, string? ipAddress = null)
        {
            var session = await _context.QRCodeSessions
                .Include(s => s.Attendances)
                .FirstOrDefaultAsync(s => s.Token == token && s.IsActive);

            if (session == null)
            {
                session = await _context.QRCodeSessions
                    .Include(s => s.Attendances)
                    .FirstOrDefaultAsync(s => s.Token == token && s.IsActive);
            }

            if (session == null)
                throw new Exception("Invalid or expired QR session");


            var expiresAt = session.CreatedAt.AddMinutes(session.DurationMinutes);
            if (expiresAt < DateTime.Now)
            {
                session.IsActive = false;
                await _context.SaveChangesAsync();
                throw new Exception("QR session has expired");
            }

            if (!session.AllowMultipleScans)
            {
                var existingScan = session.Attendances.FirstOrDefault(a => a.StudentId == studentId);
                if (existingScan != null)
                    throw new Exception("Student has already scanned this QR code");
            }

            if (session.EnableDynamicQR)
            {
                // The 'token' parameter here should be the dynamic token, not the session token
                if (!await ValidateDynamicTokenAsync(session.Id, token))
                    throw new Exception("Invalid or expired QR code. Please refresh and scan again.");
            }

            if (session.MaxScans.HasValue && session.Attendances.Count >= session.MaxScans.Value)
                throw new Exception("Maximum scan limit reached for this session");

            var attendance = new QRAttendance
            {
                QRCodeSessionId = session.Id,
                StudentId = studentId,
                DeviceInfo = deviceInfo,
                IPAddress = ipAddress,
                ScannedAt = DateTime.Now
            };

            _context.QRAttendances.Add(attendance);
            await _context.SaveChangesAsync();

            return attendance;
        }

        public async Task<List<QRCodeSession>> GetActiveSessionsAsync()
        {
            var now = DateTime.Now;
            return await _context.QRCodeSessions
                .Include(s => s.Course)
                .Include(s => s.Attendances)
                .Where(s => s.IsActive && s.CreatedAt.AddMinutes(s.DurationMinutes) > now)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<QRCodeSession?> GetSessionByIdAsync(int id)
        {
            return await _context.QRCodeSessions
                .Include(s => s.Course)
                .Include(s => s.Attendances)
                    .ThenInclude(a => a.Student)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<QRCodeSession?> GetSessionByTokenAsync(string token)
        {
            return await _context.QRCodeSessions
                .Include(s => s.Course)
                .Include(s => s.Attendances)
                .FirstOrDefaultAsync(s => s.Token == token);
        }

        public async Task<bool> ValidateSessionAsync(string token)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                    return false;

                var session = await _context.QRCodeSessions
                    .FirstOrDefaultAsync(s => s.Token == token && s.IsActive);

                if (session == null)
                    return false;

                var expiresAt = session.CreatedAt.AddMinutes(session.DurationMinutes);
                return expiresAt > DateTime.Now;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ValidateSessionAsync for token: {Token}", token);
                return false;
            }
        }

        public async Task<List<QRAttendance>> GetSessionAttendancesAsync(int sessionId)
        {
            return await _context.QRAttendances
                .Include(a => a.Student)
                .Where(a => a.QRCodeSessionId == sessionId)
                .OrderByDescending(a => a.ScannedAt)
                .ToListAsync();
        }

        // ========== EXPORT/IMPORT METHODS ==========

        public async Task<byte[]> ExportAttendanceToExcelAsync(int sessionId)
        {
            var session = await GetSessionByIdAsync(sessionId);
            if (session == null)
                throw new Exception("Session not found");

            var attendances = await GetSessionAttendancesAsync(sessionId);

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Attendance Report");

            // Title and Session Info
            worksheet.Cells[1, 1].Value = "QR ATTENDANCE REPORT";
            worksheet.Cells[1, 1, 1, 7].Merge = true;
            worksheet.Cells[1, 1].Style.Font.Bold = true;
            worksheet.Cells[1, 1].Style.Font.Size = 16;
            worksheet.Cells[1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            // Session Details
            worksheet.Cells[2, 1].Value = "Session Title:";
            worksheet.Cells[2, 2].Value = session.SessionTitle;
            worksheet.Cells[3, 1].Value = "Course:";
            worksheet.Cells[3, 2].Value = $"{session.Course?.CourseCode} - {session.Course?.CourseName}";
            worksheet.Cells[4, 1].Value = "Created:";
            worksheet.Cells[4, 2].Value = session.CreatedAt.ToString("MMM dd, yyyy HH:mm");
            worksheet.Cells[5, 1].Value = "Duration:";
            worksheet.Cells[5, 2].Value = $"{session.DurationMinutes} minutes";
            worksheet.Cells[6, 1].Value = "Total Scans:";
            worksheet.Cells[6, 2].Value = attendances.Count;

            // Attendance Data Headers
            int dataStartRow = 8;
            worksheet.Cells[dataStartRow, 1].Value = "Student ID";
            worksheet.Cells[dataStartRow, 2].Value = "Student Name";
            worksheet.Cells[dataStartRow, 3].Value = "Department";
            worksheet.Cells[dataStartRow, 4].Value = "Scan Date";
            worksheet.Cells[dataStartRow, 5].Value = "Scan Time";
            worksheet.Cells[dataStartRow, 6].Value = "Device Info";
            worksheet.Cells[dataStartRow, 7].Value = "IP Address";

            // Style headers
            using (var range = worksheet.Cells[dataStartRow, 1, dataStartRow, 7])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            // Data rows
            int row = dataStartRow + 1;
            foreach (var attendance in attendances.OrderBy(a => a.ScannedAt))
            {
                worksheet.Cells[row, 1].Value = attendance.Student?.StudentId;
                worksheet.Cells[row, 2].Value = attendance.Student?.Name;
                worksheet.Cells[row, 3].Value = attendance.Student?.Department;
                worksheet.Cells[row, 4].Value = attendance.ScannedAt.ToString("yyyy-MM-dd");
                worksheet.Cells[row, 5].Value = attendance.ScannedAt.ToString("HH:mm:ss");
                worksheet.Cells[row, 6].Value = attendance.DeviceInfo;
                worksheet.Cells[row, 7].Value = attendance.IPAddress;
                row++;
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            return package.GetAsByteArray();
        }

        public async Task<byte[]> ExportAttendanceToPdfAsync(int sessionId)
        {
            var session = await GetSessionByIdAsync(sessionId);
            if (session == null)
                throw new Exception("Session not found");

            var attendances = await GetSessionAttendancesAsync(sessionId);

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
                        .Text("QR ATTENDANCE REPORT")
                        .SemiBold().FontSize(16).FontColor(Colors.Blue.Medium);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(column =>
                        {
                            column.Spacing(10);

                            // Session Information
                            column.Item().Background(Colors.Grey.Lighten3).Padding(10).Column(infoColumn =>
                            {
                                infoColumn.Spacing(5);
                                infoColumn.Item().Text($"Session: {session.SessionTitle}");
                                infoColumn.Item().Text($"Course: {session.Course?.CourseCode} - {session.Course?.CourseName}");
                                infoColumn.Item().Text($"Date: {session.CreatedAt:MMM dd, yyyy HH:mm}");
                                infoColumn.Item().Text($"Duration: {session.DurationMinutes} minutes");
                                infoColumn.Item().Text($"Total Scans: {attendances.Count}");
                            });

                            if (attendances.Any())
                            {
                                column.Item().Text("ATTENDANCE RECORDS").SemiBold().FontSize(14);
                                column.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(3);
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(2);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Background(Colors.Blue.Medium).Padding(5).Text("Student ID").FontColor(Colors.White);
                                        header.Cell().Background(Colors.Blue.Medium).Padding(5).Text("Student Name").FontColor(Colors.White);
                                        header.Cell().Background(Colors.Blue.Medium).Padding(5).Text("Department").FontColor(Colors.White);
                                        header.Cell().Background(Colors.Blue.Medium).Padding(5).Text("Date").FontColor(Colors.White);
                                        header.Cell().Background(Colors.Blue.Medium).Padding(5).Text("Time").FontColor(Colors.White);
                                    });

                                    foreach (var attendance in attendances.OrderBy(a => a.ScannedAt))
                                    {
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(attendance.Student?.StudentId ?? "N/A");
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(attendance.Student?.Name ?? "N/A");
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(attendance.Student?.Department ?? "N/A");
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(attendance.ScannedAt.ToString("MMM dd, yyyy"));
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(attendance.ScannedAt.ToString("HH:mm:ss"));
                                    }
                                });
                            }
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(text =>
                        {
                            text.Span("Page ");
                            text.CurrentPageNumber();
                            text.Span(" of ");
                            text.TotalPages();
                        });
                });
            });

            return document.GeneratePdf();
        }

        public async Task<byte[]> ExportAllSessionsToExcelAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var sessions = await _context.QRCodeSessions
                .Include(s => s.Course)
                .Include(s => s.Attendances)
                .Where(s => startDate == null || s.CreatedAt >= startDate)
                .Where(s => endDate == null || s.CreatedAt <= endDate)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("All Sessions Report");

            worksheet.Cells[1, 1].Value = "ALL QR SESSIONS REPORT";
            worksheet.Cells[1, 1, 1, 8].Merge = true;
            worksheet.Cells[1, 1].Style.Font.Bold = true;
            worksheet.Cells[1, 1].Style.Font.Size = 16;
            worksheet.Cells[1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            int dataStartRow = 3;
            worksheet.Cells[dataStartRow, 1].Value = "Session Title";
            worksheet.Cells[dataStartRow, 2].Value = "Course";
            worksheet.Cells[dataStartRow, 3].Value = "Created By";
            worksheet.Cells[dataStartRow, 4].Value = "Created Date";
            worksheet.Cells[dataStartRow, 5].Value = "Duration (min)";
            worksheet.Cells[dataStartRow, 6].Value = "Total Scans";
            worksheet.Cells[dataStartRow, 7].Value = "Unique Students";
            worksheet.Cells[dataStartRow, 8].Value = "Status";

            using (var range = worksheet.Cells[dataStartRow, 1, dataStartRow, 8])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGreen);
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            int row = dataStartRow + 1;
            foreach (var session in sessions)
            {
                var uniqueStudents = session.Attendances.GroupBy(a => a.StudentId).Count();
                var isExpired = session.CreatedAt.AddMinutes(session.DurationMinutes) < DateTime.Now;

                worksheet.Cells[row, 1].Value = session.SessionTitle;
                worksheet.Cells[row, 2].Value = session.Course?.CourseCode;
                worksheet.Cells[row, 3].Value = session.CreatedBy;
                worksheet.Cells[row, 4].Value = session.CreatedAt.ToString("MMM dd, yyyy HH:mm");
                worksheet.Cells[row, 5].Value = session.DurationMinutes;
                worksheet.Cells[row, 6].Value = session.Attendances.Count;
                worksheet.Cells[row, 7].Value = uniqueStudents;
                worksheet.Cells[row, 8].Value = isExpired ? "Expired" : (session.IsActive ? "Active" : "Ended");
                row++;
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            return package.GetAsByteArray();
        }

        public Task<ImportResult> ImportAttendanceFromExcelAsync(Stream fileStream)
        {
            var result = new ImportResult { Success = false };

            try
            {
                using var package = new ExcelPackage(fileStream);
                var worksheet = package.Workbook.Worksheets[0];

                if (worksheet == null)
                {
                    result.Message = "No worksheet found in the Excel file.";
                    return Task.FromResult(result);
                }

                result.Success = true;
                result.Message = "Attendance import completed successfully.";
                result.ImportedCount = 0;

                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing attendance from Excel");
                result.Message = $"Import failed: {ex.Message}";
                return Task.FromResult(result);
            }
        }

        public byte[] GenerateAttendanceImportTemplate()
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Attendance Import Template");

            int dataStartRow = 1;
            worksheet.Cells[dataStartRow, 1].Value = "Session ID";
            worksheet.Cells[dataStartRow, 2].Value = "Student ID";
            worksheet.Cells[dataStartRow, 3].Value = "Scan Time";
            worksheet.Cells[dataStartRow, 4].Value = "Device Info";
            worksheet.Cells[dataStartRow, 5].Value = "IP Address";

            using (var range = worksheet.Cells[dataStartRow, 1, dataStartRow, 5])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            return package.GetAsByteArray();
        }
        public async Task<string> GetCurrentSessionTokenAsync(int sessionId)
        {
            var session = await _context.QRCodeSessions.FindAsync(sessionId);
            if (session == null)
                throw new Exception("Session not found");

            // Check if token needs refresh
            if (session.EnableDynamicQR &&
                DateTime.Now.Subtract(session.LastTokenUpdate).TotalSeconds >= session.TokenUpdateIntervalSeconds)
            {
                // Update token
                session.CurrentToken = Guid.NewGuid().ToString();
                session.LastTokenUpdate = DateTime.Now;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Refreshed token for session {SessionId}", sessionId);
            }

            return session.CurrentToken;
        }

        public async Task<bool> ValidateDynamicTokenAsync(int sessionId, string token)
        {
            var session = await _context.QRCodeSessions.FindAsync(sessionId);
            if (session == null || !session.IsActive)
                return false;

            // For dynamic QR, check current token
            if (session.EnableDynamicQR)
            {
                return session.CurrentToken == token &&
                       DateTime.Now.Subtract(session.LastTokenUpdate).TotalSeconds <= session.TokenUpdateIntervalSeconds * 2; // Allow some buffer
            }

            // For static QR, use original token
            return session.Token == token;
        }

        public async Task<bool> ReopenSessionAsync(int sessionId, int additionalMinutes = 15)
        {
            var session = await _context.QRCodeSessions.FindAsync(sessionId);
            if (session == null)
                return false;

            // Calculate new expiration
            var newExpiration = DateTime.Now.AddMinutes(additionalMinutes);
            var originalExpiration = session.CreatedAt.AddMinutes(session.DurationMinutes);

            // Only extend if the new expiration is later than original
            if (newExpiration > originalExpiration)
            {
                // Calculate new duration
                var newDuration = (int)(newExpiration - session.CreatedAt).TotalMinutes;
                session.DurationMinutes = newDuration;
                session.IsActive = true;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Reopened session {SessionId} for {AdditionalMinutes} minutes",
                    sessionId, additionalMinutes);
                return true;
            }

            return false;
        }
        public async Task<bool> DeleteSessionWithAttendanceAsync(int sessionId)
        {
            var session = await _context.QRCodeSessions
                .Include(s => s.Attendances)
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null)
                return false;

            try
            {
                // Remove all attendance records first (due to foreign key constraints)
                _context.QRAttendances.RemoveRange(session.Attendances);

                // Remove the session
                _context.QRCodeSessions.Remove(session);

                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted session {SessionId} with {AttendanceCount} attendance records",
                    sessionId, session.Attendances.Count);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting session {SessionId}", sessionId);
                throw;
            }
        }

        public async Task<bool> ReopenSessionAsync(int sessionId)
        {
            try
            {
                var session = await _context.QRCodeSessions.FindAsync(sessionId);
                if (session == null)
                    return false;

                // Check if session can be reopened (not too far in the past)
                var canReopen = session.CreatedAt.AddMinutes(session.DurationMinutes + 60) > DateTime.Now;

                if (canReopen)
                {
                    session.IsActive = true;
                    await _context.SaveChangesAsync();
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reopening session {SessionId}", sessionId);
                return false;
            }
        }
        public bool EnableDynamicQR { get; set; } = false;
        public int TokenUpdateIntervalSeconds { get; set; } = 10;
        public DateTime? LastTokenUpdate { get; set; }
        public string? CurrentToken { get; set; }

        public async Task<string> GetCurrentTokenAsync(int sessionId)
        {
            var session = await _context.QRCodeSessions.FindAsync(sessionId);
            if (session == null)
                throw new Exception("Session not found");

            if (!session.EnableDynamicQR)
                return session.Token;

            // Check if it's the default value (never updated)
            bool neverUpdated = session.LastTokenUpdate == default(DateTime);
            bool needsUpdate = neverUpdated ||
                              DateTime.Now.Subtract(session.LastTokenUpdate).TotalSeconds >= session.TokenUpdateIntervalSeconds;

            if (needsUpdate)
            {
                session.CurrentToken = Guid.NewGuid().ToString();
                session.LastTokenUpdate = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            return session.CurrentToken ?? session.Token;
        }

        public async Task UpdateSessionAsync(QRCodeSession session)
        {
            var existingSession = await _context.QRCodeSessions.FindAsync(session.Id);
            if (existingSession != null)
            {
                // Update only editable properties
                existingSession.SessionTitle = session.SessionTitle;
                existingSession.Description = session.Description;
                existingSession.CourseId = session.CourseId;
                existingSession.DurationMinutes = session.DurationMinutes;
                existingSession.MaxScans = session.MaxScans;
                existingSession.AllowMultipleScans = session.AllowMultipleScans;
                existingSession.IsActive = session.IsActive;

                _context.QRCodeSessions.Update(existingSession);
                await _context.SaveChangesAsync();
            }
        }
    }
}