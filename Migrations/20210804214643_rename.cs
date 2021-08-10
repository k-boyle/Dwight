using Microsoft.EntityFrameworkCore.Migrations;

namespace Dwight.Migrations
{
    public partial class rename : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Members_GuildSettings_GuildId",
                table: "Members");

            migrationBuilder.DropForeignKey(
                name: "FK_Reps_GuildSettings_GuildId",
                table: "Reps");

            migrationBuilder.DropForeignKey(
                name: "FK_Reps_Members_GuildId_DiscordId",
                table: "Reps");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Reps",
                table: "Reps");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Members",
                table: "Members");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GuildSettings",
                table: "GuildSettings");

            migrationBuilder.RenameTable(
                name: "Reps",
                newName: "fwa_reps");

            migrationBuilder.RenameTable(
                name: "Members",
                newName: "clan_members");

            migrationBuilder.RenameTable(
                name: "GuildSettings",
                newName: "guild_settings");

            migrationBuilder.RenameIndex(
                name: "IX_Reps_DiscordId",
                table: "fwa_reps",
                newName: "IX_fwa_reps_DiscordId");

            migrationBuilder.RenameIndex(
                name: "IX_Members_DiscordId",
                table: "clan_members",
                newName: "IX_clan_members_DiscordId");

            migrationBuilder.RenameIndex(
                name: "IX_GuildSettings_GuildId",
                table: "guild_settings",
                newName: "IX_guild_settings_GuildId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_fwa_reps",
                table: "fwa_reps",
                columns: new[] { "GuildId", "DiscordId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_clan_members",
                table: "clan_members",
                columns: new[] { "GuildId", "DiscordId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_guild_settings",
                table: "guild_settings",
                column: "GuildId");

            migrationBuilder.AddForeignKey(
                name: "FK_clan_members_guild_settings_GuildId",
                table: "clan_members",
                column: "GuildId",
                principalTable: "guild_settings",
                principalColumn: "GuildId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_fwa_reps_clan_members_GuildId_DiscordId",
                table: "fwa_reps",
                columns: new[] { "GuildId", "DiscordId" },
                principalTable: "clan_members",
                principalColumns: new[] { "GuildId", "DiscordId" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_fwa_reps_guild_settings_GuildId",
                table: "fwa_reps",
                column: "GuildId",
                principalTable: "guild_settings",
                principalColumn: "GuildId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_clan_members_guild_settings_GuildId",
                table: "clan_members");

            migrationBuilder.DropForeignKey(
                name: "FK_fwa_reps_clan_members_GuildId_DiscordId",
                table: "fwa_reps");

            migrationBuilder.DropForeignKey(
                name: "FK_fwa_reps_guild_settings_GuildId",
                table: "fwa_reps");

            migrationBuilder.DropPrimaryKey(
                name: "PK_guild_settings",
                table: "guild_settings");

            migrationBuilder.DropPrimaryKey(
                name: "PK_fwa_reps",
                table: "fwa_reps");

            migrationBuilder.DropPrimaryKey(
                name: "PK_clan_members",
                table: "clan_members");

            migrationBuilder.RenameTable(
                name: "guild_settings",
                newName: "GuildSettings");

            migrationBuilder.RenameTable(
                name: "fwa_reps",
                newName: "Reps");

            migrationBuilder.RenameTable(
                name: "clan_members",
                newName: "Members");

            migrationBuilder.RenameIndex(
                name: "IX_guild_settings_GuildId",
                table: "GuildSettings",
                newName: "IX_GuildSettings_GuildId");

            migrationBuilder.RenameIndex(
                name: "IX_fwa_reps_DiscordId",
                table: "Reps",
                newName: "IX_Reps_DiscordId");

            migrationBuilder.RenameIndex(
                name: "IX_clan_members_DiscordId",
                table: "Members",
                newName: "IX_Members_DiscordId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GuildSettings",
                table: "GuildSettings",
                column: "GuildId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Reps",
                table: "Reps",
                columns: new[] { "GuildId", "DiscordId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_Members",
                table: "Members",
                columns: new[] { "GuildId", "DiscordId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Members_GuildSettings_GuildId",
                table: "Members",
                column: "GuildId",
                principalTable: "GuildSettings",
                principalColumn: "GuildId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Reps_GuildSettings_GuildId",
                table: "Reps",
                column: "GuildId",
                principalTable: "GuildSettings",
                principalColumn: "GuildId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Reps_Members_GuildId_DiscordId",
                table: "Reps",
                columns: new[] { "GuildId", "DiscordId" },
                principalTable: "Members",
                principalColumns: new[] { "GuildId", "DiscordId" },
                onDelete: ReferentialAction.Cascade);
        }
    }
}
