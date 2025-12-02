namespace StudentManagementSystem.Models
{
    public class ManageComponentsViewModel : BaseEntity
    {
        public Course? Course { get; set; }
        public List<GradingComponent> Components { get; set; } = new List<GradingComponent>();
        public List<GradingTemplate> AvailableTemplates { get; set; } = new List<GradingTemplate>();
        public decimal TotalWeight { get; set; }

        // Add these properties for the form
        public GradingComponent? EditingComponent { get; set; }
        public bool IsValidWeight => Math.Abs(TotalWeight - 100) < 0.01m;
        public string? WeightStatus => IsValidWeight ? "Valid" : "Invalid - Total must equal 100%";
        public string WeightStatusClass => IsValidWeight ? "text-success" : "text-danger";
    }
}
