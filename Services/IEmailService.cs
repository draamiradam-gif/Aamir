namespace StudentManagementSystem.Services
{
    public interface IEmailService
    {
        Task SendGradeNotificationAsync(int studentId, int courseId, decimal grade);
        Task SendAcademicWarningAsync(int studentId, string warningType); // ✅ ADD THIS
        Task SendGradeRevisionStatusAsync(int revisionId, string status);
    }

    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;

        public EmailService(ILogger<EmailService> logger)
        {
            _logger = logger;
        }

        public async Task SendGradeNotificationAsync(int studentId, int courseId, decimal grade)
        {
            _logger.LogInformation($"Grade notification sent for student {studentId}, course {courseId}, grade {grade}");
            await Task.CompletedTask;
        }

        public async Task SendAcademicWarningAsync(int studentId, string warningType)
        {
            _logger.LogInformation($"Academic warning sent for student {studentId}: {warningType}");
            await Task.CompletedTask;
        }

        public async Task SendGradeRevisionStatusAsync(int revisionId, string status)
        {
            _logger.LogInformation($"Grade revision status update for revision {revisionId}: {status}");
            await Task.CompletedTask;
        }
    }
}