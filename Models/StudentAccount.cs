using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementSystem.Models
{
    public class StudentAccount : BaseEntity
    {
        public int StudentId { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "اسم المستخدم")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [Display(Name = "كلمة المرور")]
        public string PasswordHash { get; set; } = string.Empty;

        [Display(Name = "الحساب مغلق")]
        public bool IsBlocked { get; set; }

        [Display(Name = "آخر تسجيل دخول")]
        public DateTime? LastLogin { get; set; }

        // Remove FailedLoginAttempts for now to fix the immediate issue
        // [Display(Name = "عدد محاولات التسجيل الفاشلة")]
        // public int FailedLoginAttempts { get; set; }

        // Navigation property
        [ForeignKey("StudentId")]
        public virtual Student? Student { get; set; }
    }
}