IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [AdminPrivilegeTemplates] (
    [Id] int NOT NULL IDENTITY,
    [TemplateName] nvarchar(100) NOT NULL,
    [AdminType] int NOT NULL,
    [Description] nvarchar(500) NULL,
    [DefaultPermissionsData] nvarchar(2000) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_AdminPrivilegeTemplates] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [AspNetRoles] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(256) NULL,
    [NormalizedName] nvarchar(256) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [AspNetUsers] (
    [Id] nvarchar(450) NOT NULL,
    [UserName] nvarchar(256) NULL,
    [NormalizedUserName] nvarchar(256) NULL,
    [Email] nvarchar(256) NULL,
    [NormalizedEmail] nvarchar(256) NULL,
    [EmailConfirmed] bit NOT NULL,
    [PasswordHash] nvarchar(max) NULL,
    [SecurityStamp] nvarchar(max) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    [PhoneNumber] nvarchar(max) NULL,
    [PhoneNumberConfirmed] bit NOT NULL,
    [TwoFactorEnabled] bit NOT NULL,
    [LockoutEnd] datetimeoffset NULL,
    [LockoutEnabled] bit NOT NULL,
    [AccessFailedCount] int NOT NULL,
    CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [GradeScales] (
    [Id] int NOT NULL IDENTITY,
    [GradeLetter] nvarchar(10) NOT NULL,
    [Description] nvarchar(100) NOT NULL,
    [MinPercentage] decimal(5,2) NOT NULL,
    [MaxPercentage] decimal(5,2) NOT NULL,
    [GradePoints] decimal(3,2) NOT NULL,
    [IsPassingGrade] bit NOT NULL,
    [Classification] nvarchar(50) NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ModifiedDate] datetime2 NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    CONSTRAINT [PK_GradeScales] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [GradingTemplates] (
    [Id] int NOT NULL IDENTITY,
    [TemplateName] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NULL,
    [IsDefault] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ModifiedDate] datetime2 NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_GradingTemplates] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Permissions] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(255) NOT NULL,
    [Description] nvarchar(500) NOT NULL,
    [Category] nvarchar(100) NOT NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    CONSTRAINT [PK_Permissions] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Universities] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(200) NOT NULL,
    [Code] nvarchar(20) NOT NULL,
    [Description] nvarchar(500) NULL,
    [Address] nvarchar(200) NULL,
    [Email] nvarchar(100) NULL,
    [Phone] nvarchar(20) NULL,
    [Website] nvarchar(100) NULL,
    [EstablishmentYear] int NULL,
    [AllowMultipleColleges] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ModifiedDate] datetime2 NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Universities] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [AspNetRoleClaims] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserClaims] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserLogins] (
    [LoginProvider] nvarchar(450) NOT NULL,
    [ProviderKey] nvarchar(450) NOT NULL,
    [ProviderDisplayName] nvarchar(max) NULL,
    [UserId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
    CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserRoles] (
    [UserId] nvarchar(450) NOT NULL,
    [RoleId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserTokens] (
    [UserId] nvarchar(450) NOT NULL,
    [LoginProvider] nvarchar(450) NOT NULL,
    [Name] nvarchar(450) NOT NULL,
    [Value] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [GradingTemplateComponents] (
    [Id] int NOT NULL IDENTITY,
    [TemplateId] int NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [ComponentType] int NOT NULL,
    [WeightPercentage] decimal(5,2) NOT NULL,
    [MaximumMarks] decimal(8,2) NOT NULL,
    [IsRequired] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ModifiedDate] datetime2 NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_GradingTemplateComponents] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_GradingTemplateComponents_GradingTemplates_TemplateId] FOREIGN KEY ([TemplateId]) REFERENCES [GradingTemplates] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [RolePermissions] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] nvarchar(450) NOT NULL,
    [PermissionId] int NOT NULL,
    CONSTRAINT [PK_RolePermissions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RolePermissions_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_RolePermissions_Permissions_PermissionId] FOREIGN KEY ([PermissionId]) REFERENCES [Permissions] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [Colleges] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(200) NOT NULL,
    [UniversityId] int NOT NULL,
    [CollegeCode] nvarchar(10) NULL,
    [Description] nvarchar(500) NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ModifiedDate] datetime2 NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Colleges] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Colleges_Universities_UniversityId] FOREIGN KEY ([UniversityId]) REFERENCES [Universities] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Departments] (
    [Id] int NOT NULL IDENTITY,
    [StartYear] int NOT NULL,
    [IsMajorDepartment] bit NOT NULL,
    [MinimumGPAMajor] decimal(4,2) NULL,
    [Name] nvarchar(200) NOT NULL,
    [DepartmentCode] nvarchar(10) NULL,
    [CollegeId] int NULL,
    [Description] nvarchar(500) NULL,
    [TotalBenches] int NOT NULL,
    [AvailableBenches] int NOT NULL,
    [UniversityId] int NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ModifiedDate] datetime2 NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Departments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Departments_Colleges_CollegeId] FOREIGN KEY ([CollegeId]) REFERENCES [Colleges] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Departments_Universities_UniversityId] FOREIGN KEY ([UniversityId]) REFERENCES [Universities] ([Id])
);
GO

