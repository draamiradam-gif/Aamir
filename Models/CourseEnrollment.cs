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
        public string? ApprovedBy { get; set; } // Made nullable

        [Display(Name = "Approval Date")]
        public DateTime? ApprovalDate { get; set; }

        [Display(Name = "Drop/Withdraw Reason")]
        [StringLength(500)]
        public string? DropReason { get; set; } // Made nullable

        [Display(Name = "Drop/Withdraw Date")]
        public DateTime? DropDate { get; set; }

        [Display(Name = "Last Activity Date")]
        public DateTime LastActivityDate { get; set; } = DateTime.Now;

        [Display(Name = "Audit Trail")]
        public string? AuditTrail { get; set; } // JSON history of changes

        // Navigation properties (EXISTING - KEEP THESE, already nullable)
        [ForeignKey("CourseId")]
        public virtual Course? Course { get; set; }

        [ForeignKey("StudentId")]
        public virtual Student? Student { get; set; }

        [ForeignKey("SemesterId")]
        public virtual Semester? Semester { get; set; }

        // Computed properties (EXISTING + NEW)
        [NotMapped]
        public bool IsCompleted => GradeStatus == GradeStatus.Completed;

        [NotMapped]
        public bool IsFailed => GradeStatus == GradeStatus.Failed;

        [NotMapped]
        public bool IsPassing => Grade >= 60;

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

        // ADD THESE HELPER METHODS:
        private decimal CalculateGradePoints(decimal grade)
        {
            return grade switch
            {
                >= 90 => 4.0m,  // A
                >= 80 => 3.0m,  // B
                >= 70 => 2.0m,  // C
                >= 60 => 1.0m,  // D
                _ => 0.0m       // F
            };
        }

        private string CalculateGradeLetter(decimal grade)
        {
            return grade switch
            {
                >= 90 => "A",
                >= 80 => "B",
                >= 70 => "C",
                >= 60 => "D",
                _ => "F"
            };
        }

        private void UpdateGradeStatus()
        {
            if (Grade >= 60)
            {
                GradeStatus = GradeStatus.Completed;
                CompletionDate ??= DateTime.Now;
            }
            else
            {
                GradeStatus = GradeStatus.Failed;
            }
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
                JsonSerializer.Deserialize<List<object>>(AuditTrail) ?? new List<object>(); // Added null coalescing

            auditList.Add(audit);
            AuditTrail = JsonSerializer.Serialize(auditList);
        }
    }

    // ENUMS - Make sure these are outside the CourseEnrollment class but inside the namespace

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
        Incomplete
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