using System.ComponentModel.DataAnnotations;

namespace StudentManagementSystem.Models.ViewModels
{
    public class DuplicateViewModel
    {
        public string EntityType { get; set; } = string.Empty;
        public int SourceId { get; set; }

        [Required(ErrorMessage = "New name is required")]
        [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
        [Display(Name = "New Name")]
        public string NewName { get; set; } = string.Empty;

        [Display(Name = "Copy associated items")]
        public bool CopySubItems { get; set; } = true;

        public int? TargetParentId { get; set; }

        // Semester-specific properties
        [Display(Name = "Academic Year Offset")]
        [Range(0, 10, ErrorMessage = "Academic year offset must be between 0 and 10")]
        public int AcademicYearOffset { get; set; } = 1;

        [Display(Name = "Copy Courses")]
        public bool CopyCourses { get; set; } = true;

        [Display(Name = "Copy Students")]
        public bool CopyStudents { get; set; } = false;
    }
}