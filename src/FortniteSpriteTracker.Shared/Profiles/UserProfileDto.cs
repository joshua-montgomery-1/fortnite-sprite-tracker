namespace FortniteSpriteTracker.Shared.Profiles;

public sealed record UserProfileDto(
    Guid Id,
    Guid PublicId,
    string DisplayName,
    string? EpicDisplayName,
    bool IsProfileComplete);
