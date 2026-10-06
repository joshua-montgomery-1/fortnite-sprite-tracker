using FortniteSpriteTracker.DataAccess;
using FortniteSpriteTracker.Shared.Collections;
using Microsoft.EntityFrameworkCore;

namespace FortniteSpriteTracker.Server.Services;

public sealed class CollectionService(SpriteTrackerDbContext database)
{
    public Task<bool> IsAvailableAsync(int variantId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        return database.SeasonSpriteVariants.AnyAsync(item => item.SpriteVariantId == variantId &&
            item.Season.StartAt <= now && (item.ReleasedAt == null || item.ReleasedAt <= now), cancellationToken);
    }

    // Nullable flags change only the requested state. SQL updates keep concurrent
    // ownership/mastery changes from overwriting the other field's current value.
    public async Task<SpriteProgressDto> SetAsync(long userId, int variantId, bool? owned, bool? mastered,
        CancellationToken cancellationToken)
    {
        if (!await IsAvailableAsync(variantId, cancellationToken))
            throw new InvalidSpriteVariantException();
        var now = DateTimeOffset.UtcNow;
        if (mastered == true) owned = true;
        if (owned == false)
        {
            await database.SpriteProgress.Where(item => item.UserId == userId && item.SpriteVariantId == variantId)
                .ExecuteDeleteAsync(cancellationToken);
        }
        else if (owned == true)
        {
            var initialMastery = mastered ?? false;
            var preserveMastery = mastered is null;
            await database.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "SpriteProgress" ("UserId", "SpriteVariantId", "IsOwned", "IsMastered", "UpdatedAtUtc")
                VALUES ({userId}, {variantId}, TRUE, {initialMastery}, {now})
                ON CONFLICT ("UserId", "SpriteVariantId") DO UPDATE
                SET "IsOwned" = TRUE,
                    "IsMastered" = CASE WHEN {preserveMastery} THEN "SpriteProgress"."IsMastered" ELSE {initialMastery} END,
                    "UpdatedAtUtc" = {now}
                """, cancellationToken);
        }
        else if (mastered == false)
        {
            await database.SpriteProgress.Where(item => item.UserId == userId && item.SpriteVariantId == variantId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.IsMastered, false)
                    .SetProperty(item => item.UpdatedAtUtc, now), cancellationToken);
        }

        return await database.SpriteProgress.AsNoTracking()
            .Where(item => item.UserId == userId && item.SpriteVariantId == variantId)
            .Select(item => new SpriteProgressDto
            {
                SpriteVariantId = item.SpriteVariantId, IsOwned = item.IsOwned,
                IsMastered = item.IsMastered, UpdatedAtUtc = item.UpdatedAtUtc
            }).SingleOrDefaultAsync(cancellationToken)
            ?? new SpriteProgressDto { SpriteVariantId = variantId, IsOwned = false, IsMastered = false, UpdatedAtUtc = now };
    }
}

public sealed class InvalidSpriteVariantException() : Exception("The Sprite variant does not exist or has not been released yet.");
