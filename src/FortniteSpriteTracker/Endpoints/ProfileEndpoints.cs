using FortniteSpriteTracker.DataAccess;
using FortniteSpriteTracker.DataAccess.Entities;
using FortniteSpriteTracker.Server.Services;
using FortniteSpriteTracker.Shared.Profiles;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace FortniteSpriteTracker.Server.Endpoints;

public static class ProfileEndpoints
{
    public static IEndpointRouteBuilder MapProfileEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/me").RequireAuthorization();

        group.MapGet("/", async (HttpContext context, CurrentUserService currentUser, CancellationToken cancellationToken) =>
        {
            SetPrivateNoStoreHeaders(context.Response);
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
            user.IsCollectionPublic = request.IsCollectionPublic;
            user.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await database.SaveChangesAsync(cancellationToken);

            return Results.Ok(ToDto(user));
        });

        group.MapDelete("/", async (
            [FromBody] DeleteAccountRequest request,
            HttpContext context,
            IAntiforgery antiforgery,
            CurrentUserService currentUser,
            SpriteTrackerDbContext database,
            CancellationToken cancellationToken) =>
        {
            await antiforgery.ValidateRequestAsync(context);
            if (!string.Equals(request.Confirmation, "DELETE", StringComparison.Ordinal))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(request.Confirmation)] = ["Type DELETE to permanently delete your account."]
                });
            }

            var user = await currentUser.GetOrCreateAsync(context.User, cancellationToken);
            database.Users.Remove(user);
            await database.SaveChangesAsync(cancellationToken);
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.NoContent();
        });

        return endpoints;
    }

    private static void SetPrivateNoStoreHeaders(HttpResponse response)
    {
        response.Headers.CacheControl = "no-store, no-cache, private";
        response.Headers.Pragma = "no-cache";
        response.Headers.Vary = "Cookie";
    }

    private static UserProfileDto ToDto(UserAccount user) =>
        new(user.Id, user.PublicId, user.DisplayName, user.EpicDisplayName, user.IsCollectionPublic, !string.IsNullOrWhiteSpace(user.EpicDisplayName));
}
