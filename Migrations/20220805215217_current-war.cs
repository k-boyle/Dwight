using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dwight.Migrations
{
    public partial class currentwar : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "WarRole",
                table: "guild_settings",
                newName: "WarRoleId");

            migrationBuilder.CreateTable(
                name: "current_wars",
                columns: table => new
                {
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    EnemyClan = table.Column<string>(type: "text", nullable: false),
                    DeclaredPosted = table.Column<bool>(type: "boolean", nullable: false),
                    StartedPosted = table.Column<bool>(type: "boolean", nullable: false),
                    ReminderPosted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_current_wars", x => x.GuildId);
                    table.ForeignKey(
                        name: "FK_current_wars_guild_settings_GuildId",
                        column: x => x.GuildId,
                        principalTable: "guild_settings",
                        principalColumn: "GuildId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_current_wars_GuildId",
                table: "current_wars",
                column: "GuildId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "current_wars");

            migrationBuilder.RenameColumn(
                name: "WarRoleId",
                table: "guild_settings",
                newName: "WarRole");
        }
    }
}
