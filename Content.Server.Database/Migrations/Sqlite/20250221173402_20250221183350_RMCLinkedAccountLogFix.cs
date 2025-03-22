using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class _20250221183350_RMCLinkedAccountLogFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_rmc_linked_accounts_logs_player__player_id1",
                table: "rmc_linked_accounts_logs");

            migrationBuilder.AddForeignKey(
                name: "FK_rmc_linked_accounts_logs_player_player_id1",
                table: "rmc_linked_accounts_logs",
                column: "player_id",
                principalTable: "player",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_rmc_linked_accounts_logs_player_player_id1",
                table: "rmc_linked_accounts_logs");

            migrationBuilder.AddForeignKey(
                name: "FK_rmc_linked_accounts_logs_player__player_id1",
                table: "rmc_linked_accounts_logs",
                column: "player_id",
                principalTable: "player",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