CREATE TABLE [AdminApplications] (
    [Id] int NOT NULL IDENTITY,
    [ApplicantName] nvarchar(100) NOT NULL,
    [Email] nvarchar(100) NOT NULL,
    [Phone] nvarchar(20) NOT NULL,
    [AppliedAdminType] int NOT NULL,
    [UniversityId] int NULL,
    [FacultyId] int NULL,
    [DepartmentId] int NULL,
    [Justification] nvarchar(1000) NOT NULL,
    [Experience] nvarchar(500) NULL,
    [Qualifications] nvarchar(500) NULL,
    [Status] int NOT NULL,
    [AppliedDate] datetime2 NOT NULL,
    [ReviewedDate] datetime2 NULL,
    [ReviewedBy] nvarchar(100) NULL,
    [ReviewNotes] nvarchar(500) NULL,
    CONSTRAINT [PK_AdminApplications] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AdminApplications_Colleges_FacultyId] FOREIGN KEY ([FacultyId]) REFERENCES [Colleges] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_AdminApplications_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_AdminApplications_Universities_UniversityId] FOREIGN KEY ([UniversityId]) REFERENCES [Universities] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [AdminPrivileges] (
    [Id] int NOT NULL IDENTITY,
    [AdminId] nvarchar(450) NOT NULL,
    [AdminType] int NOT NULL,
    [UniversityScope] int NULL,
    [FacultyScope] int NULL,
    [DepartmentScope] int NULL,
    [PermissionsData] nvarchar(2000) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ModifiedDate] datetime2 NULL,
    [CreatedBy] nvarchar(100) NOT NULL,
    [UpdatedBy] nvarchar(100) NULL,
    CONSTRAINT [PK_AdminPrivileges] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AdminPrivileges_AspNetUsers_AdminId] FOREIGN KEY ([AdminId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AdminPrivileges_Colleges_FacultyScope] FOREIGN KEY ([FacultyScope]) REFERENCES [Colleges] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_AdminPrivileges_Departments_DepartmentScope] FOREIGN KEY ([DepartmentScope]) REFERENCES [Departments] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_AdminPrivileges_Universities_UniversityScope] FOREIGN KEY ([UniversityScope]) REFERENCES [Universities] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Branches] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(200) NOT NULL,
    [DepartmentId] int NOT NULL,
    [BranchCode] nvarchar(10) NULL,
    [Description] nvarchar(500) NULL,
    [ParentBranchId] int NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ModifiedDate] datetime2 NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Branches] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Branches_Branches_ParentBranchId] FOREIGN KEY ([ParentBranchId]) REFERENCES [Branches] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Branches_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Semesters] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(50) NOT NULL,
    [SemesterType] nvarchar(20) NOT NULL,
    [AcademicYear] int NOT NULL,
    [DepartmentId] int NULL,
    [BranchId] int NULL,
    [SubBranchId] int NULL,
    [EndDate] datetime2 NOT NULL,
    [RegistrationEndDate] datetime2 NOT NULL,
    [StartDate] datetime2 NOT NULL,
    [RegistrationStartDate] datetime2 NOT NULL,
    [IsCurrent] bit NOT NULL,
    [IsRegistrationOpen] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ModifiedDate] datetime2 NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Semesters] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Semester_Parent] CHECK (([DepartmentId] IS NOT NULL AND [BranchId] IS NULL AND [SubBranchId] IS NULL) OR ([DepartmentId] IS NULL AND [BranchId] IS NOT NULL AND [SubBranchId] IS NULL) OR ([DepartmentId] IS NULL AND [BranchId] IS NULL AND [SubBranchId] IS NOT NULL) OR ([DepartmentId] IS NULL AND [BranchId] IS NULL AND [SubBranchId] IS NULL)),
    CONSTRAINT [FK_Semesters_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Semesters_Branches_SubBranchId] FOREIGN KEY ([SubBranchId]) REFERENCES [Branches] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Semesters_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Courses] (
    [Id] int NOT NULL IDENTITY,
    [CourseCode] nvarchar(20) NOT NULL,
    [CourseName] nvarchar(100) NOT NULL,
    [Description] nvarchar(max) NULL,
    [Credits] int NOT NULL DEFAULT 3,
    [Department] nvarchar(50) NOT NULL,
    [GradeLevel] int NOT NULL,
    [MaxStudents] int NOT NULL DEFAULT 1000,
    [MinGPA] decimal(4,2) NULL DEFAULT 2.0,
    [MinPassedHours] int NULL,
    [PrerequisitesString] nvarchar(1000) NULL,
    [CourseSpecification] nvarchar(max) NULL,
    [Icon] nvarchar(100) NULL,
    [CurrentEnrollment] int NOT NULL,
    [DepartmentId] int NULL,
    [SemesterId] int NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ModifiedDate] datetime2 NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    CONSTRAINT [PK_Courses] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Courses_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Courses_Semesters_SemesterId] FOREIGN KEY ([SemesterId]) REFERENCES [Semesters] ([Id])
);
GO

