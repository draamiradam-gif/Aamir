using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementSystem.Models
{
    public class GradeScale : BaseEntity
    {
        [Required]
        [StringLength(10)]
        [Display(Name = "Grade Letter")]
        public string GradeLetter { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Minimum Percentage")]
        [Range(0, 100)]
        public decimal MinPercentage { get; set; }

        [Display(Name = "Maximum Percentage")]
        [Range(0, 100)]
        public decimal MaxPercentage { get; set; }

        [Display(Name = "Grade Points")]
        [Column(TypeName = "decimal(3,2)")]
        public decimal GradePoints { get; set; }

        [Display(Name = "Is Passing Grade")]
        public bool IsPassingGrade { get; set; } = true;

        //[Display(Name = "Is Active")]
        //public bool IsActive { get; set; } = true;

        [Display(Name = "Classification")]
        [StringLength(50)]
        public string Classification { get; set; } = string.Empty; // Excellent, Good, etc.
               

        // Helper method to get grade scale based on percentage
        public static GradeScale? GetGradeScale(decimal percentage, List<GradeScale> gradeScales)
        {
            return gradeScales.FirstOrDefault(gs =>
                percentage >= gs.MinPercentage &&
                percentage <= gs.MaxPercentage &&
                gs.IsActive);
        }
    }
}