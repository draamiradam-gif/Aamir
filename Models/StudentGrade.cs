using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementSystem.Models
{
    public class StudentGrade : BaseEntity
    {
        public int StudentId { get; set; }
        public int CourseEvaluationId { get; set; }

        [Range(0, 1000)]
        public decimal Score { get; set; }

        [Range(0, 1000)]
        public decimal? MaxScore { get; set; } // Can override evaluation max score

        public decimal Percentage => MaxScore > 0 ? (Score / MaxScore.Value) * 100 : 0;

        [StringLength(10)]
        public string? GradeLetter { get; set; }

        [StringLength(1000)]
        public string? Comments { get; set; }

        [Display(Name = "Graded Date")]
        public DateTime GradedDate { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? GradedBy { get; set; }

        public bool IsAbsent { get; set; } = false;

        public bool IsExcused { get; set; } = false;

        [StringLength(500)]
        public string? ExcuseReason { get; set; }

        public DateTime? SubmissionDate { get; set; }

        public bool IsLate => SubmissionDate.HasValue && DueDate.HasValue && SubmissionDate > DueDate;

        [NotMapped]
        public DateTime? DueDate => CourseEvaluation?.DueDate;

        // Navigation properties
        [ForeignKey("StudentId")]
        public virtual Student? Student { get; set; }

        [ForeignKey("CourseEvaluationId")]
        public virtual CourseEvaluation? CourseEvaluation { get; set; }
    }
}