namespace FortniteSpriteTracker.DataAccess.Entities;

public sealed class SeasonSpriteFamily
{
    public int SeasonId { get; set; }
    public int SpriteFamilyId { get; set; }
    public required string Rarity { get; set; }
    public required string RarityColor { get; set; }
    public required string Ability { get; set; }
    public required string PrimaryColor { get; set; }
    public required string SecondaryColor { get; set; }
    public int DisplayOrder { get; set; }
    public Season Season { get; set; } = null!;
    public SpriteFamily SpriteFamily { get; set; } = null!;
}
