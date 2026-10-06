using FortniteSpriteTracker.DataAccess;
using FortniteSpriteTracker.DataAccess.Entities;
using FortniteSpriteTracker.Shared.Collections;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.ComponentModel;

namespace FortniteSpriteTracker.Server.Services;

public sealed class CollectionService(SpriteTrackerDbContext database)
{
    public async Task<IReadOnlyList<SpriteProgressDto>> SetBatchAsync(long userId,
        IReadOnlyList<CollectionUpdate> updates, CancellationToken cancellationToken)
    {
        var ids = updates.Select(item => item.SpriteVariantId).ToArray();
        var now = DateTimeOffset.UtcNow;
        var available = await database.SeasonSpriteVariants
            .Where(item => ids.Contains(item.SpriteVariantId) && item.Season.StartAt <= now &&
                (item.ReleasedAt == null || item.ReleasedAt <= now))
            .Select(item => item.SpriteVariantId).Distinct().ToArrayAsync(cancellationToken);
        if (available.Length != ids.Length) throw new InvalidSpriteVariantException();

        for (var attempt = 1; ; attempt++)
        {
            var existing = await database.SpriteProgress
                .Where(item => item.UserId == userId && ids.Contains(item.SpriteVariantId))
                .ToDictionaryAsync(item => item.SpriteVariantId, cancellationToken);
            var touched = new List<SpriteProgress>();
            now = DateTimeOffset.UtcNow;
            foreach (var update in updates)
            {
                existing.TryGetValue(update.SpriteVariantId, out var progress);
                progress = Apply(userId, update.SpriteVariantId, update.IsOwned, update.IsMastered, progress, now);
                if (progress is not null) touched.Add(progress);
            }
            try
            {
                // EF Core wraps all commands in one transaction: no partial batch saves.
                await database.SaveChangesAsync(cancellationToken);
                var saved = await database.SpriteProgress.AsNoTracking()
                    .Where(item => item.UserId == userId && ids.Contains(item.SpriteVariantId))
                    .Select(item => new SpriteProgressDto
                    {
                        SpriteVariantId = item.SpriteVariantId, IsOwned = item.IsOwned,
                        IsMastered = item.IsMastered, UpdatedAtUtc = item.UpdatedAtUtc
                    }).ToDictionaryAsync(item => item.SpriteVariantId, cancellationToken);
                return ids.Select(id => saved.GetValueOrDefault(id) ?? new SpriteProgressDto
                {
                    SpriteVariantId = id, IsOwned = false, IsMastered = false, UpdatedAtUtc = now
                }).ToArray();
            }
            catch (DbUpdateException exception) when (attempt < 3 && !cancellationToken.IsCancellationRequested &&
                (exception is DbUpdateConcurrencyException || exception.InnerException is PostgresException
                    { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "PK_SpriteProgress" }))
            {
                foreach (var progress in touched) database.Entry(progress).State = EntityState.Detached;
            }
        }
    }

    private SpriteProgress? Apply(long userId, int variantId, bool? owned, bool? mastered,
        SpriteProgress? progress, DateTimeOffset now)
    {
        if (mastered == true) owned = true;
        if (owned == false)
        {
            if (progress is not null) database.SpriteProgress.Remove(progress);
        }
        else if (progress is null && owned == true)
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
        return progress;
    }

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
            progress = Apply(userId, variantId, owned, mastered, progress, now);

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

public sealed record CollectionUpdate(
    [property: Description("Exact Sprite variant ID from the catalog, not a family ID.")] int SpriteVariantId,
    [property: Description("Omit to preserve ownership. False removes ownership and mastery.")] bool? IsOwned = null,
    [property: Description("Omit to preserve mastery. True also sets ownership; false clears only mastery.")] bool? IsMastered = null);

public sealed class InvalidSpriteVariantException() : Exception("The Sprite variant does not exist or has not been released yet.");
