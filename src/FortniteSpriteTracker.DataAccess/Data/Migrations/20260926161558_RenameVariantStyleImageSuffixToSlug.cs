using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FortniteSpriteTracker.DataAccess.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameVariantStyleImageSuffixToSlug : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ImageSuffix",
                table: "VariantStyles",
                newName: "Slug");

            migrationBuilder.Sql("""
                UPDATE "SpriteFamilies"
                SET "Slug" = CASE "Id"
                    WHEN 10 THEN 'aura'
                    WHEN 11 THEN 'striker'
                    WHEN 18 THEN 'grim-reaper'
                    WHEN 19 THEN 'zero-point'
                    WHEN 20 THEN 'burnt-peanut'
                    WHEN 23 THEN 'vini-jr'
                    WHEN 24 THEN 'john-wick'
                    WHEN 27 THEN '8-bit'
                    WHEN 37 THEN 'storm-scout'
                    WHEN 39 THEN 'mega-man'
                    WHEN 40 THEN 'x-ray'
                    WHEN 42 THEN 'crash-bandicoot'
                    ELSE "Slug"
                END
                WHERE "Id" IN (10, 11, 18, 19, 20, 23, 24, 27, 37, 39, 40, 42);

                UPDATE "VariantStyles"
                SET "Slug" = CASE "Id"
                    WHEN 1 THEN 'normal'
                    WHEN 9 THEN 'cheat-master'
                    WHEN 10 THEN 'loot-hacker'
                    WHEN 13 THEN 'bounty-hunter'
                    ELSE "Slug"
                END
                WHERE "Id" IN (1, 9, 10, 13);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "SpriteFamilies"
                SET "Slug" = CASE "Id"
                    WHEN 10 THEN 'drifter'
                    WHEN 11 THEN 'soccer'
                    WHEN 18 THEN 'grimreaper'
                    WHEN 19 THEN 'zeropoint'
                    WHEN 20 THEN 'theburntpeanut'
                    WHEN 23 THEN 'vinijr'
                    WHEN 24 THEN 'johnwick'
                    WHEN 27 THEN '8bit'
                    WHEN 37 THEN 'stormscout'
                    WHEN 39 THEN 'megaman'
                    WHEN 40 THEN 'xray'
                    WHEN 42 THEN 'crashbandicoot'
                    ELSE "Slug"
                END
                WHERE "Id" IN (10, 11, 18, 19, 20, 23, 24, 27, 37, 39, 40, 42);

                UPDATE "VariantStyles"
                SET "Slug" = CASE "Id"
                    WHEN 1 THEN 'basic'
                    WHEN 9 THEN 'cheatmaster'
                    WHEN 10 THEN 'hacker'
                    WHEN 13 THEN 'bountyhunter'
                    ELSE "Slug"
                END
                WHERE "Id" IN (1, 9, 10, 13);
                """);

            migrationBuilder.RenameColumn(
                name: "Slug",
                table: "VariantStyles",
                newName: "ImageSuffix");
        }
    }
}
