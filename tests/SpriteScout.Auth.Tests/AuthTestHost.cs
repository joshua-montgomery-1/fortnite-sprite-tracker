using System.Security.Claims;
using System.Text.Encodings.Web;
using FortniteSpriteTracker.DataAccess;
using FortniteSpriteTracker.DataAccess.Entities;
using FortniteSpriteTracker.Server.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using OpenIddict.Abstractions;
using SpriteScout.Auth;
using Testcontainers.PostgreSql;

namespace SpriteScout.Auth.Tests;

public sealed class AuthTestHost : WebApplicationFactory<Program>
{
    private readonly string connectionString;
    public AuthTestHost(string connectionString)
    {
        this.connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        var settings = new Dictionary<string, string>
            {
                ["ConnectionStrings:sprite-tracker"] = connectionString,
                ["SpriteScoutAuth:Issuer"] = "https://localhost:7082/identity/",
                ["SpriteScoutAuth:Resource"] = "https://localhost:7082/mcp",
                ["SpriteScoutAuth:Clients:0:ClientId"] = "integration-desktop",
                ["SpriteScoutAuth:Clients:0:DisplayName"] = "Integration desktop",
                ["SpriteScoutAuth:Clients:0:ApplicationType"] = "native",
                ["SpriteScoutAuth:Clients:0:RedirectUris:0"] = "http://127.0.0.1/callback",
                ["SpriteScoutAuth:Clients:1:ClientId"] = "integration-web",
                ["SpriteScoutAuth:Clients:1:DisplayName"] = "Integration web",
                ["SpriteScoutAuth:Clients:1:ApplicationType"] = "web",
                ["SpriteScoutAuth:Clients:1:RedirectUris:0"] = "https://mcp-client.example/oauth/callback",
                ["Authentication:Google:ClientId"] = "integration-test",
                ["Authentication:Google:ClientSecret"] = "integration-test",
                ["Logging:LogLevel:Default"] = "Warning",
                ["Logging:LogLevel:OpenIddict"] = "Critical"
            };
        // Minimal-hosting startup reads these values before ConfigureAppConfiguration callbacks.
        foreach (var setting in settings) builder.UseSetting(setting.Key, setting.Value);
        builder.ConfigureTestServices(services =>
        {
            // Substitute only the auth browser session in this test host. No test login endpoint
            // or header-based authentication is registered by the application.
            services.AddTransient<TestAuthSession>();
            services.Configure<AuthenticationOptions>(options =>
                options.SchemeMap[AuthDefaults.SessionScheme].HandlerType = typeof(TestAuthSession));
        });
    }

    public HttpClient Browser(Guid? accountId = null)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost:7082"),
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        if (accountId is not null)
            client.DefaultRequestHeaders.Add("X-Test-Account", accountId.ToString());
        client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        client.DefaultRequestHeaders.Accept.ParseAdd("text/event-stream");
        return client;
    }
}

internal sealed class TestAuthSession(
    IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Guid.TryParse(Request.Headers["X-Test-Account"], out var id))
            return Task.FromResult(AuthenticateResult.NoResult());
        var identity = new ClaimsIdentity(AuthDefaults.SessionScheme);
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Subject, id.ToString()));
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), AuthDefaults.SessionScheme)));
    }
}

public sealed class AuthFixture : IAsyncLifetime
{
    private PostgreSqlContainer? container;
    private string? adminConnection;
    private readonly string databaseName = "auth_tests_" + Guid.NewGuid().ToString("N");
    public string ConnectionString { get; private set; } = null!;
    public AuthTestHost Host { get; private set; } = null!;
    public Guid FirstAccount { get; private set; }
    public Guid SecondAccount { get; private set; }
    public long FirstUserId { get; private set; }
    public Guid FirstPublicId { get; private set; }
    public int VariantId { get; private set; }

