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
            CollectionService collection,
            CancellationToken cancellationToken) =>
        {
            await antiforgery.ValidateRequestAsync(context);
            var user = await currentUser.GetOrCreateAsync(context.User, cancellationToken);
            try
            {
                return Results.Ok(await collection.SetAsync(user.Id, request.SpriteVariantId,
                    request.IsOwned, request.IsMastered, cancellationToken));
            }
            catch (InvalidSpriteVariantException)
            {
                return InvalidVariant(nameof(request.SpriteVariantId));
            }
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
            var now = DateTimeOffset.UtcNow;
            var availableIds = await database.SeasonSpriteVariants
                .Where(item => requestedIds.Contains(item.SpriteVariantId) &&
                    item.Season.StartAt <= now &&
                    (item.ReleasedAt == null || item.ReleasedAt <= now))
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

    private static IResult InvalidVariant(string field) =>
        Results.ValidationProblem(new Dictionary<string, string[]>
        {
            [field] = ["The Sprite variant has not been released yet."]
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
