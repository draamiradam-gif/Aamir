using Microsoft.Extensions.Logging;
using StudentManagementSystem.Data;
using System.Threading.Tasks;

namespace StudentManagementSystem.Services
{
    // Interface
    public interface ISecurityService
    {
        Task<bool> IsIPAllowed(string ipAddress, string adminId);
        Task LogAccessAttempt(string ipAddress, string adminId, bool success);
    }

    // Implementation
    public class SecurityService : ISecurityService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SecurityService> _logger;

        public SecurityService(ApplicationDbContext context, ILogger<SecurityService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> IsIPAllowed(string ipAddress, string adminId)
        {
            await Task.Delay(10);
            _logger.LogInformation($"Checking IP {ipAddress} for admin {adminId}");
            return true;
        }

        public async Task LogAccessAttempt(string ipAddress, string adminId, bool success)
        {
            await Task.Delay(10);
            _logger.LogInformation($"Access attempt from {ipAddress} for admin {adminId}: {(success ? "SUCCESS" : "FAILED")}");
        }
    }
}