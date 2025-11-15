using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementSystem.Models
{
    public class FeePayment : BaseEntity
    {
        public int StudentId { get; set; }

        [Range(0, double.MaxValue)]
        [Column(TypeName = "decimal(10,2)")]  // This fixes the warning
        public decimal Amount { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        [StringLength(20)]
        public string Semester { get; set; } = string.Empty;

        [StringLength(50)]
        public string PaymentMethod { get; set; } = "Cash";

        [StringLength(100)]
        public string ReferenceNumber { get; set; } = string.Empty;

        public bool IsVerified { get; set; }

        public virtual Student? Student { get; set; }
    }
}