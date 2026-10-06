using System.Globalization;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace SpriteScout.Auth;

public static class CentralAuthEndpoints
{
    public static void UseCentralAuthBoundary(this WebApplication app, CentralAuthOptions settings)
    {
        if (!settings.Enabled) return;
        var issuer = new Uri(settings.Issuer);
        var resource = new Uri(settings.Resource);
        var metadataPath = "/.well-known/oauth-authorization-server" + issuer.AbsolutePath.TrimEnd('/');
        app.Use(async (context, next) =>
        {
            var isAuth = context.Request.Path.StartsWithSegments(issuer.AbsolutePath.TrimEnd('/')) ||
                context.Request.Path == metadataPath;
            var isMcp = context.Request.Path.StartsWithSegments(resource.AbsolutePath) ||
                context.Request.Path == "/.well-known/oauth-protected-resource/mcp";
            var expected = isAuth ? issuer : isMcp ? resource : null;
            if (expected is not null)
            {
                if (!string.Equals(context.Request.Host.Value, expected.Authority, StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }
                context.Response.Headers.CacheControl = "no-store";
            }
            if (isMcp)
            {
                context.Response.OnStarting(() =>
                {
                    if (context.Response.StatusCode is 401 or 403)
                    {
                        var error = context.Response.StatusCode == 403 ? ", error=\"insufficient_scope\"" : "";
                        context.Response.Headers.WWWAuthenticate =
                            $"Bearer resource_metadata=\"{resource.GetLeftPart(UriPartial.Authority)}/.well-known/oauth-protected-resource/mcp\", scope=\"{AuthDefaults.AccountReadScope}\"{error}";
                    }
                    return Task.CompletedTask;
                });
            }
            await next(context);
        });
    }

    public static void MapCentralAuthEndpoints(this WebApplication app, CentralAuthOptions settings)
    {
        if (!settings.Enabled) return;
        var issuer = new Uri(settings.Issuer);
        app.MapMethods(new Uri(issuer, "connect/authorize").AbsolutePath, ["GET", "POST"], AuthorizeAsync)
            .AllowAnonymous();
        app.MapPost(new Uri(issuer, "connect/token").AbsolutePath, ExchangeAsync)
            .AllowAnonymous();
        app.MapGet(new Uri(issuer, "error").AbsolutePath, () => Results.Problem(
            title: "Google sign-in could not be completed.", statusCode: 400)).AllowAnonymous();
        app.MapGet("/.well-known/oauth-protected-resource/mcp", () => Results.Json(new
        {
            resource = settings.Resource,
            authorization_servers = new[] { settings.Issuer },
            scopes_supported = new[] { AuthDefaults.AccountReadScope, Scopes.OfflineAccess },
            bearer_methods_supported = new[] { "header" }
        })).AllowAnonymous();
    }

    private static async Task<IResult> AuthorizeAsync(
        HttpContext context, AuthDbContext database, CentralAuthOptions settings,
        IOpenIddictApplicationManager applications, IOpenIddictAuthorizationManager authorizations,
        IAntiforgery antiforgery, CancellationToken cancellationToken)
    {
        var request = context.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OAuth request was not validated.");
        if (request.GetResources().Length != 1 || request.GetResources()[0] != settings.Resource)
            return Reject(Errors.InvalidTarget, "Request the Sprite Scout MCP resource.");
        var session = await context.AuthenticateAsync(AuthDefaults.SessionScheme);
        if (session.Principal is null)
        {
            var schemes = context.RequestServices.GetRequiredService<IAuthenticationSchemeProvider>();
            if (await schemes.GetSchemeAsync(AuthDefaults.GoogleScheme) is null)
                return Results.Problem("Configure Google credentials to sign in.", statusCode: 503);
            return Results.Challenge(new AuthenticationProperties
            {
                RedirectUri = context.Request.GetEncodedUrl(),
                IsPersistent = true
            }, [AuthDefaults.GoogleScheme]);
        }
        if (!Guid.TryParse(session.Principal.GetClaim(Claims.Subject), out var accountId) ||
            !await database.Accounts.AnyAsync(account => account.Id == accountId, cancellationToken))
            return Reject(Errors.AccessDenied, "The Sprite Scout account is unavailable.");

        var application = await applications.FindByClientIdAsync(request.ClientId!, cancellationToken)
            ?? throw new InvalidOperationException("The OAuth client was not found.");
        if (HttpMethods.IsGet(context.Request.Method))
        {
            var token = antiforgery.GetAndStoreTokens(context);
            var encode = HtmlEncoder.Default;
            var name = await applications.GetDisplayNameAsync(application, cancellationToken) ?? "This application";
            context.Response.Headers.ContentSecurityPolicy = "default-src 'none'; form-action 'self'; frame-ancestors 'none'; base-uri 'none'";
            var parameters = string.Join("", context.Request.Query.SelectMany(parameter => parameter.Value.Select(value =>
                $"<input type=\"hidden\" name=\"{encode.Encode(parameter.Key)}\" value=\"{encode.Encode(value ?? "")}\">")));
            return Results.Content($"""
                <!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width">
                <title>Connect to Sprite Scout</title></head><body>
                <main><h1>Connect to Sprite Scout</h1>
                <p>{encode.Encode(name)} wants to read your Sprite Scout account name and public profile identifier.</p>
                <p>This connection cannot change your collection.
                {(request.HasScope(Scopes.OfflineAccess) ? "It can stay connected for up to 30 days." : "")}</p>
                <form method="post" action="{encode.Encode(context.Request.Path)}">
                {parameters}
                <input type="hidden" name="{encode.Encode(token.FormFieldName)}" value="{encode.Encode(token.RequestToken!)}">
                <button name="decision" value="allow">Allow connection</button>
                <button name="decision" value="deny">Cancel</button></form></main></body></html>
                """, "text/html; charset=utf-8");
        }
        try
        {
            await antiforgery.ValidateRequestAsync(context);
        }
        catch (AntiforgeryValidationException)
        {
            return Results.BadRequest(new { error = "The consent form expired. Start sign-in again." });
        }
        if (context.Request.Form["decision"] != "allow")
            return Reject(Errors.AccessDenied, "The user declined the connection.");

        var identity = new ClaimsIdentity(
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, Claims.Name, Claims.Role);
        identity.SetClaim(Claims.Subject, accountId.ToString());
        identity.SetClaim("connection_expires_at",
            DateTimeOffset.UtcNow.AddDays(30).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture));
        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(request.GetScopes());
        principal.SetResources(settings.Resource);
        var grant = await authorizations.CreateAsync(
            identity: identity, subject: accountId.ToString(),
            client: (await applications.GetIdAsync(application, cancellationToken))!,
            type: AuthorizationTypes.Permanent, scopes: principal.GetScopes(), cancellationToken: cancellationToken);
        principal.SetAuthorizationId(await authorizations.GetIdAsync(grant, cancellationToken));
        return Results.SignIn(principal, authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private static async Task<IResult> ExchangeAsync(
        HttpContext context, AuthDbContext database, CentralAuthOptions settings, CancellationToken cancellationToken)
    {
        var request = context.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OAuth request was not validated.");
        if (!request.IsAuthorizationCodeGrantType() && !request.IsRefreshTokenGrantType())
            return Reject(Errors.UnsupportedGrantType, "Use an authorization code or refresh token.");
        if (request.GetResources().Any(resource => resource != settings.Resource))
            return Reject(Errors.InvalidTarget, "The resource does not match this connection.");
        var result = await context.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        var principal = result.Principal;
        if (principal is null || !Guid.TryParse(principal.GetClaim(Claims.Subject), out var accountId) ||
            !await database.Accounts.AnyAsync(account => account.Id == accountId, cancellationToken) ||
            !long.TryParse(principal.GetClaim("connection_expires_at"), out var expiresAt) ||
            expiresAt <= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            return Reject(Errors.InvalidGrant, "The connection expired or its account is unavailable.");
        if (request.IsRefreshTokenGrantType() && !string.IsNullOrEmpty(request.Scope))
        {
            if (request.GetScopes().Except(principal.GetScopes()).Any())
                return Reject(Errors.InvalidScope, "Refresh cannot add permissions.");
            principal.SetScopes(request.GetScopes());
        }
        return Results.SignIn(principal, authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private static IResult Reject(string error, string description) => Results.Forbid(
        new AuthenticationProperties(new Dictionary<string, string?>
        {
            [OpenIddictServerAspNetCoreConstants.Properties.Error] = error,
            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = description
        }), [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]);
}
