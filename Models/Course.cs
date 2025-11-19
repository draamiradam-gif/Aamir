using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementSystem.Models
{
    public class Course : BaseEntity
    {
        [NotMapped]
        public int SerialNumber { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "Course Code")]
        public string CourseCode { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Course Name")]
        public string CourseName { get; set; } = string.Empty;

        [StringLength(5000)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required]
        [Range(1, 6)]
        [Display(Name = "Credits")]
        public int Credits { get; set; } = 3;

        // ORIGINAL string property for department name
        [Required]
        [StringLength(50)]
        [Display(Name = "Department Name")]
        public string Department { get; set; } = string.Empty;

        // ORIGINAL int property for semester number
        
        

        [Display(Name = "Grade Level")]
        [Range(1, 12)]
        public int GradeLevel { get; set; } = 1;

        [Required]
        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [Range(1, 1000)]
        [Display(Name = "Max Students")]
        public int MaxStudents { get; set; } = 1000;

        [Display(Name = "Min GPA")]
        [Column(TypeName = "decimal(4,2)")]
        public decimal MinGPA { get; set; } = 0.0m;

        [Display(Name = "Min Passed Hours")]
        public int MinPassedHours { get; set; } = 0;
                
        [StringLength(1000)]
        [Display(Name = "Prerequisites")]
        public string? PrerequisitesString { get; set; }

        [StringLength(20000)]
        [Display(Name = "Course Specification")]
        public string? CourseSpecification { get; set; }

        [StringLength(100)]
        [Display(Name = "Icon")]
        public string? Icon { get; set; }


        [NotMapped]
        [Display(Name = "Current Enrollment")]
        public int CurrentEnrollment => CourseEnrollments?.Count(e => e.IsActive && e.GradeStatus == GradeStatus.InProgress) ?? 0;



        public int? DepartmentId { get; set; }
        

        [NotMapped]
        public List<int> SelectedPrerequisiteIds { get; set; } = new List<int>();

        [NotMapped]
        public List<Course> AvailablePrerequisites { get; set; } = new List<Course>();

        [NotMapped]
        public string? PrerequisiteCodes { get; set; }

        [NotMapped]
        public string CourseCodeName => $"{CourseCode} - {CourseName}";

        // Navigation properties
        public virtual ICollection<CourseEnrollment> CourseEnrollments { get; set; } = new List<CourseEnrollment>();
        public virtual ICollection<CoursePrerequisite> Prerequisites { get; set; } = new List<CoursePrerequisite>();
        public virtual ICollection<CoursePrerequisite> RequiredFor { get; set; } = new List<CoursePrerequisite>();

        // NEW: University structure navigation properties - RENAMED to avoid conflicts
        [ForeignKey("DepartmentId")]
        public virtual Department? CourseDepartment { get; set; }

        [NotMapped]
        public int Semester
        {
            get => SemesterId;
            set => SemesterId = value;
        }

        [Required]
        [Display(Name = "Semester")]
        public int SemesterId { get; set; }  // Remove nullable

        [ForeignKey("SemesterId")]
        [Display(Name = "Semester")]
        public virtual Semester? CourseSemester { get; set; }

        [NotMapped]
        public string SemesterDisplay => CourseSemester?.Name ?? $"Semester {SemesterId}";

        // Computed properties
        [NotMapped]
        public bool HasAvailableSeats => CurrentEnrollment < MaxStudents;

        [NotMapped]
        public string StatusBadge => IsActive ?
            (HasAvailableSeats ? "bg-success" : "bg-warning") : "bg-secondary";

        [NotMapped]
        public string StatusText => IsActive ?
            (HasAvailableSeats ? "Active" : "Full") : "Inactive";

    }

    public class CoursePrerequisite : BaseEntity
    {
        public int CourseId { get; set; }
        public int PrerequisiteCourseId { get; set; }

        [Range(0, 100)]
        [Display(Name = "Minimum Grade in Prerequisite")]
        public decimal? MinGrade { get; set; }

        [Display(Name = "Required")]
        public bool IsRequired { get; set; } = true;

        // Navigation properties
        [ForeignKey("CourseId")]
        public virtual Course? Course { get; set; }

        [ForeignKey("PrerequisiteCourseId")]
        public virtual Course? PrerequisiteCourse { get; set; }


    }

    //public class CourseEnrollment : BaseEntity
    //{
    //    public int CourseId { get; set; }
    //    public int StudentId { get; set; }

    //    [Range(0, 100)]
    //    [Display(Name = "Grade")]
    //    public decimal? Grade { get; set; }

    //    [StringLength(2)]
    //    [Display(Name = "Grade Letter")]
    //    public string? GradeLetter { get; set; }

    //    [Display(Name = "Enrollment Date")]
    //    public DateTime EnrollmentDate { get; set; } = DateTime.Now;

    //    [Display(Name = "Active")]
    //    public bool IsActive { get; set; } = true;

    //    // Grade-related properties
    //    [Display(Name = "Grade Points")]
    //    [Column(TypeName = "decimal(4,2)")]
    //    public decimal? GradePoints { get; set; }

    //    [Display(Name = "Grade Status")]
    //    public GradeStatus GradeStatus { get; set; } = GradeStatus.InProgress;

    //    [Display(Name = "Completion Date")]
    //    public DateTime? CompletionDate { get; set; }

    //    [Display(Name = "Remarks")]
    //    [StringLength(500)]
    //    public string? Remarks { get; set; }

    //    // Navigation properties
    //    [ForeignKey("CourseId")]
    //    public virtual Course? Course { get; set; }

    //    [ForeignKey("StudentId")]
    //    public virtual Student? Student { get; set; }

    //    // Computed properties
    //    [NotMapped]
    //    public bool IsCompleted => GradeStatus == GradeStatus.Completed;

    //    [NotMapped]
    //    public bool IsFailed => GradeStatus == GradeStatus.Failed;
    //}

    //public enum GradeStatus
    //{
    //    [Display(Name = "In Progress")]
    //    InProgress,

    //    [Display(Name = "Completed")]
    //    Completed,

    //    [Display(Name = "Failed")]
    //    Failed,

    //    [Display(Name = "Withdrawn")]
    //    Withdrawn,

    //    [Display(Name = "Incomplete")]
    //    Incomplete
    //}


}