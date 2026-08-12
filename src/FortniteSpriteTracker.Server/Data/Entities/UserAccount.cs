namespace FortniteSpriteTracker.Server.Data.Entities;

public sealed class UserAccount
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PublicId { get; set; } = Guid.NewGuid();
    public required string GoogleSubject { get; set; }
    public required string DisplayName { get; set; }
    public string? EpicDisplayName { get; set; }
    public string? NormalizedEpicDisplayName { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public List<SpriteProgress> SpriteProgress { get; set; } = [];
}
