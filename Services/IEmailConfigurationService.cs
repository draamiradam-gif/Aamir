// Services/IEmailConfigurationService.cs
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace StudentManagementSystem.Services
{
    public interface IEmailConfigurationService
    {
        Task<EmailConfiguration> GetActiveConfigurationAsync();
        Task<EmailConfiguration> GetConfigurationByIdAsync(int id);
        Task<List<EmailConfiguration>> GetAllConfigurationsAsync();
        Task<bool> SaveConfigurationAsync(EmailConfiguration configuration, string updatedBy);
        Task<bool> SetActiveConfigurationAsync(int id, string updatedBy);
    }
}

// Services/EmailConfigurationService.cs


namespace StudentManagementSystem.Services
{
    public class EmailConfigurationService : IEmailConfigurationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EmailConfigurationService> _logger;

        public EmailConfigurationService(
            ApplicationDbContext context,
            ILogger<EmailConfigurationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<EmailConfiguration> GetActiveConfigurationAsync()
        {
            var config = await _context.EmailConfigurations
                .FirstOrDefaultAsync(e => e.IsActive);

            if (config == null)
            {
                return await CreateDefaultConfigurationAsync();
            }

            return config;
        }

        //public async Task<EmailConfiguration?> GetConfigurationByIdAsync(int id)
        //{
        //    return await _context.EmailConfigurations.FindAsync(id);
        //}
        //// Or return default:
        //public async Task<EmailConfiguration?> GetConfigurationByIdAsync(int id)
        //{
        //    return await _context.EmailConfigurations.FindAsync(id);
        //}
        // Or with null check:
        public async Task<EmailConfiguration> GetConfigurationByIdAsync(int id)
        {
            var config = await _context.EmailConfigurations.FindAsync(id);

            // Return new config if not found
            return config ?? new EmailConfiguration
            {
                Id = 0,
                SmtpServer = "smtp.gmail.com",
                SmtpPort = 587,
                SenderEmail = "dr.aamir.adam@gmail.com",
                SenderName = "Student Management System",
                Username = "dr.aamir.adam@gmail.com",
                Password = "zwfi tiuv ghsr qdsj",
                ShowSenderEmail = true,
                BccSystemEmail = false,
                SystemBccEmail = "dr.aamir.adam@gmail.com",
                IsActive = false,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now,
                UpdatedBy = "System"
            };
        }

        public async Task<List<EmailConfiguration>> GetAllConfigurationsAsync()
        {
            return await _context.EmailConfigurations
                .OrderByDescending(e => e.IsActive)
                .ThenByDescending(e => e.UpdatedDate)
                .ToListAsync();
        }

        public async Task<bool> SaveConfigurationAsync(EmailConfiguration configuration, string updatedBy)
        {
            try
            {
                if (configuration.Id == 0)
                {
                    // New configuration
                    configuration.CreatedDate = DateTime.Now;
                    configuration.UpdatedDate = DateTime.Now;
                    configuration.UpdatedBy = updatedBy;

                    // If this is set to active, deactivate others
                    if (configuration.IsActive)
                    {
                        await DeactivateAllConfigurationsAsync();
                    }

                    _context.EmailConfigurations.Add(configuration);
                }
                else
                {
                    // Update existing
                    var existing = await _context.EmailConfigurations.FindAsync(configuration.Id);
                    if (existing == null)
                        return false;

                    // Update properties
                    existing.SmtpServer = configuration.SmtpServer;
                    existing.SmtpPort = configuration.SmtpPort;
                    existing.SenderEmail = configuration.SenderEmail;
                    existing.SenderName = configuration.SenderName;
                    existing.Username = configuration.Username;
                    existing.Password = configuration.Password;
                    existing.ShowSenderEmail = configuration.ShowSenderEmail;
                    existing.BccSystemEmail = configuration.BccSystemEmail;
                    existing.SystemBccEmail = configuration.SystemBccEmail;
                    existing.UpdatedDate = DateTime.Now;
                    existing.UpdatedBy = updatedBy;

                    // If activating this one, deactivate others
                    if (configuration.IsActive && !existing.IsActive)
                    {
                        await DeactivateAllConfigurationsAsync();
                    }

                    existing.IsActive = configuration.IsActive;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving email configuration");
                return false;
            }
        }

        public async Task<bool> SetActiveConfigurationAsync(int id, string updatedBy)
        {
            try
            {
                await DeactivateAllConfigurationsAsync();

                var config = await _context.EmailConfigurations.FindAsync(id);
                if (config == null)
                    return false;

                config.IsActive = true;
                config.UpdatedDate = DateTime.Now;
                config.UpdatedBy = updatedBy;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting active configuration");
                return false;
            }
        }

        private async Task DeactivateAllConfigurationsAsync()
        {
            var activeConfigs = await _context.EmailConfigurations
                .Where(e => e.IsActive)
                .ToListAsync();

            foreach (var config in activeConfigs)
            {
                config.IsActive = false;
            }
        }

        private async Task<EmailConfiguration> CreateDefaultConfigurationAsync()
        {
            try
            {
                var defaultConfig = new EmailConfiguration
                {
                    SmtpServer = "smtp.gmail.com",
                    SmtpPort = 587,
                    SenderEmail = "dr.aamir.adam@gmail.com",
                    SenderName = "Student Management System",
                    Username = "dr.aamir.adam@gmail.com",
                    Password = "zwfi tiuv ghsr qdsj",
                    ShowSenderEmail = true,
                    BccSystemEmail = false,
                    SystemBccEmail = "dr.aamir.adam@gmail.com",
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    UpdatedDate = DateTime.Now,
                    UpdatedBy = "System"
                };

                _context.EmailConfigurations.Add(defaultConfig);
                await _context.SaveChangesAsync();

                return defaultConfig;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating default configuration");

                // Return in-memory default if database fails
                return new EmailConfiguration
                {
                    SmtpServer = "smtp.gmail.com",
                    SmtpPort = 587,
                    SenderEmail = "dr.aamir.adam@gmail.com",
                    SenderName = "Student Management System",
                    Username = "dr.aamir.adam@gmail.com",
                    Password = "zwfi tiuv ghsr qdsj",
                    ShowSenderEmail = true,
                    BccSystemEmail = false,
                    SystemBccEmail = "dr.aamir.adam@gmail.com",
                    IsActive = true
                };
            }
        }
    }
}