using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FortniteSpriteTracker.DataAccess.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCentralAccountReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CentralAccountId",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_CentralAccountId",
                table: "Users",
                column: "CentralAccountId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_CentralAccountId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CentralAccountId",
                table: "Users");
        }
    }
}
