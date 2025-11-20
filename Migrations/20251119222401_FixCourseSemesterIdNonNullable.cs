using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class FixCourseSemesterIdNonNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // First, set any NULL values to 0 (or a default semester ID)
            migrationBuilder.Sql(@"
        UPDATE Courses 
        SET SemesterId = 0 
        WHERE SemesterId IS NULL
    ");

            // Then alter the column to be non-nullable
            migrationBuilder.AlterColumn<int>(
                name: "SemesterId",
                table: "Courses",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "SemesterId",
                table: "Courses",
                nullable: true,
                oldClrType: typeof(int));
        }
    }
}
