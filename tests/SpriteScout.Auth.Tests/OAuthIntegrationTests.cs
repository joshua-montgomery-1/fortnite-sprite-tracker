using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using FortniteSpriteTracker.DataAccess;
using FortniteSpriteTracker.Server.Services;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server;

namespace SpriteScout.Auth.Tests;

public sealed class OAuthIntegrationTests(AuthFixture fixture) : IClassFixture<AuthFixture>
{
    private const string ClientId = "integration-desktop";
    private const string Issuer = "https://localhost:7082/identity/";
    private const string Resource = "https://localhost:7082/mcp";
    private const string Callback = "http://127.0.0.1:48123/callback";

    [Fact]
    public async Task Configured_web_client_can_authenticate_and_call_the_same_MCP_tool()
    {
        const string webClient = "integration-web";
        const string webCallback = "https://mcp-client.example/oauth/callback";
        using var browser = fixture.Host.Browser(fixture.FirstAccount);
        var (code, verifier) = await AuthorizeAsync(browser, clientId: webClient, callback: webCallback);
        var response = await RedeemAsync(browser, code, verifier, webClient, webCallback);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var token = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(fixture.FirstAccount.ToString(),
            await CallAsync(fixture.Host, token.GetProperty("access_token").GetString()!));
    }

    [Fact]
    public async Task Configured_clients_cannot_use_each_others_callbacks_or_authorization_codes()
    {
        using var browser = fixture.Host.Browser(fixture.FirstAccount);
        var parameters = Parameters(clientId: "integration-web");
        var response = await browser.GetAsync(QueryHelpers.AddQueryString("/identity/connect/authorize", parameters));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(response.Headers.Location);

        parameters = Parameters(clientId: "unregistered-client");
        response = await browser.GetAsync(QueryHelpers.AddQueryString("/identity/connect/authorize", parameters));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(response.Headers.Location);

        var (code, verifier) = await AuthorizeAsync(browser);
        response = await RedeemAsync(browser, code, verifier, "integration-web",
            "https://mcp-client.example/oauth/callback");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }


    [Theory]
    [InlineData("/#collection", "/#collection")]
    [InlineData("https://attacker.example", "/")]
    [InlineData("//attacker.example", "/")]
    [InlineData("/\\attacker.example", "/")]
    public async Task Website_login_preserves_only_local_destinations(string destination, string expected)
    {
        using var browser = fixture.Host.Browser();
        var page = await browser.GetStringAsync(QueryHelpers.AddQueryString("/auth/login", "returnUrl", destination));
        Assert.Contains("/css/app.css", page);
        Assert.Contains("prefers-reduced-motion:reduce", page);
        Assert.Contains("class=\"scenery\" aria-hidden=\"true\"", page);
        Assert.Single(Regex.Matches(page, "class=\"sprite-layer layer-").Cast<Match>());
        Assert.Contains("https://fortnitespritetracker.org/images/sprites/air_basic.webp", page);
        Assert.Contains("data-enhance=\"false\"", page);
        var response = await browser.PostAsync("/auth/login", Consent(page, "signin"));
        var google = fixture.Host.Services.GetRequiredService<IOptionsMonitor<Microsoft.AspNetCore.Authentication.Google.GoogleOptions>>()
            .Get(Microsoft.AspNetCore.Authentication.Google.GoogleDefaults.AuthenticationScheme);
        var state = QueryHelpers.ParseQuery(response.Headers.Location!.Query)["state"].ToString();
        Assert.Equal(expected, google.StateDataFormat.Unprotect(state)!.RedirectUri);
    }

