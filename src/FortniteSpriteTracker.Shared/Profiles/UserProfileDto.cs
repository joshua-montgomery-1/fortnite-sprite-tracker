namespace FortniteSpriteTracker.Shared.Profiles;

public sealed record UserProfileDto(
    Guid Id,
    string DisplayName,
    string? EpicDisplayName,
    bool IsProfileComplete);
