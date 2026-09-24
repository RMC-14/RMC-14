using System;
using Content.Server.Database;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    [DbContext(typeof(SqliteServerDbContext))]
    [Migration("20260206100000_RMCCommendationsSoftDelete")]
    public partial class RMCCommendationsSoftDelete : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "deleted",
                table: "rmc_commendations",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);


            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "rmc_commendations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "deleted_by_id",
                table: "rmc_commendations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_rmc_commendations_deleted_by_id",
                table: "rmc_commendations",
                column: "deleted_by_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

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
