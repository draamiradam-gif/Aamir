using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementSystem.Models
{
    public class Student : BaseEntity
    {
        [NotMapped]
        public int SerialNumber { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Student ID")]
        public string StudentId { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [Display(Name = "Seat Number")]
        public string SeatNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        [Display(Name = "Full Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(14)]
        [Display(Name = "National ID")]
        public string NationalId { get; set; } = string.Empty;

        // String properties for display names
        [StringLength(100)]
        [Display(Name = "Department Display Name")]
        public string? DepartmentName { get; set; } = "General";

        [StringLength(50)]
        [Display(Name = "Study Level")]
        public string? StudyLevel { get; set; } = "First Level";

        [StringLength(50)]
        [Display(Name = "Semester Display Name")]
        public string? SemesterName { get; set; } = "First Semester";

        [StringLength(50)]
        [Display(Name = "Grade")]
        public string? Grade { get; set; } = "First";

        [StringLength(15)]
        [Display(Name = "Phone")]
        public string? Phone { get; set; }

        [StringLength(100)]
        [EmailAddress]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Display(Name = "Selected for Update")]
        public bool SelectedForUpdate { get; set; } = true;

        // Academic performance
        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "Percentage")]
        public decimal Percentage { get; set; }

        [Column(TypeName = "decimal(4,2)")]
        [Display(Name = "GPA")]
        public decimal GPA { get; set; }

        [Display(Name = "Passed Hours")]
        public int PassedHours { get; set; }

        [Display(Name = "Available Hours")]
        public int AvailableHours { get; set; } = 0;

        // Legacy string properties (keep for backward compatibility)
        [StringLength(100)]
        [Display(Name = "Department (Legacy)")]
        public string? Department { get; set; } = "General";

        [StringLength(50)]
        [Display(Name = "Semester (Legacy)")]
        public string? Semester { get; set; } = "First Semester";

        // NEW: University structure relationships - Foreign Keys
        public int? DepartmentId { get; set; }
        public int? BranchId { get; set; }
        public int? SemesterId { get; set; }

        // Navigation properties
        public virtual ICollection<CourseEnrollment> CourseEnrollments { get; set; } = new List<CourseEnrollment>();
        public virtual ICollection<StudentCourse> Courses { get; set; } = new List<StudentCourse>();
        public virtual StudentAccount? Account { get; set; }
        public virtual ICollection<FeePayment> FeePayments { get; set; } = new List<FeePayment>();

        // NEW: University structure navigation properties - RENAMED to avoid conflicts
        [ForeignKey("DepartmentId")]
        public virtual Department? StudentDepartment { get; set; }

        [ForeignKey("BranchId")]
        public virtual Branch? StudentBranch { get; set; }

        [ForeignKey("SemesterId")]
        public virtual Semester? StudentSemester { get; set; }

        // Computed properties
        [NotMapped]
        public string FullInfo => $"{StudentId} - {Name}";

        [NotMapped]
        public bool CanRegisterForCourses => IsActive && GPA >= 2.0m;

        [NotMapped]
        public int CompletedCourses => CourseEnrollments?.Count(e => e.IsCompleted) ?? 0;

        [NotMapped]
        public int CurrentEnrollments => CourseEnrollments?.Count(e => e.IsActive && !e.IsCompleted) ?? 0;

        [NotMapped]
        public bool IsActive => true; // You might want to add an IsActive property

        /*

        public class StudentCourse : BaseEntity
        {
            public int StudentId { get; set; }
            public int CourseId { get; set; }

            [Display(Name = "Enrollment Date")]
            public DateTime EnrollmentDate { get; set; } = DateTime.Now;

            [Display(Name = "Status")]
            public string Status { get; set; } = "Enrolled";

            // Navigation properties
            [ForeignKey("StudentId")]
            public virtual Student? Student { get; set; }

            [ForeignKey("CourseId")]
            public virtual Course? Course { get; set; }
        }

        public class StudentAccount : BaseEntity
        {
            public int StudentId { get; set; }

            [StringLength(50)]
            [Display(Name = "Username")]
            public string Username { get; set; } = string.Empty;

            [StringLength(255)]
            [Display(Name = "Password Hash")]
            public string PasswordHash { get; set; } = string.Empty;

            [Display(Name = "Last Login")]
            public DateTime? LastLogin { get; set; }

            [Display(Name = "Is Active")]
            public bool IsActive { get; set; } = true;

            // Navigation property
            [ForeignKey("StudentId")]
            public virtual Student? Student { get; set; }
        }
        public class FeePayment : BaseEntity
        {
            public int StudentId { get; set; }

            [StringLength(50)]
            [Display(Name = "Payment Type")]
            public string PaymentType { get; set; } = string.Empty;

            [Column(TypeName = "decimal(10,2)")]
            [Display(Name = "Amount")]
            public decimal Amount { get; set; }

            [Display(Name = "Payment Date")]
            public DateTime PaymentDate { get; set; } = DateTime.Now;

            [StringLength(500)]
            [Display(Name = "Description")]
            public string? Description { get; set; }

            // Navigation property
            [ForeignKey("StudentId")]
            public virtual Student? Student { get; set; }
        }
        */
    }
}