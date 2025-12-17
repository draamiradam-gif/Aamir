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

        //[Required]
        //[Display(Name = "Active")]
        //public bool IsActive { get; set; } = true;

        [Range(1, 1000)]
        [Display(Name = "Max Students")]
        public int MaxStudents { get; set; } = 1000;
        //public int MaxStudents { get; set; } = 1000;
        //public int MaxStudents { get; set; } = 0; // Default value
        [NotMapped]
        public bool HasCustomMaxStudents => MaxStudents != 1000;


        [Display(Name = "Min GPA")]
        [Column(TypeName = "decimal(4,2)")]
        [Range(0.0, 4.0, ErrorMessage = "GPA must be between 0.0 and 4.0")]
        public decimal? MinGPA { get; set; }

        [Display(Name = "Min Passed Hours")]
        [Range(0, 200, ErrorMessage = "Passed hours must be between 0 and 200")]
        public int? MinPassedHours { get; set; }

        [StringLength(1000)]
        [Display(Name = "Prerequisites")]
        public string? PrerequisitesString { get; set; }

        [StringLength(20000)]
        [Display(Name = "Course Specification")]
        public string? CourseSpecification { get; set; }

        [StringLength(100)]
        [Display(Name = "Icon")]
        public string? Icon { get; set; }


        //[NotMapped]
        //[Display(Name = "Current Enrollment")]
        //public int CurrentEnrollment => CourseEnrollments?.Count(e => e.IsActive && e.GradeStatus == GradeStatus.InProgress) ?? 0;

        //[NotMapped]
        //[Display(Name = "Current Enrollment")]
        //public int CurrentEnrollment => CourseEnrollments?.Count(e =>
        //    e.IsActive && e.EnrollmentStatus == EnrollmentStatus.Active) ?? 0;

        [Display(Name = "Current Enrollment")]
        public int CurrentEnrollment { get; set; }
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

        //[NotMapped]
        //public int Semester
        //{
        //    get => SemesterId;
        //    set => SemesterId = value;
        //}

        ////[Required]
        ////[Display(Name = "Semester")]
        ////public int SemesterId { get; set; }  // Remove nullable
        //[Display(Name = "Semester")]
        //public int SemesterId { get; set; } = 0;
        //// public int? SemesterId { get; set; }
        //[ForeignKey("SemesterId")]
        //[Display(Name = "Semester")]
        //public virtual Semester? CourseSemester { get; set; }

        //[NotMapped]
        //public string SemesterDisplay => CourseSemester?.Name ?? $"Semester {SemesterId}";
        //[ForeignKey("SemesterId")]
        [Display(Name = "Semester")]
        public int? SemesterId { get; set; } // ✅ Changed to nullable

        [NotMapped]
        public int Semester
        {
            get => SemesterId ?? 0; // Handle null case
            //set => SemesterId = value;
            set => SemesterId = value == 0 ? null : value;
        }

        [ForeignKey("SemesterId")]
        [Display(Name = "Semester")]
        public virtual Semester? CourseSemester { get; set; }

        [NotMapped]
        public string SemesterDisplay => CourseSemester?.Name ??
            (SemesterId.HasValue ? $"Semester {SemesterId}" : "Not assigned");


        // Computed properties
        [NotMapped]
        public bool HasAvailableSeats => CurrentEnrollment < MaxStudents;

        [NotMapped]
        public string StatusBadge => IsActive ?
            (HasAvailableSeats ? "bg-success" : "bg-warning") : "bg-secondary";

        [NotMapped]
        public string StatusText => IsActive ?
            (HasAvailableSeats ? "Active" : "Full") : "Inactive";


        [Display(Name = "Schedule Days")]
        [StringLength(50)]
        public string? ScheduleDays { get; set; } // e.g., "MWF", "TTh"

        [Display(Name = "Start Time")]
        public TimeSpan? StartTime { get; set; }

        [Display(Name = "End Time")]
        public TimeSpan? EndTime { get; set; }

        [Display(Name = "Room Number")]
        [StringLength(20)]
        public string? RoomNumber { get; set; }

        // OR if you have a combined schedule string:
        [Display(Name = "Class Schedule")]
        [StringLength(100)]
        public string? ClassSchedule { get; set; } // e.g., "MWF 10:00-11:00 AM, Room 101"

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

        public bool IsMandatory { get; set; } = true;



    }

    public class ImportOptions
    {
        public bool CreateMissingSemesters { get; set; } = false;
        public bool PreserveSemesterAssignments { get; set; } = true;
        public string SemesterCreationMode { get; set; } = "ask"; // "ask", "auto", "ignore"
        public List<int> SemestersToCreate { get; set; } = new List<int>();
    }

    

}
/*
 6. Alternative approach: Use extension method
If you need HasValue functionality in multiple places, create an extension method:

csharp
public static class CourseExtensions
{
    public static bool HasCustomMaxStudents(this Course course)
    {
        return course.MaxStudents != 1000;
    }
    
    public static bool HasCustomMinGPA(this Course course)
    {
        return course.MinGPA.HasValue;
    }
    
    public static bool HasCustomMinPassedHours(this Course course)
    {
        return course.MinPassedHours.HasValue;
    }
}

// Usage:
if (course.HasCustomMaxStudents())
{
    // Do something
}
7. Fix the other compilation errors:
For the other errors in your codebase:

csharp
// Error: Cannot implicitly convert type 'int?' to 'int'
// In EnrollmentService.cs line 656:
var maxStudents = course.MaxStudents; // This is int
// No conversion needed since it's not nullable anymore

// Error: Cannot implicitly convert type 'decimal?' to 'decimal'
// In EnrollmentService.cs line 712:
var minGPA = course.MinGPA ?? 0; // Use null-coalescing operator

// Error: Argument 1: cannot convert from 'double?' to 'decimal'
// In SemestersController.cs:
// Change double? to decimal?
var value = (decimal?)someNullableDoubleValue; // Explicit cast
// OR
var value = (decimal)(someNullableDoubleValue ?? 0); // With default

// Error: No overload for method 'ToString' takes 1 arguments
// In Views/Semesters/Details.cshtml:
// Change from: @someValue.ToString("F2")
// To: @someValue?.ToString("F2") or @someValue.ToString()
// OR if it's nullable decimal:
@(someNullableDecimal?.ToString("F2") ?? "0.00")
 */