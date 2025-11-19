using StudentManagementSystem.Models;

public class ImportResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public List<string> Headers { get; set; } = new List<string>();
    public List<Course> ValidCourses { get; set; } = new List<Course>();
    public List<InvalidCourse> InvalidCourses { get; set; } = new List<InvalidCourse>();
    public List<Dictionary<string, object>> PreviewData { get; set; } = new List<Dictionary<string, object>>();
    public List<string> Errors { get; set; } = new List<string>();
    public int ImportedCount { get; set; }
    public int ErrorCount { get; set; }
    public List<Student> ValidStudents { get; set; } = new List<Student>();
    public List<InvalidStudent> InvalidStudents { get; set; } = new List<InvalidStudent>();


}

public class InvalidCourse
{
    public int RowNumber { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public Dictionary<string, object> RowData { get; set; } = new Dictionary<string, object>();

    // Course properties
    public List<Course> ValidCourses { get; set; } = new List<Course>();
    public List<InvalidCourse> InvalidCourses { get; set; } = new List<InvalidCourse>();

    public List<Dictionary<string, object>> PreviewData { get; set; } = new List<Dictionary<string, object>>();
    public List<string> Errors { get; set; } = new List<string>();
    public int ImportedCount { get; set; }
    public int ErrorCount { get; set; }

}

public class ImportSettings
{
    public bool UpdateExisting { get; set; } = true;
    public string DuplicateHandling { get; set; } = "Skip"; // Skip, Override, CreateNew
}

