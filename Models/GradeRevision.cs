using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementSystem.Models
{
    public class GradeRevision : BaseEntity
    {
        public int CourseEnrollmentId { get; set; }

        [Required]
        [StringLength(1000)]
        [Display(Name = "Revision Reason")]
        public string Reason { get; set; } = string.Empty;

        [Display(Name = "Requested Grade")]
        [Range(0, 100)]
        public decimal? RequestedGrade { get; set; }

        [Display(Name = "Current Grade")]
        [Range(0, 100)]
        public decimal CurrentGrade { get; set; }

        [Display(Name = "Status")]
        public GradeRevisionStatus Status { get; set; } = GradeRevisionStatus.Pending;

        [Display(Name = "Requested By")]
        [StringLength(100)]
        public string RequestedBy { get; set; } = string.Empty;

        [Display(Name = "Requested Date")]
        public DateTime RequestedDate { get; set; } = DateTime.Now;

        [Display(Name = "Reviewed By")]
        [StringLength(100)]
        public string? ReviewedBy { get; set; }

        [Display(Name = "Reviewed Date")]
        public DateTime? ReviewedDate { get; set; }

        [Display(Name = "Reviewer Comments")]
        [StringLength(2000)]
        public string? ReviewerComments { get; set; }

        [Display(Name = "Final Grade")]
        [Range(0, 100)]
        public decimal? FinalGrade { get; set; }

        // Navigation properties
        [ForeignKey("CourseEnrollmentId")]
        public virtual CourseEnrollment? CourseEnrollment { get; set; }
    }
}