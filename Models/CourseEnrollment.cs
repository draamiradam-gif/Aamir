using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementSystem.Models
{
    public class CourseEnrollment : BaseEntity
    {
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

        // Navigation properties - MAKE THEM NULLABLE
        [ForeignKey("CourseId")]
        public virtual Course? Course { get; set; } // ADDED NULLABLE

        [ForeignKey("StudentId")]
        public virtual Student? Student { get; set; } // ADDED NULLABLE

        [ForeignKey("SemesterId")]
        public virtual Semester? Semester { get; set; } // ADDED NULLABLE

        // Computed properties
        [NotMapped]
        public bool IsCompleted => GradeStatus == GradeStatus.Completed;

        [NotMapped]
        public bool IsFailed => GradeStatus == GradeStatus.Failed;

        [NotMapped]
        public bool IsPassing => Grade >= 60;



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