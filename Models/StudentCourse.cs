using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementSystem.Models
{
    public class StudentCourse : BaseEntity
    {
        public int StudentId { get; set; }
        public int CourseId { get; set; }

        [Range(0, 100)]
        [Column(TypeName = "decimal(5,2)")]
        public decimal? Mark { get; set; } // Course mark

        [Range(0, 4)]
        [Column(TypeName = "decimal(3,2)")]
        public decimal? Points { get; set; } // Grade points

        [StringLength(5)]
        public string Grade { get; set; } = string.Empty; // Letter grade

        [StringLength(10)]
        public string Status { get; set; } = "Not Started"; // Not Started, In Progress, Completed

        public bool IsPassed { get; set; }
        public bool IsAvailable { get; set; } = true;
        public bool IsAssigned { get; set; } = true;

        // Navigation properties
        [ForeignKey("StudentId")]
        public virtual Student? Student { get; set; }

        [ForeignKey("CourseId")]
        public virtual Course? Course { get; set; }

        // Method to calculate grade automatically
        public void CalculateGrade()
        {
            if (Mark.HasValue)
            {
                Points = CalculatePoints(Mark.Value);
                Grade = CalculateLetterGrade(Mark.Value);
                IsPassed = Mark.Value >= 50;
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
}