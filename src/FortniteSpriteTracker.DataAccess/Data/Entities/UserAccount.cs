using FortniteSpriteTracker.Shared.Profiles;

namespace FortniteSpriteTracker.DataAccess.Entities;

public sealed class UserAccount
{
    public long Id { get; set; }
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public required string GoogleSubject { get; set; }
    public required string DisplayName { get; set; }
    public string? EpicDisplayName { get; set; }
    public string? NormalizedEpicDisplayName { get; set; }
    public bool IsCollectionPublic { get; set; } = true;
    public ThemePreference ThemePreference { get; set; } = ThemePreference.System;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public List<SpriteProgress> SpriteProgress { get; set; } = [];
    public List<TrackedPlayer> TrackedPlayers { get; set; } = [];
    public List<TrackedPlayer> TrackedBy { get; set; } = [];
}
