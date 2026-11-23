using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class RMCMultiCharacterCleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "playtime_perks",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "xeno_postfix",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "xeno_prefix",
                table: "profile");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "playtime_perks",
                table: "profile",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "xeno_postfix",
                table: "profile",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "xeno_prefix",
                table: "profile",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
