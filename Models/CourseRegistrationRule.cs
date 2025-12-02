namespace StudentManagementSystem.Models
{
    public class CourseRegistrationRule : BaseEntity
    {
        public string RuleName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string RuleType { get; set; } = string.Empty;

        //public bool IsActive { get; set; } = true; // Add 'new' keyword

        public string Condition { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
    }
}