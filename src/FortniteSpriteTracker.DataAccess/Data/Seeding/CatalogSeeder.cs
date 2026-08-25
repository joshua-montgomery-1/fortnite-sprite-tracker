using FortniteSpriteTracker.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace FortniteSpriteTracker.DataAccess.Seeding;

public sealed class CatalogSeedResult
{
    public int Inserted { get; init; }
    public int Updated { get; init; }
}

public sealed class CatalogSeeder(SpriteTrackerDbContext database)
{
    public async Task<CatalogSeedResult> SeedAsync(CancellationToken cancellationToken = default)
    {
        ValidateDefinitions();
        var strategy = database.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(() => SeedOnceAsync(cancellationToken));
    }

    private async Task<CatalogSeedResult> SeedOnceAsync(CancellationToken cancellationToken)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        var inserted = 0;
        var updated = 0;

        var season = await database.Seasons.SingleOrDefaultAsync(
            item => item.Id == CatalogSeedData.SeasonId,
            cancellationToken);
        if (season is null)
        {
            season = new Season
            {
                Id = CatalogSeedData.SeasonId,
                Chapter = 7,
                Number = 3,
                Name = "Chapter 7 · Season 3",
                StartAt = new DateTimeOffset(2026, 6, 6, 1, 0, 0, TimeSpan.FromHours(-4)).ToUniversalTime(),
                EndAt = new DateTimeOffset(2026, 8, 19, 19, 0, 0, TimeSpan.FromHours(-4)).ToUniversalTime()
            };
            database.Seasons.Add(season);
            inserted++;
        }
        else if (ApplySeason(season))
        {
            updated++;
        }

        var nextSeason = await database.Seasons.SingleOrDefaultAsync(
            item => item.Id == 2,
            cancellationToken);
        if (nextSeason is null)
        {
            nextSeason = new Season
            {
                Id = 2,
                Chapter = 7,
                Number = 4,
                Name = "Chapter 7 · Season 4",
                StartAt = new DateTimeOffset(2026, 8, 20, 1, 0, 0, TimeSpan.FromHours(-4)).ToUniversalTime(),
                EndAt = null
            };
            database.Seasons.Add(nextSeason);
            inserted++;
        }
        else if (ApplyNextSeason(nextSeason))
        {
            updated++;
        }

        var styles = await database.VariantStyles.ToDictionaryAsync(item => item.Id, cancellationToken);
        foreach (var definition in CatalogSeedData.VariantStyles)
        {
            if (!styles.TryGetValue(definition.Id, out var style))
            {
                style = new VariantStyle
                {
                    Id = definition.Id,
                    Name = definition.Name,
                    Color = definition.Color,
                    Bonus = definition.Bonus,
                    ImageSuffix = definition.ImageSuffix,
                    DisplayOrder = definition.DisplayOrder
                };
                database.VariantStyles.Add(style);
                inserted++;
            }
            else if (ApplyStyle(style, definition))
            {
                updated++;
            }
        }

        var families = await database.SpriteFamilies.ToDictionaryAsync(item => item.Id, cancellationToken);
        var variants = await database.SpriteVariants.ToDictionaryAsync(item => item.Id, cancellationToken);
        var seasonFamilies = await database.SeasonSpriteFamilies
            .Where(item => item.SeasonId == CatalogSeedData.SeasonId)
            .ToDictionaryAsync(item => item.SpriteFamilyId, cancellationToken);
        var seasonVariants = await database.SeasonSpriteVariants
            .Where(item => item.SeasonId == CatalogSeedData.SeasonId)
            .ToDictionaryAsync(item => item.SpriteVariantId, cancellationToken);

        foreach (var definition in CatalogSeedData.Families)
        {
            if (!families.TryGetValue(definition.Id, out var family))
            {
                family = new SpriteFamily
                {
                    Id = definition.Id,
                    Name = definition.Name,
                    Slug = definition.Slug,
                    DisplayOrder = definition.DisplayOrder
                };
                database.SpriteFamilies.Add(family);
                inserted++;
            }
            else if (ApplyFamily(family, definition))
            {
                updated++;
            }

            if (!seasonFamilies.TryGetValue(definition.Id, out var seasonFamily))
            {
                seasonFamily = new SeasonSpriteFamily
                {
                    SeasonId = CatalogSeedData.SeasonId,
                    SpriteFamilyId = definition.Id,
                    Rarity = definition.Rarity,
                    RarityColor = definition.RarityColor,
                    Ability = definition.Ability,
                    PrimaryColor = definition.PrimaryColor,
                    SecondaryColor = definition.SecondaryColor,
                    ImagePath = definition.ImagePath,
                    DisplayOrder = definition.DisplayOrder
                };
                database.SeasonSpriteFamilies.Add(seasonFamily);
                inserted++;
            }
            else if (ApplySeasonFamily(seasonFamily, definition))
            {
                updated++;
            }

            foreach (var variantDefinition in definition.Variants)
            {
                if (!variants.TryGetValue(variantDefinition.Id, out var variant))
                {
                    variant = new SpriteVariant
                    {
                        Id = variantDefinition.Id,
                        SpriteFamilyId = definition.Id,
                        VariantStyleId = variantDefinition.VariantStyleId,
                        ImagePath = variantDefinition.ImagePath
                    };
                    database.SpriteVariants.Add(variant);
                    inserted++;
                }
                else if (ApplyVariant(variant, definition.Id, variantDefinition))
                {
                    updated++;
                }

                if (!seasonVariants.ContainsKey(variantDefinition.Id))
                {
                    var seasonVariant = new SeasonSpriteVariant
                    {
                        SeasonId = CatalogSeedData.SeasonId,
                        SpriteVariantId = variantDefinition.Id
                    };
                    database.SeasonSpriteVariants.Add(seasonVariant);
                    inserted++;
                }
            }
        }