CREATE TABLE [RegistrationPeriods] (
    [Id] int NOT NULL IDENTITY,
    [PeriodName] nvarchar(100) NOT NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NOT NULL,
    [SemesterId] int NOT NULL,
    [RegistrationType] int NOT NULL,
    [MaxCoursesPerStudent] int NULL,
    [MaxCreditHours] int NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ModifiedDate] datetime2 NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_RegistrationPeriods] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RegistrationPeriods_Semesters_SemesterId] FOREIGN KEY ([SemesterId]) REFERENCES [Semesters] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Students] (
    [Id] int NOT NULL IDENTITY,
    [StudentId] nvarchar(50) NOT NULL,
    [SeatNumber] nvarchar(50) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [NationalId] nvarchar(14) NOT NULL,
    [DepartmentName] nvarchar(100) NULL,
    [StudyLevel] nvarchar(50) NULL,
    [SemesterName] nvarchar(50) NULL,
    [Grade] nvarchar(50) NULL,
    [Phone] nvarchar(15) NULL,
    [Email] nvarchar(100) NULL,
    [SelectedForUpdate] bit NOT NULL,
    [Percentage] decimal(5,2) NOT NULL,
    [GPA] decimal(4,2) NOT NULL,
    [PassedHours] int NOT NULL,
    [AvailableHours] int NOT NULL,
    [Department] nvarchar(100) NULL,
    [Semester] nvarchar(50) NULL,
    [DepartmentId] int NULL,
    [BranchId] int NULL,
    [SemesterId] int NULL,
    [GradeLevel] int NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ModifiedDate] datetime2 NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    CONSTRAINT [PK_Students] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Students_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]),
    CONSTRAINT [FK_Students_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]),
    CONSTRAINT [FK_Students_Semesters_SemesterId] FOREIGN KEY ([SemesterId]) REFERENCES [Semesters] ([Id])
);
GO

