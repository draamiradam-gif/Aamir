using StudentManagementSystem.Models;

namespace StudentManagementSystem.ViewModels
{
    public class QRDashboardViewModel
    {
        public QRCodeSession Session { get; set; } = null!;
        public List<QRAttendance> Attendances { get; set; } = new();
        public int TotalStudents { get; set; }
        public int UniqueScans { get; set; }
        public double AttendancePercentage => TotalStudents > 0 ? (UniqueScans * 100.0) / TotalStudents : 0;

        public bool EnableDynamicQR => Session.EnableDynamicQR;
        public int TokenUpdateIntervalSeconds => Session.TokenUpdateIntervalSeconds;
    }
}