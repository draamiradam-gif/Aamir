using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementSystem.Models
{
    public class CourseEnrollment : BaseEntity
    {
        public int CourseId { get; set; }
        public int StudentId { get; set; }

        [Range(0, 100)]
        [Display(Name = "Grade")]
        [Column(TypeName = "decimal(5,2)")]
        public decimal? Grade { get; set; } // Percentage mark (0-100)

        [StringLength(5)] // Increased to accommodate "A+", "B-", etc.
        [Display(Name = "Grade Letter")]
        public string? GradeLetter { get; set; }

        [Display(Name = "Enrollment Date")]
        public DateTime EnrollmentDate { get; set; } = DateTime.Now;

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        // Grade-related properties
        [Display(Name = "Grade Points")]
        [Column(TypeName = "decimal(3,2)")]
        public decimal? GradePoints { get; set; }

        [Display(Name = "Grade Status")]
        public GradeStatus GradeStatus { get; set; } = GradeStatus.InProgress;

        [Display(Name = "Completion Date")]
        public DateTime? CompletionDate { get; set; }

        [Display(Name = "Remarks")]
        [StringLength(500)]
        public string? Remarks { get; set; }

        // Navigation properties
        [ForeignKey("CourseId")]
        public virtual Course? Course { get; set; }

        [ForeignKey("StudentId")]
        public virtual Student? Student { get; set; }

        // Computed properties
        [NotMapped]
        public bool IsCompleted => GradeStatus == GradeStatus.Completed;

        [NotMapped]
        public bool IsFailed => GradeStatus == GradeStatus.Failed;

        [NotMapped]
        public bool IsPassed => GradePoints >= 1.0m; // D- or better is passing

        // Method to calculate grade automatically (using your existing logic)
        public void CalculateGrade()
        {
            if (Grade.HasValue)
            {
                GradePoints = CalculatePoints(Grade.Value);
                GradeLetter = CalculateLetterGrade(Grade.Value);
                GradeStatus = Grade.Value >= 50 ? GradeStatus.Completed : GradeStatus.Failed;

                if (GradeStatus == GradeStatus.Completed)
                {
                    CompletionDate = DateTime.Now;
                }
            }
        }

        private decimal CalculatePoints(decimal mark)
        {
            return mark switch
            {
                >= 96 => 4.0m,
                >= 92 => 3.7m,
                >= 88 => 3.4m,
                >= 84 => 3.2m,
                >= 80 => 3.0m,
                >= 76 => 2.8m,
                >= 72 => 2.6m,
                >= 68 => 2.4m,
                >= 64 => 2.2m,
                >= 60 => 2.0m,
                >= 55 => 1.5m,
                >= 50 => 1.0m,
                _ => 0.0m
            };
        }

        private string CalculateLetterGrade(decimal mark)
        {
            return mark switch
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
        Incomplete
    }
}