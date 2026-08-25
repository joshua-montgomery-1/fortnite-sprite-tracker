namespace FortniteSpriteTracker.Shared.CheatCodes;

public sealed class CheatCodeProgressDto
{
    public required int CheatCodeId { get; init; }
    public DateTimeOffset UsedAtUtc { get; init; }
}

public sealed class UpdateCheatCodeProgressRequest
{
    public required int CheatCodeId { get; init; }
    public bool IsUsed { get; init; }
}
