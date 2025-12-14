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
        private readonly IEmailConfigurationService _configService;
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;

        //public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
        //{
        //    _emailSettings = emailSettings.Value;
        //    _logger = logger;
        //}

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger, IEmailConfigurationService configService)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;

            // Log what settings we have
            _logger.LogInformation($"EmailService initialized with:");
            _logger.LogInformation($"  Server: {_emailSettings.SmtpServer}:{_emailSettings.SmtpPort}");
            _logger.LogInformation($"  From: {_emailSettings.SenderEmail}");
            _logger.LogInformation($"  Username: {_emailSettings.Username}");
            _logger.LogInformation($"  Password length: {_emailSettings.Password?.Length ?? 0}");
            _configService = configService;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            await SendEmailAsync(toEmail, subject, htmlMessage, null);
        }

        //public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage, List<string>? attachments)
        //{
        //    try
        //    {
        //        // Create message
        //        var message = new MimeMessage();
        //        message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
        //        message.To.Add(new MailboxAddress("", toEmail));
        //        message.Subject = subject;

        //        // Create multipart message
        //        var multipart = new Multipart("mixed");

        //        // Add HTML body
        //        var htmlPart = new TextPart(TextFormat.Html)
        //        {
        //            Text = htmlMessage
        //        };
        //        multipart.Add(htmlPart);

        //        // Add text alternative
        //        var textPart = new TextPart(TextFormat.Plain)
        //        {
        //            Text = StripHtmlTags(htmlMessage)
        //        };
        //        multipart.Add(textPart);

        //        // Add attachments if any
        //        if (attachments != null && attachments.Any())
        //        {
        //            foreach (var attachmentPath in attachments)
        //            {
        //                if (File.Exists(attachmentPath))
        //                {
        //                    var attachment = new MimePart()
        //                    {
        //                        Content = new MimeContent(File.OpenRead(attachmentPath)),
        //                        ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
        //                        ContentTransferEncoding = ContentEncoding.Base64,
        //                        FileName = Path.GetFileName(attachmentPath)
        //                    };
        //                    multipart.Add(attachment);
        //                }
        //            }
        //        }

        //        message.Body = multipart;

        //        // Send email
        //        using var client = new SmtpClient();

        //        await client.ConnectAsync(
        //            _emailSettings.SmtpServer,
        //            _emailSettings.SmtpPort,
        //            SecureSocketOptions.StartTls);

        //        await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password);
        //        await client.SendAsync(message);
        //        await client.DisconnectAsync(true);

        //        _logger.LogInformation($"✅ Email sent successfully to: {toEmail}");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, $"❌ Failed to send email to {toEmail}");
        //        throw new Exception($"Failed to send email: {ex.Message}", ex);
        //    }
        //}

        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage, List<string>? attachments)
        {
            try
            {
                // Get active configuration from database
                var config = await _configService.GetActiveConfigurationAsync();

                if (config == null)
                {
                    throw new InvalidOperationException("No active email configuration found.");
                }

                // Validate required fields
                if (string.IsNullOrEmpty(config.SmtpServer) ||
                    string.IsNullOrEmpty(config.Username) ||
                    string.IsNullOrEmpty(config.Password))
                {
                    throw new InvalidOperationException("Email configuration is incomplete.");
                }

                // Create message
                var message = new MimeMessage();

                // Set From - always include email for SMTP, display name controls visibility
                message.From.Add(new MailboxAddress(config.SenderName, config.SenderEmail));

                message.To.Add(new MailboxAddress("", toEmail));
                message.Subject = subject;

                // Add BCC to system email if enabled
                if (config.BccSystemEmail &&
                    !string.IsNullOrEmpty(config.SystemBccEmail) &&
                    !toEmail.Equals(config.SystemBccEmail, StringComparison.OrdinalIgnoreCase))
                {
                    message.Bcc.Add(new MailboxAddress("", config.SystemBccEmail));
                    _logger.LogInformation($"📧 Added BCC to system email: {config.SystemBccEmail}");
                }

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

                // Log email details
                _logger.LogInformation($"📧 Sending email to: {toEmail}");
                _logger.LogInformation($"📧 From display: {config.SenderName}");
                _logger.LogInformation($"📧 From email (SMTP): {config.SenderEmail}");
                _logger.LogInformation($"📧 Email privacy: {(config.ShowSenderEmail ? "Visible" : "Hidden")}");
                if (config.BccSystemEmail && !string.IsNullOrEmpty(config.SystemBccEmail))
                {
                    _logger.LogInformation($"📧 BCC to system: {config.SystemBccEmail}");
                }

                // Send email
                using var client = new SmtpClient();
                await client.ConnectAsync(
                    config.SmtpServer,
                    config.SmtpPort,
                    SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(config.Username, config.Password);
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