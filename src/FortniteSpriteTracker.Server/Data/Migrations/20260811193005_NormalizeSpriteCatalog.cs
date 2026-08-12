using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FortniteSpriteTracker.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeSpriteCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SpriteVariantId",
                table: "SpriteProgress",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Seasons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Chapter = table.Column<int>(type: "integer", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Seasons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sprites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SeasonId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Slug = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Rarity = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Ability = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sprites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sprites_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SpriteVariants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SpriteId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ImagePath = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpriteVariants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpriteVariants_Sprites_SpriteId",
                        column: x => x.SpriteId,
                        principalTable: "Sprites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Seasons",
                columns: new[] { "Id", "Chapter", "Name", "Number" },
                values: new object[] { 1, 7, "Chapter 7 · Season 3", 3 });

            migrationBuilder.InsertData(
                table: "Sprites",
                columns: new[] { "Id", "Ability", "Name", "Rarity", "SeasonId", "Slug" },
                values: new object[,]
                {
                    { 1, "Replenishes shields for you and nearby squadmates while in water.", "Water", "Rare", 1, "water" },
                    { 2, "May reveal extra rare items when you open chests.", "Earth", "Rare", 1, "earth" },
                    { 3, "Releases a fiery burst after you deal enough damage.", "Fire", "Rare", 1, "fire" },
                    { 4, "Increases swim speed and boosts movement while under fire.", "Fishy", "Rare", 1, "fishy" },
                    { 5, "Increases sprint speed and jump height while equipped.", "Air", "Rare", 1, "air" },
                    { 6, "Emoting and jamming replenish your shields.", "Duck", "Epic", 1, "duck" },
                    { 7, "Briefly cloaks you whenever you reload.", "Ghost", "Epic", 1, "ghost" },
                    { 8, "Siphons health and shields after eliminations.", "Demon", "Epic", 1, "demon" },
                    { 9, "Makes your pickaxe deal significantly more damage.", "King", "Epic", 1, "king" },
                    { 10, "Grants a Shock Rock charge after enough damage.", "Aura", "Epic", 1, "drifter" },
                    { 11, "Traversal actions trigger speed, reload, and fire-rate Overdrive.", "Striker", "Epic", 1, "soccer" },
                    { 12, "Grants random loot each level and Legendary loot at max level.", "Dream", "Legendary", 1, "dream" },
                    { 13, "Can grant a short infinite-ammo effect at max mastery.", "Punk", "Legendary", 1, "punk" },
                    { 14, "Boosts your maximum Health and Shield while equipped.", "Boss", "Legendary", 1, "boss" },
                    { 15, "Reveals enemy foot trails for you and nearby squadmates.", "Seven", "Legendary", 1, "seven" },
                    { 16, "Opening Ammo Boxes has a chance to upgrade your weapon.", "Llama", "Legendary", 1, "llama" },
                    { 17, "Marks nearby rare Sprites or enemies carrying them - and you.", "Peely", "Legendary", 1, "peely" },
                    { 18, "Marks players who damage you and reveals their location.", "Grim Reaper", "Mythic", 1, "grimreaper" },
                    { 19, "Creates a Shield Bubble Jr. whenever you heal.", "Zero Point", "Mythic", 1, "zeropoint" },
                    { 20, "May award extra or Mythic loot after eliminations.", "Burnt Peanut", "Mythic", 1, "theburntpeanut" },
                    { 21, "Deploy the Bat Cape midair and improve rare-Sprite chest finds.", "Batman", "Mythic", 1, "batman" },
                    { 22, "Replenishes nearby shields after an elimination.", "Pollo", "Mythic", 1, "pollo" },
                    { 23, "Empowers destructive slides and boosts combat after slide-kicks.", "Vini Jr.", "Mythic", 1, "vinijr" },
                    { 24, "Knocks and eliminations briefly reveal nearby enemies.", "John Wick", "Mythic", 1, "johnwick" },
                    { 25, "Regenerates low health with Cloak and low gravity.", "Ironmouse", "Mythic", 1, "ironmouse" }
                });

            migrationBuilder.InsertData(
                table: "SpriteVariants",
                columns: new[] { "Id", "ImagePath", "Name", "SpriteId" },
                values: new object[,]
                {
                    { 1, "images/sprites/water_basic.webp", "Normal", 1 },
                    { 2, "images/sprites/water_gold.webp", "Gold", 1 },
                    { 3, "images/sprites/water_gummy.webp", "Gummy", 1 },
                    { 4, "images/sprites/water_galaxy.webp", "Galaxy", 1 },
                    { 5, "images/sprites/water_holofoil.webp", "Holofoil", 1 },
                    { 6, "images/sprites/water_quack.webp", "Quack", 1 },
                    { 7, "images/sprites/water_gem.webp", "Gem", 1 },
                    { 8, "images/sprites/earth_basic.webp", "Normal", 2 },
                    { 9, "images/sprites/earth_gold.webp", "Gold", 2 },
                    { 10, "images/sprites/earth_gummy.webp", "Gummy", 2 },
                    { 11, "images/sprites/earth_galaxy.webp", "Galaxy", 2 },
                    { 12, "images/sprites/earth_cube.webp", "Cube", 2 },
                    { 13, "images/sprites/earth_quack.webp", "Quack", 2 },
                    { 14, "images/sprites/earth_gem.webp", "Gem", 2 },
                    { 15, "images/sprites/fire_basic.webp", "Normal", 3 },
                    { 16, "images/sprites/fire_gold.webp", "Gold", 3 },
                    { 17, "images/sprites/fire_gummy.webp", "Gummy", 3 },
                    { 18, "images/sprites/fire_galaxy.webp", "Galaxy", 3 },
                    { 19, "images/sprites/fire_holofoil.webp", "Holofoil", 3 },
                    { 20, "images/sprites/fire_cube.webp", "Cube", 3 },
                    { 21, "images/sprites/fire_quack.webp", "Quack", 3 },
                    { 22, "images/sprites/fishy_basic.webp", "Normal", 4 },
                    { 23, "images/sprites/fishy_gold.webp", "Gold", 4 },
                    { 24, "images/sprites/fishy_gummy.webp", "Gummy", 4 },
                    { 25, "images/sprites/fishy_galaxy.webp", "Galaxy", 4 },
                    { 26, "images/sprites/fishy_cube.webp", "Cube", 4 },
                    { 27, "images/sprites/air_basic.webp", "Normal", 5 },
                    { 28, "images/sprites/air_gold.webp", "Gold", 5 },
                    { 29, "images/sprites/air_gummy.webp", "Gummy", 5 },
                    { 30, "images/sprites/air_galaxy.webp", "Galaxy", 5 },
                    { 31, "images/sprites/air_holofoil.webp", "Holofoil", 5 },
                    { 32, "images/sprites/duck_basic.webp", "Normal", 6 },
                    { 33, "images/sprites/duck_gold.webp", "Gold", 6 },
                    { 34, "images/sprites/duck_gummy.webp", "Gummy", 6 },
                    { 35, "images/sprites/duck_galaxy.webp", "Galaxy", 6 },
                    { 36, "images/sprites/duck_gem.webp", "Gem", 6 },
                    { 37, "images/sprites/ghost_basic.webp", "Normal", 7 },
                    { 38, "images/sprites/ghost_gold.webp", "Gold", 7 },
                    { 39, "images/sprites/ghost_gummy.webp", "Gummy", 7 },
                    { 40, "images/sprites/ghost_galaxy.webp", "Galaxy", 7 },
                    { 41, "images/sprites/ghost_holofoil.webp", "Holofoil", 7 },
                    { 42, "images/sprites/demon_basic.webp", "Normal", 8 },
                    { 43, "images/sprites/demon_gold.webp", "Gold", 8 },
                    { 44, "images/sprites/demon_gummy.webp", "Gummy", 8 },
                    { 45, "images/sprites/demon_galaxy.webp", "Galaxy", 8 },
                    { 46, "images/sprites/demon_gem.webp", "Gem", 8 },
                    { 47, "images/sprites/king_basic.webp", "Normal", 9 },
                    { 48, "images/sprites/king_gold.webp", "Gold", 9 },
                    { 49, "images/sprites/king_gummy.webp", "Gummy", 9 },
                    { 50, "images/sprites/king_galaxy.webp", "Galaxy", 9 },
                    { 51, "images/sprites/king_holofoil.webp", "Holofoil", 9 },
                    { 52, "images/sprites/drifter_basic.webp", "Normal", 10 },
                    { 53, "images/sprites/drifter_gold.webp", "Gold", 10 },
                    { 54, "images/sprites/drifter_gummy.webp", "Gummy", 10 },
                    { 55, "images/sprites/drifter_galaxy.webp", "Galaxy", 10 },
                    { 56, "images/sprites/drifter_gem.webp", "Gem", 10 },
                    { 57, "images/sprites/soccer_basic.webp", "Normal", 11 },
                    { 58, "images/sprites/soccer_gold.webp", "Gold", 11 },
                    { 59, "images/sprites/soccer_gummy.webp", "Gummy", 11 },
                    { 60, "images/sprites/soccer_galaxy.webp", "Galaxy", 11 },
                    { 61, "images/sprites/soccer_holofoil.webp", "Holofoil", 11 },
                    { 62, "images/sprites/dream_basic.webp", "Normal", 12 },
                    { 63, "images/sprites/dream_gold.webp", "Gold", 12 },
                    { 64, "images/sprites/dream_gummy.webp", "Gummy", 12 },
                    { 65, "images/sprites/dream_galaxy.webp", "Galaxy", 12 },
                    { 66, "images/sprites/dream_cube.webp", "Cube", 12 },
                    { 67, "images/sprites/punk_basic.webp", "Normal", 13 },
                    { 68, "images/sprites/punk_gold.webp", "Gold", 13 },
                    { 69, "images/sprites/punk_gummy.webp", "Gummy", 13 },
                    { 70, "images/sprites/punk_galaxy.webp", "Galaxy", 13 },
                    { 71, "images/sprites/punk_cube.webp", "Cube", 13 },
                    { 72, "images/sprites/boss_basic.webp", "Normal", 14 },
                    { 73, "images/sprites/boss_gold.webp", "Gold", 14 },
                    { 74, "images/sprites/boss_gummy.webp", "Gummy", 14 },
                    { 75, "images/sprites/boss_galaxy.webp", "Galaxy", 14 },
                    { 76, "images/sprites/boss_cube.webp", "Cube", 14 },
                    { 77, "images/sprites/seven_basic.webp", "Normal", 15 },
                    { 78, "images/sprites/seven_gold.webp", "Gold", 15 },
                    { 79, "images/sprites/seven_gummy.webp", "Gummy", 15 },
                    { 80, "images/sprites/seven_galaxy.webp", "Galaxy", 15 },
                    { 81, "images/sprites/seven_holofoil.webp", "Holofoil", 15 },
                    { 82, "images/sprites/llama_basic.webp", "Normal", 16 },
                    { 83, "images/sprites/llama_gold.webp", "Gold", 16 },
                    { 84, "images/sprites/llama_gummy.webp", "Gummy", 16 },
                    { 85, "images/sprites/llama_galaxy.webp", "Galaxy", 16 },
                    { 86, "images/sprites/llama_gem.webp", "Gem", 16 },
                    { 87, "images/sprites/peely_basic.webp", "Normal", 17 },
                    { 88, "images/sprites/peely_gold.webp", "Gold", 17 },
                    { 89, "images/sprites/peely_gummy.webp", "Gummy", 17 },
                    { 90, "images/sprites/peely_galaxy.webp", "Galaxy", 17 },
                    { 91, "images/sprites/peely_holofoil.webp", "Holofoil", 17 },
                    { 92, "images/sprites/grimreaper_basic.webp", "Normal", 18 },
                    { 93, "images/sprites/grimreaper_gold.webp", "Gold", 18 },
                    { 94, "images/sprites/grimreaper_gummy.webp", "Gummy", 18 },
                    { 95, "images/sprites/grimreaper_galaxy.webp", "Galaxy", 18 },
                    { 96, "images/sprites/grimreaper_cube.webp", "Cube", 18 },
                    { 97, "images/sprites/grimreaper_holofoil.webp", "Holofoil", 18 },
                    { 98, "images/sprites/grimreaper_gem.webp", "Gem", 18 },
                    { 99, "images/sprites/zeropoint_basic.webp", "Normal", 19 },
                    { 100, "images/sprites/zeropoint_gold.webp", "Gold", 19 },
                    { 101, "images/sprites/zeropoint_gummy.webp", "Gummy", 19 },
                    { 102, "images/sprites/zeropoint_galaxy.webp", "Galaxy", 19 },
                    { 103, "images/sprites/zeropoint_holofoil.webp", "Holofoil", 19 },
                    { 104, "images/sprites/zeropoint_cube.webp", "Cube", 19 },
                    { 105, "images/sprites/zeropoint_quack.webp", "Quack", 19 },
                    { 106, "images/sprites/zeropoint_gem.webp", "Gem", 19 },
                    { 107, "images/sprites/theburntpeanut_basic.webp", "Normal", 20 },
                    { 108, "images/sprites/batman_basic.webp", "Normal", 21 },
                    { 109, "images/sprites/batman_gold.webp", "Gold", 21 },
                    { 110, "images/sprites/batman_gummy.webp", "Gummy", 21 },
                    { 111, "images/sprites/batman_galaxy.webp", "Galaxy", 21 },
                    { 112, "images/sprites/batman_holofoil.webp", "Holofoil", 21 },
                    { 113, "images/sprites/batman_cube.webp", "Cube", 21 },
                    { 114, "images/sprites/pollo_basic.webp", "Normal", 22 },
                    { 115, "images/sprites/vinijr_basic.webp", "Normal", 23 },
                    { 116, "images/sprites/johnwick_basic.webp", "Normal", 24 },
                    { 117, "images/sprites/ironmouse_basic.webp", "Normal", 25 }
                });

            migrationBuilder.Sql(
                """
                UPDATE "SpriteProgress" AS progress
                SET "SpriteVariantId" = variant."Id"
                FROM "SpriteVariants" AS variant
                INNER JOIN "Sprites" AS sprite ON sprite."Id" = variant."SpriteId"
                WHERE sprite."Slug" = progress."SpriteSlug"
                  AND LOWER(variant."Name") = LOWER(progress."Variant");
                """);

            migrationBuilder.DropPrimaryKey(
                name: "PK_SpriteProgress",
                table: "SpriteProgress");

            migrationBuilder.DropColumn(
                name: "SpriteSlug",
                table: "SpriteProgress");

            migrationBuilder.DropColumn(
                name: "Variant",
                table: "SpriteProgress");

            migrationBuilder.AlterColumn<int>(
                name: "SpriteVariantId",
                table: "SpriteProgress",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_SpriteProgress",
                table: "SpriteProgress",
                columns: new[] { "UserId", "SpriteVariantId" });

            migrationBuilder.CreateIndex(
                name: "IX_SpriteProgress_SpriteVariantId",
                table: "SpriteProgress",
                column: "SpriteVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_Seasons_Chapter_Number",
                table: "Seasons",
                columns: new[] { "Chapter", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sprites_SeasonId_Name",
                table: "Sprites",
                columns: new[] { "SeasonId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sprites_Slug",
                table: "Sprites",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SpriteVariants_SpriteId_Name",
                table: "SpriteVariants",
                columns: new[] { "SpriteId", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SpriteProgress_SpriteVariants_SpriteVariantId",
                table: "SpriteProgress",
                column: "SpriteVariantId",
                principalTable: "SpriteVariants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SpriteProgress_SpriteVariants_SpriteVariantId",
                table: "SpriteProgress");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SpriteProgress",
                table: "SpriteProgress");

            migrationBuilder.DropIndex(
                name: "IX_SpriteProgress_SpriteVariantId",
                table: "SpriteProgress");

            migrationBuilder.AddColumn<string>(
                name: "SpriteSlug",
                table: "SpriteProgress",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Variant",
                table: "SpriteProgress",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "SpriteProgress" AS progress
                SET "SpriteSlug" = sprite."Slug",
                    "Variant" = variant."Name"
                FROM "SpriteVariants" AS variant
                INNER JOIN "Sprites" AS sprite ON sprite."Id" = variant."SpriteId"
                WHERE variant."Id" = progress."SpriteVariantId";
                """);

            migrationBuilder.DropColumn(
                name: "SpriteVariantId",
                table: "SpriteProgress");

            migrationBuilder.AlterColumn<string>(
                name: "SpriteSlug",
                table: "SpriteProgress",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(80)",
                oldMaxLength: 80,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Variant",
                table: "SpriteProgress",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_SpriteProgress",
                table: "SpriteProgress",
                columns: new[] { "UserId", "SpriteSlug", "Variant" });

            migrationBuilder.DropTable(
                name: "SpriteVariants");

            migrationBuilder.DropTable(
                name: "Sprites");

            migrationBuilder.DropTable(
                name: "Seasons");
        }
    }
}
