using FortniteSpriteTracker.Server.Data;
using FortniteSpriteTracker.Server.Data.Entities;
using FortniteSpriteTracker.Server.Services;
using FortniteSpriteTracker.Shared.Collections;
using FortniteSpriteTracker.Shared.Players;
using Microsoft.EntityFrameworkCore;

namespace FortniteSpriteTracker.Server.Endpoints;

public static class PlayerEndpoints
{
    public static IEndpointRouteBuilder MapPlayerEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/players").AllowAnonymous();

        group.MapGet("/search", async (string epicUsername, SpriteTrackerDbContext database, CancellationToken cancellationToken) =>
        {
            var normalized = CurrentUserService.NormalizeEpicDisplayName(epicUsername);
            if (normalized is null)
            {
                return Results.BadRequest();
            }

            var player = await database.Users.AsNoTracking()
                .Where(user => user.NormalizedEpicDisplayName == normalized && user.EpicDisplayName != null)
                .OrderBy(user => user.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            return player is null
                ? Results.NotFound()
                : Results.Ok(await ToSummaryAsync(database, player, cancellationToken));
        });

        group.MapGet("/{publicId:guid}", async (
            Guid publicId,
            HttpContext context,
            CurrentUserService currentUser,
            SpriteTrackerDbContext database,
            CancellationToken cancellationToken) =>
        {
            var player = await database.Users.AsNoTracking()
                .SingleOrDefaultAsync(user => user.PublicId == publicId && user.EpicDisplayName != null, cancellationToken);
            if (player is null)
            {
                return Results.NotFound();
            }

            var collection = await GetCollectionAsync(database, player.Id, cancellationToken);
            PlayerSummaryDto? viewerSummary = null;
            IReadOnlyList<SpriteProgressDto> viewerCollection = [];

            if (context.User.Identity?.IsAuthenticated == true)
            {
                var viewer = await currentUser.GetOrCreateAsync(context.User, cancellationToken);
                viewerSummary = await ToSummaryAsync(database, viewer, cancellationToken);
                if (viewer.Id != player.Id)
                {
                    viewerCollection = await GetCollectionAsync(database, viewer.Id, cancellationToken);
                }
                else
                {
                    viewerCollection = collection;
                }
            }

            context.Response.Headers.CacheControl = "no-store";
            var playerSummary = await ToSummaryAsync(database, player, cancellationToken);
            return Results.Ok(new PlayerCollectionDto(playerSummary, collection, viewerSummary, viewerCollection));
        });

        return endpoints;
    }

    private static async Task<IReadOnlyList<SpriteProgressDto>> GetCollectionAsync(
        SpriteTrackerDbContext database,
        Guid userId,
        CancellationToken cancellationToken) =>
        await database.SpriteProgress.AsNoTracking()
            .Where(item => item.UserId == userId && item.IsOwned)
            .OrderBy(item => item.SpriteVariant.Sprite.Slug)
            .ThenBy(item => item.SpriteVariant.Name)
            .Select(item => new SpriteProgressDto(
                item.SpriteVariant.Sprite.Slug,
                item.SpriteVariant.Name,
                item.IsOwned,
                item.IsMastered,
                item.UpdatedAtUtc))
            .ToArrayAsync(cancellationToken);

    private static async Task<PlayerSummaryDto> ToSummaryAsync(
        SpriteTrackerDbContext database,
        UserAccount user,
        CancellationToken cancellationToken)
    {
        var ownedCount = await database.SpriteProgress.CountAsync(
            item => item.UserId == user.Id && item.IsOwned,
            cancellationToken);
        var masteredCount = await database.SpriteProgress.CountAsync(
            item => item.UserId == user.Id && item.IsMastered,
            cancellationToken);
        return new PlayerSummaryDto(
            user.PublicId,
            user.DisplayName,
            user.EpicDisplayName ?? "Epic player",
            ownedCount,
            masteredCount);
    }
}
