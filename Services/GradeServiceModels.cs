namespace StudentManagementSystem.Services
{
    public class CourseGradeSummary
    {
        public int CourseId { get; set; }
        public string CourseName { get; set; } = string.Empty;
        public int TotalStudents { get; set; }
        public int GradedStudents { get; set; }
        public decimal AverageGrade { get; set; }
        public decimal HighestGrade { get; set; }
        public decimal LowestGrade { get; set; }
        public Dictionary<string, int> GradeDistribution { get; set; } = new();
        public List<StudentCourseGrade> StudentGrades { get; set; } = new();
    }

    public class StudentCourseGrade
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentIdNumber { get; set; } = string.Empty;
        public decimal FinalGrade { get; set; }
        public string GradeLetter { get; set; } = string.Empty;
        public decimal GradePoints { get; set; }
        public Dictionary<string, decimal> ComponentGrades { get; set; } = new();
    }

    public class EvaluationStatistics
    {
        public int EvaluationId { get; set; }
        public string EvaluationName { get; set; } = string.Empty;
        public decimal AverageScore { get; set; }
        public decimal MedianScore { get; set; }
        public decimal StandardDeviation { get; set; }
        public decimal HighestScore { get; set; }
        public decimal LowestScore { get; set; }
        public int TotalSubmissions { get; set; }
        public int TotalStudents { get; set; }
        public decimal SubmissionRate => TotalStudents > 0 ? (TotalSubmissions * 100.0m) / TotalStudents : 0;
    }

    public class GradeDistribution
    {
        public int CourseId { get; set; }
        public Dictionary<string, int> Distribution { get; set; } = new();
        public decimal PassRate { get; set; }
        public decimal FailRate { get; set; }
        public int TotalStudents { get; set; }
    }


    ///
    //public class CourseGradeSummary
    //{
    //    public int CourseId { get; set; }
    //    public string CourseName { get; set; } = string.Empty;
    //    public int TotalStudents { get; set; }
    //    public int GradedStudents { get; set; }
    //    public decimal AverageGrade { get; set; }
    //    public decimal HighestGrade { get; set; }
    //    public decimal LowestGrade { get; set; }
    //    public Dictionary<string, int> GradeDistribution { get; set; } = new();
    //    public List<StudentCourseGrade> StudentGrades { get; set; } = new();
    //}

    //public class StudentCourseGrade
    //{
    //    public int StudentId { get; set; }
    //    public string StudentName { get; set; } = string.Empty;
    //    public string StudentIdNumber { get; set; } = string.Empty;
    //    public decimal FinalGrade { get; set; }
    //    public string GradeLetter { get; set; } = string.Empty;
    //    public decimal GradePoints { get; set; }
    //    public Dictionary<string, decimal> ComponentGrades { get; set; } = new();
    //}

    //public class EvaluationStatistics
    //{
    //    public int EvaluationId { get; set; }
    //    public string EvaluationName { get; set; } = string.Empty;
    //    public decimal AverageScore { get; set; }
    //    public decimal MedianScore { get; set; }
    //    public decimal StandardDeviation { get; set; }
    //    public decimal HighestScore { get; set; }
    //    public decimal LowestScore { get; set; }
    //    public int TotalSubmissions { get; set; }
    //    public int TotalStudents { get; set; }
    //    public decimal SubmissionRate => TotalStudents > 0 ? (TotalSubmissions * 100.0m) / TotalStudents : 0;
    //}

    //public class GradeDistribution
    //{
    //    public int CourseId { get; set; }
    //    public Dictionary<string, int> Distribution { get; set; } = new();
    //    public decimal PassRate { get; set; }
    //    public decimal FailRate { get; set; }
    //    public int TotalStudents { get; set; }
    //}


}
