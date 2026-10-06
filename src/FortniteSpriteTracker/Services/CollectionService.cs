using FortniteSpriteTracker.DataAccess;
using FortniteSpriteTracker.DataAccess.Entities;
using FortniteSpriteTracker.Shared.Collections;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FortniteSpriteTracker.Server.Services;

public sealed class CollectionService(SpriteTrackerDbContext database)
{
    public Task<bool> IsAvailableAsync(int variantId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        return database.SeasonSpriteVariants.AnyAsync(item => item.SpriteVariantId == variantId &&
            item.Season.StartAt <= now && (item.ReleasedAt == null || item.ReleasedAt <= now), cancellationToken);
    }

    // Nullable flags change only the requested state. Explicit modification flags
    // preserve the other field and apply the requested value even if it was unchanged when read.
    public async Task<SpriteProgressDto> SetAsync(long userId, int variantId, bool? owned, bool? mastered,
        CancellationToken cancellationToken)
    {
        if (!await IsAvailableAsync(variantId, cancellationToken))
            throw new InvalidSpriteVariantException();
        if (mastered == true) owned = true;
        for (var attempt = 1; ; attempt++)
        {
            var progress = await database.SpriteProgress.SingleOrDefaultAsync(
                item => item.UserId == userId && item.SpriteVariantId == variantId, cancellationToken);
            var now = DateTimeOffset.UtcNow;
            var inserting = progress is null && owned == true;
            if (owned == false)
            {
                if (progress is not null) database.SpriteProgress.Remove(progress);
            }
            else if (inserting)
            {
                progress = new SpriteProgress
                {
                    UserId = userId, SpriteVariantId = variantId,
                    IsOwned = true, IsMastered = mastered ?? false, UpdatedAtUtc = now
                };
                database.SpriteProgress.Add(progress);
            }
            else if (progress is not null)
            {
                if (owned is not null)
                {
                    progress.IsOwned = owned.Value;
                    database.Entry(progress).Property(item => item.IsOwned).IsModified = true;
                }
                if (mastered is not null)
                {
                    progress.IsMastered = mastered.Value;
                    database.Entry(progress).Property(item => item.IsMastered).IsModified = true;
                }
                progress.UpdatedAtUtc = now;
            }

            try
            {
                await database.SaveChangesAsync(cancellationToken);
                return await ReadAsync(userId, variantId, now, cancellationToken);
            }
            catch (DbUpdateConcurrencyException) when (attempt < 3 && !cancellationToken.IsCancellationRequested)
            {
                // A concurrent removal deleted the row. Reload and apply the desired state.
                if (progress is not null) database.Entry(progress).State = EntityState.Detached;
            }
            catch (DbUpdateException exception) when (attempt < 3 && inserting &&
                exception.InnerException is PostgresException
                { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "PK_SpriteProgress" } &&
                !cancellationToken.IsCancellationRequested)
            {
                // Another request inserted this user's variant first. Retry as an EF update.
                database.Entry(progress!).State = EntityState.Detached;
            }
        }
    }

    private async Task<SpriteProgressDto> ReadAsync(long userId, int variantId, DateTimeOffset now,
        CancellationToken cancellationToken) =>
        await database.SpriteProgress.AsNoTracking()
            .Where(item => item.UserId == userId && item.SpriteVariantId == variantId)
            .Select(item => new SpriteProgressDto
            {
                SpriteVariantId = item.SpriteVariantId, IsOwned = item.IsOwned,
                IsMastered = item.IsMastered, UpdatedAtUtc = item.UpdatedAtUtc
            }).SingleOrDefaultAsync(cancellationToken)
            ?? new SpriteProgressDto { SpriteVariantId = variantId, IsOwned = false, IsMastered = false, UpdatedAtUtc = now };
}

public sealed class InvalidSpriteVariantException() : Exception("The Sprite variant does not exist or has not been released yet.");
