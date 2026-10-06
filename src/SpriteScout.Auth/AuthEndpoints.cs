using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace SpriteScout.Auth;

public static class AuthEndpoints
{
    public static void UseAuthBoundary(this WebApplication app, AuthOptions settings)
    {
        var issuer = new Uri(settings.Issuer);
        var resource = new Uri(settings.Resource);
        // Clients prioritize challenged scopes over discovery metadata. Include
        // collection access so the default consent covers all advertised tools.
        var connectionScopes = string.Join(' ', AuthDefaults.AccountReadScope,
            AuthDefaults.AccountWriteScope, AuthDefaults.CollectionReadScope, AuthDefaults.CollectionWriteScope, Scopes.OfflineAccess);
        app.Use(async (context, next) =>
        {
            var isAuth = context.Request.Path.StartsWithSegments(issuer.AbsolutePath.TrimEnd('/')) ||
                context.Request.Path.StartsWithSegments("/identity") ||
                context.Request.Path == "/.well-known/oauth-authorization-server" + issuer.AbsolutePath.TrimEnd('/');
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
                            $"Bearer resource_metadata=\"{resource.GetLeftPart(UriPartial.Authority)}/.well-known/oauth-protected-resource\", scope=\"{connectionScopes}\"{error}";
                    }
                    return Task.CompletedTask;
                });
            }
            await next(context);
        });
    }

    public static void MapAuthEndpoints(this WebApplication app, AuthOptions settings)
    {
        var issuer = new Uri(settings.Issuer);
        app.MapMethods(new Uri(issuer, "connect/authorize").AbsolutePath, ["GET", "POST"], AuthorizeAsync)
            .AllowAnonymous();
        app.MapPost(new Uri(issuer, "connect/token").AbsolutePath, ExchangeAsync)
            .AllowAnonymous();
        app.MapPost(new Uri(issuer, "login").AbsolutePath, LoginAsync).AllowAnonymous();
        // The host's routed Blazor pages use stable UI paths even with a custom issuer path.
        if (issuer.AbsolutePath != "/identity/")
        {
            app.MapGet(new Uri(issuer, "login").AbsolutePath, () => Results.Redirect("/identity/login")).AllowAnonymous();
            app.MapGet(new Uri(issuer, "error").AbsolutePath, (HttpContext context) => Results.Redirect(
                QueryHelpers.AddQueryString("/identity/error", "returnUrl",
                    AuthPages.SafeRetry(settings, context.Request.Query["returnUrl"])))).AllowAnonymous();
        }
        // Publish the RFC 9728 root location used by ChatGPT and other MCP clients.
        app.MapGet("/.well-known/oauth-protected-resource", () => Results.Json(new
        {
            resource = settings.Resource,
            authorization_servers = new[] { settings.Issuer },
            scopes_supported = new[] { AuthDefaults.AccountReadScope, AuthDefaults.AccountWriteScope, AuthDefaults.CollectionReadScope,
                AuthDefaults.CollectionWriteScope, Scopes.OfflineAccess },
            bearer_methods_supported = new[] { "header" }
        })).AllowAnonymous();
    }

    private static async Task<IResult> AuthorizeAsync(
        HttpContext context, AuthDbContext database, AuthOptions settings,
        IOpenIddictApplicationManager applications, IOpenIddictAuthorizationManager authorizations,
        IAntiforgery antiforgery, CancellationToken cancellationToken)
    {
        var request = context.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OAuth request was not validated.");
        if (request.GetResources().Length != 1 || request.GetResources()[0] != settings.Resource)
            return Reject(Errors.InvalidTarget, "Request the Sprite Scout MCP resource.");
        if (!request.HasScope(AuthDefaults.AccountReadScope))
            return Reject(Errors.InvalidScope, "Request account:read to identify your Sprite Scout account.");
        var application = await applications.FindByClientIdAsync(request.ClientId!, cancellationToken)
            ?? throw new InvalidOperationException("The OAuth client was not found.");
        var name = await applications.GetDisplayNameAsync(application, cancellationToken) ?? "This application";
        var session = await context.AuthenticateAsync(AuthDefaults.SessionScheme);
        if (session.Principal is null)
        {
            var schemes = context.RequestServices.GetRequiredService<IAuthenticationSchemeProvider>();
            var available = await schemes.GetSchemeAsync(AuthDefaults.GoogleScheme) is not null;
            if (HttpMethods.IsGet(context.Request.Method) || !available)
            {
                var form = available ? AuthPages.Form(context, antiforgery, AuthorizationFields(context)) : null;
                return AuthPages.Login(context, name, form, available);
            }
            var retry = AuthorizationReturn(settings, request);
            if (!await ValidateFormAsync(context, antiforgery))
                return AuthPages.Error(context, retry, expired: true);
            if (context.Request.Form["decision"] != "signin") return Results.BadRequest();
            return Results.Challenge(new AuthenticationProperties
            {
                RedirectUri = retry,
                IsPersistent = true
            }, [AuthDefaults.GoogleScheme]);
        }
        if (!Guid.TryParse(session.Principal.GetClaim(Claims.Subject), out var accountId) ||
            !await database.Accounts.AnyAsync(account => account.Id == accountId, cancellationToken))
            return Reject(Errors.AccessDenied, "The Sprite Scout account is unavailable.");

        if (HttpMethods.IsGet(context.Request.Method))
        {
            var form = AuthPages.Form(context, antiforgery, AuthorizationFields(context), consent: true);
            return AuthPages.Consent(context, name, request.HasScope(Scopes.OfflineAccess),
                request.HasScope(AuthDefaults.CollectionReadScope), request.HasScope(AuthDefaults.CollectionWriteScope),
                request.HasScope(AuthDefaults.AccountWriteScope), form, request.RedirectUri!);
        }
        if (!await ValidateFormAsync(context, antiforgery))
            return AuthPages.Error(context, AuthorizationReturn(settings, request), expired: true);
        if (context.Request.Form["decision"] != "allow")
            return Reject(Errors.AccessDenied, "The user declined the connection.");

        var approvedScopes = context.Request.Form["approved_scope"].Select(scope => scope ?? "")
            .Append(AuthDefaults.AccountReadScope).Distinct(StringComparer.Ordinal).ToArray();
        if (approvedScopes.Any(scope => !request.HasScope(scope) ||
            scope is not (AuthDefaults.AccountReadScope or AuthDefaults.AccountWriteScope or AuthDefaults.CollectionReadScope or
                AuthDefaults.CollectionWriteScope or Scopes.OfflineAccess)))
            return Reject(Errors.InvalidScope, "Approve only permissions requested by this application.");

        var identity = new ClaimsIdentity(
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, Claims.Name, Claims.Role);
        identity.SetClaim(Claims.Subject, accountId.ToString());
        identity.SetClaim("connection_expires_at",
            DateTimeOffset.UtcNow.AddDays(30).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture));
        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(approvedScopes);
        principal.SetResources(settings.Resource);
        var grant = await authorizations.CreateAsync(
            identity: identity, subject: accountId.ToString(),
            client: (await applications.GetIdAsync(application, cancellationToken))!,
            type: AuthorizationTypes.Permanent, scopes: principal.GetScopes(), cancellationToken: cancellationToken);
        principal.SetAuthorizationId(await authorizations.GetIdAsync(grant, cancellationToken));
        return Results.SignIn(principal, authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private static IEnumerable<AuthFormField> AuthorizationFields(HttpContext context) =>
        context.Request.Query.Where(parameter =>
                !string.Equals(parameter.Key, "approved_scope", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(parameter.Key, "decision", StringComparison.OrdinalIgnoreCase))
            .SelectMany(parameter => parameter.Value.Select(value =>
            new AuthFormField(parameter.Key, value ?? "")));

    private static string AuthorizationReturn(AuthOptions settings, OpenIddictRequest request) =>
        QueryHelpers.AddQueryString(new Uri(new Uri(settings.Issuer), "connect/authorize").AbsoluteUri,
            request.GetParameters().Where(parameter =>
                    parameter.Key is not ("decision" or "__RequestVerificationToken") &&
                    !string.Equals(parameter.Key, "approved_scope", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(parameter => parameter.Key, parameter => (string?)parameter.Value.ToString()));

    private static async Task<bool> ValidateFormAsync(HttpContext context, IAntiforgery antiforgery)
    {
        try { await antiforgery.ValidateRequestAsync(context); return true; }
        catch (AntiforgeryValidationException) { return false; }
    }

    private static async Task<IResult> LoginAsync(HttpContext context, IAntiforgery antiforgery,
        AuthOptions settings, IAuthenticationSchemeProvider schemes)
    {
        var login = new Uri(new Uri(settings.Issuer), "login").AbsoluteUri;
        if (HttpMethods.IsPost(context.Request.Method))
        {
            if (!await ValidateFormAsync(context, antiforgery))
                return AuthPages.Error(context, login, expired: true);
            if (await schemes.GetSchemeAsync(AuthDefaults.GoogleScheme) is not null)
                return Results.Challenge(new AuthenticationProperties { RedirectUri = login, IsPersistent = true },
                    [AuthDefaults.GoogleScheme]);
        }
        var session = await context.AuthenticateAsync(AuthDefaults.SessionScheme);
        if (session.Succeeded) return AuthPages.SignedIn(context);
        var available = await schemes.GetSchemeAsync(AuthDefaults.GoogleScheme) is not null;
        return AuthPages.Login(context, null, available ? AuthPages.Form(context, antiforgery, []) : null, available);
    }

    private static async Task<IResult> ExchangeAsync(
        HttpContext context, AuthDbContext database, AuthOptions settings, CancellationToken cancellationToken)
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