    public async Task InitializeAsync()
    {
        adminConnection = Environment.GetEnvironmentVariable("SPRITESCOUT_TEST_DATABASE");
        if (string.IsNullOrEmpty(adminConnection))
        {
            container = new PostgreSqlBuilder("postgres:17-alpine").Build();
            await container.StartAsync();
            ConnectionString = container.GetConnectionString();
        }
        else
        {
            await using var admin = new NpgsqlConnection(adminConnection);
            await admin.OpenAsync();
            await using var command = new NpgsqlCommand($"CREATE DATABASE {databaseName}", admin);
            await command.ExecuteNonQueryAsync();
            var settings = new NpgsqlConnectionStringBuilder(adminConnection) { Database = databaseName };
            ConnectionString = settings.ConnectionString;
        }
        await using var website = new SpriteTrackerDbContext(
            new DbContextOptionsBuilder<SpriteTrackerDbContext>().UseNpgsql(ConnectionString).Options);
        await website.Database.MigrateAsync();
        var first = new UserAccount { GoogleSubject = "google-existing-1", DisplayName = "First Scout" };
        var second = new UserAccount { GoogleSubject = "google-existing-2", DisplayName = "Second Scout" };
        var variant = new SpriteVariant
        {
            ImagePath = "https://fortnitespritetracker.org/images/sprites/air_basic.webp",
            SpriteFamily = new SpriteFamily { Name = "Test", Slug = "test" },
            VariantStyle = new VariantStyle { Name = "Normal", Slug = "normal", Color = "#fff", Bonus = "Test" }
        };
        website.AddRange(first, second, variant);
        await website.SaveChangesAsync();
        var artworkSeason = new Season { Name = "Artwork test season", Chapter = 7, Number = 3, StartAt = DateTimeOffset.UtcNow.AddDays(-1) };
        website.Seasons.Add(artworkSeason);
        await website.SaveChangesAsync();
        await website.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO \"SeasonSpriteVariants\" (\"SeasonId\", \"SpriteVariantId\") VALUES ({artworkSeason.Id}, {variant.Id})");
        website.SpriteProgress.Add(new SpriteProgress
        {
            UserId = first.Id, SpriteVariantId = variant.Id, IsOwned = true, IsMastered = true
        });
        await website.SaveChangesAsync();
        FirstUserId = first.Id;
        FirstPublicId = first.PublicId;
        // Simulate a provider-owned auth namespace: auth migrations must leave it untouched.
        await website.Database.ExecuteSqlRawAsync("""
            CREATE SCHEMA auth;
            CREATE TABLE auth.platform_marker (id integer PRIMARY KEY);
            INSERT INTO auth.platform_marker VALUES (1);
            REVOKE ALL ON SCHEMA auth FROM PUBLIC;
            """);
        website.ChangeTracker.Clear();
        Host = new AuthTestHost(ConnectionString);
        using var browser = Host.Browser();
        await using var scope = Host.Services.CreateAsyncScope();
        var identities = scope.ServiceProvider.GetRequiredService<AuthIdentityService>();
        var profiles = scope.ServiceProvider.GetRequiredService<IAccountProfileProvisioner>();
        var firstAccount = await identities.GetOrCreateGoogleAsync(first.GoogleSubject, default);
        var secondAccount = await identities.GetOrCreateGoogleAsync(second.GoogleSubject, default);
        await profiles.GoogleSignedInAsync(firstAccount, first.GoogleSubject, first.DisplayName, default);
        await profiles.GoogleSignedInAsync(secondAccount, second.GoogleSubject, second.DisplayName, default);
        var upgraded = scope.ServiceProvider.GetRequiredService<SpriteTrackerDbContext>();
        Assert.False(upgraded.Database.HasPendingModelChanges());
        Assert.False(scope.ServiceProvider.GetRequiredService<AuthDbContext>().Database.HasPendingModelChanges());
        first = await upgraded.Users.SingleAsync(user => user.Id == first.Id);
        second = await upgraded.Users.SingleAsync(user => user.Id == second.Id);
        FirstAccount = first.AccountId!.Value;
        SecondAccount = second.AccountId!.Value;
        VariantId = variant.Id;
    }

    public async Task DisposeAsync()
    {
        await Host.DisposeAsync();
        NpgsqlConnection.ClearAllPools();
        if (container is not null)
            await container.DisposeAsync();
        else if (adminConnection is not null)
        {
            await using var admin = new NpgsqlConnection(adminConnection);
            await admin.OpenAsync();
            await using var command = new NpgsqlCommand($"DROP DATABASE {databaseName} WITH (FORCE)", admin);
            await command.ExecuteNonQueryAsync();
        }
    }
}
