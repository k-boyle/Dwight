using Microsoft.EntityFrameworkCore.Migrations;

namespace Dwight.Migrations
{
    public partial class addedguildids : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reps_Members_DiscordId",
                table: "Reps");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Reps",
                table: "Reps");

            migrationBuilder.DropIndex(
                name: "IX_Reps_DiscordId",
                table: "Reps");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Members",
                table: "Members");

            migrationBuilder.DropIndex(
                name: "IX_Members_DiscordId",
                table: "Members");

            migrationBuilder.AddColumn<decimal>(
                name: "GuildId",
                table: "Reps",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "GuildId",
                table: "Members",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Reps",
                table: "Reps",
                columns: new[] { "GuildId", "DiscordId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_Members",
                table: "Members",
                columns: new[] { "GuildId", "DiscordId" });

            migrationBuilder.CreateIndex(
                name: "IX_Reps_DiscordId",
                table: "Reps",
                column: "DiscordId");

            migrationBuilder.CreateIndex(
                name: "IX_Members_DiscordId",
                table: "Members",
                column: "DiscordId");

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

        protected override void Down(MigrationBuilder migrationBuilder)
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

            migrationBuilder.DropIndex(
                name: "IX_Reps_DiscordId",
                table: "Reps");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Members",
                table: "Members");

            migrationBuilder.DropIndex(
                name: "IX_Members_DiscordId",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "GuildId",
                table: "Reps");

            migrationBuilder.DropColumn(
                name: "GuildId",
                table: "Members");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Reps",
                table: "Reps",
                column: "DiscordId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Members",
                table: "Members",
                column: "DiscordId");

            migrationBuilder.CreateIndex(
                name: "IX_Reps_DiscordId",
                table: "Reps",
                column: "DiscordId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Members_DiscordId",
                table: "Members",
                column: "DiscordId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Reps_Members_DiscordId",
                table: "Reps",
                column: "DiscordId",
                principalTable: "Members",
                principalColumn: "DiscordId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
