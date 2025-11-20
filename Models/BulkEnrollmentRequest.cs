namespace StudentManagementSystem.Models
{
    public class BulkEnrollmentRequest
    {
        public int SemesterId { get; set; }
        public List<int> CourseIds { get; set; } = new List<int>();
        public List<int> StudentIds { get; set; } = new List<int>();
        public string SelectionType { get; set; } = "specific"; // all, eligible, specific
    }
}
