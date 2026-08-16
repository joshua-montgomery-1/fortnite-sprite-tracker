namespace FortniteSpriteTracker.Shared.Collections;

public sealed class UpdateSpriteProgressRequest
{
    public required int SpriteVariantId { get; init; }
    public bool IsOwned { get; init; }
    public bool IsMastered { get; init; }
}
