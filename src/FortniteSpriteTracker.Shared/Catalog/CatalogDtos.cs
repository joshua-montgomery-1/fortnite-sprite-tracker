namespace FortniteSpriteTracker.Shared.Catalog;

public sealed class SpriteCatalogDto
{
    public required SeasonDto Season { get; init; }
    public required IReadOnlyList<VariantStyleDto> VariantStyles { get; init; }
    public required IReadOnlyList<SpriteFamilyDto> Families { get; init; }
    public int TotalEntries { get; init; }
}

public sealed class SeasonDto
{
    public int Id { get; init; }
    public int Chapter { get; init; }
    public int Number { get; init; }
    public required string Name { get; init; }
    public DateTimeOffset StartAt { get; init; }
    public DateTimeOffset? EndAt { get; init; }
    public bool IsActive { get; init; }
    public bool HasCatalog { get; init; }
}

public sealed class VariantStyleDto
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string Color { get; init; }
    public required string Bonus { get; init; }
    public int DisplayOrder { get; init; }
}

public sealed class SpriteFamilyDto
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string Slug { get; init; }
    public required string Rarity { get; init; }
    public required string RarityColor { get; init; }
    public required string Ability { get; init; }
    public required string PrimaryColor { get; init; }
    public required string SecondaryColor { get; init; }
    public int DisplayOrder { get; init; }
    public required string ImageUrl { get; init; }
    public required IReadOnlyList<SpriteVariantDto> Variants { get; init; }
}

public sealed class SpriteVariantDto
{
    public required int Id { get; init; }
    public required string ImagePath { get; init; }
    public required VariantStyleDto Style { get; init; }
}
