namespace FortniteSpriteTracker.Shared.Catalog;

public sealed record HeroSpriteViewModel(
    string ImageUrl,
    string Name,
    string Rarity,
    string RarityColor,
    string Ability);

public static class HeroSpriteSelector
{
    private static readonly IReadOnlyDictionary<string, int> RarityPriority =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["Mythic"] = 4,
            ["Legendary"] = 3,
            ["Epic"] = 2,
            ["Rare"] = 1
        };

    public static HeroSpriteViewModel? Select(SpriteCatalogDto? catalog)
    {
        var family = catalog?.Families
            .Where(item => !string.IsNullOrWhiteSpace(item.ImageUrl))
            .OrderByDescending(item => GetRarityPriority(item.Rarity))
            .ThenBy(item => item.DisplayOrder)
            .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Id)
            .FirstOrDefault();

        return family is null
            ? null
            : new HeroSpriteViewModel(
                family.ImageUrl,
                family.Name,
                family.Rarity,
                family.RarityColor,
                family.Ability);
    }

    private static int GetRarityPriority(string rarity) =>
        RarityPriority.GetValueOrDefault(rarity, 0);
}
