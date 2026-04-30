using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentHelper.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDateToSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Recurrence",
                table: "ScheduleLessons");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "ScheduleLessons");

            migrationBuilder.AddColumn<int>(
                name: "DayOfWeek",
                table: "ScheduleLessons",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsEvenWeek",
                table: "ScheduleLessons",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LessonType",
                table: "ScheduleLessons",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Room",
                table: "ScheduleLessons",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DayOfWeek",
                table: "ScheduleLessons");

            migrationBuilder.DropColumn(
                name: "IsEvenWeek",
                table: "ScheduleLessons");

            migrationBuilder.DropColumn(
                name: "LessonType",
                table: "ScheduleLessons");

            migrationBuilder.DropColumn(
                name: "Room",
                table: "ScheduleLessons");

            migrationBuilder.AddColumn<string>(
                name: "Recurrence",
                table: "ScheduleLessons",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "ScheduleLessons",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
