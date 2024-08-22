#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class RMCSquadPriority : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "squad_priority",
                table: "profile");

            migrationBuilder.CreateTable(
                name: "rmc_squad_priorities",
                columns: table => new
                {
                    profile_id = table.Column<int>(type: "INTEGER", nullable: false),
                    squad = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rmc_squad_priorities", x => x.profile_id);
                    table.ForeignKey(
                        name: "FK_rmc_squad_priorities_profile_profile_id",
                        column: x => x.profile_id,
                        principalTable: "profile",
                        principalColumn: "profile_id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rmc_squad_priorities");

            migrationBuilder.AddColumn<int>(
                name: "squad_priority",
                table: "profile",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
