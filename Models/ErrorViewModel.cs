namespace StudentManagementSystem.Models
{
    public class ErrorViewModel : BaseEntity
    {
        public string? RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        public string? ErrorMessage { get; set; }
        public string? StackTrace { get; set; }
        public int? StatusCode { get; set; }
    }
}