using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementSystem.Models
{
    public class CourseEvaluation : BaseEntity
    {
        public int CourseId { get; set; }
        public int EvaluationTypeId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Range(0, 100)]
        public decimal Weight { get; set; } // Percentage weight in final grade

        [Range(0, 100)]
        public decimal MaxScore { get; set; } = 100;

        [Display(Name = "Due Date")]
        public DateTime? DueDate { get; set; }

        [Display(Name = "Evaluation Date")]
        public DateTime? EvaluationDate { get; set; }

        public bool IsPublished { get; set; } = false;

        public bool IsGraded { get; set; } = false;

        [StringLength(50)]
        public string Semester { get; set; } = string.Empty;

        // Navigation properties
        [ForeignKey("CourseId")]
        public virtual Course? Course { get; set; }

        [ForeignKey("EvaluationTypeId")]
        public virtual EvaluationType? EvaluationType { get; set; }

        public virtual ICollection<StudentGrade> StudentGrades { get; set; } = new List<StudentGrade>();

        // Computed properties
        [NotMapped]
        public decimal AverageScore => StudentGrades.Any() ? StudentGrades.Average(g => g.Score) : 0;

        [NotMapped]
        public int TotalSubmissions => StudentGrades.Count;

        [NotMapped]
        public decimal CompletionRate => Course?.CurrentEnrollment > 0 ?
            (TotalSubmissions * 100.0m) / Course.CurrentEnrollment : 0;
    }
}