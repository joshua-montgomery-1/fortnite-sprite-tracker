namespace FortniteSpriteTracker.DataAccess.Entities;

public sealed class TrackedPlayer
{
    public long UserId { get; set; }
    public UserAccount User { get; set; } = null!;
    public long PlayerId { get; set; }
    public UserAccount Player { get; set; } = null!;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
