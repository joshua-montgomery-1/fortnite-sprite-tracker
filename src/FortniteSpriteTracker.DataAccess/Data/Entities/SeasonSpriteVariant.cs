namespace FortniteSpriteTracker.DataAccess.Entities;

public sealed class SeasonSpriteVariant
{
    public int SeasonId { get; set; }
    public int SpriteVariantId { get; set; }
    public Season Season { get; set; } = null!;
    public SpriteVariant SpriteVariant { get; set; } = null!;
}
