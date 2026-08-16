namespace FortniteSpriteTracker.DataAccess.Entities;

public sealed class SpriteFamily
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public int DisplayOrder { get; set; }
    public List<SpriteVariant> Variants { get; set; } = [];
    public List<SeasonSpriteFamily> Seasons { get; set; } = [];
}
