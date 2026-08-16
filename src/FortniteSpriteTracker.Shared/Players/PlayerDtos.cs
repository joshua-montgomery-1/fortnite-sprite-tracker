using FortniteSpriteTracker.Shared.Collections;

namespace FortniteSpriteTracker.Shared.Players;

public sealed class PlayerSummaryDto
{
    public required Guid PublicId { get; init; }
    public required string DisplayName { get; init; }
    public required string EpicDisplayName { get; init; }
    public bool IsCollectionPublic { get; init; }
    public int? OwnedCount { get; init; }
    public int? MasteredCount { get; init; }
}

public sealed class PlayerCollectionDto
{
    public required PlayerSummaryDto Player { get; init; }
    public required IReadOnlyList<SpriteProgressDto> Collection { get; init; }
    public PlayerSummaryDto? Viewer { get; init; }
    public required IReadOnlyList<SpriteProgressDto> ViewerCollection { get; init; }
    public bool CanCompare { get; init; }
}
