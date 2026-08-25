namespace FortniteSpriteTracker.DataAccess.Entities;

public sealed class CheatCodeCategory
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int DisplayOrder { get; set; }
    public List<CheatCode> CheatCodes { get; set; } = [];
}
