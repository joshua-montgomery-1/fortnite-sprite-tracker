using FortniteSpriteTracker.DataAccess;
using FortniteSpriteTracker.DataAccess.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SpriteScout.Auth;

namespace FortniteSpriteTracker.Server.Services;

public sealed class AuthArtworkSource(SpriteTrackerDbContext database, IMemoryCache cache) : IAuthArtworkSource
{
    public async Task<IReadOnlyList<AuthArtwork>> GetAsync(CancellationToken cancellationToken)
    {
        return (await cache.GetOrCreateAsync("auth-artwork-catalog", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
            var now = DateTimeOffset.UtcNow;
            var artwork = await database.SeasonSpriteVariants.AsNoTracking()
                .Where(item => item.Season.StartAt <= now)
                .Select(item => new AuthArtwork(item.SeasonId, item.SpriteVariant.ImagePath))
                .Distinct().ToArrayAsync(cancellationToken);
            var fallback = CatalogSeedData.Families
                .SelectMany(family => family.Variants)
                .Select(variant => new AuthArtwork(CatalogSeedData.SeasonId, variant.ImagePath)).ToArray();
            return artwork.Select(item => item.ImagePath).Distinct().Count() >= 10 ? artwork : artwork.Concat(fallback).Distinct().ToArray();
        }))!;
    }
}
