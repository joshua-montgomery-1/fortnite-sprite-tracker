namespace FortniteSpriteTracker.Shared.Collections;

public sealed record SpriteProgressDto(
    string SpriteSlug,
    string Variant,
    bool IsOwned,
    bool IsMastered,
    DateTimeOffset UpdatedAtUtc);
