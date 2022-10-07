using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dwight.Migrations
{
    public partial class cwlincrementingreminders : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CwlReminderPosted",
                table: "current_wars");

            migrationBuilder.AddColumn<int>(
                name: "CwlReminderLastPosted",
                table: "current_wars",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CwlRemindersPosted",
                table: "current_wars",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CwlReminderLastPosted",
                table: "current_wars");

            migrationBuilder.DropColumn(
                name: "CwlRemindersPosted",
                table: "current_wars");

            migrationBuilder.AddColumn<bool>(
                name: "CwlReminderPosted",
                table: "current_wars",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
