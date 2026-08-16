namespace FortniteSpriteTracker.Shared.Collections;

public sealed class SpriteProgressDto
{
    public required int SpriteVariantId { get; init; }
    public bool IsOwned { get; init; }
    public bool IsMastered { get; init; }
    public DateTimeOffset UpdatedAtUtc { get; init; }
}
