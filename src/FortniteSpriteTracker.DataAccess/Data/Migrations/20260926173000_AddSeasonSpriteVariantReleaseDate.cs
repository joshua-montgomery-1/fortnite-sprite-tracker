using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FortniteSpriteTracker.DataAccess.Data.Migrations
{
    public partial class AddSeasonSpriteVariantReleaseDate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReleasedAt",
                table: "SeasonSpriteVariants",
                type: "timestamp with time zone",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReleasedAt",
                table: "SeasonSpriteVariants");
        }
    }
}
