using System.ComponentModel.DataAnnotations;

namespace FortniteSpriteTracker.Shared.Profiles;

public sealed record UpdateUserProfileRequest(
    [property: Required, StringLength(80, MinimumLength = 1)] string DisplayName,
    [property: StringLength(16, MinimumLength = 3)] string? EpicDisplayName);