    [Fact]
    public async Task Website_login_requires_antiforgery_before_challenging_Google()
    {
        using var browser = fixture.Host.Browser();
        var response = await browser.PostAsync("/auth/login", new FormUrlEncodedContent(new Dictionary<string, string> { ["returnUrl"] = "/" }));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(response.Headers.Location);
        Assert.Contains("Try again", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Auth_migrations_leave_the_reserved_auth_schema_untouched()
    {
        await using var connection = new Npgsql.NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new Npgsql.NpgsqlCommand("""
            SELECT count(*) FROM pg_tables WHERE schemaname = 'auth';
            """, connection);
        Assert.Equal(1L, await command.ExecuteScalarAsync());
        command.CommandText = "SELECT count(*) FROM auth.platform_marker WHERE id = 1";
        Assert.Equal(1L, await command.ExecuteScalarAsync());
        command.CommandText = "SELECT count(*) FROM sprite_scout_auth.\"__EFMigrationsHistory\"";
        Assert.Equal(1L, await command.ExecuteScalarAsync());
    }

    [Fact]
    public async Task Sql_import_is_repeatable_and_preserves_profile_and_progress()
    {
        await using var scope = fixture.Host.Services.CreateAsyncScope();
        var auth = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var accountCount = await auth.Accounts.CountAsync();
        var identityCount = await auth.ExternalIdentities.CountAsync();
        var script = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "migrate-auth-accounts.sql"));
        var sql = script.Split("-- BEGIN ACCOUNT IMPORT")[1].Split("-- END ACCOUNT IMPORT")[0];
        await using (var transaction = await auth.Database.BeginTransactionAsync())
        {
            await auth.Database.ExecuteSqlRawAsync(sql);
            await auth.Database.ExecuteSqlRawAsync(sql);
            await transaction.CommitAsync();
        }
        Assert.Equal(accountCount, await auth.Accounts.CountAsync());
        Assert.Equal(identityCount, await auth.ExternalIdentities.CountAsync());
        var website = scope.ServiceProvider.GetRequiredService<SpriteTrackerDbContext>();
        var user = await website.Users.SingleAsync(item => item.Id == fixture.FirstUserId);
        Assert.Equal(fixture.FirstAccount, user.AccountId);
        Assert.Equal(fixture.FirstPublicId, user.PublicId);
        var progress = await website.SpriteProgress.SingleAsync(item => item.UserId == user.Id);
        Assert.Equal(fixture.VariantId, progress.SpriteVariantId);
        Assert.True(progress.IsOwned && progress.IsMastered);
        Assert.NotEqual(fixture.FirstAccount, fixture.SecondAccount);
        var identities = scope.ServiceProvider.GetRequiredService<AuthIdentityService>();
        Assert.Equal(fixture.FirstAccount, await identities.GetOrCreateGoogleAsync("google-existing-1", default));
    }

    [Fact]
    public async Task Google_observer_links_new_profiles_without_overwriting_existing_names()
    {
        await using var scope = fixture.Host.Services.CreateAsyncScope();
        var identities = scope.ServiceProvider.GetRequiredService<AuthIdentityService>();
        var observer = scope.ServiceProvider.GetRequiredService<IAccountProfileProvisioner>();
        await observer.GoogleSignedInAsync(fixture.FirstAccount, "google-existing-1", "Changed Google name", default);
        var newAccount = await identities.GetOrCreateGoogleAsync("google-new", default);
        await observer.GoogleSignedInAsync(newAccount, "google-new", "New Scout", default);
        var database = scope.ServiceProvider.GetRequiredService<SpriteTrackerDbContext>();
        Assert.Equal("First Scout", (await database.Users.SingleAsync(user => user.Id == fixture.FirstUserId)).DisplayName);
        Assert.Equal(newAccount, (await database.Users.SingleAsync(user => user.GoogleSubject == "google-new")).AccountId);
    }