CREATE TABLE [CourseMaterials] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(200) NOT NULL,
    [Description] nvarchar(1000) NULL,
    [CourseId] int NOT NULL,
    [Type] nvarchar(50) NOT NULL,
    [FileName] nvarchar(500) NOT NULL,
    [OriginalFileName] nvarchar(500) NOT NULL,
    [FilePath] nvarchar(1000) NOT NULL,
    [FileSize] bigint NOT NULL,
    [UploadDate] datetime2 NOT NULL,
    [UploadedBy] nvarchar(100) NOT NULL,
    [IsVisibleToStudents] bit NOT NULL,
    [AllowDownload] bit NOT NULL,
    [ViewOnlineOnly] bit NOT NULL,
    [AccessLevel] nvarchar(100) NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ModifiedDate] datetime2 NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_CourseMaterials] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CourseMaterials_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [CoursePrerequisites] (
    [Id] int NOT NULL IDENTITY,
    [CourseId] int NOT NULL,
    [PrerequisiteCourseId] int NOT NULL,
    [MinGrade] decimal(5,2) NULL,
    [IsRequired] bit NOT NULL,
    [IsMandatory] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ModifiedDate] datetime2 NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_CoursePrerequisites] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_CoursePrerequisite_NotSelfReferencing] CHECK ([CourseId] != [PrerequisiteCourseId]),
    CONSTRAINT [FK_CoursePrerequisites_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_CoursePrerequisites_Courses_PrerequisiteCourseId] FOREIGN KEY ([PrerequisiteCourseId]) REFERENCES [Courses] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [GradingComponents] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [ComponentType] int NOT NULL,
    [WeightPercentage] decimal(5,2) NOT NULL,
    [MaximumMarks] decimal(8,2) NOT NULL,
    [IncludeInFinalGrade] bit NOT NULL,
    [Description] nvarchar(500) NULL,
    [CourseId] int NOT NULL,
    [IsActive] bit NOT NULL,
    [IsRequired] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ModifiedDate] datetime2 NULL,
    CONSTRAINT [PK_GradingComponents] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_GradingComponents_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [QRCodeSessions] (
    [Id] int NOT NULL IDENTITY,
    [SessionTitle] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NULL,
    [CourseId] int NOT NULL,
    [Token] nvarchar(max) NOT NULL,
    [DurationMinutes] int NOT NULL,
    [MaxScans] int NULL,
    [AllowMultipleScans] bit NOT NULL,
    [CurrentToken] nvarchar(max) NOT NULL,
    [LastTokenUpdate] datetime2 NOT NULL,
    [TokenUpdateIntervalSeconds] int NOT NULL,
    [EnableDynamicQR] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ModifiedDate] datetime2 NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_QRCodeSessions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_QRCodeSessions_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [RegistrationRules] (
    [Id] int NOT NULL IDENTITY,
    [RuleName] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NULL,
    [RuleType] int NOT NULL,
    [MinimumGPA] decimal(4,2) NULL,
    [MinimumPassedHours] int NULL,
    [MaximumCreditHours] int NULL,
    [MinimumCreditHours] int NULL,
    [DepartmentId] int NULL,
    [GradeLevel] int NULL,
    [EnforcementLevel] int NOT NULL,
    [RuleCategory] int NOT NULL,
    [CourseId] int NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ModifiedDate] datetime2 NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_RegistrationRules] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RegistrationRules_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_RegistrationRules_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [CourseEnrollments] (
    [Id] int NOT NULL IDENTITY,
    [CourseId] int NOT NULL,
    [StudentId] int NOT NULL,
    [SemesterId] int NOT NULL,
    [Grade] decimal(5,2) NULL,
    [GradeLetter] nvarchar(2) NULL,
    [EnrollmentDate] datetime2 NOT NULL DEFAULT (GETDATE()),
    [GradePoints] decimal(4,2) NULL,
    [GradeStatus] int NOT NULL,
    [CompletionDate] datetime2 NULL,
    [Remarks] nvarchar(500) NULL,
    [EnrollmentType] int NOT NULL,
    [EnrollmentStatus] int NOT NULL,
    [WaitlistPosition] int NULL,
    [EnrollmentMethod] int NOT NULL,
    [ApprovedBy] nvarchar(100) NULL,
    [ApprovalDate] datetime2 NULL,
    [DropReason] nvarchar(500) NULL,
    [DropDate] datetime2 NULL,
    [LastActivityDate] datetime2 NOT NULL,
    [AuditTrail] nvarchar(max) NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ModifiedDate] datetime2 NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    CONSTRAINT [PK_CourseEnrollments] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_CourseEnrollment_Grade] CHECK ([Grade] IS NULL OR ([Grade] >= 0 AND [Grade] <= 100)),
    CONSTRAINT [FK_CourseEnrollments_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CourseEnrollments_Semesters_SemesterId] FOREIGN KEY ([SemesterId]) REFERENCES [Semesters] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CourseEnrollments_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [CourseRegistrations] (
    [Id] int NOT NULL IDENTITY,
    [StudentId] int NOT NULL,
    [CourseId] int NOT NULL,
    [SemesterId] int NOT NULL,
    [RegistrationDate] datetime2 NOT NULL,
    [Status] int NOT NULL,
    [ApprovedBy] nvarchar(100) NULL,
    [ApprovalDate] datetime2 NULL,
    [RegistrationType] int NOT NULL,
    [Remarks] nvarchar(500) NULL,
    [Priority] int NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ModifiedDate] datetime2 NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_CourseRegistrations] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_CourseRegistrations_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CourseRegistrations_Semesters_SemesterId] FOREIGN KEY ([SemesterId]) REFERENCES [Semesters] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CourseRegistrations_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [FeePayment] (
    [Id] int NOT NULL IDENTITY,
    [StudentId] int NOT NULL,
    [Amount] decimal(10,2) NOT NULL,
    [PaymentDate] datetime2 NOT NULL,
    [Semester] nvarchar(20) NOT NULL,
    [PaymentMethod] nvarchar(50) NOT NULL,
    [ReferenceNumber] nvarchar(100) NOT NULL,
    [IsVerified] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ModifiedDate] datetime2 NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_FeePayment] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_FeePayment_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [FinalGrades] (
    [Id] int NOT NULL IDENTITY,
    [StudentId] int NOT NULL,
    [CourseId] int NOT NULL,
    [SemesterId] int NOT NULL,
    [FinalPercentage] decimal(5,2) NOT NULL,
    [FinalGradeLetter] nvarchar(5) NOT NULL,
    [FinalGradePoints] decimal(4,2) NULL,
    [TotalMarksObtained] decimal(8,2) NOT NULL,
    [TotalMaximumMarks] decimal(8,2) NOT NULL,
    [GradeStatus] int NOT NULL,
    [CalculationDate] datetime2 NOT NULL,
    [GradeBreakdown] nvarchar(1000) NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ModifiedDate] datetime2 NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_FinalGrades] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_FinalGrades_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_FinalGrades_Semesters_SemesterId] FOREIGN KEY ([SemesterId]) REFERENCES [Semesters] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_FinalGrades_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [StudentAccount] (
    [Id] int NOT NULL IDENTITY,
    [StudentId] int NOT NULL,
    [Username] nvarchar(50) NOT NULL,
    [PasswordHash] nvarchar(max) NOT NULL,
    [IsBlocked] bit NOT NULL,
    [LastLogin] datetime2 NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ModifiedDate] datetime2 NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_StudentAccount] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StudentAccount_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [StudentCourse] (
    [Id] int NOT NULL IDENTITY,
    [StudentId] int NOT NULL,
    [CourseId] int NOT NULL,
    [Mark] decimal(5,2) NULL,
    [Points] decimal(3,2) NULL,
    [Grade] nvarchar(5) NOT NULL,
    [Status] nvarchar(10) NOT NULL,
    [IsPassed] bit NOT NULL,
    [IsAvailable] bit NOT NULL,
    [IsAssigned] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ModifiedDate] datetime2 NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_StudentCourse] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StudentCourse_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_StudentCourse_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [StudentEnrollment] (
    [Id] int NOT NULL IDENTITY,
    [CourseId] int NOT NULL,
    [StudentId] int NOT NULL,
    [SemesterId] int NOT NULL,
    [Grade] decimal(5,2) NULL,
    [GradeLetter] nvarchar(2) NULL,
    [EnrollmentDate] datetime2 NOT NULL,
    [GradePoints] decimal(4,2) NULL,
    [GradeStatus] int NOT NULL,
    [CompletionDate] datetime2 NULL,
    [Remarks] nvarchar(500) NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ModifiedDate] datetime2 NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_StudentEnrollment] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StudentEnrollment_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_StudentEnrollment_Semesters_SemesterId] FOREIGN KEY ([SemesterId]) REFERENCES [Semesters] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_StudentEnrollment_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [WaitlistEntries] (
    [Id] int NOT NULL IDENTITY,
    [StudentId] int NOT NULL,
    [CourseId] int NOT NULL,
    [SemesterId] int NOT NULL,
    [Position] int NOT NULL,
    [AddedDate] datetime2 NOT NULL,
    [NotifiedDate] datetime2 NULL,
    [ExpiryDate] datetime2 NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ModifiedDate] datetime2 NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_WaitlistEntries] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_WaitlistEntries_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_WaitlistEntries_Semesters_SemesterId] FOREIGN KEY ([SemesterId]) REFERENCES [Semesters] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_WaitlistEntries_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [StudentGrades] (
    [Id] int NOT NULL IDENTITY,
    [GradingComponentId] int NOT NULL,
    [StudentId] int NOT NULL,
    [CourseId] int NOT NULL,
    [SemesterId] int NOT NULL,
    [MarksObtained] decimal(8,2) NOT NULL,
    [Percentage] decimal(5,2) NOT NULL,
    [GradeLetter] nvarchar(5) NULL,
    [GradePoints] decimal(4,2) NULL,
    [GradedDate] datetime2 NOT NULL,
    [Remarks] nvarchar(500) NULL,
    [IsAbsent] bit NOT NULL,
    [IsExempted] bit NOT NULL,
    [WeightedScore] decimal(5,2) NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ModifiedDate] datetime2 NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_StudentGrades] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StudentGrades_Courses_CourseId] FOREIGN KEY ([CourseId]) REFERENCES [Courses] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_StudentGrades_GradingComponents_GradingComponentId] FOREIGN KEY ([GradingComponentId]) REFERENCES [GradingComponents] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_StudentGrades_Semesters_SemesterId] FOREIGN KEY ([SemesterId]) REFERENCES [Semesters] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_StudentGrades_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [QRAttendances] (
    [Id] int NOT NULL IDENTITY,
    [QRCodeSessionId] int NOT NULL,
    [StudentId] int NOT NULL,
    [ScannedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [DeviceInfo] nvarchar(max) NULL,
    [IPAddress] nvarchar(max) NULL,
    [IsValid] bit NOT NULL,
    [IsDuplicate] bit NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [ModifiedDate] datetime2 NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_QRAttendances] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_QRAttendances_QRCodeSessions_QRCodeSessionId] FOREIGN KEY ([QRCodeSessionId]) REFERENCES [QRCodeSessions] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_QRAttendances_Students_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Students] ([Id]) ON DELETE NO ACTION
);
GO

