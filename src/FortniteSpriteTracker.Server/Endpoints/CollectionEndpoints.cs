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

            progress.IsMastered = request.IsMastered;
            progress.IsOwned = request.IsOwned || request.IsMastered;
            progress.UpdatedAtUtc = DateTimeOffset.UtcNow;
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
}
