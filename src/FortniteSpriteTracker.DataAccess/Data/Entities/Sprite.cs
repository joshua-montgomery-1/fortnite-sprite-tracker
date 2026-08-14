namespace FortniteSpriteTracker.DataAccess.Entities;

public sealed class Sprite
{
    public int Id { get; set; }
    public int SeasonId { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public required string Rarity { get; set; }
    public required string Ability { get; set; }
    public Season Season { get; set; } = null!;
    public List<SpriteVariant> Variants { get; set; } = [];
}
