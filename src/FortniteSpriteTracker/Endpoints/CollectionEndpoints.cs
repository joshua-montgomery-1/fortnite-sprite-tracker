using FortniteSpriteTracker.DataAccess;
using FortniteSpriteTracker.DataAccess.Entities;
using FortniteSpriteTracker.Server.Services;
using FortniteSpriteTracker.Shared.Collections;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.EntityFrameworkCore;

namespace FortniteSpriteTracker.Server.Endpoints;

public static class CollectionEndpoints
{
    public static IEndpointRouteBuilder MapCollectionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/me/collection").RequireAuthorization();

        group.MapGet("/", async (
            HttpContext context,
            CurrentUserService currentUser,
            SpriteTrackerDbContext database,
            CancellationToken cancellationToken) =>
        {
            SetNoStoreHeader(context.Response);
            var user = await currentUser.GetOrCreateAsync(context.User, cancellationToken);
            var progress = await database.SpriteProgress
                .AsNoTracking()
                .Where(item => item.UserId == user.Id)
                .OrderBy(item => item.SpriteVariantId)
                .Select(item => new SpriteProgressDto
                {
                    SpriteVariantId = item.SpriteVariantId,
                    IsOwned = item.IsOwned,
                    IsMastered = item.IsMastered,
                    UpdatedAtUtc = item.UpdatedAtUtc
                })
                .ToArrayAsync(cancellationToken);
            return Results.Ok(progress);
        });

        group.MapPut("/", async (
            UpdateSpriteProgressRequest request,
            HttpContext context,
            IAntiforgery antiforgery,
            CurrentUserService currentUser,
            SpriteTrackerDbContext database,
            CancellationToken cancellationToken) =>
        {
            await antiforgery.ValidateRequestAsync(context);
            if (!await IsAvailableAsync(database, request.SpriteVariantId, cancellationToken))
            {
                return InvalidVariant(nameof(request.SpriteVariantId));
            }

            var user = await currentUser.GetOrCreateAsync(context.User, cancellationToken);
            var progress = await database.SpriteProgress.FindAsync(
                [user.Id, request.SpriteVariantId],
                cancellationToken);
            var updatedAtUtc = DateTimeOffset.UtcNow;
            var isMastered = request.IsMastered;
            var isOwned = request.IsOwned || isMastered;

            if (!isOwned)
            {
                if (progress is not null)
                {
                    database.SpriteProgress.Remove(progress);
                    await database.SaveChangesAsync(cancellationToken);
                }

                return Results.Ok(ToDto(request.SpriteVariantId, false, false, updatedAtUtc));
            }

            if (progress is null)
            {
                progress = new SpriteProgress
                {
                    UserId = user.Id,
                    SpriteVariantId = request.SpriteVariantId
                };
                database.SpriteProgress.Add(progress);
            }

            progress.IsOwned = true;
            progress.IsMastered = isMastered;
            progress.UpdatedAtUtc = updatedAtUtc;
            await database.SaveChangesAsync(cancellationToken);
            return Results.Ok(ToDto(progress.SpriteVariantId, true, isMastered, updatedAtUtc));
        });

        group.MapPut("/batch", async (
            BatchUpdateSpriteProgressRequest request,
            HttpContext context,
            IAntiforgery antiforgery,
            CurrentUserService currentUser,
            SpriteTrackerDbContext database,
            CancellationToken cancellationToken) =>
        {
            await antiforgery.ValidateRequestAsync(context);
            var maximumBatchSize = await database.SpriteVariants.CountAsync(cancellationToken);
            if (request.Updates.Count < 1 || request.Updates.Count > maximumBatchSize)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(request.Updates)] = [$"A batch must contain between 1 and {maximumBatchSize} updates."]
                });
            }

            var updates = request.Updates
                .GroupBy(item => item.SpriteVariantId)
                .Select(group => group.Last())
                .ToArray();
            var requestedIds = updates.Select(item => item.SpriteVariantId).ToArray();
            var availableIds = await database.SeasonSpriteVariants
                .Where(item => requestedIds.Contains(item.SpriteVariantId))
                .Select(item => item.SpriteVariantId)
                .ToHashSetAsync(cancellationToken);
            if (requestedIds.Any(id => !availableIds.Contains(id)))
            {
                return InvalidVariant(nameof(request.Updates));
            }

            var user = await currentUser.GetOrCreateAsync(context.User, cancellationToken);
            var existing = await database.SpriteProgress
                .Where(item => item.UserId == user.Id && requestedIds.Contains(item.SpriteVariantId))
                .ToDictionaryAsync(item => item.SpriteVariantId, cancellationToken);
            var updatedAtUtc = DateTimeOffset.UtcNow;
            var results = new List<SpriteProgressDto>(updates.Length);

            foreach (var update in updates)
            {
                existing.TryGetValue(update.SpriteVariantId, out var progress);
                var isMastered = update.IsMastered;
                var isOwned = update.IsOwned || isMastered;
                if (!isOwned && progress is not null)
                {
                    database.SpriteProgress.Remove(progress);
                }
                else if (isOwned)
                {
                    if (progress is null)
                    {
                        progress = new SpriteProgress
                        {
                            UserId = user.Id,
                            SpriteVariantId = update.SpriteVariantId
                        };
                        database.SpriteProgress.Add(progress);
                    }

                    progress.IsOwned = true;
                    progress.IsMastered = isMastered;
                    progress.UpdatedAtUtc = updatedAtUtc;
                }

                results.Add(ToDto(update.SpriteVariantId, isOwned, isMastered, updatedAtUtc));
            }

            await database.SaveChangesAsync(cancellationToken);
            return Results.Ok(results);
        });

        return endpoints;
    }

    private static Task<bool> IsAvailableAsync(
        SpriteTrackerDbContext database,
        int variantId,
        CancellationToken cancellationToken) =>
        database.SeasonSpriteVariants.AnyAsync(
            item => item.SpriteVariantId == variantId,
            cancellationToken);

    private static IResult InvalidVariant(string field) =>
        Results.ValidationProblem(new Dictionary<string, string[]>
        {
            [field] = ["The Sprite variant is not available in the active catalog."]
        });

    private static SpriteProgressDto ToDto(int id, bool isOwned, bool isMastered, DateTimeOffset updatedAtUtc) =>
        new SpriteProgressDto
        {
            SpriteVariantId = id,
            IsOwned = isOwned,
            IsMastered = isMastered,
            UpdatedAtUtc = updatedAtUtc
        };

    private static void SetNoStoreHeader(HttpResponse response)
    {
        response.Headers.CacheControl = "no-store";
    }
}
