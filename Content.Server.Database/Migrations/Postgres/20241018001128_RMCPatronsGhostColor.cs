#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class RMCPatronsGhostColor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ghost_color",
                table: "rmc_patrons",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ghost_color",
                table: "rmc_patron_tiers",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ghost_color",
                table: "rmc_patrons");

            migrationBuilder.DropColumn(
                name: "ghost_color",
                table: "rmc_patron_tiers");
        }
    }
}
