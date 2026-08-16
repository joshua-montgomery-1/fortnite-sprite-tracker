namespace FortniteSpriteTracker.Shared.Collections;

public sealed class BatchUpdateSpriteProgressRequest
{
    public required IReadOnlyList<UpdateSpriteProgressRequest> Updates { get; init; }
}