CREATE INDEX [IX_AdminApplications_DepartmentId] ON [AdminApplications] ([DepartmentId]);
GO

CREATE INDEX [IX_AdminApplications_FacultyId] ON [AdminApplications] ([FacultyId]);
GO

CREATE INDEX [IX_AdminApplications_UniversityId] ON [AdminApplications] ([UniversityId]);
GO

CREATE INDEX [IX_AdminPrivileges_AdminId] ON [AdminPrivileges] ([AdminId]);
GO

CREATE INDEX [IX_AdminPrivileges_DepartmentScope] ON [AdminPrivileges] ([DepartmentScope]);
GO

CREATE INDEX [IX_AdminPrivileges_FacultyScope] ON [AdminPrivileges] ([FacultyScope]);
GO

CREATE INDEX [IX_AdminPrivileges_UniversityScope] ON [AdminPrivileges] ([UniversityScope]);
GO

CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
GO

CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;
GO

CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
GO

CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
GO

CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
GO

CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
GO

CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;
GO

CREATE UNIQUE INDEX [IX_Branches_DepartmentId_Name] ON [Branches] ([DepartmentId], [Name]);
GO

CREATE INDEX [IX_Branches_ParentBranchId] ON [Branches] ([ParentBranchId]);
GO

CREATE UNIQUE INDEX [IX_Colleges_CollegeCode] ON [Colleges] ([CollegeCode]) WHERE [CollegeCode] IS NOT NULL;
GO

