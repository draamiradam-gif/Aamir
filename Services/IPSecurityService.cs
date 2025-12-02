namespace StudentManagementSystem.Services
{
    public interface IPSecurityService
    {
        Task<bool> IsIPAllowed(string ipAddress, string adminId);
        Task LogAccessAttempt(string ipAddress, string adminId, bool success);
    }
}