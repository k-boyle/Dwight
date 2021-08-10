using Microsoft.EntityFrameworkCore.Migrations;

namespace Dwight.Migrations
{
    public partial class moreprops : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "GeneralId",
                table: "guild_settings",
                newName: "RepRoleId");

            migrationBuilder.AlterColumn<string>(
                name: "ClanTag",
                table: "guild_settings",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CoLeaderRoleId",
                table: "guild_settings",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ElderRoleId",
                table: "guild_settings",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "GeneralChannelId",
                table: "guild_settings",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoLeaderRoleId",
                table: "guild_settings");

            migrationBuilder.DropColumn(
                name: "ElderRoleId",
                table: "guild_settings");

            migrationBuilder.DropColumn(
                name: "GeneralChannelId",
                table: "guild_settings");

            migrationBuilder.RenameColumn(
                name: "RepRoleId",
                table: "guild_settings",
                newName: "GeneralId");

            migrationBuilder.AlterColumn<string>(
                name: "ClanTag",
                table: "guild_settings",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
