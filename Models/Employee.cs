using System.ComponentModel.DataAnnotations;

namespace StudentManagementSystem.Models
{
    public class Employee : BaseEntity
    {
        [Required]
        [StringLength(50)]
        public string EmployeeId { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Role { get; set; } = "Employee"; // Admin, Registrar, Employee

        // Navigation property - make it nullable
        public virtual EmployeeAccount? Account { get; set; }
    }
}