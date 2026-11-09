using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace FortniteSpriteTracker.DataAccess.Data.Migrations;

[DbContext(typeof(SpriteTrackerDbContext))]
[Migration("20261109010000_CorrectSeasonFiveAbilities")]
public sealed class CorrectSeasonFiveAbilities : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Base abilities cross-checked against https://fnvault.app/sprites and
        // https://screenhacker.com/fortnite-treasure-hunters-all-new-sprites-guide/
        // Only repair placeholders; preserve subsequent curated descriptions.
        migrationBuilder.Sql("""
            UPDATE "SeasonSpriteFamilies" AS membership
            SET "Ability" = abilities.description
            FROM "SpriteFamilies" AS family, "Seasons" AS season,
                (VALUES
                    ('halfpeeld', 'Shows elimination locations on your map.'),
                    ('re-pete', 'Speeds up reloads and can refund ammunition when reloading.'),
                    ('barry-ofish', 'Uses less sprint stamina in water.'),
                    ('chester', 'Can add loot to Treasure Chests and prevents fall damage.'),
                    ('exmarks', 'Locates Treasure Maps and Wanted Posters.'),
                    ('wayfinder', 'Detects nearby Sprites and adds rewards to Buried Chests.'),
                    ('kelper', 'Gradually heals you while in water.'),
                    ('sweet-dreams', 'Awards items as it levels up, with Mythic loot at maximum level.'),
                    ('lil-squibbly', 'Jumping into fishing holes catches fish and awards extra loot.'),
                    ('lil-flacko', 'Landing releases a small shockwave that pushes enemies away.'),
                    ('lil-uzi-vert-jr', 'Restoring shields triggers a Chug Splash effect.'),
                    ('plundarrgh', 'Opening chests boosts movement and pickaxe speed.'),
                    ('transponder-snail', 'Resource pickups grant extras, shared with teammates carrying this Sprite.')
                ) AS abilities(slug, description)
            WHERE membership."SpriteFamilyId" = family."Id"
                AND membership."SeasonId" = season."Id"
                AND season."Chapter" = 7 AND season."Number" = 5
                AND family."Slug" = abilities.slug
                AND lower(trim(membership."Ability")) = 'season sprite ability';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Keep corrected catalog content when rolling back application code.
    }
}
