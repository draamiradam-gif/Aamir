namespace StudentManagementSystem.Models
{
    public class EmailSettings
    {
        public string SmtpServer { get; set; } = "";
        public int SmtpPort { get; set; } = 587;
        public bool UseSSL { get; set; } = true;
        public string SenderEmail { get; set; } = "";
        public string SenderName { get; set; } = "";
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public bool EnableNotifications { get; set; } = true;

        public bool ShowSenderEmail { get; set; } = true; // Default: show email
        public bool BccSystemEmail { get; set; } = false; // Default: don't BCC
        public string SystemBccEmail { get; set; } = string.Empty;
    }
}