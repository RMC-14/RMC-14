using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class RMCPatronsRolePriority : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ulong>(
                name: "discord_role",
                table: "rmc_patron_tiers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0ul);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "rmc_patron_tiers",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "priority",
                table: "rmc_patron_tiers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_rmc_patron_tiers_discord_role",
                table: "rmc_patron_tiers",
                column: "discord_role",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_rmc_patron_tiers_discord_role",
                table: "rmc_patron_tiers");

            migrationBuilder.DropColumn(
                name: "discord_role",
                table: "rmc_patron_tiers");

            migrationBuilder.DropColumn(
                name: "name",
                table: "rmc_patron_tiers");

            migrationBuilder.DropColumn(
                name: "priority",
                table: "rmc_patron_tiers");
        }
    }
}
