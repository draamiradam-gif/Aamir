// Services/EmailService.cs
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
using StudentManagementSystem.Controllers;
using StudentManagementSystem.Models;

namespace StudentManagementSystem.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
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
                // Create message
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
                message.To.Add(new MailboxAddress("", toEmail));
                message.Subject = subject;

                // Create multipart message
                var multipart = new Multipart("mixed");

                // Add HTML body
                var htmlPart = new TextPart(TextFormat.Html)
                {
                    Text = htmlMessage
                };
                multipart.Add(htmlPart);

                // Add text alternative
                var textPart = new TextPart(TextFormat.Plain)
                {
                    Text = StripHtmlTags(htmlMessage)
                };
                multipart.Add(textPart);

                // Add attachments if any
                if (attachments != null && attachments.Any())
                {
                    foreach (var attachmentPath in attachments)
                    {
                        if (File.Exists(attachmentPath))
                        {
                            var attachment = new MimePart()
                            {
                                Content = new MimeContent(File.OpenRead(attachmentPath)),
                                ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                                ContentTransferEncoding = ContentEncoding.Base64,
                                FileName = Path.GetFileName(attachmentPath)
                            };
                            multipart.Add(attachment);
                        }
                    }
                }

                message.Body = multipart;

                // Send email
                using var client = new SmtpClient();

                await client.ConnectAsync(
                    _emailSettings.SmtpServer,
                    _emailSettings.SmtpPort,
                    SecureSocketOptions.StartTls);

                await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation($"✅ Email sent successfully to: {toEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Failed to send email to {toEmail}");
                throw new Exception($"Failed to send email: {ex.Message}", ex);
            }
        }

        private string StripHtmlTags(string html)
        {
            if (string.IsNullOrEmpty(html))
                return string.Empty;

            // Simple HTML tag removal
            var result = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]*>", "");
            result = System.Text.RegularExpressions.Regex.Replace(result, "&nbsp;", " ");
            result = System.Text.RegularExpressions.Regex.Replace(result, "&amp;", "&");
            result = System.Text.RegularExpressions.Regex.Replace(result, "&lt;", "<");
            result = System.Text.RegularExpressions.Regex.Replace(result, "&gt;", ">");

            // Decode common HTML entities
            result = System.Net.WebUtility.HtmlDecode(result);

            // Remove extra whitespace
            result = System.Text.RegularExpressions.Regex.Replace(result, @"\s+", " ");

            return result.Trim();
        }
    }
}