namespace FortniteSpriteTracker.Server.Data.Entities;

public sealed class SpriteProgress
{
    public Guid UserId { get; set; }
    public required string SpriteSlug { get; set; }
    public required string Variant { get; set; }
    public bool IsOwned { get; set; }
    public bool IsMastered { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public UserAccount User { get; set; } = null!;
}
