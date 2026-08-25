using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FortniteSpriteTracker.DataAccess.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCheatCodeTracker : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CheatCodeCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheatCodeCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CheatCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SeasonId = table.Column<int>(type: "integer", nullable: false),
                    CheatCodeCategoryId = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Requirement = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsTrackable = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheatCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CheatCodes_CheatCodeCategories_CheatCodeCategoryId",
                        column: x => x.CheatCodeCategoryId,
                        principalTable: "CheatCodeCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CheatCodes_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CheatCodeProgress",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    CheatCodeId = table.Column<int>(type: "integer", nullable: false),
                    UsedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheatCodeProgress", x => new { x.UserId, x.CheatCodeId });
                    table.ForeignKey(
                        name: "FK_CheatCodeProgress_CheatCodes_CheatCodeId",
                        column: x => x.CheatCodeId,
                        principalTable: "CheatCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CheatCodeProgress_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CheatCodeCategories_Name",
                table: "CheatCodeCategories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CheatCodeProgress_CheatCodeId",
                table: "CheatCodeProgress",
                column: "CheatCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_CheatCodes_CheatCodeCategoryId",
                table: "CheatCodes",
                column: "CheatCodeCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_CheatCodes_SeasonId_CheatCodeCategoryId_DisplayOrder",
                table: "CheatCodes",
                columns: new[] { "SeasonId", "CheatCodeCategoryId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_CheatCodes_SeasonId_Code",
                table: "CheatCodes",
                columns: new[] { "SeasonId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CheatCodeProgress");

            migrationBuilder.DropTable(
                name: "CheatCodes");

            migrationBuilder.DropTable(
                name: "CheatCodeCategories");
        }
    }
}
