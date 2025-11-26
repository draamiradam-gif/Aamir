using System.ComponentModel.DataAnnotations;

namespace StudentManagementSystem.Models
{
    public class EvaluationType : BaseEntity
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Range(0, 100)]
        public decimal DefaultWeight { get; set; } // Percentage weight

        public bool IsActive { get; set; } = true;

        [StringLength(50)]
        public string Category { get; set; } = "General"; // Exam, Coursework, Lab, etc.

        public int Order { get; set; } // For display ordering
    }

    public enum GradeCategory
    {
        [Display(Name = "Examination")]
        Examination,

        [Display(Name = "Course Work")]
        CourseWork,

        [Display(Name = "Laboratory Work")]
        Laboratory,

        [Display(Name = "Project")]
        Project,

        [Display(Name = "Participation")]
        Participation,

        [Display(Name = "Attendance")]
        Attendance,

        [Display(Name = "Quiz")]
        Quiz,

        [Display(Name = "Assignment")]
        Assignment
    }
}