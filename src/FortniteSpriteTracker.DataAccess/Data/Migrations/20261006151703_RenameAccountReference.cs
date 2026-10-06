using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FortniteSpriteTracker.DataAccess.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameAccountReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CentralAccountId",
                table: "Users",
                newName: "AccountId");

            migrationBuilder.RenameIndex(
                name: "IX_Users_CentralAccountId",
                table: "Users",
                newName: "IX_Users_AccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AccountId",
                table: "Users",
                newName: "CentralAccountId");

            migrationBuilder.RenameIndex(
                name: "IX_Users_AccountId",
                table: "Users",
                newName: "IX_Users_CentralAccountId");
        }
    }
}
