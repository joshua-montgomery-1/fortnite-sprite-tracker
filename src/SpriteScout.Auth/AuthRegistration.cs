using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Validation.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace SpriteScout.Auth;

public static class AuthRegistration
{
    public static AuthOptions AddAuth(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment,
        string connectionString)
    {
        var settings = configuration.GetSection("SpriteScoutAuth").Get<AuthOptions>() ?? new();
        var issuer = ValidateConfiguration(settings, environment);
        ValidateClients(settings.Clients);
        AddAccountServices(services, settings, connectionString);
        AddBrowserAuthentication(services, configuration, issuer);
        AddOAuth(services, settings, issuer, environment);
        AddMcpAuthorization(services);
        return settings;
    }

    private static Uri ValidateConfiguration(AuthOptions settings, IHostEnvironment environment)
    {
        var issuer = new Uri(settings.Issuer, UriKind.Absolute);
        var resource = new Uri(settings.Resource, UriKind.Absolute);
        if (issuer.Scheme != "https" || resource.Scheme != "https" ||
            !settings.Issuer.EndsWith('/') ||
            issuer.AbsolutePath == "/" || issuer.Query.Length != 0 || issuer.Fragment.Length != 0 ||
            resource.Query.Length != 0 || resource.Fragment.Length != 0 ||
            issuer.UserInfo.Length != 0 || resource.UserInfo.Length != 0 ||
            (!environment.IsDevelopment() && (issuer.IsLoopback || resource.IsLoopback)))
        {
            throw new InvalidOperationException("Auth requires HTTPS URLs and an issuer path ending in '/'. Production URLs must not be loopback URLs.");
        }
        return issuer;
    }

    private static void AddAccountServices(
        IServiceCollection services, AuthOptions settings, string connectionString)
    {
        services.AddSingleton(settings);
        services.AddDbContext<AuthDbContext>(options => options.UseNpgsql(connectionString,
            postgres => postgres.MigrationsHistoryTable("__EFMigrationsHistory", "sprite_scout_auth"))
            .UseOpenIddict());
        services.AddScoped<AuthIdentityService>();
        services.AddHostedService<AuthDatabaseInitializer>();
    }

    private static void ValidateClients(IEnumerable<McpClientOptions> clients)
    {
        var clientIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var client in clients)
        {
            if (string.IsNullOrWhiteSpace(client.ClientId) || !clientIds.Add(client.ClientId) ||
                string.IsNullOrWhiteSpace(client.DisplayName) || client.RedirectUris.Count == 0 ||
                client.ApplicationType is not (ApplicationTypes.Native or ApplicationTypes.Web))
            {
                throw new InvalidOperationException("MCP clients require a unique client ID, display name, native/web application type, and callback URLs.");
            }

            foreach (var callback in client.RedirectUris)
            {
                if (!Uri.TryCreate(callback, UriKind.Absolute, out var uri) ||
                    uri.Fragment.Length != 0 || uri.UserInfo.Length != 0 ||
                    (uri.Scheme != Uri.UriSchemeHttps &&
                     !(client.ApplicationType == ApplicationTypes.Native &&
                       uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback)))
                {
                    throw new InvalidOperationException($"MCP client '{client.ClientId}' requires HTTPS callbacks or native HTTP loopback callbacks without fragments or user info.");
                }
            }
        }
    }

    private static void AddBrowserAuthentication(
        IServiceCollection services, IConfiguration configuration, Uri issuer)
    {
        var authentication = services.AddAuthentication()
            .AddCookie(AuthDefaults.SessionScheme, options =>
            {
                options.Cookie.Name = "__Host-spritescout-auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.Path = "/";
                options.ExpireTimeSpan = TimeSpan.FromDays(30);
            });

        var clientId = configuration["Authentication:Google:ClientId"];
        var clientSecret = configuration["Authentication:Google:ClientSecret"];
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret)) return;

        services.AddScoped<GoogleAuthEvents>();
        authentication.AddGoogle(AuthDefaults.GoogleScheme, options =>
        {
            options.ClientId = clientId;
            options.ClientSecret = clientSecret;
            options.SignInScheme = AuthDefaults.SessionScheme;
            options.CallbackPath = issuer.AbsolutePath.TrimEnd('/') + "/signin-google";
            options.EventsType = typeof(GoogleAuthEvents);
        });
    }

    private static void AddOAuth(IServiceCollection services, AuthOptions settings, Uri issuer, IHostEnvironment environment)
    {
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
                options.RegisterScopes(AuthDefaults.AccountReadScope, AuthDefaults.AccountWriteScope,
                    AuthDefaults.CollectionReadScope, AuthDefaults.CollectionWriteScope);
                options.RegisterResources(settings.Resource);
                options.SetAccessTokenLifetime(TimeSpan.FromMinutes(10));
                options.SetRefreshTokenLifetime(TimeSpan.FromDays(30));
                options.SetRefreshTokenReuseLeeway(TimeSpan.Zero);
                if (environment.IsDevelopment())
                {
                    options.AddDevelopmentEncryptionCertificate().AddDevelopmentSigningCertificate();
                }
                else
                {
                    options.AddSigningCertificate(LoadCertificate(settings.Certificates.SigningPath,
                        settings.Certificates.SigningBase64, settings.Certificates.SigningPassword, "Signing"));
                    options.AddEncryptionCertificate(LoadCertificate(settings.Certificates.EncryptionPath,
                        settings.Certificates.EncryptionBase64, settings.Certificates.EncryptionPassword, "Encryption"));
                }
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
    }

    private static void AddMcpAuthorization(IServiceCollection services)
    {
        services.AddScoped<IAuthorizationHandler, AccountAuthorizationHandler>();
        services.AddAuthorization(options => options.AddPolicy("McpAccount", policy =>
        {
            policy.AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
            policy.RequireAuthenticatedUser();
            policy.RequireAssertion(context => context.User.HasScope(AuthDefaults.AccountReadScope));
            policy.AddRequirements(new AccountRequirement());
        }));
    }

    private static X509Certificate2 LoadCertificate(string path, string base64, string? password, string purpose)
    {
        var hasPath = !string.IsNullOrWhiteSpace(path);
        var hasBase64 = !string.IsNullOrWhiteSpace(base64);
        if (hasPath == hasBase64)
            throw new InvalidOperationException($"Production auth requires exactly one of SpriteScoutAuth:Certificates:{purpose}Base64 or {purpose}Path.");
        if (hasPath)
            return X509CertificateLoader.LoadPkcs12FromFile(path, password, X509KeyStorageFlags.EphemeralKeySet);

        byte[] bytes;
        try { bytes = Convert.FromBase64String(base64); }
        catch (FormatException)
        {
            throw new InvalidOperationException($"SpriteScoutAuth:Certificates:{purpose}Base64 must contain a Base64-encoded PFX.");
        }
        try { return X509CertificateLoader.LoadPkcs12(bytes, password, X509KeyStorageFlags.EphemeralKeySet); }
        finally { System.Security.Cryptography.CryptographicOperations.ZeroMemory(bytes); }
    }
}
