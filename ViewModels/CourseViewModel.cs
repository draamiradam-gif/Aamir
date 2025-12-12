public class CourseViewModel
{
    public int Id { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Credits { get; set; }
    public string Department { get; set; } = string.Empty;
    public int? SemesterId { get; set; }
    public decimal? MinGPA { get; set; }
    public int? MinPassedHours { get; set; }
    public int MaxStudents { get; set; }
    public int CurrentEnrollment { get; set; }
    public bool HasAvailableSeats => CurrentEnrollment < MaxStudents;
    public bool IsActive { get; set; }

    // For display
    public string? SemesterName { get; set; }
    public string? DepartmentName { get; set; }
    public List<string> Prerequisites { get; set; } = new List<string>();
}