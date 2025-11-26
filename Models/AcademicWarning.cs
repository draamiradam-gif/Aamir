namespace StudentManagementSystem.Models
{
    public class AcademicWarning
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public decimal CurrentGPA { get; set; }
        public decimal RequiredGPA { get; set; }
        public string WarningType { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public DateTime GeneratedDate { get; set; } = DateTime.Now;
    }
}