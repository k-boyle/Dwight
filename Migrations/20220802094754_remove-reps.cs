using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dwight.Migrations
{
    public partial class removereps : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fwa_reps");

            migrationBuilder.DropColumn(
                name: "CalendarLink",
                table: "guild_settings");

            migrationBuilder.DropColumn(
                name: "RepRoleId",
                table: "guild_settings");

            migrationBuilder.DropColumn(
                name: "StartTimeChannelId",
                table: "guild_settings");

            migrationBuilder.AlterColumn<string[]>(
                name: "Tags",
                table: "clan_members",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0],
                oldClrType: typeof(string[]),
                oldType: "text[]",
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CalendarLink",
                table: "guild_settings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RepRoleId",
                table: "guild_settings",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "StartTimeChannelId",
                table: "guild_settings",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<string[]>(
                name: "Tags",
                table: "clan_members",
                type: "text[]",
                nullable: true,
                oldClrType: typeof(string[]),
                oldType: "text[]");

            migrationBuilder.CreateTable(
                name: "fwa_reps",
                columns: table => new
                {
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    DiscordId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    TimeZone = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fwa_reps", x => new { x.GuildId, x.DiscordId });
                    table.ForeignKey(
                        name: "FK_fwa_reps_clan_members_GuildId_DiscordId",
                        columns: x => new { x.GuildId, x.DiscordId },
                        principalTable: "clan_members",
                        principalColumns: new[] { "GuildId", "DiscordId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_fwa_reps_guild_settings_GuildId",
                        column: x => x.GuildId,
                        principalTable: "guild_settings",
                        principalColumn: "GuildId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fwa_reps_DiscordId",
                table: "fwa_reps",
                column: "DiscordId");
        }
    }
}
