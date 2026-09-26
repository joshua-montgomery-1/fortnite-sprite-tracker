namespace FortniteSpriteTracker.Shared.Catalog;

public sealed record SpriteVariantDetail(
    SpriteFamilyDto Family,
    SpriteVariantDto Variant,
    IReadOnlyList<SpriteVariantDto> RelatedVariants);

public static class SpriteCatalogRoutes
{
    public static string VariantPath(SpriteFamilyDto family, SpriteVariantDto variant) =>
        $"/sprites/{family.Slug}/{variant.Style.Slug}";

    public static SpriteVariantDetail? FindVariant(
        SpriteCatalogDto? catalog,
        string? familySlug,
        string? variantSlug)
    {
        if (catalog is null || string.IsNullOrWhiteSpace(familySlug) || string.IsNullOrWhiteSpace(variantSlug))
        {
            return null;
        }

        var family = catalog.Families.FirstOrDefault(item =>
            string.Equals(item.Slug, familySlug, StringComparison.OrdinalIgnoreCase));
        if (family is null)
        {
            return null;
        }

        var variant = family.Variants.FirstOrDefault(item =>
            string.Equals(item.Style.Slug, variantSlug, StringComparison.OrdinalIgnoreCase));
        if (variant is null)
        {
            return null;
        }

        return new SpriteVariantDetail(family, variant, family.Variants);
    }
}
