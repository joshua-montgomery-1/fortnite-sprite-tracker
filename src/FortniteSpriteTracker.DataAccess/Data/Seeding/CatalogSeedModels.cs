namespace FortniteSpriteTracker.DataAccess.Seeding;

public sealed class VariantStyleSeed
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string Color { get; init; }
    public required string Bonus { get; init; }
    public required string ImageSuffix { get; init; }
    public required int DisplayOrder { get; init; }
}

public sealed class SpriteVariantSeed
{
    public required int Id { get; init; }
    public required int VariantStyleId { get; init; }
    public required string ImagePath { get; init; }
}

public sealed class SpriteFamilySeed
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string Slug { get; init; }
    public required string Rarity { get; init; }
    public required string RarityColor { get; init; }
    public required string Ability { get; init; }
    public required string PrimaryColor { get; init; }
    public required string SecondaryColor { get; init; }
    public required string ImagePath { get; init; }
    public required int DisplayOrder { get; init; }
    public required IReadOnlyList<SpriteVariantSeed> Variants { get; init; }
}