    [Fact]
    public async Task Discovery_and_missing_credentials_provide_OAuth_metadata()
    {
        using var client = fixture.Host.Browser();
        var discovery = await client.GetFromJsonAsync<JsonElement>("/identity/.well-known/openid-configuration");
        Assert.Equal(Issuer, discovery.GetProperty("issuer").GetString());
        Assert.True(discovery.GetProperty("authorization_response_iss_parameter_supported").GetBoolean());
        Assert.Contains("S256", discovery.GetProperty("code_challenge_methods_supported").EnumerateArray().Select(item => item.GetString()));
        Assert.DoesNotContain("plain", discovery.GetProperty("code_challenge_methods_supported").EnumerateArray().Select(item => item.GetString()));
        var resource = await client.GetFromJsonAsync<JsonElement>("/.well-known/oauth-protected-resource/mcp");
        Assert.Equal(Resource, resource.GetProperty("resource").GetString());
        var response = await client.PostAsJsonAsync("/mcp", new { jsonrpc = "2.0", id = 1, method = "tools/list" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("resource_metadata=", response.Headers.WwwAuthenticate.ToString());
        Assert.Equal("no-store", response.Headers.CacheControl!.ToString());
        // MCP tokens do not replace the existing website's cookie scheme.
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/me/")).StatusCode);
    }

    [Theory]
    [InlineData("missing-pkce")]
    [InlineData("plain-pkce")]
    [InlineData("wrong-callback")]
    [InlineData("wrong-loopback-path")]
    [InlineData("wrong-loopback-host")]
    [InlineData("unknown-client")]
    [InlineData("missing-resource")]
    [InlineData("wrong-resource")]
    [InlineData("unknown-scope")]
    public async Task Invalid_authorization_requests_are_rejected(string variation)
    {
        var parameters = Parameters();
        if (variation == "missing-pkce") { parameters.Remove("code_challenge"); parameters.Remove("code_challenge_method"); }
        if (variation == "plain-pkce") parameters["code_challenge_method"] = "plain";
        if (variation == "wrong-callback") parameters["redirect_uri"] = "https://attacker.example/callback";
        if (variation == "wrong-loopback-path") parameters["redirect_uri"] = "http://127.0.0.1:48123/other";
        if (variation == "wrong-loopback-host") parameters["redirect_uri"] = "http://localhost:48123/callback";
        if (variation == "unknown-client") parameters["client_id"] = "unknown";
        if (variation == "missing-resource") parameters.Remove("resource");
        if (variation == "wrong-resource") parameters["resource"] = "https://attacker.example/api";
        if (variation == "unknown-scope") parameters["scope"] = "admin:all";
        using var client = fixture.Host.Browser(fixture.FirstAccount);
        var response = await client.GetAsync(QueryHelpers.AddQueryString("/identity/connect/authorize", parameters));
        Assert.True(response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Redirect);
        if (response.Headers.Location is { } location)
        {
            Assert.StartsWith("http://127.0.0.1:", location.AbsoluteUri);
            Assert.Contains("error=", location.Query);
            Assert.DoesNotContain("code=", location.Query);
            Assert.Equal(Issuer, QueryHelpers.ParseQuery(location.Query)["iss"].ToString());
        }
    }

    [Fact]
    public async Task Google_challenge_uses_separate_callback_and_native_callback_ports_are_supported()
    {
        using var anonymous = fixture.Host.Browser();
        var path = QueryHelpers.AddQueryString("/identity/connect/authorize", Parameters());
        var login = await anonymous.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var html = await login.Content.ReadAsStringAsync();
        Assert.Contains("Continue with Google", html);
        Assert.Contains("Integration desktop", html);
        Assert.Contains("form-action 'self'", login.Headers.GetValues("Content-Security-Policy").Single());
        Assert.Contains("form-action 'self' https://accounts.google.com", login.Headers.GetValues("Content-Security-Policy").Single());
        var authChallenge = await anonymous.PostAsync(path, Consent(html, "signin"));
        Assert.Equal(HttpStatusCode.Redirect, authChallenge.StatusCode);
        Assert.Equal("accounts.google.com", authChallenge.Headers.Location!.Host);
        Assert.Equal(Issuer + "signin-google", QueryHelpers.ParseQuery(authChallenge.Headers.Location.Query)["redirect_uri"].ToString());
        var google = fixture.Host.Services.GetRequiredService<IOptionsMonitor<Microsoft.AspNetCore.Authentication.Google.GoogleOptions>>()
            .Get(AuthDefaults.GoogleScheme);
        var properties = google.StateDataFormat.Unprotect(QueryHelpers.ParseQuery(authChallenge.Headers.Location.Query)["state"]!);
        Assert.NotNull(properties);
        var preserved = QueryHelpers.ParseQuery(new Uri(properties.RedirectUri!).Query);
        Assert.Equal("integration-state", preserved["state"].ToString());
        Assert.Equal(Parameters()["code_challenge"], preserved["code_challenge"].ToString());
        Assert.False(preserved.ContainsKey("decision"));
        Assert.False(preserved.ContainsKey("__RequestVerificationToken"));
        var websitePage = await anonymous.GetStringAsync("/auth/login");
        var website = await anonymous.PostAsync("/auth/login", Consent(websitePage, "signin"));
        Assert.Equal(HttpStatusCode.Redirect, website.StatusCode);
        Assert.Equal("https://localhost:7082/signin-google", QueryHelpers.ParseQuery(website.Headers.Location!.Query)["redirect_uri"].ToString());
        using var browser = fixture.Host.Browser(fixture.FirstAccount);
        foreach (var callback in new[] { "http://127.0.0.1:43117/callback", "http://127.0.0.1:59123/callback" })
        {
            var parameters = Parameters();
            parameters["redirect_uri"] = callback;
            var consent = await browser.GetAsync(QueryHelpers.AddQueryString("/identity/connect/authorize", parameters));
            Assert.Equal(HttpStatusCode.OK, consent.StatusCode);
            Assert.Contains(new Uri(callback).GetLeftPart(UriPartial.Authority),
                consent.Headers.GetValues("Content-Security-Policy").Single());
        }
    }

    [Fact]
    public async Task Standalone_login_is_branded_and_requires_a_valid_form_before_Google()
    {
        using var browser = fixture.Host.Browser();
        var html = await browser.GetStringAsync("/identity/login");
        Assert.Contains("Welcome to Sprite Scout", html);
        Assert.Contains("Continue with Google", html);
        var rejected = await browser.PostAsync("/identity/login", new FormUrlEncodedContent(new Dictionary<string, string>()));
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        Assert.Null(rejected.Headers.Location);
        var challenge = await browser.PostAsync("/identity/login", Consent(html, "signin"));
        Assert.Equal(HttpStatusCode.Redirect, challenge.StatusCode);
        Assert.Equal("accounts.google.com", challenge.Headers.Location!.Host);
        using var signedIn = fixture.Host.Browser(fixture.FirstAccount);
        Assert.Contains("You're signed in", WebUtility.HtmlDecode(await signedIn.GetStringAsync("/identity/login")));
    }

    [Fact]
    public async Task Anonymous_OAuth_login_requires_antiforgery_before_redirecting_to_Google()
    {
        using var browser = fixture.Host.Browser();
        var values = Parameters().ToDictionary(item => item.Key, item => item.Value!);
        values["decision"] = "signin";
        var response = await browser.PostAsync("/identity/connect/authorize", new FormUrlEncodedContent(values));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(response.Headers.Location);
        Assert.Contains("consent form expired", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("https://attacker.example/identity/login")]
    [InlineData("https://localhost:7082/auth/logout")]
    [InlineData("//attacker.example/identity/login")]
    public async Task Sign_in_error_page_does_not_link_to_untrusted_retry_destinations(string retry)
    {
        using var browser = fixture.Host.Browser();
        var response = await browser.GetAsync(QueryHelpers.AddQueryString("/identity/error", "returnUrl", retry));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Google sign-in", html);
        Assert.Contains("href=\"https://localhost:7082/identity/login\"", html);
        Assert.DoesNotContain(retry, html);
    }

    [Fact]
    public async Task Failed_Google_callback_returns_the_friendly_sign_in_error_page()
    {
        using var browser = fixture.Host.Browser();
        var path = QueryHelpers.AddQueryString("/identity/connect/authorize", Parameters());
        var login = await browser.GetStringAsync(path);
        var challenge = await browser.PostAsync(path, Consent(login, "signin"));
        var state = QueryHelpers.ParseQuery(challenge.Headers.Location!.Query)["state"].ToString();
        var callback = await browser.GetAsync(QueryHelpers.AddQueryString("/identity/signin-google",
            new Dictionary<string, string?> { ["error"] = "access_denied", ["state"] = state }));
        Assert.Equal(HttpStatusCode.Redirect, callback.StatusCode);
        Assert.StartsWith(Issuer + "error", callback.Headers.Location!.AbsoluteUri);
        var retry = new Uri(QueryHelpers.ParseQuery(callback.Headers.Location.Query)["returnUrl"].ToString());
        Assert.Equal("/identity/connect/authorize", retry.AbsolutePath);
        Assert.Equal("integration-state", QueryHelpers.ParseQuery(retry.Query)["state"].ToString());
        var error = await browser.GetAsync(callback.Headers.Location);
        Assert.Equal(HttpStatusCode.BadRequest, error.StatusCode);
        Assert.Contains("Try again", await error.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Consent_requires_antiforgery_and_denial_returns_no_code()
    {
        using var browser = fixture.Host.Browser(fixture.FirstAccount);
        var path = QueryHelpers.AddQueryString("/identity/connect/authorize", Parameters());
        var html = await browser.GetStringAsync(path);
        var noAntiforgery = Parameters().ToDictionary(item => item.Key, item => item.Value!);
        noAntiforgery["decision"] = "allow";
        var rejected = await browser.PostAsync(path, new FormUrlEncodedContent(noAntiforgery));
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        Assert.Contains("consent form expired", await rejected.Content.ReadAsStringAsync());
        var response = await browser.PostAsync(path, Consent(html, "deny"));
        Assert.Contains("error=access_denied", response.Headers.Location!.Query);
        Assert.DoesNotContain("code=", response.Headers.Location.Query);
    }

    [Fact]
    public async Task Authorization_code_requires_correct_verifier_and_cannot_be_replayed()
    {
        using var browser = fixture.Host.Browser(fixture.FirstAccount);
        var (code, verifier) = await AuthorizeAsync(browser);
        var bad = await RedeemAsync(browser, code, Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32)));
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
        (code, verifier) = await AuthorizeAsync(browser);
        Assert.Equal(HttpStatusCode.OK, (await RedeemAsync(browser, code, verifier)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await RedeemAsync(browser, code, verifier)).StatusCode);
    }

    [Fact]
    public async Task Two_accounts_receive_their_own_identity_through_MCP()
    {
        using var first = fixture.Host.Browser(fixture.FirstAccount);
        using var second = fixture.Host.Browser(fixture.SecondAccount);
        var firstToken = await TokenAsync(first);
        var secondToken = await TokenAsync(second);
        var firstResult = await CallAsync(fixture.Host, firstToken.GetProperty("access_token").GetString()!);
        var secondResult = await CallAsync(fixture.Host, secondToken.GetProperty("access_token").GetString()!);
        Assert.Contains("First Scout", firstResult);
        Assert.Contains(fixture.FirstAccount.ToString(), firstResult);
        Assert.Contains(fixture.FirstPublicId.ToString(), firstResult);
        Assert.DoesNotContain("Second Scout", firstResult);
        Assert.Contains("Second Scout", secondResult);
        Assert.Contains(fixture.SecondAccount.ToString(), secondResult);
        Assert.DoesNotContain("google-existing", firstResult + secondResult);
    }

    [Fact]
    public async Task Tokens_survive_host_restart_and_refresh_rotation_rejects_reuse()
    {
        using var browser = fixture.Host.Browser(fixture.FirstAccount);
        var token = await TokenAsync(browser);
        await using var restarted = new AuthTestHost(fixture.ConnectionString);
        Assert.Contains("First Scout", await CallAsync(restarted, token.GetProperty("access_token").GetString()!));
        using var client = restarted.Browser();
        var refresh = token.GetProperty("refresh_token").GetString()!;
        var values = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token", ["client_id"] = ClientId, ["refresh_token"] = refresh
        };
        var renewal = await client.PostAsync("/identity/connect/token", new FormUrlEncodedContent(values));
        Assert.Equal(HttpStatusCode.OK, renewal.StatusCode);
        var renewed = await renewal.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotEqual(refresh, renewed.GetProperty("refresh_token").GetString());
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsync("/identity/connect/token", new FormUrlEncodedContent(values))).StatusCode);
    }

