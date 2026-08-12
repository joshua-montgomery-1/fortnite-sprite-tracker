namespace FortniteSpriteTracker.Shared.Collections;

public sealed record BatchUpdateSpriteProgressRequest(
    IReadOnlyList<UpdateSpriteProgressRequest> Updates);
