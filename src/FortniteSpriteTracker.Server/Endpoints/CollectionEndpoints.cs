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
                .OrderBy(item => item.SpriteSlug)
                .ThenBy(item => item.Variant)
                .Select(item => new SpriteProgressDto(
                    item.SpriteSlug,
                    item.Variant,
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

            var progress = await database.SpriteProgress.FindAsync(
                [user.Id, spriteSlug, variant],
                cancellationToken);

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
                    SpriteSlug = spriteSlug,
                    Variant = variant
                };
                database.SpriteProgress.Add(progress);
            }

            progress.IsMastered = isMastered;
            progress.IsOwned = isOwned;
            progress.UpdatedAtUtc = updatedAtUtc;
            await database.SaveChangesAsync(cancellationToken);

            return Results.Ok(new SpriteProgressDto(
                progress.SpriteSlug,
                progress.Variant,
                progress.IsOwned,
                progress.IsMastered,
                progress.UpdatedAtUtc));
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
