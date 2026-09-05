using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class RMCDistressSignalPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "rmc_distress_signal_carryover_votes",
                columns: table => new
                {
                    server_id = table.Column<int>(type: "integer", nullable: false),
                    planet_id = table.Column<string>(type: "text", nullable: false),
                    votes = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rmc_distress_signal_carryover_votes", x => new { x.server_id, x.planet_id });
                    table.ForeignKey(
                        name: "FK_rmc_distress_signal_carryover_votes_server_server_id",
                        column: x => x.server_id,
                        principalTable: "server",
                        principalColumn: "server_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rmc_distress_signal_rounds",
                columns: table => new
                {
                    round_id = table.Column<int>(type: "integer", nullable: false),
                    planet_id = table.Column<string>(type: "text", nullable: false),
                    result = table.Column<int>(type: "integer", nullable: true),
                    marines_per_xeno_before = table.Column<float>(type: "real", nullable: false),
                    marines_per_xeno_after = table.Column<float>(type: "real", nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    finished_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rmc_distress_signal_rounds", x => x.round_id);
                    table.ForeignKey(
                        name: "FK_rmc_distress_signal_rounds_round_round_id",
                        column: x => x.round_id,
                        principalTable: "round",
                        principalColumn: "round_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rmc_distress_signal_state",
                columns: table => new
                {
                    server_id = table.Column<int>(type: "integer", nullable: false),
                    marines_per_xeno = table.Column<float>(type: "real", nullable: false),
                    selected_planet_id = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rmc_distress_signal_state", x => x.server_id);
                    table.ForeignKey(
                        name: "FK_rmc_distress_signal_state_server_server_id",
                        column: x => x.server_id,
                        principalTable: "server",
                        principalColumn: "server_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_rmc_distress_signal_rounds_planet_id",
                table: "rmc_distress_signal_rounds",
                column: "planet_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rmc_distress_signal_carryover_votes");

            migrationBuilder.DropTable(
                name: "rmc_distress_signal_rounds");

            migrationBuilder.DropTable(
                name: "rmc_distress_signal_state");
        }
    }
}
