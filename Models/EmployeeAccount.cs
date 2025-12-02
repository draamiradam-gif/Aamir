using Microsoft.AspNetCore.Identity;

namespace StudentManagementSystem.Models
{
    public class EmployeeAccount : BaseEntity
    {
        public int EmployeeId { get; set; }
        //public bool IsActive { get; set; } = true;
        //public new DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property - make it nullable
        public virtual Employee? Employee { get; set; }
    }
}