CREATE UNIQUE INDEX [IX_Colleges_UniversityId_Name] ON [Colleges] ([UniversityId], [Name]);
GO

CREATE UNIQUE INDEX [IX_CourseEnrollment_UniqueActive] ON [CourseEnrollments] ([CourseId], [StudentId], [SemesterId]) WHERE [IsActive] = 1;
GO

CREATE INDEX [IX_CourseEnrollments_SemesterId] ON [CourseEnrollments] ([SemesterId]);
GO

CREATE INDEX [IX_CourseEnrollments_StudentId] ON [CourseEnrollments] ([StudentId]);
GO

CREATE INDEX [IX_CourseMaterials_CourseId] ON [CourseMaterials] ([CourseId]);
GO

CREATE UNIQUE INDEX [IX_CoursePrerequisites_CourseId_PrerequisiteCourseId] ON [CoursePrerequisites] ([CourseId], [PrerequisiteCourseId]);
GO

CREATE INDEX [IX_CoursePrerequisites_PrerequisiteCourseId] ON [CoursePrerequisites] ([PrerequisiteCourseId]);
GO

CREATE INDEX [IX_CourseRegistrations_CourseId] ON [CourseRegistrations] ([CourseId]);
GO

CREATE INDEX [IX_CourseRegistrations_SemesterId] ON [CourseRegistrations] ([SemesterId]);
GO

CREATE UNIQUE INDEX [IX_CourseRegistrations_StudentId_CourseId_SemesterId] ON [CourseRegistrations] ([StudentId], [CourseId], [SemesterId]);
GO

CREATE UNIQUE INDEX [IX_Courses_CourseCode] ON [Courses] ([CourseCode]);
GO

CREATE INDEX [IX_Courses_DepartmentId] ON [Courses] ([DepartmentId]);
GO

CREATE INDEX [IX_Courses_SemesterId] ON [Courses] ([SemesterId]);
GO

CREATE INDEX [IX_Departments_CollegeId] ON [Departments] ([CollegeId]);
GO

CREATE UNIQUE INDEX [IX_Departments_DepartmentCode] ON [Departments] ([DepartmentCode]) WHERE [DepartmentCode] IS NOT NULL;
GO

CREATE INDEX [IX_Departments_UniversityId] ON [Departments] ([UniversityId]);
GO

CREATE INDEX [IX_FeePayment_StudentId] ON [FeePayment] ([StudentId]);
GO

