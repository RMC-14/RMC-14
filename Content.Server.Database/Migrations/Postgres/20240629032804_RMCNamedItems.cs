using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class RMCNamedItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "rmc_named_items",
                columns: table => new
                {
                    profile_id = table.Column<int>(type: "integer", nullable: false),
                    primary_gun_name = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    sidearm_name = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    helmet_name = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    armor_name = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rmc_named_items", x => x.profile_id);
                    table.ForeignKey(
                        name: "FK_rmc_named_items_profile_profile_id",
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
                name: "rmc_named_items");
        }
    }
}
