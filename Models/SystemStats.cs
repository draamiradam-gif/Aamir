using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentManagementSystem.Models
{
    public class SystemStats : BaseEntity
    {
        public int TotalEnrollments { get; set; }
        public int TotalEligibilityChecks { get; set; }
        public int TotalBulkEnrollments { get; set; }
        public int TotalWaitlistEntries { get; set; }

        public DateTime LastEnrollment { get; set; }
        public DateTime LastEligibilityCheck { get; set; }
        public DateTime LastBulkEnrollment { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal AverageEnrollmentRate { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal AverageEligibilityRate { get; set; }

        public string? MostPopularCourse { get; set; }
        public int MostPopularCourseCount { get; set; }

        [StringLength(50)]
        public string? BusiestDay { get; set; }
        public int PeakHour { get; set; }

        // Daily statistics
        public int TodayEnrollments { get; set; }
        public int TodayEligibilityChecks { get; set; }
        public DateTime LastResetDate { get; set; } = DateTime.Today;

        public void IncrementEligibilityChecks()
        {
            TotalEligibilityChecks++;
            TodayEligibilityChecks++;
            LastEligibilityCheck = DateTime.Now;

            // Reset daily stats if it's a new day
            if (DateTime.Today > LastResetDate)
            {
                TodayEnrollments = 0;
                TodayEligibilityChecks = 0;
                LastResetDate = DateTime.Today;
            }
        }

        public void IncrementEnrollments()
        {
            TotalEnrollments++;
            TodayEnrollments++;
            LastEnrollment = DateTime.Now;

            if (DateTime.Today > LastResetDate)
            {
                TodayEnrollments = 1; // Start with 1 since we just added
                TodayEligibilityChecks = 0;
                LastResetDate = DateTime.Today;
            }
        }
    }
}