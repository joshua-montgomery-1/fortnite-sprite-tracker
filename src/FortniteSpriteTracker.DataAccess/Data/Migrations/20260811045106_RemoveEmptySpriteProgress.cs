using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FortniteSpriteTracker.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveEmptySpriteProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM "SpriteProgress"
                WHERE NOT "IsOwned" AND NOT "IsMastered";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Deleted empty progress rows contain no user state to restore.
        }
    }
}
