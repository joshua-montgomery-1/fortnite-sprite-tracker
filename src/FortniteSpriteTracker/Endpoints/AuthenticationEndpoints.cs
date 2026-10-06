using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using SpriteScout.Auth;

namespace FortniteSpriteTracker.Server.Endpoints;

public static class AuthenticationEndpoints
{
    public static IEndpointRouteBuilder MapAuthenticationEndpoints(
        this IEndpointRouteBuilder endpoints,
        bool googleAuthenticationConfigured)
    {
        endpoints.MapMethods("/auth/login", ["GET", "POST"], async (HttpContext context, IAntiforgery antiforgery) =>
        {
            if (HttpMethods.IsGet(context.Request.Method))
            {
                var destination = context.Request.Query["returnUrl"].ToString();
                return AuthPages.WebsiteLogin(context, antiforgery,
                    IsLocalReturnUrl(destination) ? destination : "/", googleAuthenticationConfigured);
            }
            try { await antiforgery.ValidateRequestAsync(context); }
            catch (AntiforgeryValidationException) { return AuthPages.WebsiteError(context); }
            if (!googleAuthenticationConfigured) return AuthPages.WebsiteLogin(context, antiforgery, "/", false);
            var returnUrl = context.Request.Form["returnUrl"].ToString();
            var safeReturnUrl = IsLocalReturnUrl(returnUrl) ? returnUrl! : "/";
            return Results.Challenge(
                new AuthenticationProperties
                {
                    RedirectUri = safeReturnUrl,
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
                },
                [GoogleDefaults.AuthenticationScheme]);
        }).AllowAnonymous();

        endpoints.MapPost("/auth/logout", async (HttpContext context, IAntiforgery antiforgery) =>
        {
            await antiforgery.ValidateRequestAsync(context);
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.NoContent();
        }).RequireAuthorization();

        endpoints.MapGet("/api/antiforgery/token", (HttpContext context, IAntiforgery antiforgery) =>
        {
            var tokens = antiforgery.GetAndStoreTokens(context);
            return Results.Ok(new { token = tokens.RequestToken });
        }).RequireAuthorization();

        return endpoints;
    }

    private static bool IsLocalReturnUrl(string? returnUrl) =>
        !string.IsNullOrWhiteSpace(returnUrl)
        && returnUrl.StartsWith('/')
        && !returnUrl.StartsWith("//")
        && !returnUrl.Contains('\\')
        && !returnUrl.Any(char.IsControl);
}
