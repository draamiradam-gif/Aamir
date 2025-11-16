namespace StudentManagementSystem.Models
{
    public class InvalidCourse
    {
        public int RowNumber { get; set; }
        public string? CourseCode { get; set; }
        public string? CourseName { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, object>? RowData { get; set; }
    }
}