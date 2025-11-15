using System.Collections.Generic;

namespace StudentManagementSystem.Models.ViewModels
{
    public class UniversityHierarchyViewModel
    {
        public List<University> Universities { get; set; } = new();
        public int TotalUniversities { get; set; }
        public int TotalColleges { get; set; }
        public int TotalDepartments { get; set; }
        public int TotalBranches { get; set; }
        public int TotalSemesters { get; set; }
        public int TotalStudents { get; set; }
        public int TotalCourses { get; set; }
        public string AccessScope { get; set; } = "all"; // all, university, college, department
    }
}