using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class FixQRCodeSessionSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QRAttendances_QRCodeSessions_SessionId",
                table: "QRAttendances");

            migrationBuilder.DropIndex(
                name: "IX_QRAttendances_SessionId",
                table: "QRAttendances");

            migrationBuilder.DropColumn(
                name: "ScanTime",
                table: "QRAttendances");

            migrationBuilder.DropColumn(
                name: "SessionId",
                table: "QRAttendances");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ScanTime",
                table: "QRAttendances",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "SessionId",
                table: "QRAttendances",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_QRAttendances_SessionId",
                table: "QRAttendances",
                column: "SessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_QRAttendances_QRCodeSessions_SessionId",
                table: "QRAttendances",
                column: "SessionId",
                principalTable: "QRCodeSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
