using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementSystem.Models
{
    public class StudentEnrollment : BaseEntity
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

        //[Display(Name = "Active")]
        //public bool IsActive { get; set; } = true;

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

        // FIX: Make navigation properties nullable
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
        public bool IsPassing => Grade >= 50;
    }

    
}