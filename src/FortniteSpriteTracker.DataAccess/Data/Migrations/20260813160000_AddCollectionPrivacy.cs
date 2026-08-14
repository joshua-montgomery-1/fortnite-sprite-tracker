using FortniteSpriteTracker.DataAccess;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FortniteSpriteTracker.DataAccess.Migrations;

[DbContext(typeof(SpriteTrackerDbContext))]
[Migration("20260813160000_AddCollectionPrivacy")]
public partial class AddCollectionPrivacy : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsCollectionPublic",
            table: "Users",
            type: "boolean",
            nullable: false,
            defaultValue: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "IsCollectionPublic",
            table: "Users");
    }
}
