using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FortniteSpriteTracker.DataAccess.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DataProtectionKeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FriendlyName = table.Column<string>(type: "text", nullable: true),
                    Xml = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProtectionKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Seasons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Chapter = table.Column<int>(type: "integer", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    StartAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Seasons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SpriteFamilies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Slug = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpriteFamilies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PublicId = table.Column<Guid>(type: "uuid", nullable: false),
                    GoogleSubject = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    EpicDisplayName = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    NormalizedEpicDisplayName = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    IsCollectionPublic = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VariantStyles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Bonus = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ImageSuffix = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VariantStyles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SeasonSpriteFamilies",
                columns: table => new
                {
                    SeasonId = table.Column<int>(type: "integer", nullable: false),
                    SpriteFamilyId = table.Column<int>(type: "integer", nullable: false),
                    Rarity = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    RarityColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Ability = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PrimaryColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SecondaryColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeasonSpriteFamilies", x => new { x.SeasonId, x.SpriteFamilyId });
                    table.ForeignKey(
                        name: "FK_SeasonSpriteFamilies_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SeasonSpriteFamilies_SpriteFamilies_SpriteFamilyId",
                        column: x => x.SpriteFamilyId,
                        principalTable: "SpriteFamilies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SpriteVariants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SpriteFamilyId = table.Column<int>(type: "integer", nullable: false),
                    VariantStyleId = table.Column<int>(type: "integer", nullable: false),
                    ImagePath = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpriteVariants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpriteVariants_SpriteFamilies_SpriteFamilyId",
                        column: x => x.SpriteFamilyId,
                        principalTable: "SpriteFamilies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SpriteVariants_VariantStyles_VariantStyleId",
                        column: x => x.VariantStyleId,
                        principalTable: "VariantStyles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SeasonSpriteVariants",
                columns: table => new
                {
                    SeasonId = table.Column<int>(type: "integer", nullable: false),
                    SpriteVariantId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeasonSpriteVariants", x => new { x.SeasonId, x.SpriteVariantId });
                    table.ForeignKey(
                        name: "FK_SeasonSpriteVariants_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SeasonSpriteVariants_SpriteVariants_SpriteVariantId",
                        column: x => x.SpriteVariantId,
                        principalTable: "SpriteVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SpriteProgress",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    SpriteVariantId = table.Column<int>(type: "integer", nullable: false),
                    IsOwned = table.Column<bool>(type: "boolean", nullable: false),
                    IsMastered = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpriteProgress", x => new { x.UserId, x.SpriteVariantId });
                    table.ForeignKey(
                        name: "FK_SpriteProgress_SpriteVariants_SpriteVariantId",
                        column: x => x.SpriteVariantId,
                        principalTable: "SpriteVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SpriteProgress_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Seasons_Chapter_Number",
                table: "Seasons",
                columns: new[] { "Chapter", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SeasonSpriteFamilies_SeasonId_DisplayOrder",
                table: "SeasonSpriteFamilies",
                columns: new[] { "SeasonId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_SeasonSpriteFamilies_SpriteFamilyId",
                table: "SeasonSpriteFamilies",
                column: "SpriteFamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_SeasonSpriteVariants_SpriteVariantId",
                table: "SeasonSpriteVariants",
                column: "SpriteVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_SpriteFamilies_Slug",
                table: "SpriteFamilies",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SpriteProgress_SpriteVariantId",
                table: "SpriteProgress",
                column: "SpriteVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_SpriteVariants_SpriteFamilyId_VariantStyleId",
                table: "SpriteVariants",
                columns: new[] { "SpriteFamilyId", "VariantStyleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SpriteVariants_VariantStyleId",
                table: "SpriteVariants",
                column: "VariantStyleId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_GoogleSubject",
                table: "Users",
                column: "GoogleSubject",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_NormalizedEpicDisplayName",
                table: "Users",
                column: "NormalizedEpicDisplayName");

            migrationBuilder.CreateIndex(
                name: "IX_Users_PublicId",
                table: "Users",
                column: "PublicId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VariantStyles_Name",
                table: "VariantStyles",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataProtectionKeys");

            migrationBuilder.DropTable(
                name: "SeasonSpriteFamilies");

            migrationBuilder.DropTable(
                name: "SeasonSpriteVariants");

            migrationBuilder.DropTable(
                name: "SpriteProgress");

            migrationBuilder.DropTable(
                name: "Seasons");

            migrationBuilder.DropTable(
                name: "SpriteVariants");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "SpriteFamilies");

            migrationBuilder.DropTable(
                name: "VariantStyles");
        }
    }
}
