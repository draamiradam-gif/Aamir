using StudentManagementSystem.Models;

namespace StudentManagementSystem.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalStudents { get; set; }
        public int StudentsThisMonth { get; set; }
        public decimal AverageGPA { get; set; }
        public decimal AveragePercentage { get; set; }

        // Department statistics
        public List<DepartmentStats> DepartmentStatistics { get; set; } = new List<DepartmentStats>();

        // GPA distribution
        public List<GPAStats> GPAStatistics { get; set; } = new List<GPAStats>();

        // Recent students
        public List<Student> RecentStudents { get; set; } = new List<Student>();

        // Import/Export stats
        public int TotalImports { get; set; }
        public int TotalExports { get; set; }
    }

    public class DepartmentStats
    {
        public string DepartmentName { get; set; } = string.Empty;
        public int StudentCount { get; set; }
        public decimal Percentage { get; set; }
    }

    public class GPAStats
    {
        public string Range { get; set; } = string.Empty;
        public int StudentCount { get; set; }
        public string Color { get; set; } = string.Empty;
    }
}