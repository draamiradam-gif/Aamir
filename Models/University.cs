using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementSystem.Models
{
    public class University : BaseEntity
    {
        [Required]
        [StringLength(200)]
        [Display(Name = "University Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        [Display(Name = "University Code")]
        public string Code { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [StringLength(200)]
        [Display(Name = "Address")]
        public string? Address { get; set; }

        [StringLength(100)]
        [EmailAddress]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [StringLength(20)]
        [Display(Name = "Phone")]
        public string? Phone { get; set; }

        [StringLength(100)]
        [Display(Name = "Website")]
        public string? Website { get; set; }

        [Display(Name = "Establishment Year")]
        [Range(1800, 2100)]
        public int? EstablishmentYear { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Allow Multiple Colleges")]
        public bool AllowMultipleColleges { get; set; } = true;

        // Navigation properties

        public virtual ICollection<College> Colleges { get; set; } = new List<College>();
        //public virtual ICollection<AcademicRule> AcademicRules { get; set; } = new List<AcademicRule>();

        // Computed properties
        [NotMapped]
        public int CollegeCount => Colleges?.Count ?? 0;

        [NotMapped]
        public string DisplayName => $"{Name} ({Code})";
    }
}