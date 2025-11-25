using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddCoursesToSemester : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SemesterId1",
                table: "Courses",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Courses_SemesterId1",
                table: "Courses",
                column: "SemesterId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_Semesters_SemesterId1",
                table: "Courses",
                column: "SemesterId1",
                principalTable: "Semesters",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Courses_Semesters_SemesterId1",
                table: "Courses");

            migrationBuilder.DropIndex(
                name: "IX_Courses_SemesterId1",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "SemesterId1",
                table: "Courses");
        }
    }
}
