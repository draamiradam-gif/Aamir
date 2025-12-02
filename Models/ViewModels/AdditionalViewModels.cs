using System.ComponentModel.DataAnnotations;

namespace StudentManagementSystem.Models
{
    public class Setup2FAViewModel
    {
        public string AdminId { get; set; } = string.Empty;
        public string AuthenticatorKey { get; set; } = string.Empty;
        public string QRCodeUrl { get; set; } = string.Empty;
    }

    public class ApproveApplicationRequest
    {
        [Required]
        public int ApplicationId { get; set; }

        [Required]
        public ApplicationStatus Status { get; set; }

        public string? ReviewNotes { get; set; }

        public bool SendWelcomeEmail { get; set; } = true;
    }

    public class SystemSettingsViewModel
    {
        [Display(Name = "Admin Session Timeout (minutes)")]
        [Range(5, 480)]
        public int AdminSessionTimeout { get; set; } = 60;

        [Display(Name = "Max Login Attempts")]
        [Range(1, 10)]
        public int MaxLoginAttempts { get; set; } = 5;

        [Display(Name = "Enable Two-Factor Authentication")]
        public bool Enable2FA { get; set; } = true;

        [Display(Name = "Require Strong Passwords")]
        public bool RequireStrongPasswords { get; set; } = true;

        [Display(Name = "Auto Logout Inactive Admins")]
        public bool AutoLogoutInactive { get; set; } = true;

        [Display(Name = "Email Notifications")]
        public bool EmailNotifications { get; set; } = true;

        [Display(Name = "Audit Log Retention (days)")]
        [Range(1, 3650)]
        public int AuditLogRetentionDays { get; set; } = 365;
    }

    public class EmailTemplateViewModel
    {
        public int Id { get; set; }

        [Required]
        public string TemplateType { get; set; } = string.Empty;

        [Required]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Body { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
        public DateTime LastModified { get; set; } = DateTime.Now;
    }

    


}