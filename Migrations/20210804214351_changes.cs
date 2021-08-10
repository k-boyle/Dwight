using Microsoft.EntityFrameworkCore.Migrations;

namespace Dwight.Migrations
{
    public partial class changes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RepChannelId",
                table: "GuildSettings");

            migrationBuilder.AddColumn<string>(
                name: "ClanTag",
                table: "GuildSettings",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClanTag",
                table: "GuildSettings");

            migrationBuilder.AddColumn<decimal>(
                name: "RepChannelId",
                table: "GuildSettings",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
