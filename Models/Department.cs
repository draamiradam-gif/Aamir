// Models/Department.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementSystem.Models
{
    public class Department : BaseEntity
    {
        [Display(Name = "Start Year")]
        [Range(1, 5)]
        public int StartYear { get; set; } = 1;

        [Display(Name = "Is Major Department")]
        public bool IsMajorDepartment { get; set; } = false;

        [Display(Name = "Minimum GPA for Major")]
        [Range(0.0, 4.0)]
        public decimal? MinimumGPAMajor { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Department Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(10)]
        [Display(Name = "Department Code")]
        public string? DepartmentCode { get; set; }

        public int? CollegeId { get; set; }

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Total Benches")]
        public int TotalBenches { get; set; } = 0;

        [Display(Name = "Available Benches")]
        public int AvailableBenches { get; set; } = 0;

        //[Display(Name = "Active")]
        //public bool IsActive { get; set; } = true;

        // Navigation properties
        [ForeignKey("CollegeId")]
        public virtual College? College { get; set; }
        public virtual ICollection<Branch> Branches { get; set; } = new List<Branch>();
        public virtual ICollection<Semester> Semesters { get; set; } = new List<Semester>();
        public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
        public virtual ICollection<Student> Students { get; set; } = new List<Student>();
        [NotMapped]
        public string FullPath => College != null ?
            $"{College.University?.Name} → {College.Name} → {Name}" : Name;

        public int? UniversityId { get; set; }

        [ForeignKey("UniversityId")]
        public virtual University? University { get; set; }

        /* 
          [NotMapped]
 public string FullPath => College != null ? 
     $"{College.University?.Name} → {College.Name} → {Name}" : Name;

 */
        //public int? CollegeId { get; set; }

        
        //public virtual College? College { get; set; }
    }
}