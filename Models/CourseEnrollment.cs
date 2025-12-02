using StudentManagementSystem.Services;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace StudentManagementSystem.Models
{
    public class CourseEnrollment : BaseEntity
    {
        // Core enrollment properties (EXISTING - KEEP THESE)
        public int CourseId { get; set; }
        public int StudentId { get; set; }
        public int SemesterId { get; set; }

        [Range(0, 100)]
        [Display(Name = "Grade")]
        public decimal? Grade { get; set; }

        [StringLength(2)]
        [Display(Name = "Grade Letter")]
        public string? GradeLetter { get; set; }

        [Display(Name = "Enrollment Date")]
        public DateTime EnrollmentDate { get; set; } = DateTime.Now;

        //[Display(Name = "Active")]
        //public bool IsActive { get; set; } = true;

        // Grade-related properties (EXISTING - KEEP THESE)
        [Display(Name = "Grade Points")]
        [Column(TypeName = "decimal(4,2)")]
        public decimal? GradePoints { get; set; }

        [Display(Name = "Grade Status")]
        public GradeStatus GradeStatus { get; set; } = GradeStatus.InProgress;

        [Display(Name = "Completion Date")]
        public DateTime? CompletionDate { get; set; }

        [Display(Name = "Remarks")]
        [StringLength(500)]
        public string? Remarks { get; set; }

        // NEW PROFESSIONAL PROPERTIES (ADD THESE - NO DUPLICATES)
        [Display(Name = "Enrollment Type")]
        public EnrollmentType EnrollmentType { get; set; } = EnrollmentType.Regular;

        [Display(Name = "Enrollment Status")]
        public EnrollmentStatus EnrollmentStatus { get; set; } = EnrollmentStatus.Active;

        [Display(Name = "Waitlist Position")]
        public int? WaitlistPosition { get; set; }

        [Display(Name = "Enrollment Method")]
        public EnrollmentMethod EnrollmentMethod { get; set; } = EnrollmentMethod.Web;

        [Display(Name = "Approved By")]
        [StringLength(100)]
        public string? ApprovedBy { get; set; }

        [Display(Name = "Approval Date")]
        public DateTime? ApprovalDate { get; set; }

        [Display(Name = "Drop/Withdraw Reason")]
        [StringLength(500)]
        public string? DropReason { get; set; }

        [Display(Name = "Drop/Withdraw Date")]
        public DateTime? DropDate { get; set; }

        [Display(Name = "Last Activity Date")]
        public DateTime LastActivityDate { get; set; } = DateTime.Now;

        [Display(Name = "Audit Trail")]
        public string? AuditTrail { get; set; }

        // Navigation properties
        [ForeignKey("CourseId")]
        public virtual Course? Course { get; set; }

        [ForeignKey("StudentId")]
        public virtual Student? Student { get; set; }

        [ForeignKey("SemesterId")]
        public virtual Semester? Semester { get; set; }

        // Computed properties
        [NotMapped]
        public bool IsCompleted => GradeStatus == GradeStatus.Completed;

        [NotMapped]
        public bool IsFailed => GradeStatus == GradeStatus.Failed;

        [NotMapped]
        public bool IsPassing => Grade >= 50; // Updated: Passing grade is 50%

        [NotMapped]
        public bool IsWaitlisted => WaitlistPosition.HasValue && EnrollmentStatus == EnrollmentStatus.Waitlisted;

        [NotMapped]
        public bool CanBeDropped => EnrollmentStatus == EnrollmentStatus.Active &&
                                   Semester?.IsRegistrationPeriod == true;

        // Methods
        public void CalculateGrade()
        {
            if (Grade.HasValue)
            {
                // Calculate grade points based on percentage
                GradePoints = CalculateGradePoints(Grade.Value);

                // Determine grade letter
                GradeLetter = CalculateGradeLetter(Grade.Value);

                // Update grade status
                UpdateGradeStatus();
            }
        }

        // UPDATED: New grading system
        private decimal CalculateGradePoints(decimal grade)
        {
            return grade switch
            {
                >= 96 => 4.0m,    // A+
                >= 92 => 3.7m,    // A
                >= 88 => 3.4m,    // A-
                >= 84 => 3.2m,    // B+
                >= 80 => 3.0m,    // B
                >= 76 => 2.8m,    // B-
                >= 72 => 2.6m,    // C+
                >= 68 => 2.4m,    // C
                >= 64 => 2.2m,    // C-
                >= 60 => 2.0m,    // D+
                >= 55 => 1.5m,    // D
                >= 50 => 1.0m,    // D-
                _ => 0.0m         // F
            };
        }

        private string CalculateGradeLetter(decimal grade)
        {
            return grade switch
            {
                >= 96 => "A+",
                >= 92 => "A",
                >= 88 => "A-",
                >= 84 => "B+",
                >= 80 => "B",
                >= 76 => "B-",
                >= 72 => "C+",
                >= 68 => "C",
                >= 64 => "C-",
                >= 60 => "D+",
                >= 55 => "D",
                >= 50 => "D-",
                _ => "F"
            };
        }

        private void UpdateGradeStatus()
        {
            if (Grade >= 50) // Updated: Passing grade is 50%
            {
                GradeStatus = GradeStatus.Completed;
                CompletionDate ??= DateTime.Now;
            }
            else
            {
                GradeStatus = GradeStatus.Failed;
            }
        }

        // NEW: Get full grade description
        public string GetGradeDescription()
        {
            if (!Grade.HasValue)
                return "No Grade";

            var letter = CalculateGradeLetter(Grade.Value);
            var points = CalculateGradePoints(Grade.Value);

            return $"{letter} ({Grade:F1}%) - {points:F2} points";
        }

        // NEW: Get grade classification
        public string GetGradeClassification()
        {
            if (!Grade.HasValue)
                return "Ungraded";

            return Grade.Value switch
            {
                >= 90 => "Excellent",
                >= 80 => "Very Good",
                >= 70 => "Good",
                >= 60 => "Average",
                >= 50 => "Below Average (Pass)",
                < 50 => "Fail"
                //_ => "Unknown"
            };
        }

        public void AddAuditEntry(string action, string performedBy, string? notes = null)
        {
            var audit = new
            {
                Timestamp = DateTime.Now,
                Action = action,
                PerformedBy = performedBy,
                Notes = notes,
                OldStatus = EnrollmentStatus,
                NewGrade = Grade
            };

            var auditList = string.IsNullOrEmpty(AuditTrail) ?
                new List<object>() :
                JsonSerializer.Deserialize<List<object>>(AuditTrail) ?? new List<object>();

            auditList.Add(audit);
            AuditTrail = JsonSerializer.Serialize(auditList);
        }
    }

    // UPDATED: Extended GradeStatus enum with special cases
    public enum GradeStatus
    {
        [Display(Name = "In Progress")]
        InProgress,

        [Display(Name = "Completed")]
        Completed,

        [Display(Name = "Failed")]
        Failed,

        [Display(Name = "Withdrawn")]
        Withdrawn,

        [Display(Name = "Incomplete")]
        Incomplete,

        [Display(Name = "Fail with FX")]
        FailedFX, // FX: Failed due to academic dishonesty

        [Display(Name = "Incomplete Course")]
        IncompleteCourse, // IC: Incomplete (needs makeup work)
    }

    public enum EnrollmentStatus
    {
        Active,
        Waitlisted,
        Dropped,
        Withdrawn,
        Completed,
        Failed
    }

    public enum EnrollmentMethod
    {
        Web,
        Admin,
        Bulk,
        Api
    }

    public enum EnrollmentType
    {
        Regular,
        Audit,
        CrossRegistration,
        IndependentStudy
    }
}