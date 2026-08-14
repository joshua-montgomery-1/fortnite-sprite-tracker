using FortniteSpriteTracker.Server.Data;
using FortniteSpriteTracker.Server.Data.Entities;
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
            SetPrivateNoStoreHeaders(context.Response);
            var user = await currentUser.GetOrCreateAsync(context.User, cancellationToken);
            var progress = await database.SpriteProgress
                .AsNoTracking()
                .Where(item => item.UserId == user.Id)
                .OrderBy(item => item.SpriteVariant.Sprite.Slug)
                .ThenBy(item => item.SpriteVariant.Name)
                .Select(item => new SpriteProgressDto(
                    item.SpriteVariant.Sprite.Slug,
                    item.SpriteVariant.Name,
                    item.IsOwned,
                    item.IsMastered,
                    item.UpdatedAtUtc))
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
            var user = await currentUser.GetOrCreateAsync(context.User, cancellationToken);
            var spriteSlug = request.SpriteSlug.Trim().ToLowerInvariant();
            var variant = request.Variant.Trim();

            var spriteVariant = await database.SpriteVariants
                .Include(item => item.Sprite)
                .SingleOrDefaultAsync(
                    item => item.Sprite.Slug == spriteSlug && item.Name == variant,
                    cancellationToken);

            if (spriteVariant is null)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(request.SpriteSlug)] = ["The Sprite or variant is not available in the catalog."]
                });
            }

            var progress = await database.SpriteProgress.FindAsync(
                [user.Id, spriteVariant.Id], cancellationToken);

            var isMastered = request.IsMastered;
            var isOwned = request.IsOwned || isMastered;
            var updatedAtUtc = DateTimeOffset.UtcNow;

            if (!isOwned && !isMastered)
            {
                if (progress is not null)
                {
                    database.SpriteProgress.Remove(progress);
                    await database.SaveChangesAsync(cancellationToken);
                }

                return Results.Ok(new SpriteProgressDto(
                    spriteSlug,
                    variant,
                    false,
                    false,
                    updatedAtUtc));
            }

            if (progress is null)
            {
                progress = new SpriteProgress
                {
                    UserId = user.Id,
                    SpriteVariantId = spriteVariant.Id
                };
                database.SpriteProgress.Add(progress);
            }

            progress.IsMastered = isMastered;
            progress.IsOwned = isOwned;
            progress.UpdatedAtUtc = updatedAtUtc;
            await database.SaveChangesAsync(cancellationToken);

            return Results.Ok(new SpriteProgressDto(
                spriteVariant.Sprite.Slug,
                spriteVariant.Name,
                progress.IsOwned,
                progress.IsMastered,
                progress.UpdatedAtUtc));
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

            if (request.Updates.Count is < 1 or > 117)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(request.Updates)] = ["A batch must contain between 1 and 117 updates."]
                });
            }

            var normalizedUpdates = request.Updates
                .Select(update => new
                {
                    Slug = update.SpriteSlug.Trim().ToLowerInvariant(),
                    Variant = update.Variant.Trim(),
                    update.IsOwned,
                    update.IsMastered
                })
                .GroupBy(update => $"{update.Slug}::{update.Variant}", StringComparer.OrdinalIgnoreCase)
                .Select(group => group.Last())
                .ToArray();

            var slugs = normalizedUpdates.Select(update => update.Slug).Distinct().ToArray();
            var variants = await database.SpriteVariants
                .Include(item => item.Sprite)
                .Where(item => slugs.Contains(item.Sprite.Slug))
                .ToArrayAsync(cancellationToken);
            var variantByKey = variants.ToDictionary(
                item => $"{item.Sprite.Slug}::{item.Name}",
                StringComparer.OrdinalIgnoreCase);

            if (normalizedUpdates.Any(update =>
                    !variantByKey.ContainsKey($"{update.Slug}::{update.Variant}")))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(request.Updates)] = ["The batch contains a Sprite or variant that is not available in the catalog."]
                });
            }

            var user = await currentUser.GetOrCreateAsync(context.User, cancellationToken);
            var variantIds = variantByKey.Values.Select(item => item.Id).ToArray();
            var existingProgress = await database.SpriteProgress
                .Where(item => item.UserId == user.Id && variantIds.Contains(item.SpriteVariantId))
                .ToDictionaryAsync(item => item.SpriteVariantId, cancellationToken);
            var results = new List<SpriteProgressDto>(normalizedUpdates.Length);
            var updatedAtUtc = DateTimeOffset.UtcNow;

            foreach (var update in normalizedUpdates)
            {
                var spriteVariant = variantByKey[$"{update.Slug}::{update.Variant}"];
                existingProgress.TryGetValue(spriteVariant.Id, out var progress);
                var isMastered = update.IsMastered;
                var isOwned = update.IsOwned || isMastered;

                if (!isOwned)
                {
                    if (progress is not null)
                    {
                        database.SpriteProgress.Remove(progress);
                    }
                }
                else
                {
                    if (progress is null)
                    {
                        progress = new SpriteProgress
                        {
                            UserId = user.Id,
                            SpriteVariantId = spriteVariant.Id
                        };
                        database.SpriteProgress.Add(progress);
                    }

                    progress.IsOwned = true;
                    progress.IsMastered = isMastered;
                    progress.UpdatedAtUtc = updatedAtUtc;
                }

                results.Add(new SpriteProgressDto(
                    spriteVariant.Sprite.Slug,
                    spriteVariant.Name,
                    isOwned,
                    isMastered,
                    updatedAtUtc));
            }

            await database.SaveChangesAsync(cancellationToken);
            return Results.Ok(results);
        });

        return endpoints;
    }

    private static void SetPrivateNoStoreHeaders(HttpResponse response)
    {
        response.Headers.CacheControl = "no-store, no-cache, private";
        response.Headers.Pragma = "no-cache";
        response.Headers.Vary = "Cookie";
    }
}
