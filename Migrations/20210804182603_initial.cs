using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Dwight.Migrations
{
    public partial class initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GuildSettings",
                columns: table => new
                {
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    WelcomeChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    VerifiedRoleId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    UnverifiedRoleId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    WarChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    StartTimeChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    RepChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    GeneralId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    CalendarLink = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildSettings", x => x.GuildId);
                });

            migrationBuilder.CreateTable(
                name: "Members",
                columns: table => new
                {
                    DiscordId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Tags = table.Column<string[]>(type: "text[]", nullable: true),
                    MainTag = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Members", x => x.DiscordId);
                });

            migrationBuilder.CreateTable(
                name: "Reps",
                columns: table => new
                {
                    DiscordId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    TimeZone = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reps", x => x.DiscordId);
                    table.ForeignKey(
                        name: "FK_Reps_Members_DiscordId",
                        column: x => x.DiscordId,
                        principalTable: "Members",
                        principalColumn: "DiscordId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GuildSettings_GuildId",
                table: "GuildSettings",
                column: "GuildId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Members_DiscordId",
                table: "Members",
                column: "DiscordId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reps_DiscordId",
                table: "Reps",
                column: "DiscordId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuildSettings");

            migrationBuilder.DropTable(
                name: "Reps");

            migrationBuilder.DropTable(
                name: "Members");
        }
    }
}
