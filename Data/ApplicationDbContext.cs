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
        public DbSet<University> Universities { get; set; } = null!;
        public DbSet<College> Colleges { get; set; } = null!;
        public DbSet<Department> Departments { get; set; } = null!;
        public DbSet<Branch> Branches { get; set; } = null!;
        public DbSet<Semester> Semesters { get; set; } = null!;

        // Other entities
        public DbSet<GradeScale> GradeScales { get; set; }
        public DbSet<QRCodeSession> QRCodeSessions { get; set; }
        public DbSet<QRAttendance> QRAttendances { get; set; }
        public DbSet<WaitlistEntry> WaitlistEntries { get; set; } = null!;

        // ✅ NEW: Comprehensive Grading System
        public DbSet<EvaluationType> EvaluationTypes { get; set; } = null!;
        public DbSet<CourseEvaluation> CourseEvaluations { get; set; } = null!;
        public DbSet<StudentGrade> StudentGrades { get; set; } = null!;
        public DbSet<GradingTemplate> GradingTemplates { get; set; } = null!;
        public DbSet<GradingTemplateItem> GradingTemplateItems { get; set; } = null!;

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
                entity.Property(s => s.IsActive).HasDefaultValue(true);

                entity.Property(s => s.Percentage).HasColumnType("decimal(5,2)");
                entity.Property(s => s.GPA).HasColumnType("decimal(4,2)");

                // ✅ NEW: Additional indexes for student academic performance
                entity.HasIndex(s => s.GPA)
                      .HasDatabaseName("IX_Student_GPA");

                entity.HasIndex(s => new { s.IsActive, s.GPA })
                      .HasDatabaseName("IX_Student_ActiveGPA");

                entity.HasIndex(s => s.DepartmentId)
                      .HasDatabaseName("IX_Student_DepartmentId");

                entity.HasIndex(s => s.SemesterId)
                      .HasDatabaseName("IX_Student_SemesterId");
            });

            // Configure Course entity
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

                // ✅ FIXED: Use consistent delete behavior
                entity.HasOne(c => c.CourseSemester)
                      .WithMany()
                      .HasForeignKey(c => c.SemesterId)
                      .OnDelete(DeleteBehavior.SetNull)
                      .IsRequired(false);

                entity.HasOne(c => c.CourseDepartment)
                      .WithMany(d => d.Courses)
                      .HasForeignKey(c => c.DepartmentId)
                      .OnDelete(DeleteBehavior.Restrict);

                // ✅ ADD: Configure cascade delete for Course -> Prerequisites
                entity.HasMany(c => c.Prerequisites)
                      .WithOne(cp => cp.Course)
                      .HasForeignKey(cp => cp.CourseId)
                      .OnDelete(DeleteBehavior.Cascade);

                // ✅ ADD: Configure cascade delete for Course -> Enrollments
                entity.HasMany(c => c.CourseEnrollments)
                      .WithOne(ce => ce.Course)
                      .HasForeignKey(ce => ce.CourseId)
                      .OnDelete(DeleteBehavior.Cascade);

                // ✅ NEW: Additional indexes for course management
                entity.HasIndex(c => new { c.DepartmentId, c.IsActive })
                      .HasDatabaseName("IX_Course_DepartmentActive");

                entity.HasIndex(c => c.SemesterId)
                      .HasDatabaseName("IX_Course_SemesterId");

                // ✅ NEW: Check constraint for course credits
                entity.ToTable(tb => tb.HasCheckConstraint(
                    "CK_Course_Credits",
                    "[Credits] > 0 AND [Credits] <= 6"
                ));
            });

            // ✅ ENHANCED: Configure CourseEnrollment entity with new properties
            builder.Entity<CourseEnrollment>(entity =>
            {
                // Existing configuration
                entity.HasIndex(e => new { e.CourseId, e.StudentId, e.SemesterId })
                    .HasDatabaseName("IX_CourseEnrollment_UniqueActive")
                    .HasFilter("[IsActive] = 1")
                    .IsUnique();

                entity.Property(e => e.Grade).HasColumnType("decimal(5,2)");
                entity.Property(e => e.GradeLetter).HasMaxLength(2);
                entity.Property(e => e.EnrollmentDate).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.IsActive).HasDefaultValue(true);

                // ✅ FIX: Use ClientCascade for all relationships
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

                // ✅ NEW: Configure enhanced properties
                entity.Property(e => e.EnrollmentType)
                      .HasConversion<string>()
                      .HasMaxLength(50)
                      .HasDefaultValue(EnrollmentType.Regular);

                entity.Property(e => e.EnrollmentStatus)
                      .HasConversion<string>()
                      .HasMaxLength(50)
                      .HasDefaultValue(EnrollmentStatus.Active);

                entity.Property(e => e.EnrollmentMethod)
                      .HasConversion<string>()
                      .HasMaxLength(50)
                      .HasDefaultValue(EnrollmentMethod.Web);

                entity.Property(e => e.GradeStatus)
                      .HasConversion<string>()
                      .HasMaxLength(50)
                      .HasDefaultValue(GradeStatus.InProgress);

                entity.Property(e => e.GradeRevisionStatus)
                      .HasConversion<string>()
                      .HasMaxLength(50)
                      .HasDefaultValue(GradeRevisionStatus.None);

                entity.Property(e => e.WaitlistPosition).IsRequired(false);
                entity.Property(e => e.ApprovedBy).HasMaxLength(100).IsRequired(false);
                entity.Property(e => e.ApprovalDate).IsRequired(false);
                entity.Property(e => e.DropReason).HasMaxLength(500).IsRequired(false);
                entity.Property(e => e.DropDate).IsRequired(false);
                entity.Property(e => e.CompletionDate).IsRequired(false);
                entity.Property(e => e.Remarks).HasMaxLength(500).IsRequired(false);
                entity.Property(e => e.GradeRevisionReason).HasMaxLength(1000).IsRequired(false);
                entity.Property(e => e.AuditTrail).IsRequired(false); // JSON data

                // ✅ NEW: Academic performance properties
                entity.Property(e => e.AttendancePercentage)
                      .HasColumnType("decimal(5,2)")
                      .IsRequired(false);

                entity.Property(e => e.AssignmentAverage)
                      .HasColumnType("decimal(5,2)")
                      .IsRequired(false);

                entity.Property(e => e.MidtermGrade)
                      .HasColumnType("decimal(5,2)")
                      .IsRequired(false);

                entity.Property(e => e.FinalExamGrade)
                      .HasColumnType("decimal(5,2)")
                      .IsRequired(false);

                entity.Property(e => e.LastActivityDate)
                      .HasDefaultValueSql("GETDATE()");

                entity.Property(e => e.GradeRevisionRequested)
                      .HasDefaultValue(false);

                // ✅ NEW: Additional indexes for performance
                entity.HasIndex(e => new { e.StudentId, e.GradeStatus })
                      .HasDatabaseName("IX_CourseEnrollment_StudentGradeStatus");

                entity.HasIndex(e => new { e.CourseId, e.GradeStatus })
                      .HasDatabaseName("IX_CourseEnrollment_CourseGradeStatus");

                entity.HasIndex(e => new { e.EnrollmentStatus, e.IsActive })
                      .HasDatabaseName("IX_CourseEnrollment_StatusActive");

                entity.HasIndex(e => e.WaitlistPosition)
                      .HasDatabaseName("IX_CourseEnrollment_WaitlistPosition")
                      .HasFilter("[WaitlistPosition] IS NOT NULL");

                entity.HasIndex(e => e.LastActivityDate)
                      .HasDatabaseName("IX_CourseEnrollment_LastActivity");

                // ✅ Check constraints
                entity.ToTable(tb => tb.HasCheckConstraint(
                    "CK_CourseEnrollment_Grade",
                    "[Grade] IS NULL OR ([Grade] >= 0 AND [Grade] <= 100)"
                ));

                // ✅ NEW: Additional check constraints
                entity.ToTable(tb => tb.HasCheckConstraint(
                    "CK_CourseEnrollment_Attendance",
                    "[AttendancePercentage] IS NULL OR ([AttendancePercentage] >= 0 AND [AttendancePercentage] <= 100)"
                ));

                entity.ToTable(tb => tb.HasCheckConstraint(
                    "CK_CourseEnrollment_AssignmentAverage",
                    "[AssignmentAverage] IS NULL OR ([AssignmentAverage] >= 0 AND [AssignmentAverage] <= 100)"
                ));

                entity.ToTable(tb => tb.HasCheckConstraint(
                    "CK_CourseEnrollment_WaitlistPosition",
                    "[WaitlistPosition] IS NULL OR [WaitlistPosition] > 0"
                ));
            });

            // Configure Semester entity
            builder.Entity<Semester>(entity =>
            {
                entity.HasIndex(s => new { s.DepartmentId, s.BranchId, s.SubBranchId, s.Name }).IsUnique();

                // ✅ FIX: Use ClientSetNull for all Semester relationships
                entity.HasOne(s => s.Department)
                      .WithMany(d => d.Semesters)
                      .HasForeignKey(s => s.DepartmentId)
                      .OnDelete(DeleteBehavior.ClientSetNull)
                      .IsRequired(false);

                entity.HasOne(s => s.Branch)
                      .WithMany(b => b.BranchSemesters)
                      .HasForeignKey(s => s.BranchId)
                      .OnDelete(DeleteBehavior.ClientSetNull)
                      .IsRequired(false);

                entity.HasOne(s => s.SubBranch)
                      .WithMany(b => b.SubBranchSemesters)
                      .HasForeignKey(s => s.SubBranchId)
                      .OnDelete(DeleteBehavior.ClientSetNull)
                      .IsRequired(false);

                // ✅ NEW: Additional properties for semester management
                entity.Property(s => s.IsRegistrationPeriod)
                      .HasDefaultValue(false);

                entity.Property(s => s.IsGradingPeriod)
                      .HasDefaultValue(false);

                // ✅ NEW: Index for semester dates
                entity.HasIndex(s => new { s.StartDate, s.EndDate })
                      .HasDatabaseName("IX_Semester_Dates");

                entity.ToTable(tb => tb.HasCheckConstraint(
                    "CK_Semester_Parent",
                    "([DepartmentId] IS NOT NULL AND [BranchId] IS NULL AND [SubBranchId] IS NULL) OR " +
                    "([DepartmentId] IS NULL AND [BranchId] IS NOT NULL AND [SubBranchId] IS NULL) OR " +
                    "([DepartmentId] IS NULL AND [BranchId] IS NULL AND [SubBranchId] IS NOT NULL) OR " +
                    "([DepartmentId] IS NULL AND [BranchId] IS NULL AND [SubBranchId] IS NULL)"
                ));
            });

            // Configure CoursePrerequisite entity
            builder.Entity<CoursePrerequisite>(entity =>
            {
                entity.ToTable(tb => tb.HasCheckConstraint(
                    "CK_CoursePrerequisite_NotSelfReferencing",
                    "[CourseId] != [PrerequisiteCourseId]"
                ));

                entity.HasIndex(cp => new { cp.CourseId, cp.PrerequisiteCourseId })
                      .IsUnique();

                entity.Property(cp => cp.MinGrade).HasColumnType("decimal(5,2)");

                // CASCADE delete when course is deleted
                entity.HasOne(cp => cp.Course)
                      .WithMany(c => c.Prerequisites)
                      .HasForeignKey(cp => cp.CourseId)
                      .OnDelete(DeleteBehavior.Cascade);

                // RESTRICT delete of prerequisite courses
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

                // ✅ NEW: Additional university properties
                entity.Property(u => u.IsActive).HasDefaultValue(true);
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

                // ✅ NEW: Additional college properties
                entity.Property(c => c.IsActive).HasDefaultValue(true);
            });

            // Department Configuration
            builder.Entity<Department>(entity =>
            {
                entity.HasIndex(d => d.DepartmentCode).IsUnique();

                entity.HasOne(d => d.College)
                      .WithMany(c => c.Departments)
                      .HasForeignKey(d => d.CollegeId)
                      .OnDelete(DeleteBehavior.Restrict);

                // ✅ NEW: Additional department properties
                entity.Property(d => d.IsActive).HasDefaultValue(true);
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

                // ✅ NEW: Additional branch properties
                entity.Property(b => b.IsActive).HasDefaultValue(true);
            });

            // ✅ SINGLE CONFIGURATION: Configure GradeScale entity (removed duplicate)
            builder.Entity<GradeScale>(entity =>
            {
                entity.HasIndex(gs => gs.GradeLetter).IsUnique();
                entity.Property(gs => gs.GradeLetter).IsRequired().HasMaxLength(10);
                entity.Property(gs => gs.Description).IsRequired().HasMaxLength(100);
                entity.Property(gs => gs.MinPercentage).HasColumnType("decimal(5,2)");
                entity.Property(gs => gs.MaxPercentage).HasColumnType("decimal(5,2)");
                entity.Property(gs => gs.GradePoints).HasColumnType("decimal(3,2)");
                entity.Property(gs => gs.IsActive).HasDefaultValue(true);
                entity.Property(gs => gs.IsPassingGrade).HasDefaultValue(true);

                // ✅ NEW: Check constraints for grade scale
                entity.ToTable(tb => tb.HasCheckConstraint(
                    "CK_GradeScale_PercentageRange",
                    "[MinPercentage] >= 0 AND [MaxPercentage] <= 100 AND [MinPercentage] <= [MaxPercentage]"
                ));

                entity.ToTable(tb => tb.HasCheckConstraint(
                    "CK_GradeScale_GradePoints",
                    "[GradePoints] >= 0 AND [GradePoints] <= 4.00"
                ));
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

            // ✅ NEW: Configure Comprehensive Grading System
            ConfigureComprehensiveGradingSystem(builder);

            // ✅ NEW: Seed initial GradeScale data
            SeedGradeScales(builder);
        }

        // ✅ NEW: Configure Comprehensive Grading System
        private void ConfigureComprehensiveGradingSystem(ModelBuilder builder)
        {
            // Configure EvaluationType
            builder.Entity<EvaluationType>(entity =>
            {
                entity.HasIndex(et => et.Name).IsUnique();
                entity.Property(et => et.Name).IsRequired().HasMaxLength(100);
                entity.Property(et => et.Description).HasMaxLength(500);
                entity.Property(et => et.DefaultWeight).HasColumnType("decimal(5,2)");
                entity.Property(et => et.Category).HasMaxLength(50);
                entity.Property(et => et.IsActive).HasDefaultValue(true);
            });

            // Configure CourseEvaluation
            builder.Entity<CourseEvaluation>(entity =>
            {
                entity.HasIndex(ce => new { ce.CourseId, ce.Title }).IsUnique();

                entity.Property(ce => ce.Title).IsRequired().HasMaxLength(200);
                entity.Property(ce => ce.Description).HasMaxLength(1000);
                entity.Property(ce => ce.Weight).HasColumnType("decimal(5,2)");
                entity.Property(ce => ce.MaxScore).HasColumnType("decimal(5,2)");
                entity.Property(ce => ce.Semester).HasMaxLength(50);

                entity.Property(ce => ce.IsPublished).HasDefaultValue(false);
                entity.Property(ce => ce.IsGraded).HasDefaultValue(false);

                entity.HasOne(ce => ce.Course)
                      .WithMany(c => c.CourseEvaluations)
                      .HasForeignKey(ce => ce.CourseId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(ce => ce.EvaluationType)
                      .WithMany()
                      .HasForeignKey(ce => ce.EvaluationTypeId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure StudentGrade
            builder.Entity<StudentGrade>(entity =>
            {
                entity.HasIndex(sg => new { sg.StudentId, sg.CourseEvaluationId }).IsUnique();

                entity.Property(sg => sg.Score).HasColumnType("decimal(5,2)");
                entity.Property(sg => sg.MaxScore).HasColumnType("decimal(5,2)");
                entity.Property(sg => sg.GradeLetter).HasMaxLength(10);
                entity.Property(sg => sg.Comments).HasMaxLength(1000);
                entity.Property(sg => sg.GradedBy).HasMaxLength(100);
                entity.Property(sg => sg.ExcuseReason).HasMaxLength(500);

                entity.Property(sg => sg.GradedDate).HasDefaultValueSql("GETDATE()");
                entity.Property(sg => sg.IsAbsent).HasDefaultValue(false);
                entity.Property(sg => sg.IsExcused).HasDefaultValue(false);

                entity.HasOne(sg => sg.Student)
                      .WithMany()
                      .HasForeignKey(sg => sg.StudentId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(sg => sg.CourseEvaluation)
                      .WithMany(ce => ce.StudentGrades)
                      .HasForeignKey(sg => sg.CourseEvaluationId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure GradingTemplate
            builder.Entity<GradingTemplate>(entity =>
            {
                entity.HasIndex(gt => gt.Name).IsUnique();

                entity.Property(gt => gt.Name).IsRequired().HasMaxLength(100);
                entity.Property(gt => gt.Description).HasMaxLength(1000);
                entity.Property(gt => gt.Department).HasMaxLength(100);
                entity.Property(gt => gt.IsDefault).HasDefaultValue(false);
            });

            // Configure GradingTemplateItem
            builder.Entity<GradingTemplateItem>(entity =>
            {
                entity.Property(gti => gti.Name).IsRequired().HasMaxLength(200);
                entity.Property(gti => gti.Weight).HasColumnType("decimal(5,2)");
                entity.Property(gti => gti.MaxScore).HasColumnType("decimal(5,2)");
                entity.Property(gti => gti.IsRequired).HasDefaultValue(true);

                entity.HasOne(gti => gti.GradingTemplate)
                      .WithMany(gt => gt.Items)
                      .HasForeignKey(gti => gti.GradingTemplateId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(gti => gti.EvaluationType)
                      .WithMany()
                      .HasForeignKey(gti => gti.EvaluationTypeId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Seed default evaluation types
            SeedEvaluationTypes(builder);
        }

        private void SeedEvaluationTypes(ModelBuilder builder)
        {
            var evaluationTypes = new List<EvaluationType>
            {
                new() { Id = 1, Name = "Final Exam", Category = "Examination", DefaultWeight = 40, Order = 1, IsActive = true },
                new() { Id = 2, Name = "Midterm Exam", Category = "Examination", DefaultWeight = 20, Order = 2, IsActive = true },
                new() { Id = 3, Name = "Quiz", Category = "Quiz", DefaultWeight = 10, Order = 3, IsActive = true },
                new() { Id = 4, Name = "Assignment", Category = "Assignment", DefaultWeight = 15, Order = 4, IsActive = true },
                new() { Id = 5, Name = "Project", Category = "Project", DefaultWeight = 25, Order = 5, IsActive = true },
                new() { Id = 6, Name = "Laboratory Work", Category = "Laboratory", DefaultWeight = 20, Order = 6, IsActive = true },
                new() { Id = 7, Name = "Class Participation", Category = "Participation", DefaultWeight = 5, Order = 7, IsActive = true },
                new() { Id = 8, Name = "Attendance", Category = "Attendance", DefaultWeight = 5, Order = 8, IsActive = true },
                new() { Id = 9, Name = "Presentation", Category = "Project", DefaultWeight = 15, Order = 9, IsActive = true },
                new() { Id = 10, Name = "Research Paper", Category = "Project", DefaultWeight = 30, Order = 10, IsActive = true }
            };

            builder.Entity<EvaluationType>().HasData(evaluationTypes);
        }

        // ✅ NEW: Add this method to seed default grade scales
        private static void SeedGradeScales(ModelBuilder builder)
        {
            var gradeScales = new List<GradeScale>
            {
                new() { Id = 1, GradeLetter = "A+", Description = "Exceptional", MinPercentage = 97, MaxPercentage = 100, GradePoints = 4.0m, IsPassingGrade = true, IsActive = true },
                new() { Id = 2, GradeLetter = "A", Description = "Excellent", MinPercentage = 93, MaxPercentage = 96, GradePoints = 4.0m, IsPassingGrade = true, IsActive = true },
                new() { Id = 3, GradeLetter = "A-", Description = "Excellent", MinPercentage = 90, MaxPercentage = 92, GradePoints = 3.7m, IsPassingGrade = true, IsActive = true },
                new() { Id = 4, GradeLetter = "B+", Description = "Good", MinPercentage = 87, MaxPercentage = 89, GradePoints = 3.3m, IsPassingGrade = true, IsActive = true },
                new() { Id = 5, GradeLetter = "B", Description = "Good", MinPercentage = 83, MaxPercentage = 86, GradePoints = 3.0m, IsPassingGrade = true, IsActive = true },
                new() { Id = 6, GradeLetter = "B-", Description = "Good", MinPercentage = 80, MaxPercentage = 82, GradePoints = 2.7m, IsPassingGrade = true, IsActive = true },
                new() { Id = 7, GradeLetter = "C+", Description = "Satisfactory", MinPercentage = 77, MaxPercentage = 79, GradePoints = 2.3m, IsPassingGrade = true, IsActive = true },
                new() { Id = 8, GradeLetter = "C", Description = "Satisfactory", MinPercentage = 73, MaxPercentage = 76, GradePoints = 2.0m, IsPassingGrade = true, IsActive = true },
                new() { Id = 9, GradeLetter = "C-", Description = "Satisfactory", MinPercentage = 70, MaxPercentage = 72, GradePoints = 1.7m, IsPassingGrade = true, IsActive = true },
                new() { Id = 10, GradeLetter = "D+", Description = "Poor", MinPercentage = 67, MaxPercentage = 69, GradePoints = 1.3m, IsPassingGrade = true, IsActive = true },
                new() { Id = 11, GradeLetter = "D", Description = "Poor", MinPercentage = 63, MaxPercentage = 66, GradePoints = 1.0m, IsPassingGrade = true, IsActive = true },
                new() { Id = 12, GradeLetter = "D-", Description = "Poor", MinPercentage = 60, MaxPercentage = 62, GradePoints = 0.7m, IsPassingGrade = true, IsActive = true },
                new() { Id = 13, GradeLetter = "F", Description = "Failure", MinPercentage = 0, MaxPercentage = 59, GradePoints = 0.0m, IsPassingGrade = false, IsActive = true }
            };

            builder.Entity<GradeScale>().HasData(gradeScales);
        }
    }
}