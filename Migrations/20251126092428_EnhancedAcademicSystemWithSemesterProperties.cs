using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class EnhancedAcademicSystemWithSemesterProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 11, 26, 11, 24, 26, 322, DateTimeKind.Local).AddTicks(2090));

            migrationBuilder.UpdateData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2025, 11, 26, 11, 24, 26, 322, DateTimeKind.Local).AddTicks(2239));

            migrationBuilder.UpdateData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2025, 11, 26, 11, 24, 26, 322, DateTimeKind.Local).AddTicks(2252));

            migrationBuilder.UpdateData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2025, 11, 26, 11, 24, 26, 322, DateTimeKind.Local).AddTicks(2263));

            migrationBuilder.UpdateData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2025, 11, 26, 11, 24, 26, 322, DateTimeKind.Local).AddTicks(2269));

            migrationBuilder.UpdateData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2025, 11, 26, 11, 24, 26, 322, DateTimeKind.Local).AddTicks(2287));

            migrationBuilder.UpdateData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2025, 11, 26, 11, 24, 26, 322, DateTimeKind.Local).AddTicks(2340));

            migrationBuilder.UpdateData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2025, 11, 26, 11, 24, 26, 322, DateTimeKind.Local).AddTicks(2350));

            migrationBuilder.UpdateData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2025, 11, 26, 11, 24, 26, 322, DateTimeKind.Local).AddTicks(2375));

            migrationBuilder.UpdateData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2025, 11, 26, 11, 24, 26, 322, DateTimeKind.Local).AddTicks(2388));

            migrationBuilder.UpdateData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2025, 11, 26, 11, 24, 26, 322, DateTimeKind.Local).AddTicks(2406));

            migrationBuilder.UpdateData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2025, 11, 26, 11, 24, 26, 322, DateTimeKind.Local).AddTicks(2417));

            migrationBuilder.UpdateData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2025, 11, 26, 11, 24, 26, 322, DateTimeKind.Local).AddTicks(2424));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2025, 11, 26, 11, 23, 41, 698, DateTimeKind.Local).AddTicks(9340));

            migrationBuilder.UpdateData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2025, 11, 26, 11, 23, 41, 698, DateTimeKind.Local).AddTicks(9462));

            migrationBuilder.UpdateData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2025, 11, 26, 11, 23, 41, 698, DateTimeKind.Local).AddTicks(9469));

            migrationBuilder.UpdateData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedDate",
                value: new DateTime(2025, 11, 26, 11, 23, 41, 698, DateTimeKind.Local).AddTicks(9474));

            migrationBuilder.UpdateData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedDate",
                value: new DateTime(2025, 11, 26, 11, 23, 41, 698, DateTimeKind.Local).AddTicks(9479));

            migrationBuilder.UpdateData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedDate",
                value: new DateTime(2025, 11, 26, 11, 23, 41, 698, DateTimeKind.Local).AddTicks(9489));

            migrationBuilder.UpdateData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedDate",
                value: new DateTime(2025, 11, 26, 11, 23, 41, 698, DateTimeKind.Local).AddTicks(9525));

            migrationBuilder.UpdateData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedDate",
                value: new DateTime(2025, 11, 26, 11, 23, 41, 698, DateTimeKind.Local).AddTicks(9530));

            migrationBuilder.UpdateData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedDate",
                value: new DateTime(2025, 11, 26, 11, 23, 41, 698, DateTimeKind.Local).AddTicks(9546));

            migrationBuilder.UpdateData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedDate",
                value: new DateTime(2025, 11, 26, 11, 23, 41, 698, DateTimeKind.Local).AddTicks(9611));

            migrationBuilder.UpdateData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedDate",
                value: new DateTime(2025, 11, 26, 11, 23, 41, 698, DateTimeKind.Local).AddTicks(9625));

            migrationBuilder.UpdateData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedDate",
                value: new DateTime(2025, 11, 26, 11, 23, 41, 698, DateTimeKind.Local).AddTicks(9632));

            migrationBuilder.UpdateData(
                table: "GradeScales",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedDate",
                value: new DateTime(2025, 11, 26, 11, 23, 41, 698, DateTimeKind.Local).AddTicks(9636));
        }
    }
}
