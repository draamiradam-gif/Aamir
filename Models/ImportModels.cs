using System.Collections.Generic;

namespace StudentManagementSystem.Models
{
    public class ImportResult : BaseEntity
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int TotalRows { get; set; }
        public int ImportedCount { get; set; }
        public int ErrorCount { get; set; }
        public List<string> Headers { get; set; } = new List<string>();
        public List<Dictionary<string, object>> PreviewData { get; set; } = new List<Dictionary<string, object>>();
        public List<string> Errors { get; set; } = new List<string>();
        public List<Student> ValidStudents { get; set; } = new List<Student>();

        // ADD THESE MISSING PROPERTIES:
        public List<InvalidStudent> InvalidStudents { get; set; } = new List<InvalidStudent>();
    }

    // ADD THIS CLASS FOR INVALID STUDENTS
    public class InvalidStudent
    {
        public int RowNumber { get; set; }
        public string? StudentId { get; set; }
        public string? Name { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, object>? RowData { get; set; }
    }

    public class ImportSettings
    {
        public bool OverrideExisting { get; set; } = false;
        public DuplicateHandling DuplicateHandling { get; set; } = DuplicateHandling.Skip;
        public bool UpdateExisting { get; set; } = true;
        public bool SkipErrors { get; set; } = true;
        public string DateFormat { get; set; } = "yyyy-MM-dd";
    }

    public enum DuplicateHandling
    {
        Skip,           // Skip duplicates
        Override,       // Override existing
        CreateNew       // Create new records
    }


}