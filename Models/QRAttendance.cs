using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementSystem.Models
{
    public class QRAttendance : BaseEntity
    {
        //public int Id { get; set; }

        [Required]
        public int QRCodeSessionId { get; set; }

        [ForeignKey("QRCodeSessionId")]
        public QRCodeSession? QRCodeSession { get; set; }

        [Required]
        public int StudentId { get; set; }

        [ForeignKey("StudentId")]
        public Student? Student { get; set; }

        public DateTime ScannedAt { get; set; } = DateTime.Now;

        public string? DeviceInfo { get; set; }

        public string? IPAddress { get; set; }

        public bool IsValid { get; set; } = true;

        // ✅ REMOVED: SessionId, ScanTime, Session (duplicates causing conflicts)
    }
}