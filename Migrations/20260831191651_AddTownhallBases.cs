using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dwight.Migrations
{
    /// <inheritdoc />
    public partial class AddTownhallBases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "townhall_bases",
                columns: table => new
                {
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Link = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_townhall_bases", x => new { x.GuildId, x.Level });
                });

            // Backfill the base links that used to live in the "Clash:BaseLinkByLevel" config section,
            // for every guild the bot is already configured for.
            migrationBuilder.Sql("""
                INSERT INTO townhall_bases ("GuildId", "Level", "Link")
                SELECT gs."GuildId", seed.level, seed.link
                FROM guild_settings gs
                CROSS JOIN (VALUES
                    (17, 'https://link.clashofclans.com/en/?action=OpenLayout&id=TH17%3AWB%3AAAAAAgAAAAL0STpO94J_0xlLhBoIvlo7'),
                    (16, 'https://link.clashofclans.com/en?action=OpenLayout&id=TH16%3AWB%3AAAAAFAAAAAJ8GsLllTN31bw7jyUTcrLV'),
                    (15, 'https://link.clashofclans.com/en?action=OpenLayout&id=TH15%3AWB%3AAAAAIgAAAAJr19U9CANtPiBA5hc7MjNG'),
                    (14, 'https://link.clashofclans.com/en?action=OpenLayout&id=TH14%3AWB%3AAAAAAgAAAALbebjXe2AIygHLYa0816kc'),
                    (13, 'https://link.clashofclans.com/en?action=OpenLayout&id=TH13%3AWB%3AAAAAIwAAAAJqt0h6ttRhKEdly05yz8Je'),
                    (12, 'https://link.clashofclans.com/en?action=OpenLayout&id=TH12%3AWB%3AAAAAGwAAAAJ0REmG3zvS8ScAKtav26_K'),
                    (11, 'https://link.clashofclans.com/en?action=OpenLayout&id=TH11%3AWB%3AAAAAUgAAAAGcixSiz8R3aEYn85icOxGz'),
                    (10, 'https://link.clashofclans.com/en?action=OpenLayout&id=TH10%3AWB%3AAAAAPQAAAAInc6dn4p9xCQjDM3wDrRHJ'),
                    (9, 'https://link.clashofclans.com/en?action=OpenLayout&id=TH9%3AWB%3AAAAAEgAAAAKLXRyKLu3kn1vhcOhrxVEd'),
                    (8, 'https://link.clashofclans.com/en?action=OpenLayout&id=TH8%3AWB%3AAAAAGwAAAAJOby8BDYTGtTluqXtqUL50')
                ) AS seed(level, link);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "townhall_bases");
        }
    }
}
