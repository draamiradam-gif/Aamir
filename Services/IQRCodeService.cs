using StudentManagementSystem.Models;

namespace StudentManagementSystem.Services
{
    public interface IQRCodeService
    {
        // Session Management
        Task<QRCodeSession> CreateSessionAsync(QRCodeSession session);
        Task<QRAttendance> ScanQRCodeAsync(string token, int studentId, string? deviceInfo = null, string? ipAddress = null);
        Task<List<QRCodeSession>> GetActiveSessionsAsync();
        Task<QRCodeSession?> GetSessionByIdAsync(int id);
        Task<QRCodeSession?> GetSessionByTokenAsync(string token);
        Task<bool> ValidateSessionAsync(string token);
        Task<List<QRAttendance>> GetSessionAttendancesAsync(int sessionId);
        Task<bool> ReopenSessionAsync(int sessionId, int additionalMinutes = 15);
        Task<bool> DeleteSessionWithAttendanceAsync(int sessionId);

        // Export/Import
        Task<byte[]> ExportAttendanceToExcelAsync(int sessionId);
        Task<byte[]> ExportAttendanceToPdfAsync(int sessionId);
        Task<byte[]> ExportAllSessionsToExcelAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<ImportResult> ImportAttendanceFromExcelAsync(Stream fileStream);
        byte[] GenerateAttendanceImportTemplate();
        Task<string> GetCurrentTokenAsync(int sessionId);
        Task UpdateSessionAsync(QRCodeSession session);

        // ✅ REMOVED: Conflicting duplicate methods
        // string GenerateSessionToken();
        // Task<QRCodeSession> CreateSessionAsync(int courseId, int durationMinutes = 60);
        // Task<bool> ValidateQRScanAsync(string sessionToken, int studentId, string? deviceInfo = null, string? ipAddress = null);
        // Task<bool> EndSessionAsync(int sessionId);
        // Task<bool> IsSessionActiveAsync(int sessionId);
        // Task<QRCodeSession?> GetActiveSessionByCourseAsync(int courseId);
    }
}