using System.ComponentModel.DataAnnotations;

namespace FortniteSpriteTracker.Shared.Collections;

public sealed record UpdateSpriteProgressRequest(
    [property: Required, StringLength(80)] string SpriteSlug,
    [property: Required, StringLength(40)] string Variant,
    bool IsOwned,
    bool IsMastered);
