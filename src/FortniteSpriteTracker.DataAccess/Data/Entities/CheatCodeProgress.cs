namespace FortniteSpriteTracker.DataAccess.Entities;

public sealed class CheatCodeProgress
{
    public long UserId { get; set; }
    public int CheatCodeId { get; set; }
    public DateTimeOffset UsedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public UserAccount User { get; set; } = null!;
    public CheatCode CheatCode { get; set; } = null!;
}