CREATE INDEX [IX_FinalGrades_CourseId] ON [FinalGrades] ([CourseId]);
GO

CREATE INDEX [IX_FinalGrades_SemesterId] ON [FinalGrades] ([SemesterId]);
GO

CREATE UNIQUE INDEX [IX_FinalGrades_StudentId_CourseId_SemesterId] ON [FinalGrades] ([StudentId], [CourseId], [SemesterId]);
GO

CREATE UNIQUE INDEX [IX_GradeScales_GradeLetter] ON [GradeScales] ([GradeLetter]);
GO

CREATE UNIQUE INDEX [IX_GradingComponents_CourseId_Name] ON [GradingComponents] ([CourseId], [Name]);
GO

CREATE INDEX [IX_GradingTemplateComponents_TemplateId] ON [GradingTemplateComponents] ([TemplateId]);
GO

CREATE UNIQUE INDEX [IX_GradingTemplates_TemplateName] ON [GradingTemplates] ([TemplateName]);
GO

CREATE UNIQUE INDEX [IX_Permissions_Name] ON [Permissions] ([Name]);
GO

CREATE INDEX [IX_QRAttendances_QRCodeSessionId] ON [QRAttendances] ([QRCodeSessionId]);
GO

CREATE INDEX [IX_QRAttendances_StudentId] ON [QRAttendances] ([StudentId]);
GO

CREATE INDEX [IX_QRCodeSessions_CourseId] ON [QRCodeSessions] ([CourseId]);
GO

CREATE UNIQUE INDEX [IX_RegistrationPeriods_SemesterId_RegistrationType] ON [RegistrationPeriods] ([SemesterId], [RegistrationType]);
GO

CREATE INDEX [IX_RegistrationRules_CourseId] ON [RegistrationRules] ([CourseId]);
GO

CREATE INDEX [IX_RegistrationRules_DepartmentId] ON [RegistrationRules] ([DepartmentId]);
GO

CREATE INDEX [IX_RolePermissions_PermissionId] ON [RolePermissions] ([PermissionId]);
GO

CREATE UNIQUE INDEX [IX_RolePermissions_RoleId_PermissionId] ON [RolePermissions] ([RoleId], [PermissionId]);
GO

CREATE INDEX [IX_Semesters_BranchId] ON [Semesters] ([BranchId]);
GO

CREATE UNIQUE INDEX [IX_Semesters_DepartmentId_BranchId_SubBranchId_Name] ON [Semesters] ([DepartmentId], [BranchId], [SubBranchId], [Name]) WHERE [DepartmentId] IS NOT NULL AND [BranchId] IS NOT NULL AND [SubBranchId] IS NOT NULL;
GO

CREATE INDEX [IX_Semesters_SubBranchId] ON [Semesters] ([SubBranchId]);
GO

CREATE UNIQUE INDEX [IX_StudentAccount_StudentId] ON [StudentAccount] ([StudentId]);
GO

CREATE INDEX [IX_StudentCourse_CourseId] ON [StudentCourse] ([CourseId]);
GO

CREATE INDEX [IX_StudentCourse_StudentId] ON [StudentCourse] ([StudentId]);
GO

CREATE INDEX [IX_StudentEnrollment_CourseId] ON [StudentEnrollment] ([CourseId]);
GO

CREATE INDEX [IX_StudentEnrollment_SemesterId] ON [StudentEnrollment] ([SemesterId]);
GO

CREATE INDEX [IX_StudentEnrollment_StudentId] ON [StudentEnrollment] ([StudentId]);
GO

CREATE INDEX [IX_StudentGrades_CourseId] ON [StudentGrades] ([CourseId]);
GO

CREATE INDEX [IX_StudentGrades_GradingComponentId] ON [StudentGrades] ([GradingComponentId]);
GO

CREATE INDEX [IX_StudentGrades_SemesterId] ON [StudentGrades] ([SemesterId]);
GO

CREATE UNIQUE INDEX [IX_StudentGrades_StudentId_GradingComponentId_CourseId_SemesterId] ON [StudentGrades] ([StudentId], [GradingComponentId], [CourseId], [SemesterId]);
GO

CREATE INDEX [IX_Students_BranchId] ON [Students] ([BranchId]);
GO

CREATE INDEX [IX_Students_DepartmentId] ON [Students] ([DepartmentId]);
GO

CREATE UNIQUE INDEX [IX_Students_NationalId] ON [Students] ([NationalId]);
GO

CREATE UNIQUE INDEX [IX_Students_SeatNumber] ON [Students] ([SeatNumber]);
GO

