using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementSystem.Models
{
    public class GradingComponent : BaseEntity
    {
        [Required]
        [StringLength(100)]
        [Display(Name = "Component Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        [Display(Name = "Component Type")]
        public GradingComponentType ComponentType { get; set; } = GradingComponentType.FinalExam;

        [Required]
        [Range(0, 100)]
        [Display(Name = "Weight Percentage")]
        public decimal WeightPercentage { get; set; }

        [Required]
        [Range(0, 1000)]
        [Display(Name = "Maximum Marks")]
        public decimal MaximumMarks { get; set; } = 100;

        // REMOVE THIS DUPLICATE PROPERTY:
        // [Display(Name = "Is Active")]
        // public bool IsActive { get; set; } = true;

        [Display(Name = "Include in Final Grade")]
        public bool IncludeInFinalGrade { get; set; } = true;

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        // Navigation properties
        public int CourseId { get; set; }
        public virtual Course? Course { get; set; }

        [Display(Name = "Is Active")]
        public new bool IsActive { get; set; } = true;

        [Display(Name = "Is Required")]
        public bool IsRequired { get; set; } = true;
        public virtual ICollection<StudentGrade> StudentGrades { get; set; } = new List<StudentGrade>();
    }

    public class StudentGrade : BaseEntity
    {
        public int GradingComponentId { get; set; }
        public int StudentId { get; set; }
        public int CourseId { get; set; }
        public int SemesterId { get; set; }

        [Range(0, 1000)]
        [Display(Name = "Marks Obtained")]
        public decimal MarksObtained { get; set; }

        [Range(0, 100)]
        [Display(Name = "Percentage")]
        public decimal Percentage { get; set; }

        [StringLength(5)]
        [Display(Name = "Grade Letter")]
        public string? GradeLetter { get; set; }

        [Display(Name = "Grade Points")]
        [Column(TypeName = "decimal(4,2)")]
        public decimal? GradePoints { get; set; }

        [Display(Name = "Graded Date")]
        public DateTime GradedDate { get; set; } = DateTime.Now;

        [StringLength(500)]
        [Display(Name = "Remarks")]
        public string? Remarks { get; set; }

        [Display(Name = "Is Absent")]
        public bool IsAbsent { get; set; }

        [Display(Name = "Is Exempted")]
        public bool IsExempted { get; set; }

        // Navigation properties
        [ForeignKey("GradingComponentId")]
        public virtual GradingComponent? GradingComponent { get; set; }

        [ForeignKey("StudentId")]
        public virtual Student? Student { get; set; }

        [ForeignKey("CourseId")]
        public virtual Course? Course { get; set; }

        [ForeignKey("SemesterId")]
        public virtual Semester? Semester { get; set; }

        // Computed properties
        //[NotMapped]
        //public decimal WeightedScore => (MarksObtained / GradingComponent?.MaximumMarks ?? 1) * (GradingComponent?.WeightPercentage ?? 0);
        public decimal WeightedScore { get; set; }

        [NotMapped]
        public bool IsPassing => Percentage >= 50; // Adjust threshold as needed
    }

    public class FinalGrade : BaseEntity
    {
        public int StudentId { get; set; }
        public int CourseId { get; set; }
        public int SemesterId { get; set; }

        [Range(0, 100)]
        [Display(Name = "Final Percentage")]
        public decimal FinalPercentage { get; set; }

        [StringLength(5)]
        [Display(Name = "Final Grade")]
        public string FinalGradeLetter { get; set; } = string.Empty;

        [Display(Name = "Final Grade Points")]
        [Column(TypeName = "decimal(4,2)")]
        public decimal? FinalGradePoints { get; set; }

        [Display(Name = "Total Marks Obtained")]
        public decimal TotalMarksObtained { get; set; }

        [Display(Name = "Total Maximum Marks")]
        public decimal TotalMaximumMarks { get; set; }

        [Display(Name = "Grade Status")]
        public GradeStatus GradeStatus { get; set; } = GradeStatus.InProgress;

        [Display(Name = "Calculation Date")]
        public DateTime CalculationDate { get; set; } = DateTime.Now;

        [StringLength(1000)]
        [Display(Name = "Grade Breakdown")]
        public string? GradeBreakdown { get; set; } // JSON string for component details

        // Navigation properties
        [ForeignKey("StudentId")]
        public virtual Student? Student { get; set; }

        [ForeignKey("CourseId")]
        public virtual Course? Course { get; set; }

        [ForeignKey("SemesterId")]
        public virtual Semester? Semester { get; set; }
    }

    public class GradingTemplate : BaseEntity
    {
        [Required]
        [StringLength(100)]
        [Display(Name = "Template Name")]
        public string TemplateName { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Is Default")]
        public bool IsDefault { get; set; }

        [Display(Name = "Total Weight")]
        public decimal TotalWeight => Components?.Sum(c => c.WeightPercentage) ?? 0;

        // Navigation properties
        public virtual ICollection<GradingTemplateComponent> Components { get; set; } = new List<GradingTemplateComponent>();
    }

    public class GradingTemplateComponent : BaseEntity
    {
        public int TemplateId { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Component Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Component Type")]
        public GradingComponentType ComponentType { get; set; }

        [Required]
        [Range(0, 100)]
        [Display(Name = "Weight Percentage")]
        public decimal WeightPercentage { get; set; }

        [Required]
        [Range(0, 1000)]
        [Display(Name = "Maximum Marks")]
        public decimal MaximumMarks { get; set; } = 100;

        [Display(Name = "Is Required")]
        public bool IsRequired { get; set; } = true;

        // Navigation properties
        [ForeignKey("TemplateId")]
        public virtual GradingTemplate? Template { get; set; }
    }

    public enum GradingComponentType
    {
        [Display(Name = "Final Exam")]
        FinalExam,
        [Display(Name = "Midterm Exam")]
        MidtermExam,
        [Display(Name = "Quiz")]
        Quiz,
        [Display(Name = "Laboratory")]
        Laboratory,
        [Display(Name = "Assignment")]
        Assignment,
        [Display(Name = "Project")]
        Project,
        [Display(Name = "Presentation")]
        Presentation,
        [Display(Name = "Participation")]
        Participation,
        [Display(Name = "Attendance")]
        Attendance,
        [Display(Name = "Course Work")]
        CourseWork,
        [Display(Name = "Practical Exam")]
        PracticalExam,
        [Display(Name = "Oral Exam")]
        OralExam
    }

    
}