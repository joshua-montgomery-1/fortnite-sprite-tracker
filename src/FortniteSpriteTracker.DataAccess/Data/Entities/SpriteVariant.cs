namespace FortniteSpriteTracker.DataAccess.Entities;

public sealed class SpriteVariant
{
    public int Id { get; set; }
    public int SpriteFamilyId { get; set; }
    public int VariantStyleId { get; set; }
    public required string ImagePath { get; set; }
    public SpriteFamily SpriteFamily { get; set; } = null!;
    public VariantStyle VariantStyle { get; set; } = null!;
    public List<SeasonSpriteVariant> Seasons { get; set; } = [];
    public List<SpriteProgress> Progress { get; set; } = [];
}