CREATE INDEX [IX_Students_SemesterId] ON [Students] ([SemesterId]);
GO

CREATE UNIQUE INDEX [IX_Students_StudentId] ON [Students] ([StudentId]);
GO

CREATE UNIQUE INDEX [IX_Universities_Code] ON [Universities] ([Code]);
GO

CREATE UNIQUE INDEX [IX_Universities_Name] ON [Universities] ([Name]);
GO

CREATE INDEX [IX_WaitlistEntries_CourseId] ON [WaitlistEntries] ([CourseId]);
GO

CREATE INDEX [IX_WaitlistEntries_SemesterId] ON [WaitlistEntries] ([SemesterId]);
GO

CREATE INDEX [IX_WaitlistEntries_StudentId] ON [WaitlistEntries] ([StudentId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251203232206_InitialCreate', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [AdminApplications] ADD [CreatedDate] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251204113621_MissingProberties', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [AdminApplications] ADD [AssignedAdminType] int NULL;
GO

ALTER TABLE [AdminApplications] ADD [BlockReason] nvarchar(500) NULL;
GO

ALTER TABLE [AdminApplications] ADD [BlockedBy] nvarchar(100) NULL;
GO

ALTER TABLE [AdminApplications] ADD [BlockedDate] datetime2 NULL;
GO

ALTER TABLE [AdminApplications] ADD [IsBlocked] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

CREATE TABLE [BlockedUsers] (
    [Id] int NOT NULL IDENTITY,
    [Email] nvarchar(100) NOT NULL,
    [UserName] nvarchar(100) NULL,
    [Reason] nvarchar(500) NOT NULL,
    [BlockedBy] nvarchar(max) NOT NULL,
    [BlockedDate] datetime2 NOT NULL,
    [UnblockedDate] datetime2 NULL,
    [UnblockedBy] nvarchar(100) NULL,
    [UnblockReason] nvarchar(500) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_BlockedUsers] PRIMARY KEY ([Id])
);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251204171911_AddAdminApplicationsAndBlockedUsers', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [AdminApplications] ADD [ApplicantId] nvarchar(max) NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251205164512_AddApplicantIdToAdminApplication', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [AdminPrivileges] ADD [DepartmentId] int NULL;
GO

ALTER TABLE [AdminPrivileges] ADD [FacultyId] int NULL;
GO

ALTER TABLE [AdminPrivileges] ADD [ModifiedBy] nvarchar(max) NULL;
GO

ALTER TABLE [AdminPrivileges] ADD [UniversityId] int NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251206213912_AddScopeColumnsToAdminPrivileges', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [EmailConfigurations] (
    [Id] int NOT NULL IDENTITY,
    [SmtpServer] nvarchar(100) NOT NULL,
    [SmtpPort] int NOT NULL,
    [SenderEmail] nvarchar(100) NOT NULL,
    [SenderName] nvarchar(100) NOT NULL,
    [Username] nvarchar(100) NOT NULL,
    [Password] nvarchar(max) NOT NULL,
    [ShowSenderEmail] bit NOT NULL,
    [BccSystemEmail] bit NOT NULL,
    [SystemBccEmail] nvarchar(100) NULL,
    [CreatedDate] datetime2 NOT NULL,
    [UpdatedDate] datetime2 NOT NULL,
    [UpdatedBy] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_EmailConfigurations] PRIMARY KEY ([Id])
);
GO

CREATE INDEX [IX_EmailConfigurations_IsActive] ON [EmailConfigurations] ([IsActive]) WHERE IsActive = 1;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251214222839_AddEmailConfiguration', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [WaitlistEntries] ADD [ExpirationDate] datetime2 NULL;
GO

ALTER TABLE [WaitlistEntries] ADD [NotificationSent] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

ALTER TABLE [WaitlistEntries] ADD [ProcessedDate] datetime2 NULL;
GO

ALTER TABLE [WaitlistEntries] ADD [RequestedBy] nvarchar(100) NULL;
GO

ALTER TABLE [Courses] ADD [ClassSchedule] nvarchar(100) NULL;
GO

ALTER TABLE [Courses] ADD [EndTime] time NULL;
GO

ALTER TABLE [Courses] ADD [RoomNumber] nvarchar(20) NULL;
GO

ALTER TABLE [Courses] ADD [ScheduleDays] nvarchar(50) NULL;
GO

ALTER TABLE [Courses] ADD [StartTime] time NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251215213548_AddScheduleAndWaitlistProperties', N'8.0.0');
GO

COMMIT;
GO

