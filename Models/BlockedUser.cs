// Models/BlockedUser.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace StudentManagementSystem.Models
{
    public class BlockedUser
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [StringLength(100)]
        public string? UserName { get; set; }

        [Required]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;

        [Required]
        public string BlockedBy { get; set; } = string.Empty;

        public DateTime BlockedDate { get; set; } = DateTime.UtcNow;

        public DateTime? UnblockedDate { get; set; }

        [StringLength(100)]
        public string? UnblockedBy { get; set; }

        [StringLength(500)]
        public string? UnblockReason { get; set; }

        public bool IsActive { get; set; } = true;
    }
}