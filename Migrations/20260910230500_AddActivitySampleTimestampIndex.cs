using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dwight.Migrations
{
    /// <inheritdoc />
    public partial class AddActivitySampleTimestampIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_activity_samples_Timestamp",
                table: "activity_samples",
                column: "Timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_activity_samples_Timestamp",
                table: "activity_samples");
        }
    }
}