        var categories = await database.CheatCodeCategories.ToDictionaryAsync(item => item.Id, cancellationToken);
        foreach (var definition in CheatCodeSeedData.Categories)
        {
            if (!categories.TryGetValue(definition.Id, out var category))
            {
                database.CheatCodeCategories.Add(new CheatCodeCategory
                {
                    Id = definition.Id,
                    Name = definition.Name,
                    DisplayOrder = definition.DisplayOrder
                });
                inserted++;
            }
            else if (category.Name != definition.Name || category.DisplayOrder != definition.DisplayOrder)
            {
                category.Name = definition.Name;
                category.DisplayOrder = definition.DisplayOrder;
                updated++;
            }
        }

        var cheatCodes = await database.CheatCodes
            .Where(item => item.SeasonId == CheatCodeSeedData.SeasonId)
            .ToDictionaryAsync(item => item.Id, cancellationToken);
        foreach (var definition in CheatCodeSeedData.Codes)
        {
            if (!cheatCodes.TryGetValue(definition.Id, out var code))
            {
                database.CheatCodes.Add(new CheatCode
                {
                    Id = definition.Id,
                    SeasonId = CheatCodeSeedData.SeasonId,
                    CheatCodeCategoryId = definition.CategoryId,
                    Code = definition.Code,
                    Description = definition.Description,
                    Requirement = definition.Requirement,
                    IsTrackable = definition.IsTrackable,
                    DisplayOrder = definition.DisplayOrder
                });
                inserted++;
            }
            else if (ApplyCheatCode(code, definition))
            {
                updated++;
            }
        }

        await database.SaveChangesAsync(cancellationToken);
        await database.Database.ExecuteSqlRawAsync(
            """
            SELECT setval(
                pg_get_serial_sequence('"VariantStyles"', 'Id'),
                GREATEST((SELECT MAX("Id") FROM "VariantStyles"), 1));
            SELECT setval(
                pg_get_serial_sequence('"SpriteFamilies"', 'Id'),
                GREATEST((SELECT MAX("Id") FROM "SpriteFamilies"), 1));
            SELECT setval(
                pg_get_serial_sequence('"SpriteVariants"', 'Id'),
                GREATEST((SELECT MAX("Id") FROM "SpriteVariants"), 1));
            SELECT setval(
                pg_get_serial_sequence('"CheatCodeCategories"', 'Id'),
                GREATEST((SELECT MAX("Id") FROM "CheatCodeCategories"), 1));
            SELECT setval(
                pg_get_serial_sequence('"CheatCodes"', 'Id'),
                GREATEST((SELECT MAX("Id") FROM "CheatCodes"), 1));
            """,
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new CatalogSeedResult
        {
            Inserted = inserted,
            Updated = updated
        };
    }

    private static void ValidateDefinitions()
    {
        if (CatalogSeedData.Families.Count != 25 || CatalogSeedData.Families.Sum(item => item.Variants.Count) != 117)
        {
            throw new InvalidOperationException("The committed catalog must contain 25 families and 117 variants.");
        }

        if (CheatCodeSeedData.Categories.Count != 6 || CheatCodeSeedData.Codes.Count != 22 ||
            CheatCodeSeedData.Codes.Count(item => !item.IsTrackable) != 2 ||
            CheatCodeSeedData.Codes.Select(item => item.Code).Distinct(StringComparer.Ordinal).Count() != CheatCodeSeedData.Codes.Count ||
            CheatCodeSeedData.Codes.Any(item => item.Code != item.Code.ToUpperInvariant()))
        {
            throw new InvalidOperationException("The Season 4 Lobby Hack catalog must contain 22 uniquely-cased codes, including two reference-only effects.");
        }

        var styleIds = CatalogSeedData.VariantStyles.Select(item => item.Id).ToArray();
        var familyIds = CatalogSeedData.Families.Select(item => item.Id).ToArray();
        var variantIds = CatalogSeedData.Families.SelectMany(item => item.Variants).Select(item => item.Id).ToArray();
        if (styleIds.Any(item => item <= 0) || styleIds.Distinct().Count() != styleIds.Length ||
            familyIds.Any(item => item <= 0) || familyIds.Distinct().Count() != familyIds.Length ||
            variantIds.Any(item => item <= 0) || variantIds.Distinct().Count() != variantIds.Length)
        {
            throw new InvalidOperationException("Every committed catalog ID must be a unique positive integer within its entity type.");
        }
    }

