namespace FortniteSpriteTracker.Shared.Profiles;

public sealed class UserProfileDto
{
    public required Guid PublicId { get; init; }
    public required string DisplayName { get; init; }
    public string? EpicDisplayName { get; init; }
    public bool IsCollectionPublic { get; init; }
    public bool IsProfileComplete { get; init; }
}
