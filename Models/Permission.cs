using System.ComponentModel.DataAnnotations;

namespace StudentManagementSystem.Models
{
    public class Permission
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty; // e.g., "Courses.View"

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Category { get; set; } = string.Empty; // e.g., "Courses", "Students"

        public bool IsActive { get; set; } = true;

        // Navigation property
        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}