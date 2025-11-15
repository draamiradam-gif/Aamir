using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddQRAttendanceSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QRAttendances_Students_StudentId",
                table: "QRAttendances");

            migrationBuilder.DropForeignKey(
                name: "FK_QRCodeSessions_Courses_CourseId",
                table: "QRCodeSessions");

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
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_QRAttendances_Students_StudentId",
                table: "QRAttendances",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_QRCodeSessions_Courses_CourseId",
                table: "QRCodeSessions",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QRAttendances_QRCodeSessions_SessionId",
                table: "QRAttendances");

            migrationBuilder.DropForeignKey(
                name: "FK_QRAttendances_Students_StudentId",
                table: "QRAttendances");

            migrationBuilder.DropForeignKey(
                name: "FK_QRCodeSessions_Courses_CourseId",
                table: "QRCodeSessions");

            migrationBuilder.DropIndex(
                name: "IX_QRAttendances_SessionId",
                table: "QRAttendances");

            migrationBuilder.DropColumn(
                name: "ScanTime",
                table: "QRAttendances");

            migrationBuilder.DropColumn(
                name: "SessionId",
                table: "QRAttendances");

            migrationBuilder.AddForeignKey(
                name: "FK_QRAttendances_Students_StudentId",
                table: "QRAttendances",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_QRCodeSessions_Courses_CourseId",
                table: "QRCodeSessions",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }
    }
}
