using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using NpgsqlTypes;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class RMCChatBans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "rmc_chat_bans",
                columns: table => new
                {
                    rmc_chat_bans_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    round_id = table.Column<int>(type: "integer", nullable: true),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    address = table.Column<NpgsqlInet>(type: "inet", nullable: true),
                    hwid = table.Column<byte[]>(type: "bytea", nullable: true),
                    hwid_type = table.Column<int>(type: "integer", nullable: true, defaultValue: 0),
                    type = table.Column<int>(type: "integer", nullable: false),
                    banned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: false),
                    banning_admin_id = table.Column<Guid>(type: "uuid", nullable: true),
                    unbanning_admin_id = table.Column<Guid>(type: "uuid", nullable: true),
                    unbanned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_edited_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_edited_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rmc_chat_bans", x => x.rmc_chat_bans_id);
                    table.ForeignKey(
                        name: "FK_rmc_chat_bans_player_banning_admin_id1",
                        column: x => x.banning_admin_id,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_rmc_chat_bans_player_last_edited_by_id1",
                        column: x => x.last_edited_by_id,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_rmc_chat_bans_player_player_id",
                        column: x => x.player_id,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_rmc_chat_bans_player_unbanning_admin_id1",
                        column: x => x.unbanning_admin_id,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_rmc_chat_bans_round_round_id",
                        column: x => x.round_id,
                        principalTable: "round",
                        principalColumn: "round_id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_rmc_chat_bans_address",
                table: "rmc_chat_bans",
                column: "address");

            migrationBuilder.CreateIndex(
                name: "IX_rmc_chat_bans_banning_admin_id",
                table: "rmc_chat_bans",
                column: "banning_admin_id");

            migrationBuilder.CreateIndex(
                name: "IX_rmc_chat_bans_last_edited_by_id",
                table: "rmc_chat_bans",
                column: "last_edited_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_rmc_chat_bans_player_id",
                table: "rmc_chat_bans",
                column: "player_id");

            migrationBuilder.CreateIndex(
                name: "IX_rmc_chat_bans_round_id",
                table: "rmc_chat_bans",
                column: "round_id");

            migrationBuilder.CreateIndex(
                name: "IX_rmc_chat_bans_unbanning_admin_id",
                table: "rmc_chat_bans",
                column: "unbanning_admin_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rmc_chat_bans");
        }
    }
}