    private static bool ApplySeason(Season season)
    {
        var startAt = new DateTimeOffset(2026, 6, 6, 1, 0, 0, TimeSpan.FromHours(-4)).ToUniversalTime();
        var endAt = new DateTimeOffset(2026, 8, 19, 19, 0, 0, TimeSpan.FromHours(-4)).ToUniversalTime();
        var changed = season.Chapter != 7 || season.Number != 3 || season.Name != "Chapter 7 · Season 3" ||
            season.StartAt != startAt || season.EndAt != endAt;
        season.Chapter = 7;
        season.Number = 3;
        season.Name = "Chapter 7 · Season 3";
        season.StartAt = startAt;
        season.EndAt = endAt;
        return changed;
    }

    private static bool ApplyNextSeason(Season season)
    {
        var startAt = new DateTimeOffset(2026, 8, 20, 1, 0, 0, TimeSpan.FromHours(-4)).ToUniversalTime();
        var changed = season.Chapter != 7 || season.Number != 4 || season.Name != "Chapter 7 · Season 4" ||
            season.StartAt != startAt || season.EndAt is not null;
        season.Chapter = 7;
        season.Number = 4;
        season.Name = "Chapter 7 · Season 4";
        season.StartAt = startAt;
        season.EndAt = null;
        return changed;
    }

    private static bool ApplyStyle(VariantStyle style, VariantStyleSeed definition)
    {
        var changed = style.Name != definition.Name || style.Color != definition.Color ||
            style.Bonus != definition.Bonus || style.ImageSuffix != definition.ImageSuffix ||
            style.DisplayOrder != definition.DisplayOrder;
        style.Name = definition.Name;
        style.Color = definition.Color;
        style.Bonus = definition.Bonus;
        style.ImageSuffix = definition.ImageSuffix;
        style.DisplayOrder = definition.DisplayOrder;
        return changed;
    }

    private static bool ApplyFamily(SpriteFamily family, SpriteFamilySeed definition)
    {
        var changed = family.Name != definition.Name || family.Slug != definition.Slug ||
            family.DisplayOrder != definition.DisplayOrder;
        family.Name = definition.Name;
        family.Slug = definition.Slug;
        family.DisplayOrder = definition.DisplayOrder;
        return changed;
    }

    private static bool ApplySeasonFamily(SeasonSpriteFamily item, SpriteFamilySeed definition)
    {
        var changed = item.Rarity != definition.Rarity || item.RarityColor != definition.RarityColor ||
            item.Ability != definition.Ability || item.PrimaryColor != definition.PrimaryColor ||
            item.SecondaryColor != definition.SecondaryColor || item.ImagePath != definition.ImagePath ||
            item.DisplayOrder != definition.DisplayOrder;
        item.Rarity = definition.Rarity;
        item.RarityColor = definition.RarityColor;
        item.Ability = definition.Ability;
        item.PrimaryColor = definition.PrimaryColor;
        item.SecondaryColor = definition.SecondaryColor;
        item.ImagePath = definition.ImagePath;
        item.DisplayOrder = definition.DisplayOrder;
        return changed;
    }

    private static bool ApplyVariant(SpriteVariant variant, int familyId, SpriteVariantSeed definition)
    {
        var changed = variant.SpriteFamilyId != familyId || variant.VariantStyleId != definition.VariantStyleId ||
            variant.ImagePath != definition.ImagePath;
        variant.SpriteFamilyId = familyId;
        variant.VariantStyleId = definition.VariantStyleId;
        variant.ImagePath = definition.ImagePath;
        return changed;
    }

    private static bool ApplyCheatCode(CheatCode code, CheatCodeSeed definition)
    {
        var changed = code.CheatCodeCategoryId != definition.CategoryId || code.Code != definition.Code ||
            code.Description != definition.Description || code.Requirement != definition.Requirement ||
            code.IsTrackable != definition.IsTrackable || code.DisplayOrder != definition.DisplayOrder;
        code.CheatCodeCategoryId = definition.CategoryId;
        code.Code = definition.Code;
        code.Description = definition.Description;
        code.Requirement = definition.Requirement;
        code.IsTrackable = definition.IsTrackable;
        code.DisplayOrder = definition.DisplayOrder;
        return changed;
    }
}
