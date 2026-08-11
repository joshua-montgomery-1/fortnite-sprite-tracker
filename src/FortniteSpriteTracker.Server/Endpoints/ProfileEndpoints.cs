using FortniteSpriteTracker.Server.Data;
using FortniteSpriteTracker.Server.Services;
using FortniteSpriteTracker.Shared.Profiles;
using Microsoft.AspNetCore.Antiforgery;

namespace FortniteSpriteTracker.Server.Endpoints;

public static class ProfileEndpoints
{
    public static IEndpointRouteBuilder MapProfileEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/me").RequireAuthorization();

        group.MapGet("/", async (HttpContext context, CurrentUserService currentUser, CancellationToken cancellationToken) =>
        {
            var user = await currentUser.GetOrCreateAsync(context.User, cancellationToken);
            return Results.Ok(ToDto(user));
        });

        group.MapPut("/profile", async (
            UpdateUserProfileRequest request,
            HttpContext context,
            IAntiforgery antiforgery,
            CurrentUserService currentUser,
            SpriteTrackerDbContext database,
            CancellationToken cancellationToken) =>
        {
            await antiforgery.ValidateRequestAsync(context);

            var displayName = request.DisplayName.Trim();
            var epicDisplayName = request.EpicDisplayName?.Trim();
            if (displayName.Length is < 1 or > 80)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(request.DisplayName)] = ["Display name must be between 1 and 80 characters."]
                });
            }

            if (epicDisplayName is not null && epicDisplayName.Length is < 3 or > 16)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(request.EpicDisplayName)] = ["Epic Games Display Name must be between 3 and 16 characters."]
                });
            }

            var user = await currentUser.GetOrCreateAsync(context.User, cancellationToken);
            user.DisplayName = displayName;
            user.EpicDisplayName = string.IsNullOrWhiteSpace(epicDisplayName) ? null : epicDisplayName;
            user.NormalizedEpicDisplayName = CurrentUserService.NormalizeEpicDisplayName(epicDisplayName);
            user.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await database.SaveChangesAsync(cancellationToken);

            return Results.Ok(ToDto(user));
        });

        return endpoints;
    }

    private static UserProfileDto ToDto(Data.Entities.UserAccount user) =>
        new(user.Id, user.DisplayName, user.EpicDisplayName, !string.IsNullOrWhiteSpace(user.EpicDisplayName));
}
