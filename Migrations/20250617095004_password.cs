using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dwight.Migrations
{
    public partial class password : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CwlSheetId",
                table: "guild_settings",
                newName: "Password");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Password",
                table: "guild_settings",
                newName: "CwlSheetId");
        }
    }
}
