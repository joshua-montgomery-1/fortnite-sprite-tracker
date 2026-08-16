using FortniteSpriteTracker.DataAccess;
using FortniteSpriteTracker.DataAccess.Entities;
using FortniteSpriteTracker.Server.Services;
using FortniteSpriteTracker.Shared.Collections;
using FortniteSpriteTracker.Shared.Players;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Antiforgery;

namespace FortniteSpriteTracker.Server.Endpoints;

public static class PlayerEndpoints
{
    private const int MaximumTrackedPlayers = 100;

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
                : Results.Ok(await ToSummaryAsync(database, player, false, cancellationToken));
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

            var collection = player.IsCollectionPublic
                ? await GetCollectionAsync(database, player.Id, cancellationToken)
                : [];
            PlayerSummaryDto? viewerSummary = null;
            IReadOnlyList<SpriteProgressDto> viewerCollection = [];
            var isTracked = false;

            if (context.User.Identity?.IsAuthenticated == true)
            {
                var viewer = await currentUser.GetOrCreateAsync(context.User, cancellationToken);
                isTracked = viewer.Id != player.Id && await database.TrackedPlayers.AsNoTracking()
                    .AnyAsync(item => item.UserId == viewer.Id && item.PlayerId == player.Id, cancellationToken);
                if (player.IsCollectionPublic && viewer.Id != player.Id)
                {
                    viewerCollection = await GetCollectionAsync(database, viewer.Id, cancellationToken);
                }
                else if (player.IsCollectionPublic)
                {
                    viewerCollection = collection;
                }

                viewerSummary = player.IsCollectionPublic
                    ? ToSummary(viewer, viewerCollection.Count, viewerCollection.Count(item => item.IsMastered))
                    : await ToSummaryAsync(database, viewer, true, cancellationToken);
            }

