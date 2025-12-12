// Services/SendGridEmailService.cs
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using StudentManagementSystem.Models;

namespace StudentManagementSystem.Services
{
    public class SendGridEmailService : IEmailService
    {
        private readonly SendGridSettings _sendGridSettings;
        private readonly ILogger<SendGridEmailService> _logger;

        public SendGridEmailService(IOptions<SendGridSettings> sendGridSettings, ILogger<SendGridEmailService> logger)
        {
            _sendGridSettings = sendGridSettings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            await SendEmailAsync(toEmail, subject, htmlMessage, null);
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage, List<string>? attachments)
        {
            try
            {
                var client = new SendGridClient(_sendGridSettings.ApiKey);
                var from = new EmailAddress(_sendGridSettings.SenderEmail, _sendGridSettings.SenderName);
                var to = new EmailAddress(toEmail);

                var msg = MailHelper.CreateSingleEmail(from, to, subject, "", htmlMessage);

                // Add attachments if any
                if (attachments != null && attachments.Any())
                {
                    foreach (var attachmentPath in attachments)
                    {
                        if (File.Exists(attachmentPath))
                        {
                            var bytes = await File.ReadAllBytesAsync(attachmentPath);
                            var file = Convert.ToBase64String(bytes);
                            msg.AddAttachment(Path.GetFileName(attachmentPath), file);
                        }
                    }
                }

                var response = await client.SendEmailAsync(msg);

                if (response.StatusCode == System.Net.HttpStatusCode.Accepted)
                {
                    _logger.LogInformation($"✅ Email sent successfully to {toEmail}");
                }
                else
                {
                    var responseBody = await response.Body.ReadAsStringAsync();
                    _logger.LogError($"❌ Failed to send email to {toEmail}. Status: {response.StatusCode}, Response: {responseBody}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Failed to send email to {toEmail}");
                throw;
            }
        }
    }

    public class SendGridSettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string SenderEmail { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
    }
}