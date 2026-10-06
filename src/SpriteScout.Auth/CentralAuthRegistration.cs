using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Validation.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace SpriteScout.Auth;

public static class CentralAuthRegistration
{
    public static CentralAuthOptions AddCentralAuth(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment,
        string connectionString)
    {
        var settings = configuration.GetSection("CentralAuth").Get<CentralAuthOptions>() ?? new();
        if (!settings.Enabled)
        {
            return settings;
        }
        if (!environment.IsDevelopment())
        {
            throw new InvalidOperationException("Phase 1 central auth is available only in Development.");
        }
        var issuer = new Uri(settings.Issuer, UriKind.Absolute);
        var resource = new Uri(settings.Resource, UriKind.Absolute);
        if (issuer.Scheme != "https" || resource.Scheme != "https" ||
            !issuer.IsLoopback || !resource.IsLoopback || !settings.Issuer.EndsWith('/') ||
            issuer.AbsolutePath == "/" || issuer.Query.Length != 0 || issuer.Fragment.Length != 0 ||
            resource.Query.Length != 0 || resource.Fragment.Length != 0)
        {
            throw new InvalidOperationException("Phase 1 requires HTTPS loopback URLs and an issuer path ending in '/'.");
        }
        services.AddSingleton(settings);
        services.AddDbContext<AuthDbContext>(options => options.UseNpgsql(connectionString,
            postgres => postgres.MigrationsHistoryTable("__EFMigrationsHistory", "sprite_scout_auth"))
            .UseOpenIddict());
        services.AddScoped<AuthIdentityService>();
        services.AddHostedService<AuthDatabaseInitializer>();
        services.AddScoped<IAuthorizationHandler, CentralAccountAuthorizationHandler>();

        services.AddAuthentication()
            .AddCookie(AuthDefaults.SessionScheme, options =>
            {
                options.Cookie.Name = "__Host-spritescout-central";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
                options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
                options.Cookie.Path = "/";
                options.ExpireTimeSpan = TimeSpan.FromDays(30);
            });
        var googleClientId = configuration["Authentication:Google:ClientId"];
        var googleClientSecret = configuration["Authentication:Google:ClientSecret"];
        if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
        {
            services.AddAuthentication().AddGoogle(AuthDefaults.GoogleScheme, options =>
            {
                options.ClientId = googleClientId;
                options.ClientSecret = googleClientSecret;
                options.SignInScheme = AuthDefaults.SessionScheme;
                options.CallbackPath = issuer.AbsolutePath.TrimEnd('/') + "/signin-google";
                options.Events.OnCreatingTicket = async context =>
                {
                    var subject = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? throw new InvalidOperationException("Google did not return a subject.");
                    var accounts = context.HttpContext.RequestServices.GetRequiredService<AuthIdentityService>();
                    var accountId = await accounts.GetOrCreateGoogleAsync(subject, context.HttpContext.RequestAborted);
                    foreach (var observer in context.HttpContext.RequestServices.GetServices<ICentralAccountObserver>())
                    {
                        await observer.GoogleSignedInAsync(accountId, subject,
                            context.Principal?.FindFirstValue(ClaimTypes.Name), context.HttpContext.RequestAborted);
                    }
                    var identity = new ClaimsIdentity(AuthDefaults.SessionScheme);
                    identity.AddClaim(new Claim(Claims.Subject, accountId.ToString()));
                    context.Principal = new ClaimsPrincipal(identity);
                };
                options.Events.OnRemoteFailure = context =>
                {
                    context.Response.Redirect(new Uri(issuer, "error").AbsoluteUri);
                    context.HandleResponse();
                    return Task.CompletedTask;
                };
            });
        }

        services.AddOpenIddict()
            .AddCore(options => options.UseEntityFrameworkCore().UseDbContext<AuthDbContext>())
            .AddServer(options =>
            {
                options.SetIssuer(issuer);
                options.SetAuthorizationEndpointUris(new Uri(issuer, "connect/authorize"));
                options.SetTokenEndpointUris(new Uri(issuer, "connect/token"));
                options.SetConfigurationEndpointUris(new Uri(issuer, ".well-known/openid-configuration"),
                    new Uri(issuer.GetLeftPart(UriPartial.Authority) +
                        "/.well-known/oauth-authorization-server" + issuer.AbsolutePath.TrimEnd('/')));
                options.SetJsonWebKeySetEndpointUris(new Uri(issuer, ".well-known/jwks"));
                options.AllowAuthorizationCodeFlow().AllowRefreshTokenFlow();
                options.RequireProofKeyForCodeExchange();
                options.RegisterScopes(AuthDefaults.AccountReadScope);
                options.RegisterResources(settings.Resource);
                options.SetAccessTokenLifetime(TimeSpan.FromMinutes(10));
                options.SetRefreshTokenLifetime(TimeSpan.FromDays(30));
                options.SetRefreshTokenReuseLeeway(TimeSpan.Zero);
                options.AddDevelopmentEncryptionCertificate().AddDevelopmentSigningCertificate();
                options.DisableAccessTokenEncryption();
                options.Configure(server => server.CodeChallengeMethods.Remove(CodeChallengeMethods.Plain));
                options.UseAspNetCore().EnableAuthorizationEndpointPassthrough().EnableTokenEndpointPassthrough();
                // RFC 9207 protects callbacks against authorization-server mix-up.
                options.AddEventHandler<OpenIddictServerEvents.ApplyConfigurationResponseContext>(handler =>
                    handler.UseInlineHandler(context =>
                    {
                        context.Response["authorization_response_iss_parameter_supported"] = true;
                        return default;
                    }));
                options.AddEventHandler<OpenIddictServerEvents.ApplyAuthorizationResponseContext>(handler =>
                    handler.UseInlineHandler(context =>
                    {
                        context.Response["iss"] = settings.Issuer;
                        return default;
                    }));
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.AddAudiences(settings.Resource);
                options.EnableTokenEntryValidation();
                options.EnableAuthorizationEntryValidation();
                options.UseAspNetCore();
            });
        services.AddAuthorization(options => options.AddPolicy("CentralMcp", policy =>
        {
            policy.AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
            policy.RequireAuthenticatedUser();
            policy.RequireAssertion(context => context.User.HasScope(AuthDefaults.AccountReadScope));
            policy.AddRequirements(new CentralAccountRequirement());
        }));
        return settings;
    }
}

internal sealed class CentralAccountRequirement : IAuthorizationRequirement;

internal sealed class CentralAccountAuthorizationHandler(AuthDbContext database)
    : AuthorizationHandler<CentralAccountRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, CentralAccountRequirement requirement)
    {
        if (Guid.TryParse(context.User.GetClaim(Claims.Subject), out var id) &&
            await database.Accounts.AnyAsync(account => account.Id == id))
        {
            context.Succeed(requirement);
        }
    }
}
