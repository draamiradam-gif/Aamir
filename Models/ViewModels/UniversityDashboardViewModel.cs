using System.Collections.Generic;

namespace StudentManagementSystem.Models.ViewModels
{
    public class UniversityDashboardViewModel
    {
        public List<University> Universities { get; set; } = new();
        public int TotalUniversities { get; set; }
        public int TotalColleges { get; set; }
        public int TotalStudents { get; set; }
        public int TotalCourses { get; set; }
        public int ActiveSemesters { get; set; }
        public University? University { get; set; }
        public int TotalDepartments { get; set; }
    }
}