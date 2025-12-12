using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace StudentManagementSystem.ViewModels
{
    public class EmailAnnouncementsViewModel
    {
        [Required(ErrorMessage = "Subject is required")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Message is required")]
        public string Message { get; set; } = string.Empty;

        public bool SendToEveryone { get; set; }
        public bool SendToAllAdmins { get; set; }
        public List<string> SelectedUsers { get; set; } = new List<string>();
        public List<string> CustomEmails { get; set; } = new List<string>();

        // For display only
        public int UserCount { get; set; }
        public int AdminCount { get; set; }
        public int TotalRecipients { get; set; }
    }
}
