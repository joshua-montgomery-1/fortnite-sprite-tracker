namespace FortniteSpriteTracker.DataAccess.Entities;

public sealed class SpriteProgress
{
    public Guid UserId { get; set; }
    public int SpriteVariantId { get; set; }
    public bool IsOwned { get; set; }
    public bool IsMastered { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public UserAccount User { get; set; } = null!;
    public SpriteVariant SpriteVariant { get; set; } = null!;
}
