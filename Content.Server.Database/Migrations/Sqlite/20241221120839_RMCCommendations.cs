#nullable disable

using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class RMCCommendations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "rmc_commendations",
                columns: table => new
                {
                    rmc_commendations_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    giver_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    receiver_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    round_id = table.Column<int>(type: "INTEGER", nullable: false),
                    giver_name = table.Column<string>(type: "TEXT", nullable: false),
                    receiver_name = table.Column<string>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    text = table.Column<string>(type: "TEXT", nullable: false),
                    type = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rmc_commendations", x => x.rmc_commendations_id);
                    table.ForeignKey(
                        name: "FK_rmc_commendations_player_giver_id",
                        column: x => x.giver_id,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_rmc_commendations_player_receiver_id",
                        column: x => x.receiver_id,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_rmc_commendations_round_round_id",
                        column: x => x.round_id,
                        principalTable: "round",
                        principalColumn: "round_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_rmc_commendations_giver_id",
                table: "rmc_commendations",
                column: "giver_id");

            migrationBuilder.CreateIndex(
                name: "IX_rmc_commendations_receiver_id",
                table: "rmc_commendations",
                column: "receiver_id");

            migrationBuilder.CreateIndex(
                name: "IX_rmc_commendations_round_id",
                table: "rmc_commendations",
                column: "round_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rmc_commendations");
        }
    }
}
