using Microsoft.AspNetCore.Identity;

namespace StudentManagementSystem.Models
{
    public class ApplicationRole : IdentityRole
    {
        // Add custom properties here for full control
        public string? Description { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public string? CreatedBy { get; set; }
        public bool IsSystemRole { get; set; } = false;
        public int PermissionLevel { get; set; } = 0;

        // You can add navigation properties if needed
        // public virtual ICollection<RolePermission>? RolePermissions { get; set; }
    }
}