using StudentManagementSystem.Services;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace StudentManagementSystem.Models
{
    public class CourseEnrollment : BaseEntity
    {
        // Core enrollment properties
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

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        // Grade-related properties
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

        // Professional properties
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

        // NEW: Academic performance tracking
        [Display(Name = "Attendance Percentage")]
        [Column(TypeName = "decimal(5,2)")]
        public decimal? AttendancePercentage { get; set; }

        [Display(Name = "Assignment Average")]
        [Column(TypeName = "decimal(5,2)")]
        public decimal? AssignmentAverage { get; set; }

        [Display(Name = "Midterm Grade")]
        [Column(TypeName = "decimal(5,2)")]
        public decimal? MidtermGrade { get; set; }

        [Display(Name = "Final Exam Grade")]
        [Column(TypeName = "decimal(5,2)")]
        public decimal? FinalExamGrade { get; set; }

        [Display(Name = "Grade Revision Requested")]
        public bool GradeRevisionRequested { get; set; }

        [Display(Name = "Grade Revision Reason")]
        [StringLength(1000)]
        public string? GradeRevisionReason { get; set; }

        [Display(Name = "Grade Revision Status")]
        public GradeRevisionStatus GradeRevisionStatus { get; set; } = GradeRevisionStatus.None;

        // Navigation properties
        [ForeignKey("CourseId")]
        public virtual Course? Course { get; set; }

        [ForeignKey("StudentId")]
        public virtual Student? Student { get; set; }

        [ForeignKey("SemesterId")]
        public virtual Semester? Semester { get; set; }

        // Computed properties
        [NotMapped]
        [Display(Name = "Is Completed")]
        public bool IsCompleted => GradeStatus == GradeStatus.Completed;

        [NotMapped]
        [Display(Name = "Is Failed")]
        public bool IsFailed => GradeStatus == GradeStatus.Failed;

        [NotMapped]
        [Display(Name = "Is Passing")]
        public bool IsPassing => Grade >= 60;

        [NotMapped]
        [Display(Name = "Is Waitlisted")]
        public bool IsWaitlisted => WaitlistPosition.HasValue && EnrollmentStatus == EnrollmentStatus.Waitlisted;

        [NotMapped]
        [Display(Name = "Can Be Dropped")]
        public bool CanBeDropped => EnrollmentStatus == EnrollmentStatus.Active &&
                           (Semester?.CanRegister == true || Semester?.CanGrade == true);

        [NotMapped]
        [Display(Name = "Overall Performance")]
        public decimal OverallPerformance => CalculateOverallPerformance();

        [NotMapped]
        [Display(Name = "Risk Level")]
        public string RiskLevel => CalculateRiskLevel();

        // Methods
        public void CalculateGrade()
        {
            if (Grade.HasValue)
            {
                // Use the grade service for accurate calculation
                var gradeService = GetGradeService();
                if (gradeService != null)
                {
                    GradeLetter = gradeService.CalculateGradeFromMark(Grade.Value).Result;
                    GradePoints = gradeService.CalculatePointsFromMark(Grade.Value).Result;
                }
                else
                {
                    // Fallback calculation
                    GradePoints = CalculateGradePoints(Grade.Value);
                    GradeLetter = CalculateGradeLetter(Grade.Value);
                }

                UpdateGradeStatus();
            }
        }

        private GradeScale GetGradeScale(decimal percentage)
        {
            // Simple default implementation - you can enhance this
            return percentage switch
            {
                >= 90 => new GradeScale { GradeLetter = "A", GradePoints = 4.0m },
                >= 80 => new GradeScale { GradeLetter = "B", GradePoints = 3.0m },
                >= 70 => new GradeScale { GradeLetter = "C", GradePoints = 2.0m },
                >= 60 => new GradeScale { GradeLetter = "D", GradePoints = 1.0m },
                _ => new GradeScale { GradeLetter = "F", GradePoints = 0.0m }
            };
        }


        public void CalculateComprehensiveGrade()
        {
            // Calculate final grade based on multiple components
            if (AssignmentAverage.HasValue || MidtermGrade.HasValue || FinalExamGrade.HasValue)
            {
                decimal calculatedGrade = 0m;
                int components = 0;

                // You can implement your own weighting logic here
                if (AssignmentAverage.HasValue)
                {
                    calculatedGrade += AssignmentAverage.Value * 0.3m; // 30% weight
                    components++;
                }
                if (MidtermGrade.HasValue)
                {
                    calculatedGrade += MidtermGrade.Value * 0.3m; // 30% weight
                    components++;
                }
                if (FinalExamGrade.HasValue)
                {
                    calculatedGrade += FinalExamGrade.Value * 0.4m; // 40% weight
                    components++;
                }

                if (components > 0)
                {
                    Grade = calculatedGrade;
                    CalculateGrade();
                }
            }
        }

        private decimal CalculateOverallPerformance()
        {
            decimal performance = 0m;
            int factors = 0;

            if (Grade.HasValue)
            {
                performance += Grade.Value;
                factors++;
            }
            if (AttendancePercentage.HasValue)
            {
                performance += AttendancePercentage.Value;
                factors++;
            }
            if (AssignmentAverage.HasValue)
            {
                performance += AssignmentAverage.Value;
                factors++;
            }

            return factors > 0 ? performance / factors : 0m;
        }

        private string CalculateRiskLevel()
        {
            if (!Grade.HasValue) return "Unknown";

            var performance = OverallPerformance;
            if (performance >= 80) return "Low";
            if (performance >= 60) return "Medium";
            return "High";
        }

        private static IGradeService? GetGradeService()
        {
            // This would typically be injected, but for model-level calculations,
            // you might need to use a different approach
            return null;
        }

        private decimal CalculateGradePoints(decimal grade)
        {
            return grade switch
            {
                >= 97 => 4.0m,  // A+
                >= 93 => 4.0m,  // A
                >= 90 => 3.7m,  // A-
                >= 87 => 3.3m,  // B+
                >= 83 => 3.0m,  // B
                >= 80 => 2.7m,  // B-
                >= 77 => 2.3m,  // C+
                >= 73 => 2.0m,  // C
                >= 70 => 1.7m,  // C-
                >= 67 => 1.3m,  // D+
                >= 63 => 1.0m,  // D
                >= 60 => 0.7m,  // D-
                _ => 0.0m       // F
            };
        }

        private string CalculateGradeLetter(decimal grade)
        {
            return grade switch
            {
                >= 97 => "A+",
                >= 93 => "A",
                >= 90 => "A-",
                >= 87 => "B+",
                >= 83 => "B",
                >= 80 => "B-",
                >= 77 => "C+",
                >= 73 => "C",
                >= 70 => "C-",
                >= 67 => "D+",
                >= 63 => "D",
                >= 60 => "D-",
                _ => "F"
            };
        }

        private void UpdateGradeStatus()
        {
            if (Grade >= 60)
            {
                GradeStatus = GradeStatus.Completed;
                EnrollmentStatus = EnrollmentStatus.Completed;
                CompletionDate ??= DateTime.Now;
            }
            else if (Grade < 60 && Grade.HasValue)
            {
                GradeStatus = GradeStatus.Failed;
                EnrollmentStatus = EnrollmentStatus.Failed;
            }
        }

        public void RequestGradeRevision(string reason, string requestedBy)
        {
            GradeRevisionRequested = true;
            GradeRevisionReason = reason;
            GradeRevisionStatus = GradeRevisionStatus.Pending;
            AddAuditEntry("Grade Revision Requested", requestedBy, reason);
        }

        public void ApproveGradeRevision(string approvedBy, string? comments = null)
        {
            GradeRevisionRequested = false;
            GradeRevisionStatus = GradeRevisionStatus.Approved;
            AddAuditEntry("Grade Revision Approved", approvedBy, comments);
        }

        public void RejectGradeRevision(string rejectedBy, string? comments = null)
        {
            GradeRevisionRequested = false;
            GradeRevisionStatus = GradeRevisionStatus.Rejected;
            AddAuditEntry("Grade Revision Rejected", rejectedBy, comments);
        }

        public void AddAuditEntry(string action, string performedBy, string? notes = null)
        {
            var auditEntry = new AuditEntry
            {
                Timestamp = DateTime.Now,
                Action = action,
                PerformedBy = performedBy,
                Notes = notes,
                OldStatus = EnrollmentStatus.ToString(),
                NewGrade = Grade,
                IPAddress = "System" // You can pass this from controller
            };

            var auditList = string.IsNullOrEmpty(AuditTrail)
                ? new List<AuditEntry>()
                : JsonSerializer.Deserialize<List<AuditEntry>>(AuditTrail) ?? new List<AuditEntry>();

            auditList.Add(auditEntry);
            AuditTrail = JsonSerializer.Serialize(auditList);
            LastActivityDate = DateTime.Now;
        }

        public List<AuditEntry> GetAuditHistory()
        {
            return string.IsNullOrEmpty(AuditTrail)
                ? new List<AuditEntry>()
                : JsonSerializer.Deserialize<List<AuditEntry>>(AuditTrail) ?? new List<AuditEntry>();
        }
    }

    // Supporting classes for audit trail
    public class AuditEntry
    {
        public DateTime Timestamp { get; set; }
        public string Action { get; set; } = string.Empty;
        public string PerformedBy { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public string? OldStatus { get; set; }
        public decimal? NewGrade { get; set; }
        public string? IPAddress { get; set; }
    }

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

        [Display(Name = "Grade Pending")]
        GradePending
    }

    public enum EnrollmentStatus
    {
        [Display(Name = "Active")]
        Active,

        [Display(Name = "Waitlisted")]
        Waitlisted,

        [Display(Name = "Dropped")]
        Dropped,

        [Display(Name = "Withdrawn")]
        Withdrawn,

        [Display(Name = "Completed")]
        Completed,

        [Display(Name = "Failed")]
        Failed,

        [Display(Name = "Pending Approval")]
        PendingApproval
    }

    public enum EnrollmentMethod
    {
        [Display(Name = "Web Portal")]
        Web,

        [Display(Name = "Administrator")]
        Admin,

        [Display(Name = "Bulk Import")]
        Bulk,

        [Display(Name = "API")]
        Api,

        [Display(Name = "Mobile App")]
        Mobile
    }

    public enum EnrollmentType
    {
        [Display(Name = "Regular")]
        Regular,

        [Display(Name = "Audit")]
        Audit,

        [Display(Name = "Cross Registration")]
        CrossRegistration,

        [Display(Name = "Independent Study")]
        IndependentStudy,

        [Display(Name = "Thesis")]
        Thesis,

        [Display(Name = "Internship")]
        Internship
    }

    public enum GradeRevisionStatus
    {
        [Display(Name = "None")]
        None,

        [Display(Name = "Pending")]
        Pending,

        [Display(Name = "Approved")]
        Approved,

        [Display(Name = "Rejected")]
        Rejected,

        [Display(Name = "Under Review")]
        UnderReview
    }
}