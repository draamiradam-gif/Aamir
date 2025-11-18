using DocumentFormat.OpenXml.Bibliography;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StudentManagementSystem.Models;
using Department = StudentManagementSystem.Models.Department;

namespace StudentManagementSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Students
        public DbSet<Student> Students { get; set; }

        // Courses
        public DbSet<Course> Courses { get; set; }
        public DbSet<CourseEnrollment> CourseEnrollments { get; set; }
        public DbSet<CoursePrerequisite> CoursePrerequisites { get; set; }

        // University Structure
        public DbSet<University> Universities { get; set; } = null!; // FIXED LINE
        public DbSet<College> Colleges { get; set; } = null!;
        public DbSet<Department> Departments { get; set; } = null!;
        public DbSet<Branch> Branches { get; set; } = null!;
        public DbSet<Semester> Semesters { get; set; } = null!;


        // Other entities
        public DbSet<GradeScale> GradeScales { get; set; }
        public DbSet<QRCodeSession> QRCodeSessions { get; set; }
        public DbSet<QRAttendance> QRAttendances { get; set; }
       

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure Student entity
            builder.Entity<Student>(entity =>
            {
                entity.HasIndex(s => s.StudentId).IsUnique();
                entity.HasIndex(s => s.SeatNumber).IsUnique();
                entity.HasIndex(s => s.NationalId).IsUnique();

                entity.Property(s => s.StudentId).IsRequired().HasMaxLength(50);
                entity.Property(s => s.SeatNumber).IsRequired().HasMaxLength(50);
                entity.Property(s => s.Name).IsRequired().HasMaxLength(200);
                entity.Property(s => s.NationalId).IsRequired().HasMaxLength(14);
                entity.Property(s => s.Department).HasMaxLength(100);
                entity.Property(s => s.StudyLevel).HasMaxLength(50);
                entity.Property(s => s.Semester).HasMaxLength(50);
                entity.Property(s => s.Grade).HasMaxLength(50);
                entity.Property(s => s.Phone).HasMaxLength(15);
                entity.Property(s => s.Email).HasMaxLength(100);

                entity.Property(s => s.Percentage).HasColumnType("decimal(5,2)");
                entity.Property(s => s.GPA).HasColumnType("decimal(4,2)");

                // University structure relationships
                //entity.HasOne(s => s.DepartmentNavigation)
                //      .WithMany()
                //      .HasForeignKey(s => s.DepartmentId)
                //      .OnDelete(DeleteBehavior.Restrict)
                //      .IsRequired(false);

                //entity.HasOne(s => s.Branch)
                //      .WithMany()
                //      .HasForeignKey(s => s.BranchId)
                //      .OnDelete(DeleteBehavior.Restrict)
                //      .IsRequired(false);

                //entity.HasOne(s => s.SemesterNavigation)
                //      .WithMany()
                //      .HasForeignKey(s => s.SemesterId)
                //      .OnDelete(DeleteBehavior.Restrict)
                //      .IsRequired(false);
            });

            // Configure Course entity
            // Configure Course entity
            builder.Entity<Course>(entity =>
            {
                entity.HasIndex(c => c.CourseCode).IsUnique();

                entity.Property(c => c.CourseCode).IsRequired().HasMaxLength(20);
                entity.Property(c => c.CourseName).IsRequired().HasMaxLength(100);
                entity.Property(c => c.Description).HasMaxLength(5000);
                entity.Property(c => c.Department).IsRequired().HasMaxLength(50);

                entity.Property(c => c.Credits).HasDefaultValue(3);
                entity.Property(c => c.Semester).HasDefaultValue(1);
                entity.Property(c => c.IsActive).HasDefaultValue(true);
                entity.Property(c => c.MaxStudents).HasDefaultValue(1000);
                entity.Property(c => c.MinGPA).HasColumnType("decimal(4,2)").HasDefaultValue(2.0m);
                entity.Property(c => c.CourseSpecification)
                    .HasMaxLength(20000);

                entity.Property(c => c.Icon)
                    .HasMaxLength(100);
            });

            // Configure CourseEnrollment entity - USE CASCADE HERE
            builder.Entity<CourseEnrollment>(entity =>
            {
                entity.HasIndex(e => new { e.CourseId, e.StudentId, e.SemesterId })
          .HasDatabaseName("IX_CourseEnrollment_UniqueActive")
          .HasFilter("[IsActive] = 1")
          .IsUnique();

                entity.Property(e => e.Grade).HasColumnType("decimal(5,2)");
                entity.Property(e => e.GradeLetter).HasMaxLength(2);

                entity.Property(e => e.EnrollmentDate).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.IsActive).HasDefaultValue(true);

                entity.HasOne(e => e.Course)
                      .WithMany(c => c.CourseEnrollments)
                      .HasForeignKey(e => e.CourseId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Student)
                      .WithMany(s => s.CourseEnrollments)
                      .HasForeignKey(e => e.StudentId)
                      .OnDelete(DeleteBehavior.Restrict);

                // ADD THIS: Semester relationship
                entity.HasOne(e => e.Semester)
                      .WithMany() // Semester doesn't need CourseEnrollments navigation
                      .HasForeignKey(e => e.SemesterId)
                      .OnDelete(DeleteBehavior.Restrict);

                // FIX: Use the new HasCheckConstraint syntax
                entity.ToTable(tb => tb.HasCheckConstraint(
                    "CK_CourseEnrollment_Grade",
                    "[Grade] IS NULL OR ([Grade] >= 0 AND [Grade] <= 100)"
                ));
            });

            // Configure CoursePrerequisite entity - USE CLIENT CASCADE
            builder.Entity<CoursePrerequisite>(entity =>
            {
                entity.ToTable(tb => tb.HasCheckConstraint(
                    "CK_CoursePrerequisite_NotSelfReferencing",
                    "[CourseId] != [PrerequisiteCourseId]"
                ));

                entity.HasIndex(cp => new { cp.CourseId, cp.PrerequisiteCourseId })
                      .IsUnique();

                entity.Property(cp => cp.MinGrade).HasColumnType("decimal(5,2)");

                entity.HasOne(cp => cp.Course)
                      .WithMany(c => c.Prerequisites)
                      .HasForeignKey(cp => cp.CourseId)
                      .OnDelete(DeleteBehavior.Cascade); // CLIENT CASCADE for course side

                entity.HasOne(cp => cp.PrerequisiteCourse)
                      .WithMany(c => c.RequiredFor)
                      .HasForeignKey(cp => cp.PrerequisiteCourseId)
                      .OnDelete(DeleteBehavior.Restrict); // RESTRICT for prerequisite side
            });

            // Institution Configuration
            // University Configuration
            builder.Entity<University>(entity =>
            {
                entity.HasIndex(u => u.Name).IsUnique();
                entity.HasIndex(u => u.Code).IsUnique();
                entity.Property(u => u.Name).IsRequired().HasMaxLength(200);
                entity.Property(u => u.Code).IsRequired().HasMaxLength(20);
            });

            // College Configuration
            builder.Entity<College>(entity =>
            {
                entity.HasIndex(c => new { c.UniversityId, c.Name }).IsUnique();
                entity.HasIndex(c => c.CollegeCode).IsUnique();

                entity.HasOne(c => c.University)
                      .WithMany(u => u.Colleges)
                      .HasForeignKey(c => c.UniversityId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Department Configuration
            builder.Entity<Department>(entity =>
            {
                entity.HasIndex(d => d.DepartmentCode).IsUnique();

                entity.HasOne(d => d.College)
                      .WithMany(c => c.Departments)
                      .HasForeignKey(d => d.CollegeId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Branch Configuration
            builder.Entity<Branch>(entity =>
            {
                entity.HasIndex(b => new { b.DepartmentId, b.Name }).IsUnique();

                entity.HasOne(b => b.Department)
                      .WithMany(d => d.Branches)
                      .HasForeignKey(b => b.DepartmentId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(b => b.ParentBranch)
                      .WithMany(b => b.SubBranches)
                      .HasForeignKey(b => b.ParentBranchId)
                      .OnDelete(DeleteBehavior.Restrict)
                      .IsRequired(false);
            });

            // Semester Configuration
            builder.Entity<Semester>(entity =>
            {
                entity.HasIndex(s => new { s.DepartmentId, s.BranchId, s.SubBranchId, s.Name }).IsUnique();

                entity.HasOne(s => s.Department)
                      .WithMany(d => d.Semesters)
                      .HasForeignKey(s => s.DepartmentId)
                      .OnDelete(DeleteBehavior.Restrict)
                      .IsRequired(false);

                entity.HasOne(s => s.Branch)
                      .WithMany(b => b.BranchSemesters)
                      .HasForeignKey(s => s.BranchId)
                      .OnDelete(DeleteBehavior.Restrict)
                      .IsRequired(false);

                entity.HasOne(s => s.SubBranch)
                      .WithMany(b => b.SubBranchSemesters) // Updated this line
                      .HasForeignKey(s => s.SubBranchId)
                      .OnDelete(DeleteBehavior.Restrict)
                      .IsRequired(false);

                entity.ToTable(tb => tb.HasCheckConstraint(
                    "CK_Semester_Parent",
                    "([DepartmentId] IS NOT NULL AND [BranchId] IS NULL AND [SubBranchId] IS NULL) OR " +
                    "([DepartmentId] IS NULL AND [BranchId] IS NOT NULL AND [SubBranchId] IS NULL) OR " +
                    "([DepartmentId] IS NULL AND [BranchId] IS NULL AND [SubBranchId] IS NOT NULL) OR " +
                    "([DepartmentId] IS NULL AND [BranchId] IS NULL AND [SubBranchId] IS NULL)" // Allow no parent
                ));

                entity.ToTable(tb => tb.HasCheckConstraint(
                    "CK_Semester_Dates",
                    "[StartDate] < [EndDate] AND [RegistrationStartDate] < [RegistrationEndDate]"
                ));
            });


            // Existing configurations for other entities...
            builder.Entity<GradeScale>(entity =>
            {
                entity.HasIndex(gs => gs.GradeLetter).IsUnique();
                entity.Property(gs => gs.GradeLetter).IsRequired().HasMaxLength(10);
                entity.Property(gs => gs.Description).IsRequired().HasMaxLength(100);
                entity.Property(gs => gs.MinPercentage).HasColumnType("decimal(5,2)");
                entity.Property(gs => gs.MaxPercentage).HasColumnType("decimal(5,2)");
                entity.Property(gs => gs.GradePoints).HasColumnType("decimal(3,2)");
                entity.Property(gs => gs.IsActive).HasDefaultValue(true);
            });

            // Configure QRCodeSession
            builder.Entity<QRCodeSession>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Course)
                      .WithMany()
                      .HasForeignKey(e => e.CourseId)
                      .OnDelete(DeleteBehavior.NoAction);
            });

            // Configure QRAttendance
            builder.Entity<QRAttendance>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.QRCodeSession)
                      .WithMany(e => e.Attendances)
                      .HasForeignKey(e => e.QRCodeSessionId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Student)
                      .WithMany()
                      .HasForeignKey(e => e.StudentId)
                      .OnDelete(DeleteBehavior.NoAction);
            });
        }
    }
}