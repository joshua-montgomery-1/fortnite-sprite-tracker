using FortniteSpriteTracker.DataAccess;
using FortniteSpriteTracker.DataAccess.Seeding;
using FortniteSpriteTracker.Server.Services;
using FortniteSpriteTracker.Shared.Catalog;
using FortniteSpriteTracker.Shared.CheatCodes;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.EntityFrameworkCore;

namespace FortniteSpriteTracker.Server.Endpoints;

public static class CheatCodeEndpoints
{
    public static IEndpointRouteBuilder MapCheatCodeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/cheat-codes", async (SpriteTrackerDbContext database, CancellationToken cancellationToken) =>
        {
            var season = await database.Seasons
                .AsNoTracking()
                .Include(item => item.CheatCodes)
                    .ThenInclude(item => item.Category)
                .SingleOrDefaultAsync(item => item.Id == CheatCodeSeedData.SeasonId, cancellationToken);
            if (season is null || season.CheatCodes.Count == 0)
            {
                return Results.NotFound();
            }

            var now = DateTimeOffset.UtcNow;
            var categories = season.CheatCodes
                .GroupBy(item => new
                {
                    item.CheatCodeCategoryId,
                    item.Category.Name,
                    item.Category.DisplayOrder
                })
                .OrderBy(group => group.Key.DisplayOrder)
                .Select(group => new CheatCodeCategoryDto
                {
                    Id = group.Key.CheatCodeCategoryId,
                    Name = group.Key.Name,
                    DisplayOrder = group.Key.DisplayOrder,
                    Codes = group.OrderBy(item => item.DisplayOrder)
                        .Select(item => new CheatCodeDto
                        {
                            Id = item.Id,
                            Code = item.Code,
                            Description = item.Description,
                            Requirement = item.Requirement,
                            IsTrackable = item.IsTrackable,
                            DisplayOrder = item.DisplayOrder
                        })
                        .ToArray()
                })
                .ToArray();

            return Results.Ok(new CheatCodeCatalogDto
            {
                Season = new SeasonDto
                {
                    Id = season.Id,
                    Chapter = season.Chapter,
                    Number = season.Number,
                    Name = season.Name,
                    StartAt = season.StartAt,
                    EndAt = season.EndAt,
                    IsActive = season.StartAt <= now && (season.EndAt is null || now < season.EndAt),
                    HasCatalog = season.SpriteFamilies.Count > 0 && season.SpriteVariants.Count > 0,
                    HasCheatCodes = true
                },
                Categories = categories,
                TrackableCodeCount = season.CheatCodes.Count(item => item.IsTrackable)
            });
        })
        .AllowAnonymous()
        .WithPublicCache(TimeSpan.FromMinutes(5));

        var group = endpoints.MapGroup("/api/me/cheat-codes").RequireAuthorization();
        group.MapGet("/", async (
            HttpContext context,
            CurrentUserService currentUser,
            SpriteTrackerDbContext database,
            CancellationToken cancellationToken) =>
        {
            SetNoStoreHeader(context.Response);
            var user = await currentUser.GetOrCreateAsync(context.User, cancellationToken);
            var progress = await database.CheatCodeProgress
                .AsNoTracking()
                .Where(item => item.UserId == user.Id && item.CheatCode.SeasonId == CheatCodeSeedData.SeasonId)
                .OrderBy(item => item.CheatCodeId)
                .Select(item => new CheatCodeProgressDto
                {
                    CheatCodeId = item.CheatCodeId,
                    UsedAtUtc = item.UsedAtUtc
                })
                .ToArrayAsync(cancellationToken);
            return Results.Ok(progress);
        });

        group.MapPut("/", async (
            UpdateCheatCodeProgressRequest request,
            HttpContext context,
            IAntiforgery antiforgery,
            CurrentUserService currentUser,
            SpriteTrackerDbContext database,
            CancellationToken cancellationToken) =>
        {
            await antiforgery.ValidateRequestAsync(context);
            var code = await database.CheatCodes.SingleOrDefaultAsync(
                item => item.Id == request.CheatCodeId && item.SeasonId == CheatCodeSeedData.SeasonId && item.IsTrackable,
                cancellationToken);
            if (code is null)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(request.CheatCodeId)] = ["The cheat code cannot be tracked."]
                });
            }

            var user = await currentUser.GetOrCreateAsync(context.User, cancellationToken);
            var progress = await database.CheatCodeProgress.FindAsync([user.Id, code.Id], cancellationToken);
            if (!request.IsUsed)
            {
                if (progress is not null)
                {
                    database.CheatCodeProgress.Remove(progress);
                    await database.SaveChangesAsync(cancellationToken);
                }

                return Results.Ok(new CheatCodeProgressDto { CheatCodeId = code.Id, UsedAtUtc = DateTimeOffset.UtcNow });
            }

            if (progress is null)
            {
                progress = new DataAccess.Entities.CheatCodeProgress { UserId = user.Id, CheatCodeId = code.Id };
                database.CheatCodeProgress.Add(progress);
                await database.SaveChangesAsync(cancellationToken);
            }

            return Results.Ok(new CheatCodeProgressDto { CheatCodeId = code.Id, UsedAtUtc = progress.UsedAtUtc });
        });

        return endpoints;
    }

    private static void SetNoStoreHeader(HttpResponse response) => response.Headers.CacheControl = "no-store";
}