            context.Response.Headers.CacheControl = "no-store";
            var playerSummary = player.IsCollectionPublic
                ? ToSummary(player, collection.Count, collection.Count(item => item.IsMastered))
                : ToSummary(player, null, null);
            return Results.Ok(new PlayerCollectionDto
            {
                Player = playerSummary,
                Collection = collection,
                Viewer = viewerSummary,
                ViewerCollection = viewerCollection,
                CanCompare = player.IsCollectionPublic && viewerSummary is not null,
                IsTracked = isTracked
            });
        });

        var tracked = endpoints.MapGroup("/api/me/tracked-players").RequireAuthorization();

        tracked.MapGet("/", async (
            HttpContext context,
            CurrentUserService currentUser,
            SpriteTrackerDbContext database,
            CancellationToken cancellationToken) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            var user = await currentUser.GetOrCreateAsync(context.User, cancellationToken);
            var players = await (
                from trackedPlayer in database.TrackedPlayers.AsNoTracking()
                where trackedPlayer.UserId == user.Id
                join progress in database.SpriteProgress.AsNoTracking()
                    on trackedPlayer.PlayerId equals progress.UserId into playerProgress
                from progress in playerProgress.DefaultIfEmpty()
                group progress by new
                {
                    trackedPlayer.Player.PublicId,
                    trackedPlayer.Player.DisplayName,
                    trackedPlayer.Player.EpicDisplayName
                }
                into player
                select new TrackedPlayerDto
                {
                    PublicId = player.Key.PublicId,
                    DisplayName = player.Key.DisplayName,
                    EpicDisplayName = player.Key.EpicDisplayName ?? "Epic player",
                    TotalSprites = player.Count(progress => progress != null && progress.IsOwned),
                    MasteredSprites = player.Count(progress => progress != null && progress.IsMastered)
                })
                .OrderByDescending(item => item.TotalSprites)
                .ThenByDescending(item => item.MasteredSprites)
                .ThenBy(item => item.DisplayName)
                .Take(MaximumTrackedPlayers)
                .ToArrayAsync(cancellationToken);
            return Results.Ok(players);
        });

        tracked.MapPost("/{publicId:guid}", async (
            Guid publicId,
            HttpContext context,
            IAntiforgery antiforgery,
            CurrentUserService currentUser,
            SpriteTrackerDbContext database,
            CancellationToken cancellationToken) =>
        {
            await antiforgery.ValidateRequestAsync(context);
            var user = await currentUser.GetOrCreateAsync(context.User, cancellationToken);
            var player = await database.Users.SingleOrDefaultAsync(
                item => item.PublicId == publicId && item.EpicDisplayName != null,
                cancellationToken);
            if (player is null) return Results.NotFound();
            if (player.Id == user.Id) return Results.BadRequest("You cannot track your own profile.");
            var isAlreadyTracked = await database.TrackedPlayers.AnyAsync(
                item => item.UserId == user.Id && item.PlayerId == player.Id,
                cancellationToken);
            if (!isAlreadyTracked)
            {
                var trackedCount = await database.TrackedPlayers.CountAsync(
                    item => item.UserId == user.Id,
                    cancellationToken);
                if (trackedCount >= MaximumTrackedPlayers)
                {
                    return Results.Conflict($"You can track up to {MaximumTrackedPlayers} players.");
                }

                database.TrackedPlayers.Add(new TrackedPlayer { UserId = user.Id, PlayerId = player.Id });
                await database.SaveChangesAsync(cancellationToken);
            }
            return Results.NoContent();
        });

        tracked.MapDelete("/{publicId:guid}", async (
            Guid publicId,
            HttpContext context,
            IAntiforgery antiforgery,
            CurrentUserService currentUser,
            SpriteTrackerDbContext database,
            CancellationToken cancellationToken) =>
        {
            await antiforgery.ValidateRequestAsync(context);
            var user = await currentUser.GetOrCreateAsync(context.User, cancellationToken);
            var relationship = await database.TrackedPlayers.SingleOrDefaultAsync(
                item => item.UserId == user.Id && item.Player.PublicId == publicId,
                cancellationToken);
            if (relationship is not null)
            {
                database.TrackedPlayers.Remove(relationship);
                await database.SaveChangesAsync(cancellationToken);
            }
            return Results.NoContent();
        });

        return endpoints;
    }

    private static async Task<IReadOnlyList<SpriteProgressDto>> GetCollectionAsync(
        SpriteTrackerDbContext database,
        long userId,
        CancellationToken cancellationToken) =>
        await database.SpriteProgress.AsNoTracking()
            .Where(item => item.UserId == userId && item.IsOwned)
            .OrderBy(item => item.SpriteVariantId)
            .Select(item => new SpriteProgressDto
            {
                SpriteVariantId = item.SpriteVariantId,
                IsOwned = item.IsOwned,
                IsMastered = item.IsMastered,
                UpdatedAtUtc = item.UpdatedAtUtc
            })
            .ToArrayAsync(cancellationToken);

    private static async Task<PlayerSummaryDto> ToSummaryAsync(
        SpriteTrackerDbContext database,
        UserAccount user,
        bool includePrivateStats,
        CancellationToken cancellationToken)
    {
        if (!user.IsCollectionPublic && !includePrivateStats)
        {
            return ToSummary(user, null, null);
        }

        var counts = await database.SpriteProgress.AsNoTracking()
            .Where(item => item.UserId == user.Id)
            .GroupBy(_ => 1)
            .Select(items => new
            {
                Owned = items.Count(item => item.IsOwned),
                Mastered = items.Count(item => item.IsMastered)
            })
            .SingleOrDefaultAsync(cancellationToken);
        return ToSummary(user, counts?.Owned ?? 0, counts?.Mastered ?? 0);
    }

    private static PlayerSummaryDto ToSummary(UserAccount user, int? ownedCount, int? masteredCount) =>
        new()
        {
            PublicId = user.PublicId,
            DisplayName = user.DisplayName,
            EpicDisplayName = user.EpicDisplayName ?? "Epic player",
            IsCollectionPublic = user.IsCollectionPublic,
            OwnedCount = ownedCount,
            MasteredCount = masteredCount
        };
}
