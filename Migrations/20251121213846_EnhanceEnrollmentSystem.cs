using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class EnhanceEnrollmentSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "MinPassedHours",
                table: "Courses",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<decimal>(
                name: "MinGPA",
                table: "Courses",
                type: "decimal(4,2)",
                nullable: true,
                defaultValue: 2.0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(4,2)",
                oldDefaultValue: 2.0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovalDate",
                table: "CourseEnrollments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedBy",
                table: "CourseEnrollments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AuditTrail",
                table: "CourseEnrollments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DropDate",
                table: "CourseEnrollments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DropReason",
                table: "CourseEnrollments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EnrollmentMethod",
                table: "CourseEnrollments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EnrollmentStatus",
                table: "CourseEnrollments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EnrollmentType",
                table: "CourseEnrollments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastActivityDate",
                table: "CourseEnrollments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "WaitlistPosition",
                table: "CourseEnrollments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WaitlistEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    SemesterId = table.Column<int>(type: "int", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    AddedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NotifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WaitlistEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WaitlistEntries_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict); // CHANGED: Cascade → Restrict
                    table.ForeignKey(
                        name: "FK_WaitlistEntries_Semesters_SemesterId",
                        column: x => x.SemesterId,
                        principalTable: "Semesters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict); // CHANGED: Cascade → Restrict
                    table.ForeignKey(
                        name: "FK_WaitlistEntries_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict); // CHANGED: Cascade → Restrict
                });

            migrationBuilder.CreateIndex(
                name: "IX_WaitlistEntries_CourseId",
                table: "WaitlistEntries",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_WaitlistEntries_SemesterId",
                table: "WaitlistEntries",
                column: "SemesterId");

            migrationBuilder.CreateIndex(
                name: "IX_WaitlistEntries_StudentId",
                table: "WaitlistEntries",
                column: "StudentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WaitlistEntries");

            migrationBuilder.DropColumn(
                name: "ApprovalDate",
                table: "CourseEnrollments");

            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                table: "CourseEnrollments");

            migrationBuilder.DropColumn(
                name: "AuditTrail",
                table: "CourseEnrollments");

            migrationBuilder.DropColumn(
                name: "DropDate",
                table: "CourseEnrollments");

            migrationBuilder.DropColumn(
                name: "DropReason",
                table: "CourseEnrollments");

            migrationBuilder.DropColumn(
                name: "EnrollmentMethod",
                table: "CourseEnrollments");

            migrationBuilder.DropColumn(
                name: "EnrollmentStatus",
                table: "CourseEnrollments");

            migrationBuilder.DropColumn(
                name: "EnrollmentType",
                table: "CourseEnrollments");

            migrationBuilder.DropColumn(
                name: "LastActivityDate",
                table: "CourseEnrollments");

            migrationBuilder.DropColumn(
                name: "WaitlistPosition",
                table: "CourseEnrollments");

            migrationBuilder.AlterColumn<int>(
                name: "MinPassedHours",
                table: "Courses",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "MinGPA",
                table: "Courses",
                type: "decimal(4,2)",
                nullable: false,
                defaultValue: 2.0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(4,2)",
                oldNullable: true,
                oldDefaultValue: 2.0m);
        }
    }
}