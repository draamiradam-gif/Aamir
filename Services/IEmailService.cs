namespace StudentManagementSystem.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlMessage);
        Task SendEmailAsync(string toEmail, string subject, string htmlMessage, List<string>? attachments);
    }
}