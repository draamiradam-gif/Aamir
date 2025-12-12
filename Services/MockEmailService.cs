// Services/MockEmailService.cs
namespace StudentManagementSystem.Services
{
    public class MockEmailService : IEmailService
    {
        private readonly ILogger<MockEmailService> _logger;
        private readonly IWebHostEnvironment _env;

        public MockEmailService(ILogger<MockEmailService> logger, IWebHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            await SendEmailAsync(toEmail, subject, htmlMessage, null);
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage, List<string>? attachments)
        {
            // Create logs directory if it doesn't exist
            var logsDir = Path.Combine(_env.ContentRootPath, "EmailLogs");
            Directory.CreateDirectory(logsDir);

            // Generate filename with timestamp
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var logFile = Path.Combine(logsDir, $"email_{timestamp}.html");

            // Create HTML log file
            var logContent = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <title>Email Log - {subject}</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        .header {{ background: #f0f0f0; padding: 15px; border-radius: 5px; margin-bottom: 20px; }}
        .info {{ margin-bottom: 10px; }}
        .label {{ font-weight: bold; color: #555; }}
        .body {{ border: 1px solid #ddd; padding: 20px; margin-top: 20px; }}
        .attachments {{ margin-top: 20px; padding: 10px; background: #f9f9f9; border-radius: 5px; }}
    </style>
</head>
<body>
    <div class='header'>
        <h2>📧 Email Sent (Mock)</h2>
        <div class='info'><span class='label'>Date:</span> {DateTime.Now}</div>
        <div class='info'><span class='label'>To:</span> {toEmail}</div>
        <div class='info'><span class='label'>Subject:</span> {subject}</div>
        <div class='info'><span class='label'>Attachments:</span> {(attachments?.Count ?? 0)} files</div>
    </div>
    <div class='body'>
        {htmlMessage}
    </div>
    {(attachments?.Any() == true ? $@"
    <div class='attachments'>
        <h3>Attachments ({attachments.Count}):</h3>
        <ul>
            {string.Join("", attachments.Select(a => $"<li>{Path.GetFileName(a)}</li>"))}
        </ul>
    </div>
    " : "")}
</body>
</html>";

            await File.WriteAllTextAsync(logFile, logContent);

            // Log to console
            _logger.LogInformation($"📧 MOCK EMAIL LOGGED:");
            _logger.LogInformation($"   To: {toEmail}");
            _logger.LogInformation($"   Subject: {subject}");
            _logger.LogInformation($"   Saved to: {logFile}");

            if (attachments?.Any() == true)
            {
                _logger.LogInformation($"   Attachments: {string.Join(", ", attachments.Select(Path.GetFileName))}");
            }

            await Task.CompletedTask;
        }
    }
}