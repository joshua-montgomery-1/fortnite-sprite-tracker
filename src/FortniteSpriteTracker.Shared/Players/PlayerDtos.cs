using FortniteSpriteTracker.Shared.Collections;

namespace FortniteSpriteTracker.Shared.Players;

public sealed record PlayerSummaryDto(
    Guid PublicId,
    string DisplayName,
    string EpicDisplayName,
    int OwnedCount,
    int MasteredCount);

public sealed record PlayerCollectionDto(
    PlayerSummaryDto Player,
    IReadOnlyList<SpriteProgressDto> Collection,
    PlayerSummaryDto? Viewer,
    IReadOnlyList<SpriteProgressDto> ViewerCollection);
