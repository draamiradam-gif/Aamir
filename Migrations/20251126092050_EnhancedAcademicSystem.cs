using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace StudentManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class EnhancedAcademicSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Courses_DepartmentId",
                table: "Courses");

            migrationBuilder.DropIndex(
                name: "IX_CourseEnrollments_StudentId",
                table: "CourseEnrollments");

            migrationBuilder.RenameIndex(
                name: "IX_Students_SemesterId",
                table: "Students",
                newName: "IX_Student_SemesterId");

            migrationBuilder.RenameIndex(
                name: "IX_Students_DepartmentId",
                table: "Students",
                newName: "IX_Student_DepartmentId");

            migrationBuilder.RenameIndex(
                name: "IX_Courses_SemesterId",
                table: "Courses",
                newName: "IX_Course_SemesterId");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Universities",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AddColumn<bool>(
                name: "IsGradingPeriod",
                table: "Semesters",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsRegistrationPeriod",
                table: "Semesters",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<bool>(
                name: "IsPassingGrade",
                table: "GradeScales",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Departments",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastActivityDate",
                table: "CourseEnrollments",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "GradeStatus",
                table: "CourseEnrollments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "InProgress",
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "EnrollmentType",
                table: "CourseEnrollments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Regular",
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "EnrollmentStatus",
                table: "CourseEnrollments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Active",
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "EnrollmentMethod",
                table: "CourseEnrollments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Web",
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<decimal>(
                name: "AssignmentAverage",
                table: "CourseEnrollments",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AttendancePercentage",
                table: "CourseEnrollments",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FinalExamGrade",
                table: "CourseEnrollments",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GradeRevisionReason",
                table: "CourseEnrollments",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "GradeRevisionRequested",
                table: "CourseEnrollments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "GradeRevisionStatus",
                table: "CourseEnrollments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "None");

            migrationBuilder.AddColumn<decimal>(
                name: "MidtermGrade",
                table: "CourseEnrollments",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Colleges",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Branches",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.InsertData(
                table: "GradeScales",
                columns: new[] { "Id", "CreatedDate", "Description", "GradeLetter", "GradePoints", "IsActive", "IsPassingGrade", "MaxPercentage", "MinPercentage", "ModifiedDate" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 11, 26, 11, 20, 48, 446, DateTimeKind.Local).AddTicks(804), "Exceptional", "A+", 4.0m, true, true, 100m, 97m, null },
                    { 2, new DateTime(2025, 11, 26, 11, 20, 48, 446, DateTimeKind.Local).AddTicks(930), "Excellent", "A", 4.0m, true, true, 96m, 93m, null },
                    { 3, new DateTime(2025, 11, 26, 11, 20, 48, 446, DateTimeKind.Local).AddTicks(936), "Excellent", "A-", 3.7m, true, true, 92m, 90m, null },
                    { 4, new DateTime(2025, 11, 26, 11, 20, 48, 446, DateTimeKind.Local).AddTicks(942), "Good", "B+", 3.3m, true, true, 89m, 87m, null },
                    { 5, new DateTime(2025, 11, 26, 11, 20, 48, 446, DateTimeKind.Local).AddTicks(947), "Good", "B", 3.0m, true, true, 86m, 83m, null },
                    { 6, new DateTime(2025, 11, 26, 11, 20, 48, 446, DateTimeKind.Local).AddTicks(1052), "Good", "B-", 2.7m, true, true, 82m, 80m, null },
                    { 7, new DateTime(2025, 11, 26, 11, 20, 48, 446, DateTimeKind.Local).AddTicks(1081), "Satisfactory", "C+", 2.3m, true, true, 79m, 77m, null },
                    { 8, new DateTime(2025, 11, 26, 11, 20, 48, 446, DateTimeKind.Local).AddTicks(1086), "Satisfactory", "C", 2.0m, true, true, 76m, 73m, null },
                    { 9, new DateTime(2025, 11, 26, 11, 20, 48, 446, DateTimeKind.Local).AddTicks(1105), "Satisfactory", "C-", 1.7m, true, true, 72m, 70m, null },
                    { 10, new DateTime(2025, 11, 26, 11, 20, 48, 446, DateTimeKind.Local).AddTicks(1112), "Poor", "D+", 1.3m, true, true, 69m, 67m, null },
                    { 11, new DateTime(2025, 11, 26, 11, 20, 48, 446, DateTimeKind.Local).AddTicks(1121), "Poor", "D", 1.0m, true, true, 66m, 63m, null },
                    { 12, new DateTime(2025, 11, 26, 11, 20, 48, 446, DateTimeKind.Local).AddTicks(1126), "Poor", "D-", 0.7m, true, true, 62m, 60m, null }
                });

            migrationBuilder.InsertData(
                table: "GradeScales",
                columns: new[] { "Id", "CreatedDate", "Description", "GradeLetter", "GradePoints", "IsActive", "MaxPercentage", "MinPercentage", "ModifiedDate" },
                values: new object[] { 13, new DateTime(2025, 11, 26, 11, 20, 48, 446, DateTimeKind.Local).AddTicks(1131), "Failure", "F", 0.0m, true, 59m, 0m, null });

            migrationBuilder.CreateIndex(
                name: "IX_Student_ActiveGPA",
                table: "Students",
                columns: new[] { "IsActive", "GPA" });

            migrationBuilder.CreateIndex(
                name: "IX_Student_GPA",
                table: "Students",
                column: "GPA");

            migrationBuilder.CreateIndex(
                name: "IX_Semester_Dates",
                table: "Semesters",
                columns: new[] { "StartDate", "EndDate" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_GradeScale_GradePoints",
                table: "GradeScales",
                sql: "[GradePoints] >= 0 AND [GradePoints] <= 4.00");

            migrationBuilder.AddCheckConstraint(
                name: "CK_GradeScale_PercentageRange",
                table: "GradeScales",
                sql: "[MinPercentage] >= 0 AND [MaxPercentage] <= 100 AND [MinPercentage] <= [MaxPercentage]");

            migrationBuilder.CreateIndex(
                name: "IX_Course_DepartmentActive",
                table: "Courses",
                columns: new[] { "DepartmentId", "IsActive" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Course_Credits",
                table: "Courses",
                sql: "[Credits] > 0 AND [Credits] <= 6");

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollment_CourseGradeStatus",
                table: "CourseEnrollments",
                columns: new[] { "CourseId", "GradeStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollment_LastActivity",
                table: "CourseEnrollments",
                column: "LastActivityDate");

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollment_StatusActive",
                table: "CourseEnrollments",
                columns: new[] { "EnrollmentStatus", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollment_StudentGradeStatus",
                table: "CourseEnrollments",
                columns: new[] { "StudentId", "GradeStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollment_WaitlistPosition",
                table: "CourseEnrollments",
                column: "WaitlistPosition",
                filter: "[WaitlistPosition] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CourseEnrollment_AssignmentAverage",
                table: "CourseEnrollments",
                sql: "[AssignmentAverage] IS NULL OR ([AssignmentAverage] >= 0 AND [AssignmentAverage] <= 100)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CourseEnrollment_Attendance",
                table: "CourseEnrollments",
                sql: "[AttendancePercentage] IS NULL OR ([AttendancePercentage] >= 0 AND [AttendancePercentage] <= 100)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CourseEnrollment_WaitlistPosition",
                table: "CourseEnrollments",
                sql: "[WaitlistPosition] IS NULL OR [WaitlistPosition] > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Student_ActiveGPA",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Student_GPA",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Semester_Dates",
                table: "Semesters");

            migrationBuilder.DropCheckConstraint(
                name: "CK_GradeScale_GradePoints",
                table: "GradeScales");

            migrationBuilder.DropCheckConstraint(
                name: "CK_GradeScale_PercentageRange",
                table: "GradeScales");

            migrationBuilder.DropIndex(
                name: "IX_Course_DepartmentActive",
                table: "Courses");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Course_Credits",
                table: "Courses");

            migrationBuilder.DropIndex(
                name: "IX_CourseEnrollment_CourseGradeStatus",
                table: "CourseEnrollments");

            migrationBuilder.DropIndex(
                name: "IX_CourseEnrollment_LastActivity",
                table: "CourseEnrollments");

            migrationBuilder.DropIndex(
                name: "IX_CourseEnrollment_StatusActive",
                table: "CourseEnrollments");

            migrationBuilder.DropIndex(
                name: "IX_CourseEnrollment_StudentGradeStatus",
                table: "CourseEnrollments");

            migrationBuilder.DropIndex(
                name: "IX_CourseEnrollment_WaitlistPosition",
                table: "CourseEnrollments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CourseEnrollment_AssignmentAverage",
                table: "CourseEnrollments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CourseEnrollment_Attendance",
                table: "CourseEnrollments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CourseEnrollment_WaitlistPosition",
                table: "CourseEnrollments");

            migrationBuilder.DeleteData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DropColumn(
                name: "IsGradingPeriod",
                table: "Semesters");

            migrationBuilder.DropColumn(
                name: "IsRegistrationPeriod",
                table: "Semesters");

            migrationBuilder.DropColumn(
                name: "AssignmentAverage",
                table: "CourseEnrollments");

            migrationBuilder.DropColumn(
                name: "AttendancePercentage",
                table: "CourseEnrollments");

            migrationBuilder.DropColumn(
                name: "FinalExamGrade",
                table: "CourseEnrollments");

            migrationBuilder.DropColumn(
                name: "GradeRevisionReason",
                table: "CourseEnrollments");

            migrationBuilder.DropColumn(
                name: "GradeRevisionRequested",
                table: "CourseEnrollments");

            migrationBuilder.DropColumn(
                name: "GradeRevisionStatus",
                table: "CourseEnrollments");

            migrationBuilder.DropColumn(
                name: "MidtermGrade",
                table: "CourseEnrollments");

            migrationBuilder.RenameIndex(
                name: "IX_Student_SemesterId",
                table: "Students",
                newName: "IX_Students_SemesterId");

            migrationBuilder.RenameIndex(
                name: "IX_Student_DepartmentId",
                table: "Students",
                newName: "IX_Students_DepartmentId");

            migrationBuilder.RenameIndex(
                name: "IX_Course_SemesterId",
                table: "Courses",
                newName: "IX_Courses_SemesterId");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Universities",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsPassingGrade",
                table: "GradeScales",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Departments",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastActivityDate",
                table: "CourseEnrollments",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETDATE()");

            migrationBuilder.AlterColumn<int>(
                name: "GradeStatus",
                table: "CourseEnrollments",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldDefaultValue: "InProgress");

            migrationBuilder.AlterColumn<int>(
                name: "EnrollmentType",
                table: "CourseEnrollments",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldDefaultValue: "Regular");

            migrationBuilder.AlterColumn<int>(
                name: "EnrollmentStatus",
                table: "CourseEnrollments",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldDefaultValue: "Active");

            migrationBuilder.AlterColumn<int>(
                name: "EnrollmentMethod",
                table: "CourseEnrollments",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldDefaultValue: "Web");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Colleges",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Branches",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.CreateIndex(
                name: "IX_Courses_DepartmentId",
                table: "Courses",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollments_StudentId",
                table: "CourseEnrollments",
                column: "StudentId");
        }
    }
}
