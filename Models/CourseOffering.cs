// Models/CourseOffering.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementSystem.Models
{
    public class CourseOffering : BaseEntity
    {
        [Required]
        public int CourseId { get; set; }

        [Required]
        public int SemesterId { get; set; }

        [StringLength(10)]
        public string Section { get; set; } = "A";

        [StringLength(100)]
        public string? Instructor { get; set; }

        [Range(1, 500)]
        public int Capacity { get; set; } = 30;

        [Range(0, 500)]
        public int Enrolled { get; set; } = 0;

        [Display(Name = "Is Registration Open")]
        public bool IsRegistrationOpen { get; set; } = true;

        // Navigation properties
        [ForeignKey("CourseId")]
        public virtual Course? Course { get; set; }

        [ForeignKey("SemesterId")]
        public virtual Semester? Semester { get; set; }
    }
}