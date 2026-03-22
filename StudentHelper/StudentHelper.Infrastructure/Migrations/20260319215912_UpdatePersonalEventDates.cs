using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentHelper.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePersonalEventDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "PersonalEvents");

            migrationBuilder.DropColumn(
                name: "StartTime",
                table: "PersonalEvents");

            migrationBuilder.AddColumn<DateTime>(
                name: "EndAt",
                table: "PersonalEvents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "StartAt",
                table: "PersonalEvents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndAt",
                table: "PersonalEvents");

            migrationBuilder.DropColumn(
                name: "StartAt",
                table: "PersonalEvents");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "EndTime",
                table: "PersonalEvents",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<TimeSpan>(
                name: "StartTime",
                table: "PersonalEvents",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));
        }
    }
}
