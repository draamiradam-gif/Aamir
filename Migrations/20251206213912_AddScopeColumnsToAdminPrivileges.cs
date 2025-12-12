using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddScopeColumnsToAdminPrivileges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                table: "AdminPrivileges",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FacultyId",
                table: "AdminPrivileges",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "AdminPrivileges",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UniversityId",
                table: "AdminPrivileges",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "AdminPrivileges");

            migrationBuilder.DropColumn(
                name: "FacultyId",
                table: "AdminPrivileges");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "AdminPrivileges");

            migrationBuilder.DropColumn(
                name: "UniversityId",
                table: "AdminPrivileges");
        }
    }
}
