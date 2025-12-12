using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminApplicationsAndBlockedUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AssignedAdminType",
                table: "AdminApplications",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BlockReason",
                table: "AdminApplications",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BlockedBy",
                table: "AdminApplications",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BlockedDate",
                table: "AdminApplications",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBlocked",
                table: "AdminApplications",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "BlockedUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    BlockedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BlockedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UnblockedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UnblockedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UnblockReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlockedUsers", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlockedUsers");

            migrationBuilder.DropColumn(
                name: "AssignedAdminType",
                table: "AdminApplications");

            migrationBuilder.DropColumn(
                name: "BlockReason",
                table: "AdminApplications");

            migrationBuilder.DropColumn(
                name: "BlockedBy",
                table: "AdminApplications");

            migrationBuilder.DropColumn(
                name: "BlockedDate",
                table: "AdminApplications");

            migrationBuilder.DropColumn(
                name: "IsBlocked",
                table: "AdminApplications");
        }
    }
}
