using FortniteSpriteTracker.Shared.Catalog;

namespace FortniteSpriteTracker.Shared.CheatCodes;

public sealed class CheatCodeCatalogDto
{
    public required SeasonDto Season { get; init; }
    public required IReadOnlyList<CheatCodeCategoryDto> Categories { get; init; }
    public int TrackableCodeCount { get; init; }
}

public sealed class CheatCodeCategoryDto
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public int DisplayOrder { get; init; }
    public required IReadOnlyList<CheatCodeDto> Codes { get; init; }
}

public sealed class CheatCodeDto
{
    public required int Id { get; init; }
    public required string Code { get; init; }
    public required string Description { get; init; }
    public string? Requirement { get; init; }
    public bool IsTrackable { get; init; }
    public int DisplayOrder { get; init; }
}
