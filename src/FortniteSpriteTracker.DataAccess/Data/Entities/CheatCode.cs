namespace FortniteSpriteTracker.DataAccess.Entities;

public sealed class CheatCode
{
    public int Id { get; set; }
    public int SeasonId { get; set; }
    public int CheatCodeCategoryId { get; set; }
    public required string Code { get; set; }
    public required string Description { get; set; }
    public string? Requirement { get; set; }
    public bool IsTrackable { get; set; } = true;
    public int DisplayOrder { get; set; }
    public Season Season { get; set; } = null!;
    public CheatCodeCategory Category { get; set; } = null!;
    public List<CheatCodeProgress> Progress { get; set; } = [];
}
