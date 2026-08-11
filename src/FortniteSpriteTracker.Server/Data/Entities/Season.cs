namespace FortniteSpriteTracker.Server.Data.Entities;

public sealed class Season
{
    public int Id { get; set; }
    public int Chapter { get; set; }
    public int Number { get; set; }
    public required string Name { get; set; }
    public List<Sprite> Sprites { get; set; } = [];
}
