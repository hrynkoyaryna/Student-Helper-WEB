using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentHelper.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixTeacherAndExamRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Tasks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Exams",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Exams",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Exams");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Exams");
        }
    }
}
