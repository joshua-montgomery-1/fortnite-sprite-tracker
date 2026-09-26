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

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Slug",
                table: "VariantStyles",
                newName: "ImageSuffix");
        }
    }
}
