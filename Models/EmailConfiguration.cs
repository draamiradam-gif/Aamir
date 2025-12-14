// Models/EmailConfiguration.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace StudentManagementSystem.Models
{
    public class EmailConfiguration
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string SmtpServer { get; set; } = string.Empty;

        [Required]
        public int SmtpPort { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string SenderEmail { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string SenderName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty; // Encrypted in real app

        public bool ShowSenderEmail { get; set; } = true;
        public bool BccSystemEmail { get; set; } = false;

        [EmailAddress]
        [StringLength(100)]
        public string? SystemBccEmail { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime UpdatedDate { get; set; } = DateTime.Now;
        public string? UpdatedBy { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;
    }
}