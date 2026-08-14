namespace FortniteSpriteTracker.DataAccess.Entities;

public sealed class SpriteVariant
{
    public int Id { get; set; }
    public int SpriteId { get; set; }
    public required string Name { get; set; }
    public required string ImagePath { get; set; }
    public Sprite Sprite { get; set; } = null!;
    public List<SpriteProgress> Progress { get; set; } = [];
}
