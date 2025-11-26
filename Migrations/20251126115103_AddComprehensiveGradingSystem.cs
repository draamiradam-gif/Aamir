using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace StudentManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddComprehensiveGradingSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EvaluationTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DefaultWeight = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GradingTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Department = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradingTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CourseEvaluations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    EvaluationTypeId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    MaxScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EvaluationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsGraded = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Semester = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseEvaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseEvaluations_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourseEvaluations_EvaluationTypes_EvaluationTypeId",
                        column: x => x.EvaluationTypeId,
                        principalTable: "EvaluationTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GradingTemplateItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GradingTemplateId = table.Column<int>(type: "int", nullable: false),
                    EvaluationTypeId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    MaxScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradingTemplateItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GradingTemplateItems_EvaluationTypes_EvaluationTypeId",
                        column: x => x.EvaluationTypeId,
                        principalTable: "EvaluationTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GradingTemplateItems_GradingTemplates_GradingTemplateId",
                        column: x => x.GradingTemplateId,
                        principalTable: "GradingTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentGrades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    CourseEvaluationId = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    MaxScore = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    GradeLetter = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    GradedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    GradedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsAbsent = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsExcused = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ExcuseReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SubmissionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentGrades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentGrades_CourseEvaluations_CourseEvaluationId",
                        column: x => x.CourseEvaluationId,
                        principalTable: "CourseEvaluations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentGrades_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "EvaluationTypes",
                columns: new[] { "Id", "Category", "CreatedDate", "DefaultWeight", "Description", "IsActive", "ModifiedDate", "Name", "Order" },
                values: new object[,]
                {
                    { 1, "Examination", new DateTime(2025, 11, 26, 13, 50, 59, 910, DateTimeKind.Local).AddTicks(516), 40m, "", true, null, "Final Exam", 1 },
                    { 2, "Examination", new DateTime(2025, 11, 26, 13, 50, 59, 910, DateTimeKind.Local).AddTicks(661), 20m, "", true, null, "Midterm Exam", 2 },
                    { 3, "Quiz", new DateTime(2025, 11, 26, 13, 50, 59, 910, DateTimeKind.Local).AddTicks(667), 10m, "", true, null, "Quiz", 3 },
                    { 4, "Assignment", new DateTime(2025, 11, 26, 13, 50, 59, 910, DateTimeKind.Local).AddTicks(671), 15m, "", true, null, "Assignment", 4 },
                    { 5, "Project", new DateTime(2025, 11, 26, 13, 50, 59, 910, DateTimeKind.Local).AddTicks(675), 25m, "", true, null, "Project", 5 },
                    { 6, "Laboratory", new DateTime(2025, 11, 26, 13, 50, 59, 910, DateTimeKind.Local).AddTicks(683), 20m, "", true, null, "Laboratory Work", 6 },
                    { 7, "Participation", new DateTime(2025, 11, 26, 13, 50, 59, 910, DateTimeKind.Local).AddTicks(706), 5m, "", true, null, "Class Participation", 7 },
                    { 8, "Attendance", new DateTime(2025, 11, 26, 13, 50, 59, 910, DateTimeKind.Local).AddTicks(710), 5m, "", true, null, "Attendance", 8 },
                    { 9, "Project", new DateTime(2025, 11, 26, 13, 50, 59, 910, DateTimeKind.Local).AddTicks(723), 15m, "", true, null, "Presentation", 9 },
                    { 10, "Project", new DateTime(2025, 11, 26, 13, 50, 59, 910, DateTimeKind.Local).AddTicks(728), 30m, "", true, null, "Research Paper", 10 }
                });

            migrationBuilder.UpdateData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 11, 26, 13, 50, 59, 910, DateTimeKind.Local).AddTicks(960));

            migrationBuilder.UpdateData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2025, 11, 26, 13, 50, 59, 910, DateTimeKind.Local).AddTicks(1101));

            migrationBuilder.UpdateData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2025, 11, 26, 13, 50, 59, 910, DateTimeKind.Local).AddTicks(1120));

            migrationBuilder.UpdateData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2025, 11, 26, 13, 50, 59, 910, DateTimeKind.Local).AddTicks(1128));

            migrationBuilder.UpdateData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2025, 11, 26, 13, 50, 59, 910, DateTimeKind.Local).AddTicks(1135));

            migrationBuilder.UpdateData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2025, 11, 26, 13, 50, 59, 910, DateTimeKind.Local).AddTicks(1280));

            migrationBuilder.UpdateData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2025, 11, 26, 13, 50, 59, 910, DateTimeKind.Local).AddTicks(1289));

            migrationBuilder.UpdateData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2025, 11, 26, 13, 50, 59, 910, DateTimeKind.Local).AddTicks(1295));

            migrationBuilder.UpdateData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2025, 11, 26, 13, 50, 59, 910, DateTimeKind.Local).AddTicks(1299));

            migrationBuilder.UpdateData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2025, 11, 26, 13, 50, 59, 910, DateTimeKind.Local).AddTicks(1305));

            migrationBuilder.UpdateData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2025, 11, 26, 13, 50, 59, 910, DateTimeKind.Local).AddTicks(1320));

            migrationBuilder.UpdateData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2025, 11, 26, 13, 50, 59, 910, DateTimeKind.Local).AddTicks(1324));

            migrationBuilder.UpdateData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2025, 11, 26, 13, 50, 59, 910, DateTimeKind.Local).AddTicks(1329));

            migrationBuilder.CreateIndex(
                name: "IX_CourseEvaluations_CourseId_Title",
                table: "CourseEvaluations",
                columns: new[] { "CourseId", "Title" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseEvaluations_EvaluationTypeId",
                table: "CourseEvaluations",
                column: "EvaluationTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationTypes_Name",
                table: "EvaluationTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GradingTemplateItems_EvaluationTypeId",
                table: "GradingTemplateItems",
                column: "EvaluationTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_GradingTemplateItems_GradingTemplateId",
                table: "GradingTemplateItems",
                column: "GradingTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_GradingTemplates_Name",
                table: "GradingTemplates",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentGrades_CourseEvaluationId",
                table: "StudentGrades",
                column: "CourseEvaluationId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentGrades_StudentId_CourseEvaluationId",
                table: "StudentGrades",
                columns: new[] { "StudentId", "CourseEvaluationId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GradingTemplateItems");

            migrationBuilder.DropTable(
                name: "StudentGrades");

            migrationBuilder.DropTable(
                name: "GradingTemplates");

            migrationBuilder.DropTable(
                name: "CourseEvaluations");

            migrationBuilder.DropTable(
                name: "EvaluationTypes");

            migrationBuilder.UpdateData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 11, 26, 12, 16, 8, 930, DateTimeKind.Local).AddTicks(1216));

            migrationBuilder.UpdateData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2025, 11, 26, 12, 16, 8, 930, DateTimeKind.Local).AddTicks(1337));

            migrationBuilder.UpdateData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2025, 11, 26, 12, 16, 8, 930, DateTimeKind.Local).AddTicks(1343));

            migrationBuilder.UpdateData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2025, 11, 26, 12, 16, 8, 930, DateTimeKind.Local).AddTicks(1348));

            migrationBuilder.UpdateData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2025, 11, 26, 12, 16, 8, 930, DateTimeKind.Local).AddTicks(1352));

            migrationBuilder.UpdateData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2025, 11, 26, 12, 16, 8, 930, DateTimeKind.Local).AddTicks(1361));

            migrationBuilder.UpdateData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2025, 11, 26, 12, 16, 8, 930, DateTimeKind.Local).AddTicks(1381));

            migrationBuilder.UpdateData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2025, 11, 26, 12, 16, 8, 930, DateTimeKind.Local).AddTicks(1385));

            migrationBuilder.UpdateData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2025, 11, 26, 12, 16, 8, 930, DateTimeKind.Local).AddTicks(1409));

            migrationBuilder.UpdateData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2025, 11, 26, 12, 16, 8, 930, DateTimeKind.Local).AddTicks(1416));

            migrationBuilder.UpdateData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2025, 11, 26, 12, 16, 8, 930, DateTimeKind.Local).AddTicks(1425));

            migrationBuilder.UpdateData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2025, 11, 26, 12, 16, 8, 930, DateTimeKind.Local).AddTicks(1430));

            migrationBuilder.UpdateData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2025, 11, 26, 12, 16, 8, 930, DateTimeKind.Local).AddTicks(1434));
        }
    }
}
