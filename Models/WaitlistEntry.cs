using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementSystem.Models 
{
    public class WaitlistEntry : BaseEntity
    {
        public int StudentId { get; set; }
        public int CourseId { get; set; }
        public int SemesterId { get; set; }
        public int Position { get; set; }
        public DateTime AddedDate { get; set; } = DateTime.Now;
        public DateTime? NotifiedDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        //public bool IsActive { get; set; } = true;

        // Navigation properties
        [ForeignKey("StudentId")]
        public virtual Student? Student { get; set; }

        [ForeignKey("CourseId")]
        public virtual Course? Course { get; set; }

        [ForeignKey("SemesterId")]
        public virtual Semester? Semester { get; set; }
    }
}