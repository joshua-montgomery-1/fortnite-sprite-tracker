namespace FortniteSpriteTracker.DataAccess.Entities;

public sealed class VariantStyle
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Color { get; set; }
    public required string Bonus { get; set; }
    public required string ImageSuffix { get; set; }
    public int DisplayOrder { get; set; }
    public List<SpriteVariant> Variants { get; set; } = [];
}
