using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class RMCRankPreference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "rank_preference",
                table: "profile");

            migrationBuilder.CreateTable(
                name: "rank",
                columns: table => new
                {
                    rank_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    profile_id = table.Column<int>(type: "INTEGER", nullable: false),
                    job_name = table.Column<string>(type: "TEXT", nullable: false),
                    priority = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rank", x => x.rank_id);
                    table.ForeignKey(
                        name: "FK_rank_profile_profile_id",
                        column: x => x.profile_id,
                        principalTable: "profile",
                        principalColumn: "profile_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_rank_profile_id",
                table: "rank",
                column: "profile_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rank");

            migrationBuilder.AddColumn<string>(
                name: "rank_preference",
                table: "profile",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
