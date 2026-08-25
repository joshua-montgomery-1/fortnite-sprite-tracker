namespace FortniteSpriteTracker.DataAccess.Entities;

public sealed class Season
{
    public int Id { get; set; }
    public int Chapter { get; set; }
    public int Number { get; set; }
    public required string Name { get; set; }
    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset? EndAt { get; set; }
    public List<SeasonSpriteFamily> SpriteFamilies { get; set; } = [];
    public List<SeasonSpriteVariant> SpriteVariants { get; set; } = [];
    public List<CheatCode> CheatCodes { get; set; } = [];
}
