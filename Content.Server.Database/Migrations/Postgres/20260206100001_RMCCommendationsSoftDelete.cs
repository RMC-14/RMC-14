using System;
using Content.Server.Database;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    [DbContext(typeof(PostgresServerDbContext))]
    [Migration("20260206100001_RMCCommendationsSoftDelete")]
    public partial class RMCCommendationsSoftDelete : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "deleted",
                table: "rmc_commendations",
                type: "boolean",
                nullable: false,
                defaultValue: false);


            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "rmc_commendations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "deleted_by_id",
                table: "rmc_commendations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_rmc_commendations_deleted_by_id",
                table: "rmc_commendations",
                column: "deleted_by_id");

            migrationBuilder.AddForeignKey(
                name: "FK_rmc_commendations_player_deleted_by_id",
                table: "rmc_commendations",
                column: "deleted_by_id",
                principalTable: "player",
                principalColumn: "user_id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_rmc_commendations_player_deleted_by_id",
                table: "rmc_commendations");

            migrationBuilder.DropIndex(
                name: "IX_rmc_commendations_deleted_by_id",
                table: "rmc_commendations");

            migrationBuilder.DropColumn(
                name: "deleted",
                table: "rmc_commendations");

            migrationBuilder.DropColumn(
                name: "deleted_by_id",
                table: "rmc_commendations");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "rmc_commendations");
        }
    }
}
