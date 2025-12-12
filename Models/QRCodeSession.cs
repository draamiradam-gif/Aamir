using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementSystem.Models
{
    [Table("QRCodeSessions")]
    public class QRCodeSession : BaseEntity
    {

        [Required(ErrorMessage = "Session title is required")]
        [StringLength(100, ErrorMessage = "Title cannot exceed 100 characters")]
        [Display(Name = "Session Title")]
        public string SessionTitle { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Please select a course")]
        [Display(Name = "Course")]
        public int CourseId { get; set; }

        [ForeignKey("CourseId")]
        public virtual Course? Course { get; set; }

        [Required]
        public string Token { get; set; } = Guid.NewGuid().ToString();

        [Required(ErrorMessage = "Duration is required")]
        [Range(1, 120, ErrorMessage = "Duration must be between 1 and 120 minutes")]
        [Display(Name = "Duration (minutes)")]
        public int DurationMinutes { get; set; } = 15;

        [Range(1, 1000, ErrorMessage = "Maximum scans must be between 1 and 1000")]
        [Display(Name = "Maximum Scans")]
        public int? MaxScans { get; set; }

        [Display(Name = "Allow Multiple Scans")]
        public bool AllowMultipleScans { get; set; }

        // Dynamic QR Properties
        [Display(Name = "Current Token")]
        public string CurrentToken { get; set; } = Guid.NewGuid().ToString();

        [Display(Name = "Last Token Update")]
        public DateTime LastTokenUpdate { get; set; } = DateTime.Now;

        [Range(5, 60, ErrorMessage = "Token update interval must be between 5 and 60 seconds")]
        [Display(Name = "Token Update Interval (seconds)")]
        public int TokenUpdateIntervalSeconds { get; set; } = 10;

        [Display(Name = "Enable Dynamic QR")]
        public bool EnableDynamicQR { get; set; } = false;

        // ✅ CRITICAL: ExpiresAt property
        //[Required]
        //[Display(Name = "Expires At")]
        //public DateTime ExpiresAt { get; set; }

        [NotMapped]
        [Display(Name = "Expires At")]
        public DateTime ExpiresAt => CreatedAt.AddMinutes(DurationMinutes);

        // Navigation property
        public virtual ICollection<QRAttendance> Attendances { get; set; } = new List<QRAttendance>();

        public QRCodeSession()
        {
            // Initialize with default values
            Token = Guid.NewGuid().ToString();
            CurrentToken = Token;
            LastTokenUpdate = DateTime.Now;
        }

        // Helper method to calculate expiration
        //public void CalculateExpiration()
        //{
        //    if (CreatedAt != default && DurationMinutes > 0)
        //    {
        //        ExpiresAt = CreatedAt.AddMinutes(DurationMinutes);
        //    }
        //}
        public void CalculateExpiration()
        {
            // This method is now redundant since ExpiresAt is calculated property
            // But you can keep it for backward compatibility
            var expires = CreatedAt.AddMinutes(DurationMinutes);
            Console.WriteLine($"Expires at: {expires}");




        }
    }
}