    [Fact]
    public async Task Invalid_expired_wrong_audience_and_wrong_issuer_tokens_are_rejected()
    {
        using var browser = fixture.Host.Browser(fixture.FirstAccount);
        var token = (await TokenAsync(browser)).GetProperty("access_token").GetString()!;
        var reader = new JwtSecurityTokenHandler();
        var original = reader.ReadJwtToken(token);
        var credentials = fixture.Host.Services.GetRequiredService<IOptionsMonitor<OpenIddictServerOptions>>()
            .CurrentValue.SigningCredentials.First();
        var claims = original.Claims.Where(claim => claim.Type is not ("iss" or "aud" or "exp" or "nbf" or "iat"));
        var wrongAudience = reader.WriteToken(new JwtSecurityToken(Issuer, "https://other.example/mcp",
            claims, original.ValidFrom, original.ValidTo, credentials) { Header = { ["typ"] = "at+jwt" } });
        var wrongIssuer = reader.WriteToken(new JwtSecurityToken("https://other.example/", Resource, claims,
            original.ValidFrom, original.ValidTo, credentials) { Header = { ["typ"] = "at+jwt" } });
        foreach (var invalid in new[] { "invalid-token", wrongAudience, wrongIssuer })
        {
            using var client = fixture.Host.Browser();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", invalid);
            var response = await client.PostAsJsonAsync("/mcp", new { jsonrpc = "2.0", id = 1, method = "tools/list" });
            Assert.True(response.StatusCode == HttpStatusCode.Unauthorized,
                $"Token case {Array.IndexOf(new[] { "invalid-token", wrongAudience, wrongIssuer }, invalid)}: {response.StatusCode} {await response.Content.ReadAsStringAsync()}");
        }
        // Entry validation restores the authoritative expiration from PostgreSQL, not a forged JWT.
        await using var scope = fixture.Host.Services.CreateAsyncScope();
        var tokens = scope.ServiceProvider.GetRequiredService<IOpenIddictTokenManager>();
        var entry = await tokens.FindByIdAsync(original.Claims.Single(claim =>
            claim.Type == OpenIddictConstants.Claims.Private.TokenId).Value);
        Assert.NotNull(entry);
        var descriptor = new OpenIddictTokenDescriptor();
        await tokens.PopulateAsync(descriptor, entry);
        descriptor.ExpirationDate = DateTimeOffset.UtcNow.AddHours(-1);
        await tokens.UpdateAsync(entry, descriptor);
        using var expiredClient = fixture.Host.Browser();
        expiredClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        Assert.Equal(HttpStatusCode.Unauthorized, (await expiredClient.PostAsJsonAsync("/mcp",
            new { jsonrpc = "2.0", id = 1, method = "tools/list" })).StatusCode);
    }

