using DocumentFormat.OpenXml.Bibliography;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using StudentManagementSystem.Controllers;
using StudentManagementSystem.Models;
using System.Reflection.Emit;
using Department = StudentManagementSystem.Models.Department;

namespace StudentManagementSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser, IdentityRole, string>
    {
        // ✅ ADD THIS: Parameterless constructor for design-time
        public ApplicationDbContext()
        {
        }

        // ✅ KEEP THIS: Constructor with options
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Your DbSets remain the same...
        public DbSet<Student> Students { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<CourseEnrollment> CourseEnrollments { get; set; }
        public DbSet<CoursePrerequisite> CoursePrerequisites { get; set; }
        public DbSet<University> Universities { get; set; } = null!;
        public DbSet<College> Colleges { get; set; } = null!;
        public DbSet<Department> Departments { get; set; } = null!;
        public DbSet<Branch> Branches { get; set; } = null!;
        public DbSet<Semester> Semesters { get; set; } = null!;
        public DbSet<CourseRegistration> CourseRegistrations { get; set; } = null!;
        public DbSet<RegistrationRule> RegistrationRules { get; set; } = null!;
        public DbSet<RegistrationPeriod> RegistrationPeriods { get; set; } = null!;
        public DbSet<GradeScale> GradeScales { get; set; } = null!;
        public DbSet<QRCodeSession> QRCodeSessions { get; set; }
        public DbSet<QRAttendance> QRAttendances { get; set; }
        public DbSet<WaitlistEntry> WaitlistEntries { get; set; } = null!;
        public DbSet<GradingComponent> GradingComponents { get; set; } = null!;
        public DbSet<StudentGrade> StudentGrades { get; set; } = null!;
        public DbSet<FinalGrade> FinalGrades { get; set; } = null!;
        public DbSet<GradingTemplate> GradingTemplates { get; set; } = null!;
        public DbSet<GradingTemplateComponent> GradingTemplateComponents { get; set; } = null!;

        //public DbSet<ApplicationRole> ApplicationRoles { get; set; } = null!;
        public DbSet<Permission> Permissions { get; set; } = null!;
        public DbSet<RolePermission> RolePermissions { get; set; } = null!;
        public DbSet<AdminApplication> AdminApplications { get; set; }
        public DbSet<AdminPrivilege> AdminPrivileges { get; set; }
        public DbSet<AdminPrivilegeTemplate> AdminPrivilegeTemplates { get; set; }
        public DbSet<CourseMaterial> CourseMaterials { get; set; }
        public DbSet<BlockedUser> BlockedUsers { get; set; }
        public DbSet<EmailConfiguration> EmailConfigurations { get; set; }
        public DbSet<SystemLog> SystemLogs { get; set; }
        public DbSet<SystemSettings> SystemSettings { get; set; }
        public DbSet<SystemStats> SystemStats { get; set; }



        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // ✅ Only configure if not already configured (for design-time)
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=StudentManagementSystem;Trusted_Connection=True;MultipleActiveResultSets=true");
            }
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure all entities to use Restrict instead of Cascade
            //foreach (var relationship in builder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            //{
            //    relationship.DeleteBehavior = DeleteBehavior.Restrict;
            //}

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
                entity.Property(s => s.IsActive).HasDefaultValue(true);

                entity.Property(s => s.Percentage).HasColumnType("decimal(5,2)");
                entity.Property(s => s.GPA).HasColumnType("decimal(4,2)");
            });

            // Configure Course entity - FIXED Restrict PATHS
            builder.Entity<Course>(entity =>
            {
                entity.HasIndex(c => c.CourseCode).IsUnique();

                entity.Property(c => c.CourseCode).IsRequired().HasMaxLength(20);
                entity.Property(c => c.CourseName).IsRequired().HasMaxLength(100);
                entity.Property(c => c.Description).HasMaxLength(5000);
                entity.Property(c => c.Department).IsRequired().HasMaxLength(50);

                entity.Property(c => c.Credits).HasDefaultValue(3);
                entity.Property(c => c.IsActive).HasDefaultValue(true);
                entity.Property(c => c.MaxStudents).HasDefaultValue(1000);
                entity.Property(c => c.MinGPA).HasColumnType("decimal(4,2)").HasDefaultValue(2.0m);
                entity.Property(c => c.CourseSpecification).HasMaxLength(20000);
                entity.Property(c => c.Icon).HasMaxLength(100);

                // ✅ FIXED: Use Restrict instead of ClientSetNull to avoid Restrict conflicts
                entity.HasOne(c => c.CourseSemester)
                      .WithMany(s => s.Courses) // Added this navigation
                      .HasForeignKey(c => c.SemesterId)
                      .OnDelete(DeleteBehavior.SetNull) // Changed to Restrict
                      .IsRequired(false);

                entity.HasOne(c => c.CourseDepartment)
                      .WithMany(d => d.Courses)
                      .HasForeignKey(c => c.DepartmentId)
                      .OnDelete(DeleteBehavior.Restrict);

                // ✅ FIXED: Keep as Restrict to avoid multiple Restrict paths
                entity.HasMany(c => c.Prerequisites)
                      .WithOne(cp => cp.Course)
                      .HasForeignKey(cp => cp.CourseId)
                      .OnDelete(DeleteBehavior.Cascade);

                // ✅ FIXED: Keep as Restrict to avoid multiple Restrict paths
                entity.HasMany(c => c.CourseEnrollments)
                      .WithOne(ce => ce.Course)
                      .HasForeignKey(ce => ce.CourseId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure CourseEnrollment entity
            builder.Entity<CourseEnrollment>(entity =>
            {
                entity.HasIndex(e => new { e.CourseId, e.StudentId, e.SemesterId })
                    .HasDatabaseName("IX_CourseEnrollment_UniqueActive")
                    .HasFilter("[IsActive] = 1")
                    .IsUnique();

                entity.Property(ce => ce.Grade).HasColumnType("decimal(5,2)");
                entity.Property(ce => ce.GradePoints).HasColumnType("decimal(4,2)");
                entity.Property(e => e.GradeLetter).HasMaxLength(2);
                entity.Property(e => e.EnrollmentDate).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.IsActive).HasDefaultValue(true);

                // ✅ FIXED: All relationships use Restrict
                entity.HasOne(e => e.Course)
                      .WithMany(c => c.CourseEnrollments)
                      .HasForeignKey(e => e.CourseId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Student)
                      .WithMany(s => s.CourseEnrollments)
                      .HasForeignKey(e => e.StudentId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Semester)
                      .WithMany()
                      .HasForeignKey(e => e.SemesterId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.ToTable(tb => tb.HasCheckConstraint(
                    "CK_CourseEnrollment_Grade",
                    "[Grade] IS NULL OR ([Grade] >= 0 AND [Grade] <= 100)"
                ));
            });

            builder.Entity<StudentEnrollment>(entity =>
            {
                entity.Property(e => e.Grade)
                      .HasColumnType("decimal(5,2)"); // This fixes the decimal warning
            });

            // Configure Semester entity - FIXED Restrict PATHS
            builder.Entity<Semester>(entity =>
            {
                entity.HasIndex(s => new { s.DepartmentId, s.BranchId, s.SubBranchId, s.Name }).IsUnique();

                // ✅ FIXED: Changed all to Restrict to avoid Restrict conflicts
                entity.HasOne(s => s.Department)
                      .WithMany(d => d.Semesters)
                      .HasForeignKey(s => s.DepartmentId)
                      .OnDelete(DeleteBehavior.Restrict) // Changed from ClientSetNull to Restrict
                      .IsRequired(false);

                entity.HasOne(s => s.Branch)
                      .WithMany(b => b.BranchSemesters)
                      .HasForeignKey(s => s.BranchId)
                      .OnDelete(DeleteBehavior.Restrict) // Changed from ClientSetNull to Restrict
                      .IsRequired(false);

                entity.HasOne(s => s.SubBranch)
                      .WithMany(b => b.SubBranchSemesters)
                      .HasForeignKey(s => s.SubBranchId)
                      .OnDelete(DeleteBehavior.Restrict) // Changed from ClientSetNull to Restrict
                      .IsRequired(false);

                // ✅ FIXED: Added this relationship to match the Course configuration
                entity.HasMany(s => s.Courses)
                      .WithOne(c => c.CourseSemester)
                      .HasForeignKey(c => c.SemesterId)
                      .OnDelete(DeleteBehavior.NoAction)
                      .IsRequired(false);

                entity.ToTable(tb => tb.HasCheckConstraint(
                    "CK_Semester_Parent",
                    "([DepartmentId] IS NOT NULL AND [BranchId] IS NULL AND [SubBranchId] IS NULL) OR " +
                    "([DepartmentId] IS NULL AND [BranchId] IS NOT NULL AND [SubBranchId] IS NULL) OR " +
                    "([DepartmentId] IS NULL AND [BranchId] IS NULL AND [SubBranchId] IS NOT NULL) OR " +
                    "([DepartmentId] IS NULL AND [BranchId] IS NULL AND [SubBranchId] IS NULL)"
                ));
            });

            // Configure CoursePrerequisite entity - FIXED Restrict PATHS
            builder.Entity<CoursePrerequisite>(entity =>
            {
                entity.ToTable(tb => tb.HasCheckConstraint(
                    "CK_CoursePrerequisite_NotSelfReferencing",
                    "[CourseId] != [PrerequisiteCourseId]"
                ));

                entity.HasIndex(cp => new { cp.CourseId, cp.PrerequisiteCourseId })
                      .IsUnique();

                entity.Property(cp => cp.MinGrade).HasColumnType("decimal(5,2)");

                // ✅ FIXED: Changed from Restrict to Restrict to avoid multiple paths
                entity.HasOne(cp => cp.Course)
                      .WithMany(c => c.Prerequisites)
                      .HasForeignKey(cp => cp.CourseId)
                      .OnDelete(DeleteBehavior.Cascade); // Changed from Restrict

                // ✅ FIXED: Keep as Restrict
                entity.HasOne(cp => cp.PrerequisiteCourse)
                      .WithMany(c => c.RequiredFor)
                      .HasForeignKey(cp => cp.PrerequisiteCourseId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

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
                entity.Property(d => d.MinimumGPAMajor).HasColumnType("decimal(4,2)");
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

            // GradeScale Configuration
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

            builder.Entity<QRCodeSession>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.Course)
                      .WithMany()
                      .HasForeignKey(e => e.CourseId)
                      .OnDelete(DeleteBehavior.Restrict);

                // ✅ FIXED: Proper relationship with Attendances
                entity.HasMany(e => e.Attendances)
                      .WithOne(e => e.QRCodeSession)
                      .HasForeignKey(e => e.QRCodeSessionId)
                      .OnDelete(DeleteBehavior.Cascade);                
            });

            // QRAttendance configuration
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
                      .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.ScannedAt)
                      .HasDefaultValueSql("GETUTCDATE()");
            });

            // Grading System Configuration - FIXED Restrict PATHS
            builder.Entity<GradingComponent>(entity =>
            {
                entity.HasIndex(gc => new { gc.CourseId, gc.Name }).IsUnique();
                entity.Property(gc => gc.WeightPercentage).HasColumnType("decimal(5,2)");
                entity.Property(gc => gc.MaximumMarks).HasColumnType("decimal(8,2)");

                // ✅ FIXED: Changed from Restrict to Restrict
                entity.HasOne(gc => gc.Course)
                      .WithMany()
                      .HasForeignKey(gc => gc.CourseId)
                      .OnDelete(DeleteBehavior.Restrict); // Changed from Restrict
            });

            builder.Entity<StudentGrade>(entity =>
            {
                entity.HasIndex(sg => new { sg.StudentId, sg.GradingComponentId, sg.CourseId, sg.SemesterId })
                      .IsUnique();

                entity.Property(sg => sg.MarksObtained).HasColumnType("decimal(8,2)");
                entity.Property(sg => sg.Percentage).HasColumnType("decimal(5,2)");
                entity.Property(sg => sg.GradePoints).HasColumnType("decimal(4,2)");

                entity.HasOne(sg => sg.GradingComponent)
                      .WithMany(gc => gc.StudentGrades)
                      .HasForeignKey(sg => sg.GradingComponentId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(sg => sg.Student)
                      .WithMany()
                      .HasForeignKey(sg => sg.StudentId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(sg => sg.Course)
                      .WithMany()
                      .HasForeignKey(sg => sg.CourseId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(sg => sg.Semester)
                      .WithMany()
                      .HasForeignKey(sg => sg.SemesterId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<FinalGrade>(entity =>
            {
                entity.HasIndex(fg => new { fg.StudentId, fg.CourseId, fg.SemesterId }).IsUnique();

                entity.Property(fg => fg.FinalPercentage).HasColumnType("decimal(5,2)");
                entity.Property(fg => fg.FinalGradePoints).HasColumnType("decimal(4,2)");
                entity.Property(fg => fg.TotalMarksObtained).HasColumnType("decimal(8,2)");
                entity.Property(fg => fg.TotalMaximumMarks).HasColumnType("decimal(8,2)");
            });

            builder.Entity<GradingTemplate>(entity =>
            {
                entity.HasIndex(gt => gt.TemplateName).IsUnique();
            });

            builder.Entity<GradingTemplateComponent>(entity =>
            {
                entity.Property(gtc => gtc.WeightPercentage).HasColumnType("decimal(5,2)");
                entity.Property(gtc => gtc.MaximumMarks).HasColumnType("decimal(8,2)");

                // This can stay as Restrict since it's a direct parent-child relationship
                entity.HasOne(gtc => gtc.Template)
                      .WithMany(gt => gt.Components)
                      .HasForeignKey(gtc => gtc.TemplateId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Registration System Configuration
            builder.Entity<CourseRegistration>(entity =>
            {
                entity.HasIndex(r => new { r.StudentId, r.CourseId, r.SemesterId }).IsUnique();

                entity.HasOne(r => r.Student)
                      .WithMany()
                      .HasForeignKey(r => r.StudentId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.Course)
                      .WithMany()
                      .HasForeignKey(r => r.CourseId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.Semester)
                      .WithMany()
                      .HasForeignKey(r => r.SemesterId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<RegistrationPeriod>(entity =>
            {
                entity.HasKey(rp => rp.Id);
                entity.Property(rp => rp.PeriodName).IsRequired().HasMaxLength(100);
                entity.HasIndex(rp => new { rp.SemesterId, rp.RegistrationType }).IsUnique();

                entity.HasOne(rp => rp.Semester)
                      .WithMany()
                      .HasForeignKey(rp => rp.SemesterId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<RegistrationRule>(entity =>
            {
                entity.HasKey(rr => rr.Id);
                entity.Property(rr => rr.RuleName).IsRequired().HasMaxLength(100);
                entity.Property(rr => rr.Description).HasMaxLength(500);

                entity.Property(rr => rr.MinimumGPA).HasColumnType("decimal(4,2)");

                entity.HasOne(rr => rr.Department)
                      .WithMany()
                      .HasForeignKey(rr => rr.DepartmentId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(rr => rr.Course)
                    .WithMany()
                    .HasForeignKey(rr => rr.CourseId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Permission>(entity =>
            {
                entity.HasIndex(p => p.Name).IsUnique();
                entity.Property(p => p.Name).IsRequired().HasMaxLength(255);
                entity.Property(p => p.Description).HasMaxLength(500);
                entity.Property(p => p.Category).HasMaxLength(100);
                entity.Property(p => p.IsActive).HasDefaultValue(true);
            });

            builder.Entity<RolePermission>(entity =>
            {
                entity.HasKey(rp => rp.Id);

                // FIXED: Use IdentityRole instead of ApplicationRole
                entity.HasOne(rp => rp.Role)
                      .WithMany() // Remove (r => r.RolePermissions) since IdentityRole doesn't have this navigation
                      .HasForeignKey(rp => rp.RoleId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(rp => rp.Permission)
                      .WithMany(p => p.RolePermissions)
                      .HasForeignKey(rp => rp.PermissionId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(rp => new { rp.RoleId, rp.PermissionId }).IsUnique();
            });

            //builder.Entity<ApplicationRole>(entity =>
            //{
            //    entity.Property(r => r.Description).HasMaxLength(500);
            //    entity.Property(r => r.CreatedBy).HasMaxLength(100);
            //    entity.Property(r => r.CreatedDate).HasDefaultValueSql("GETDATE()");

            //    // Index for better performance
            //    entity.HasIndex(r => r.IsSystemRole);
            //    entity.HasIndex(r => r.PermissionLevel);
            //});

            // Configure AdminPrivilege
            // Configure value converter for string properties
            /*
    var permissionConverter = new ValueConverter<List<PermissionModule>, string>(
        v => string.Join(',', v),
        v => v.Split(',', StringSplitOptions.RemoveEmptyEntries)
              .Select(p => Enum.Parse<PermissionModule>(p))
              .ToList()
    );

    builder.Entity<AdminPrivilege>()
        .Property(p => p.Permissions)
        .HasConversion(permissionConverter);

    builder.Entity<AdminPrivilegeTemplate>()
        .Property(p => p.DefaultPermissions)
        .HasConversion(permissionConverter);
    */

            //var permissionConverter = new PermissionListStringConverter();

            builder.Entity<AdminPrivilegeTemplate>(entity =>
            {
                // Explicitly ignore the computed property
                entity.Ignore(t => t.DefaultPermissions);

                // Configure the actual database column
                entity.Property(t => t.DefaultPermissionsData)
                      .IsRequired()
                      .HasMaxLength(2000); // Adjust size as needed
            });

            // Configure AdminPrivilege similarly
            builder.Entity<AdminPrivilege>(entity =>
            {
                entity.Ignore(p => p.Permissions);
                entity.Property(p => p.PermissionsData)
                      .IsRequired()
                      .HasMaxLength(2000);
            });
        

        // Configure relationships
            builder.Entity<AdminApplication>()
                .HasOne(a => a.University)
                .WithMany()
                .HasForeignKey(a => a.UniversityId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<AdminApplication>()
                .HasOne(a => a.Faculty)
                .WithMany()
                .HasForeignKey(a => a.FacultyId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<AdminApplication>()
                .HasOne(a => a.Department)
                .WithMany()
                .HasForeignKey(a => a.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<AdminPrivilege>()
                .HasOne(p => p.University)
                .WithMany()
                .HasForeignKey(p => p.UniversityScope)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<AdminPrivilege>()
                .HasOne(p => p.Faculty)
                .WithMany()
                .HasForeignKey(p => p.FacultyScope)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<AdminPrivilege>()
                .HasOne(p => p.Department)
                .WithMany()
                .HasForeignKey(p => p.DepartmentScope)
                .OnDelete(DeleteBehavior.Restrict);


            builder.Entity<CourseMaterial>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
                entity.Property(e => e.FileName).IsRequired().HasMaxLength(500);
                entity.Property(e => e.OriginalFileName).IsRequired().HasMaxLength(500);
                entity.Property(e => e.FilePath).IsRequired().HasMaxLength(1000);
                entity.Property(e => e.UploadedBy).HasMaxLength(100);
                entity.Property(e => e.AccessLevel).HasMaxLength(100);

                // Relationships
                entity.HasOne(e => e.Course)
                    .WithMany()
                    .HasForeignKey(e => e.CourseId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<EmailConfiguration>()
            .HasIndex(e => e.IsActive)
            .HasFilter("IsActive = 1");


            builder.Entity<SystemStats>()
            .HasKey(s => s.Id);

            builder.Entity<SystemStats>()
                .Property(s => s.AverageEnrollmentRate)
                .HasPrecision(5, 2);

            builder.Entity<SystemStats>()
                .Property(s => s.AverageEligibilityRate)
                .HasPrecision(5, 2);

            // Ensure only one stats record exists
            builder.Entity<SystemStats>()
                .HasIndex(s => s.Id)
                .IsUnique();


        }
    }
}