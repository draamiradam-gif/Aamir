using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementSystem.Models
{
    [Table("QRCodeSessions")]
    public class QRCodeSession : BaseEntity
    {
        //public int Id { get; set; }

        [Required]
        public string SessionTitle { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public int CourseId { get; set; }

        [ForeignKey("CourseId")]
        public Course? Course { get; set; }

        [Required]
        public string Token { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [Range(1, 120)]
        public int DurationMinutes { get; set; } = 15;

        public int? MaxScans { get; set; }

        public bool AllowMultipleScans { get; set; }

        //public bool IsActive { get; set; } = true;

        //public string CreatedBy { get; set; } = string.Empty;

        //public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime ExpiresAt => CreatedAt.AddMinutes(DurationMinutes);

        // Dynamic QR Properties
        public string CurrentToken { get; set; } = Guid.NewGuid().ToString();
        public DateTime LastTokenUpdate { get; set; } = DateTime.Now;
        public int TokenUpdateIntervalSeconds { get; set; } = 10;
        public bool EnableDynamicQR { get; set; } = true;

        // Navigation property
        public ICollection<QRAttendance> Attendances { get; set; } = new List<QRAttendance>();

        // ✅ REMOVED: SessionToken, StartTime, EndTime (duplicates causing conflicts)
    }
}