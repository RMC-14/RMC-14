using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class RMCPatronsRolePriority : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "discord_role",
                table: "rmc_patron_tiers",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "rmc_patron_tiers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "priority",
                table: "rmc_patron_tiers",
                type: "integer",
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
