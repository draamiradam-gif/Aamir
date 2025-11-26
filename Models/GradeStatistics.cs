namespace StudentManagementSystem.Models
{
    public class GradeStatistics
    {
        public int TotalStudents { get; set; }
        public decimal AverageGrade { get; set; }
        public decimal HighestGrade { get; set; }
        public decimal LowestGrade { get; set; }
        public Dictionary<string, int> GradeDistribution { get; set; } = new Dictionary<string, int>();
    }
}