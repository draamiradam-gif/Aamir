// Models/College.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementSystem.Models
{
    public class College : BaseEntity
    {
        [Required]
        [StringLength(200)]
        [Display(Name = "College Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Display(Name = "University")]
        public int UniversityId { get; set; }

        [StringLength(10)]
        [Display(Name = "College Code")]
        public string? CollegeCode { get; set; }

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
        [NotMapped]
        public int DepartmentCount => Departments?.Count ?? 0;

        // Navigation properties
        [ForeignKey("UniversityId")]
        public virtual University? University { get; set; }
        public virtual ICollection<Department> Departments { get; set; } = new List<Department>();

        [NotMapped]
        public string DisplayName => $"{Name} ({CollegeCode})";

        [NotMapped]
        public string FullPath => $"{University?.Name} → {Name}";


    }
}