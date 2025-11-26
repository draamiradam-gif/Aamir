using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementSystem.Models
{
    public class GradingTemplate : BaseEntity
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        public bool IsDefault { get; set; } = false;

        // ✅ ADD THIS PROPERTY
        public bool IsActive { get; set; } = true;

        public string Department { get; set; } = "General";

        public virtual ICollection<GradingTemplateItem> Items { get; set; } = new List<GradingTemplateItem>();

        [NotMapped]
        public decimal TotalWeight => Items.Sum(i => i.Weight);
    }

    public class GradingTemplateItem : BaseEntity
    {
        public int GradingTemplateId { get; set; }
        public int EvaluationTypeId { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Range(0, 100)]
        public decimal Weight { get; set; }

        [Range(0, 100)]
        public decimal MaxScore { get; set; } = 100;

        public int Order { get; set; }

        public bool IsRequired { get; set; } = true;

        // Navigation properties
        [ForeignKey("GradingTemplateId")]
        public virtual GradingTemplate? GradingTemplate { get; set; }

        [ForeignKey("EvaluationTypeId")]
        public virtual EvaluationType? EvaluationType { get; set; }
    }
}