    [Fact]
    public async Task Missing_scope_is_forbidden_and_revoked_grants_are_rejected()
    {
        using var browser = fixture.Host.Browser(fixture.SecondAccount);
        var readless = await TokenAsync(browser, "offline_access");
        using var client = fixture.Host.Browser();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", readless.GetProperty("access_token").GetString());
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync("/mcp", new { jsonrpc = "2.0", id = 1, method = "tools/list" })).StatusCode);
        var token = await TokenAsync(browser);
        await using var scope = fixture.Host.Services.CreateAsyncScope();
        var authorizations = scope.ServiceProvider.GetRequiredService<IOpenIddictAuthorizationManager>();
        var count = 0;
        await foreach (var grant in authorizations.FindBySubjectAsync(fixture.SecondAccount.ToString()))
        {
            Assert.True(await authorizations.TryRevokeAsync(grant));
            count++;
        }
        Assert.True(count > 0);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.GetProperty("access_token").GetString());
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/mcp", new { jsonrpc = "2.0", id = 1, method = "tools/list" })).StatusCode);
    }

    [Fact]
    public async Task Disabled_feature_and_wrong_hostname_do_not_expose_auth_proof()
    {
        await using var disabled = new AuthTestHost(fixture.ConnectionString, enabled: false);
        using var client = disabled.Browser();
        var response = await client.GetAsync("/identity/.well-known/openid-configuration");
        Assert.NotEqual("application/json", response.Content.Headers.ContentType?.MediaType);
        using var enabled = fixture.Host.Browser();
        enabled.DefaultRequestHeaders.Host = "other.example";
        Assert.Equal(HttpStatusCode.NotFound, (await enabled.GetAsync("/identity/.well-known/openid-configuration")).StatusCode);
    }

    private static Dictionary<string, string?> Parameters(string? verifier = null, string? scopes = null, string clientId = ClientId, string callback = Callback) => new()
    {
        ["client_id"] = clientId, ["response_type"] = "code",
        ["redirect_uri"] = callback, ["scope"] = scopes ?? "account:read offline_access",
        ["state"] = "integration-state", ["resource"] = Resource,
        ["code_challenge_method"] = "S256",
        ["code_challenge"] = Base64UrlEncoder.Encode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier ?? new string('a', 43))))
    };

    private static FormUrlEncodedContent Consent(string html, string decision)
    {
        var fields = Regex.Matches(html, "<input type=\"hidden\" name=\"([^\"]+)\" value=\"([^\"]*)\"")
            .ToDictionary(match => WebUtility.HtmlDecode(match.Groups[1].Value),
                match => WebUtility.HtmlDecode(match.Groups[2].Value));
        fields["decision"] = decision;
        return new FormUrlEncodedContent(fields);
    }

    private static async Task<(string Code, string Verifier)> AuthorizeAsync(HttpClient browser, string? scopes = null, string clientId = ClientId, string callback = Callback)
    {
        var verifier = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));
        var path = QueryHelpers.AddQueryString("/identity/connect/authorize", Parameters(verifier, scopes, clientId, callback));
        var response = await browser.GetAsync(path);
        Assert.True(response.StatusCode == HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var html = await response.Content.ReadAsStringAsync();
        var consent = await browser.PostAsync(path, Consent(html, "allow"));
        Assert.True(consent.StatusCode == HttpStatusCode.Redirect, await consent.Content.ReadAsStringAsync());
        var query = QueryHelpers.ParseQuery(consent.Headers.Location!.Query);
        Assert.Equal(Issuer, query["iss"].ToString());
        Assert.Equal("integration-state", query["state"].ToString());
        Assert.True(query.ContainsKey("code"), consent.Headers.Location.ToString());
        return (query["code"].ToString(), verifier);
    }

    private static Task<HttpResponseMessage> RedeemAsync(HttpClient browser, string code, string verifier, string clientId = ClientId, string callback = Callback) =>
        browser.PostAsync("/identity/connect/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = clientId, ["grant_type"] = "authorization_code",
            ["code"] = code, ["redirect_uri"] = callback, ["code_verifier"] = verifier, ["resource"] = Resource
        }));

    private static async Task<JsonElement> TokenAsync(HttpClient browser, string? scopes = null)
    {
        var (code, verifier) = await AuthorizeAsync(browser, scopes);
        var response = await RedeemAsync(browser, code, verifier);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<string> CallAsync(AuthTestHost host, string token)
    {
        using var client = host.Browser();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("MCP-Protocol-Version", "2025-11-25");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        client.DefaultRequestHeaders.Accept.ParseAdd("text/event-stream");
        var response = await client.PostAsJsonAsync("/mcp", new
        {
            jsonrpc = "2.0", id = 1, method = "tools/call",
            @params = new { name = "who_am_i", arguments = new { } }
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var text = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("\"isError\":true", text);
        Assert.DoesNotContain("\"error\":", text);
        return text;
    }
}
