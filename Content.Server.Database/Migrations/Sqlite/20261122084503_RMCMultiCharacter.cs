using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class RMCMultiCharacter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "rmc_disabled_profiles",
                columns: table => new
                {
                    profile_id = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rmc_disabled_profiles", x => x.profile_id);
                    table.ForeignKey(
                        name: "FK_rmc_disabled_profiles_profile_profile_id",
                        column: x => x.profile_id,
                        principalTable: "profile",
                        principalColumn: "profile_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rmc_player_job_priorities",
                columns: table => new
                {
                    preference_id = table.Column<int>(type: "INTEGER", nullable: false),
                    job_name = table.Column<string>(type: "TEXT", nullable: false),
                    priority = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rmc_player_job_priorities", x => new { x.preference_id, x.job_name });
                    table.ForeignKey(
                        name: "FK_rmc_player_job_priorities_preference_preference_id",
                        column: x => x.preference_id,
                        principalTable: "preference",
                        principalColumn: "preference_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rmc_player_preferences",
                columns: table => new
                {
                    preference_id = table.Column<int>(type: "INTEGER", nullable: false),
                    preference_unavailable = table.Column<int>(type: "INTEGER", nullable: false),
                    xeno_prefix = table.Column<string>(type: "TEXT", nullable: false),
                    xeno_postfix = table.Column<string>(type: "TEXT", nullable: false),
                    playtime_perks = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rmc_player_preferences", x => x.preference_id);
                    table.ForeignKey(
                        name: "FK_rmc_player_preferences_preference_preference_id",
                        column: x => x.preference_id,
                        principalTable: "preference",
                        principalColumn: "preference_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(@"
INSERT INTO rmc_player_job_priorities (preference_id, job_name, priority)
SELECT pr.preference_id, j.job_name, j.priority
FROM job j
JOIN profile p ON p.profile_id = j.profile_id
JOIN preference pr ON pr.preference_id = p.preference_id
WHERE p.slot = pr.selected_character_slot;");

            migrationBuilder.Sql(@"
INSERT INTO rmc_player_preferences (preference_id, preference_unavailable, xeno_prefix, xeno_postfix, playtime_perks)
SELECT pr.preference_id, p.pref_unavailable, p.xeno_prefix, p.xeno_postfix, p.playtime_perks
FROM preference pr
JOIN profile p ON p.preference_id = pr.preference_id AND p.slot = pr.selected_character_slot;");

            migrationBuilder.Sql(@"
INSERT INTO rmc_disabled_profiles (profile_id)
SELECT p.profile_id
FROM profile p
JOIN preference pr ON pr.preference_id = p.preference_id
WHERE p.slot <> pr.selected_character_slot;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rmc_disabled_profiles");

            migrationBuilder.DropTable(
                name: "rmc_player_job_priorities");

            migrationBuilder.DropTable(
                name: "rmc_player_preferences");
        }
    }
}
