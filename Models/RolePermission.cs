using Microsoft.AspNetCore.Identity;

namespace StudentManagementSystem.Models
{
    public class RolePermission
    {
        public int Id { get; set; }
        public string RoleId { get; set; } = string.Empty;
        public int PermissionId { get; set; }

        // Navigation properties
        //public virtual ApplicationRole Role { get; set; } = null!;
        public virtual IdentityRole Role { get; set; } = null!;
        public virtual Permission Permission { get; set; } = null!;
    }
}