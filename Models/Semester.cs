// Models/Semester.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementSystem.Models
{
    public class Semester : BaseEntity
    {
        [Required]
        [StringLength(50)]
        [Display(Name = "Semester Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        [Display(Name = "Semester Type")]
        public string SemesterType { get; set; } = "Spring"; // Spring, Fall, Summer

        [Required]
        [Display(Name = "Academic Year")]
        public int AcademicYear { get; set; } = DateTime.Now.Year;

        // Can be linked to Department, Branch, or Sub-Branch
        public int? DepartmentId { get; set; }
        public int? BranchId { get; set; }
        public int? SubBranchId { get; set; }

        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; } = DateTime.Now;

        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; } = DateTime.Now.AddMonths(4);

        [Display(Name = "Registration Start Date")]
        public DateTime RegistrationStartDate { get; set; } = DateTime.Now.AddDays(-7);

        [Display(Name = "Registration End Date")]
        public DateTime RegistrationEndDate { get; set; } = DateTime.Now.AddDays(14);

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Current Semester")]
        public bool IsCurrent { get; set; } = false;

        [Display(Name = "Registration Open")]
        public bool IsRegistrationOpen { get; set; } = true;

        // Navigation properties
        public virtual Department? Department { get; set; }
        public virtual Branch? Branch { get; set; }

        [ForeignKey("SubBranchId")]
        public virtual Branch? SubBranch { get; set; }

        // Remove this line since we're using Courses directly
        // public virtual ICollection<CourseOffering> CourseOfferings { get; set; } = new List<CourseOffering>();
        /*
                [NotMapped]

                public string FullPath
                {
                    get
                    {
                        if (SubBranchId.HasValue)
                            return $"{SubBranch?.FullPath} → {Name}";
                        if (BranchId.HasValue)
                            return $"{Branch?.FullPath} → {Name}";
                        if (DepartmentId.HasValue)
                            return $"{Department?.FullPath} → {Name}";
                        return Name;
                    }
                }
        */

        [NotMapped]
        public string FullPath
        {
            get
            {
                if (SubBranchId.HasValue && SubBranch != null)
                    return $"{SubBranch.FullPath} → {Name}";
                if (BranchId.HasValue && Branch != null)
                    return $"{Branch.FullPath} → {Name}";
                if (DepartmentId.HasValue && Department != null)
                    return $"{Department.FullPath} → {Name}";
                return Name;
            }
        }

        [NotMapped]
        public bool IsRegistrationPeriod =>
            DateTime.Now >= RegistrationStartDate && DateTime.Now <= RegistrationEndDate;
    }
}