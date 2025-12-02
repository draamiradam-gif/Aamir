using StudentManagementSystem.Models;

namespace StudentManagementSystem.ViewModels
{
    public class StudentPortalViewModel
    {
        public Student? Student { get; set; }
        public Semester? CurrentSemester { get; set; }
        public StudentEligibility Eligibility { get; set; } = new StudentEligibility();
        public List<CourseRegistration> CurrentRegistrations { get; set; } = new List<CourseRegistration>();
        public List<CourseEligibility> EligibleCourses { get; set; } = new List<CourseEligibility>();
        public List<RegistrationPeriod> ActivePeriods { get; set; } = new List<RegistrationPeriod>();
    }

    public class RegistrationManagementViewModel
    {
        public List<Semester> Semesters { get; set; } = new List<Semester>();
        public Semester? SelectedSemester { get; set; }
        public List<CourseRegistration> Registrations { get; set; } = new List<CourseRegistration>();
        public int PendingCount { get; set; }
        public int ApprovedCount { get; set; }
        public int TotalCount { get; set; }
        public string CurrentFilter { get; set; } = "all";
        public string CurrentSort { get; set; } = "date";
        public string CurrentSortOrder { get; set; } = "desc";
    }

    public class RulesManagementViewModel
    {
        public List<RegistrationRule> Rules { get; set; } = new List<RegistrationRule>();
        public List<Department> Departments { get; set; } = new List<Department>();
    }

    public class PeriodsManagementViewModel
    {
        public List<RegistrationPeriod> Periods { get; set; } = new List<RegistrationPeriod>();
        public List<Semester> Semesters { get; set; } = new List<Semester>();
    }

    public class RegistrationReportViewModel
    {
        public List<Semester> Semesters { get; set; } = new List<Semester>();
        public Semester? SelectedSemester { get; set; }
        public RegistrationReportData ReportData { get; set; } = new RegistrationReportData();
    }

    public class StudentRegistrationHistoryViewModel
    {
        public Student Student { get; set; } = new Student();
        public List<CourseRegistration> RegistrationHistory { get; set; } = new List<CourseRegistration>();
    }
}