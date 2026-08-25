using FortniteSpriteTracker.DataAccess;
using FortniteSpriteTracker.Shared.Catalog;
using Microsoft.EntityFrameworkCore;

namespace FortniteSpriteTracker.Server.Endpoints;

public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/seasons", async (
            SpriteTrackerDbContext database,
            CancellationToken cancellationToken) =>
        {
            var now = DateTimeOffset.UtcNow;
            var seasons = await database.Seasons
                .AsNoTracking()
                .OrderByDescending(item => item.Chapter)
                .ThenByDescending(item => item.Number)
                .Select(item => new SeasonDto
                {
                    Id = item.Id,
                    Chapter = item.Chapter,
                    Number = item.Number,
                    Name = item.Name,
                    StartAt = item.StartAt,
                    EndAt = item.EndAt,
                    IsActive = item.StartAt <= now && (item.EndAt == null || now < item.EndAt),
                    HasCatalog = item.SpriteFamilies.Any() && item.SpriteVariants.Any(),
                    HasCheatCodes = item.CheatCodes.Any()
                })
                .ToArrayAsync(cancellationToken);
            return Results.Ok(seasons);
        })
        .AllowAnonymous()
        .WithPublicCache(TimeSpan.FromMinutes(30));

        endpoints.MapGet("/api/catalog/{seasonId:int}", async (
            int seasonId,
            SpriteTrackerDbContext database,
            CancellationToken cancellationToken) =>
        {
            var now = DateTimeOffset.UtcNow;
            var season = await database.Seasons
                .AsNoTracking()
                .Where(item => item.Id == seasonId && item.SpriteFamilies.Any() && item.SpriteVariants.Any())
                .FirstOrDefaultAsync(cancellationToken);
            if (season is null)
            {
                return Results.NotFound();
            }

            var memberships = await database.SeasonSpriteFamilies
                .AsNoTracking()
                .Where(item => item.SeasonId == season.Id)
                .Include(item => item.SpriteFamily)
                    .ThenInclude(family => family.Variants)
                        .ThenInclude(variant => variant.VariantStyle)
                .OrderBy(item => item.DisplayOrder)
                .ToArrayAsync(cancellationToken);
            var availableVariantIds = await database.SeasonSpriteVariants
                .AsNoTracking()
                .Where(item => item.SeasonId == season.Id)
                .Select(item => item.SpriteVariantId)
                .ToHashSetAsync(cancellationToken);
            var hasCheatCodes = await database.CheatCodes
                .AnyAsync(item => item.SeasonId == season.Id, cancellationToken);

            var stylesById = new Dictionary<int, VariantStyleDto>();
            var families = memberships.Select(item =>
            {
                var variants = item.SpriteFamily.Variants
                    .Where(variant => availableVariantIds.Contains(variant.Id))
                    .OrderBy(variant => variant.VariantStyle.DisplayOrder)
                    .Select(variant =>
                    {
                        if (!stylesById.TryGetValue(variant.VariantStyleId, out var style))
                        {
                            style = new VariantStyleDto
                            {
                                Id = variant.VariantStyle.Id,
                                Name = variant.VariantStyle.Name,
                                Color = variant.VariantStyle.Color,
                                Bonus = variant.VariantStyle.Bonus,
                                DisplayOrder = variant.VariantStyle.DisplayOrder
                            };
                            stylesById.Add(style.Id, style);
                        }

                        return new SpriteVariantDto
                        {
                            Id = variant.Id,
                            ImagePath = variant.ImagePath,
                            Style = style
                        };
                    })
                    .ToArray();

                return new SpriteFamilyDto
                {
                    Id = item.SpriteFamily.Id,
                    Name = item.SpriteFamily.Name,
                    Slug = item.SpriteFamily.Slug,
                    Rarity = item.Rarity,
                    RarityColor = item.RarityColor,
                    Ability = item.Ability,
                    PrimaryColor = item.PrimaryColor,
                    SecondaryColor = item.SecondaryColor,
                    DisplayOrder = item.DisplayOrder,
                    ImageUrl = item.ImagePath ?? string.Empty,
                    Variants = variants
                };
            }).ToArray();

            var catalog = new SpriteCatalogDto
            {
                Season = new SeasonDto
                {
                    Id = season.Id,
                    Chapter = season.Chapter,
                    Number = season.Number,
                    Name = season.Name,
                    StartAt = season.StartAt,
                    EndAt = season.EndAt,
                    IsActive = season.StartAt <= now && (season.EndAt == null || now < season.EndAt),
                    HasCatalog = true,
                    HasCheatCodes = hasCheatCodes
                },
                VariantStyles = stylesById.Values.OrderBy(item => item.DisplayOrder).ToArray(),
                Families = families,
                TotalEntries = families.Sum(item => item.Variants.Count)
            };

            return Results.Ok(catalog);
        })
        .AllowAnonymous()
        .WithPublicCache(TimeSpan.FromMinutes(5));

        return endpoints;
    }
}
