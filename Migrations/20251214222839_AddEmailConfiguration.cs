using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SmtpServer = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SmtpPort = table.Column<int>(type: "int", nullable: false),
                    SenderEmail = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SenderName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ShowSenderEmail = table.Column<bool>(type: "bit", nullable: false),
                    BccSystemEmail = table.Column<bool>(type: "bit", nullable: false),
                    SystemBccEmail = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailConfigurations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailConfigurations_IsActive",
                table: "EmailConfigurations",
                column: "IsActive",
                filter: "IsActive = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailConfigurations");
        }
    }
}
