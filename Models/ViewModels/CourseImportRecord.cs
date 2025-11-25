namespace StudentManagementSystem.Models.ViewModels
{
    public class CourseImportRecord
    {
        public string CourseCode { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Credits { get; set; } = 3;
        public string Department { get; set; } = "General";
        public int? SemesterId { get; set; }
        public int MaxStudents { get; set; } = 30;
        public decimal MinGPA { get; set; } = 2.0m;
        public int MinPassedHours { get; set; } = 0;

        // NEW: Prerequisites field
        public string Prerequisites { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
        public int RowNumber { get; set; }
        public int SerialNumber { get; set; }
    }
}
