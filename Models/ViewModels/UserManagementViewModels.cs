using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace StudentManagementSystem.Models.ViewModels
{
    public class UserViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? UserName { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
    }

    public class CreateUserViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        // For template selection
        [Display(Name = "Template")]
        public int? TemplateId { get; set; }

        public List<AdminPrivilegeTemplate> AvailableTemplates { get; set; } = new List<AdminPrivilegeTemplate>();

        // For role selection
        public List<string> SelectedRoles { get; set; } = new List<string>();

        // For permission selection (if using template)
        public List<PermissionModule> SelectedPermissions { get; set; } = new List<PermissionModule>();

        // Select List for UI
        public SelectList TemplateList => new SelectList(AvailableTemplates, "Id", "TemplateName", TemplateId);
    }

    public class EditUserViewModel
    {
        public string Id { get; set; } = string.Empty; // Initialized
                

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Username is required")]
        [Display(Name = "Username")]
        public string UserName { get; set; } = string.Empty;

        public List<string> SelectedRoles { get; set; } = new List<string>(); // Initialized
        public List<IdentityRole>? AllRoles { get; set; } = new List<IdentityRole>(); // Initialized
    }

}