using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class RMCPatronLobbyAndRoundEnd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "rmc_patron_lobby_messages",
                columns: table => new
                {
                    patron_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    message = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rmc_patron_lobby_messages", x => x.patron_id);
                    table.ForeignKey(
                        name: "FK_rmc_patron_lobby_messages_rmc_patrons_patron_id",
                        column: x => x.patron_id,
                        principalTable: "rmc_patrons",
                        principalColumn: "player_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rmc_patron_round_end_marine_shoutouts",
                columns: table => new
                {
                    patron_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rmc_patron_round_end_marine_shoutouts", x => x.patron_id);
                    table.ForeignKey(
                        name: "FK_rmc_patron_round_end_marine_shoutouts_rmc_patrons_patron_id",
                        column: x => x.patron_id,
                        principalTable: "rmc_patrons",
                        principalColumn: "player_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rmc_patron_round_end_xeno_shoutouts",
                columns: table => new
                {
                    patron_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rmc_patron_round_end_xeno_shoutouts", x => x.patron_id);
                    table.ForeignKey(
                        name: "FK_rmc_patron_round_end_xeno_shoutouts_rmc_patrons_patron_id",
                        column: x => x.patron_id,
                        principalTable: "rmc_patrons",
                        principalColumn: "player_id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rmc_patron_lobby_messages");

            migrationBuilder.DropTable(
                name: "rmc_patron_round_end_marine_shoutouts");

            migrationBuilder.DropTable(
                name: "rmc_patron_round_end_xeno_shoutouts");
        }
    }
}
