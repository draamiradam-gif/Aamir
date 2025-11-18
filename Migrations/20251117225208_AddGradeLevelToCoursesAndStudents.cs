using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddGradeLevelToCoursesAndStudents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CourseEnrollment_UniqueActive",
                table: "CourseEnrollments");

            migrationBuilder.AddColumn<int>(
                name: "GradeLevel",
                table: "Students",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GradeLevel",
                table: "Courses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<decimal>(
                name: "GradePoints",
                table: "CourseEnrollments",
                type: "decimal(4,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(3,2)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SemesterId",
                table: "CourseEnrollments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "StudentEnrollment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    SemesterId = table.Column<int>(type: "int", nullable: false),
                    Grade = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    GradeLetter = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    EnrollmentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    GradePoints = table.Column<decimal>(type: "decimal(4,2)", nullable: true),
                    GradeStatus = table.Column<int>(type: "int", nullable: false),
                    CompletionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentEnrollment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentEnrollment_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentEnrollment_Semesters_SemesterId",
                        column: x => x.SemesterId,
                        principalTable: "Semesters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentEnrollment_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollment_UniqueActive",
                table: "CourseEnrollments",
                columns: new[] { "CourseId", "StudentId", "SemesterId" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollments_SemesterId",
                table: "CourseEnrollments",
                column: "SemesterId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CourseEnrollment_Grade",
                table: "CourseEnrollments",
                sql: "[Grade] IS NULL OR ([Grade] >= 0 AND [Grade] <= 100)");

            migrationBuilder.CreateIndex(
                name: "IX_StudentEnrollment_CourseId",
                table: "StudentEnrollment",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentEnrollment_SemesterId",
                table: "StudentEnrollment",
                column: "SemesterId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentEnrollment_StudentId",
                table: "StudentEnrollment",
                column: "StudentId");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseEnrollments_Semesters_SemesterId",
                table: "CourseEnrollments",
                column: "SemesterId",
                principalTable: "Semesters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseEnrollments_Semesters_SemesterId",
                table: "CourseEnrollments");

            migrationBuilder.DropTable(
                name: "StudentEnrollment");

            migrationBuilder.DropIndex(
                name: "IX_CourseEnrollment_UniqueActive",
                table: "CourseEnrollments");

            migrationBuilder.DropIndex(
                name: "IX_CourseEnrollments_SemesterId",
                table: "CourseEnrollments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CourseEnrollment_Grade",
                table: "CourseEnrollments");

            migrationBuilder.DropColumn(
                name: "GradeLevel",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "GradeLevel",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "SemesterId",
                table: "CourseEnrollments");

            migrationBuilder.AlterColumn<decimal>(
                name: "GradePoints",
                table: "CourseEnrollments",
                type: "decimal(3,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(4,2)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseEnrollment_UniqueActive",
                table: "CourseEnrollments",
                columns: new[] { "CourseId", "StudentId" },
                filter: "[IsActive] = 1");
        }
    }
}
