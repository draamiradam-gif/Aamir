using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSemesterConstraintsAndRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Semester_Parent",
                table: "Semesters");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Semester_Dates",
                table: "Semesters",
                sql: "[StartDate] < [EndDate] AND [RegistrationStartDate] < [RegistrationEndDate]");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Semester_Parent",
                table: "Semesters",
                sql: "([DepartmentId] IS NOT NULL AND [BranchId] IS NULL AND [SubBranchId] IS NULL) OR ([DepartmentId] IS NULL AND [BranchId] IS NOT NULL AND [SubBranchId] IS NULL) OR ([DepartmentId] IS NULL AND [BranchId] IS NULL AND [SubBranchId] IS NOT NULL) OR ([DepartmentId] IS NULL AND [BranchId] IS NULL AND [SubBranchId] IS NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Semester_Dates",
                table: "Semesters");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Semester_Parent",
                table: "Semesters");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Semester_Parent",
                table: "Semesters",
                sql: "([DepartmentId] IS NOT NULL AND [BranchId] IS NULL AND [SubBranchId] IS NULL) OR ([DepartmentId] IS NULL AND [BranchId] IS NOT NULL AND [SubBranchId] IS NULL) OR ([DepartmentId] IS NULL AND [BranchId] IS NULL AND [SubBranchId] IS NOT NULL)");
        }
    }
}
