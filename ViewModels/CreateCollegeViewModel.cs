using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using StudentManagementSystem.Models;

namespace StudentManagementSystem.ViewModels
{
    public class CreateCollegeViewModel
    {
        [Required]
        [StringLength(200)]
        [Display(Name = "College Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(10)]
        [Display(Name = "College Code")]
        public string? CollegeCode { get; set; }

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "University")]
        public int UniversityId { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        // Parent info for display
        public University? ParentUniversity { get; set; }

        // Dropdown items
        public List<SelectListItem> Universities { get; set; } = new List<SelectListItem>();
    }
}