using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Data;
using StudentManagementSystem.Models;

namespace StudentManagementSystem.Data
{
    public static class EnrollmentDataSeeder
    {
        public static void SeedEnrollmentData(ApplicationDbContext context)
        {
            Console.WriteLine("Starting enrollment data seeding...");

            // Update existing CourseEnrollment records with default values
            var existingEnrollments = context.CourseEnrollments
                .Where(ce => ce.EnrollmentStatus == 0) // Default enum value
                .ToList();

            int updatedCount = 0;

            foreach (var enrollment in existingEnrollments)
            {
                // Set EnrollmentStatus based on existing data
                if (enrollment.IsActive)
                {
                    enrollment.EnrollmentStatus = EnrollmentStatus.Active;
                }
                else if (enrollment.GradeStatus == GradeStatus.Withdrawn)
                {
                    enrollment.EnrollmentStatus = EnrollmentStatus.Withdrawn;
                }
                else if (enrollment.GradeStatus == GradeStatus.Completed || enrollment.GradeStatus == GradeStatus.Failed)
                {
                    enrollment.EnrollmentStatus = EnrollmentStatus.Completed;
                }
                else
                {
                    enrollment.EnrollmentStatus = EnrollmentStatus.Dropped;
                }

                // Set other default values
                enrollment.EnrollmentType = EnrollmentType.Regular;
                enrollment.EnrollmentMethod = EnrollmentMethod.Web;
                enrollment.LastActivityDate = enrollment.ModifiedDate ?? enrollment.CreatedDate;

                // Set default approval
                if (string.IsNullOrEmpty(enrollment.ApprovedBy))
                {
                    enrollment.ApprovedBy = "System";
                    enrollment.ApprovalDate = enrollment.CreatedDate;
                }

                updatedCount++;
            }

            context.SaveChanges();
            Console.WriteLine($"Successfully updated {updatedCount} enrollment records.");
        }
    }
}