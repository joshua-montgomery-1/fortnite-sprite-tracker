# Phase 1: central accounts and local OAuth proof

## Architecture and boundaries

The existing ASP.NET host loads `SpriteScout.Auth` as a separate library. It owns its EF context, migrations, `sprite_scout_auth` schema, external identities, OAuth clients, grants, tokens, and central browser cookie. The host owns website profiles and progress; `ICentralAccountObserver` is the integration boundary. Both contexts use the existing PostgreSQL database. There is no new process, container, port, or production hostname in this phase.

Central account IDs are UUIDs. Google identities have a unique `(provider, issuer, subject)` key. Email is not an identity key or an automatic linking rule. A nullable, unique central UUID reference is added to existing website users without replacing their primary/public IDs or collection relationships. Startup backfill persists each identity before attaching the profile, making interruption/repetition safe. Website Google sign-in and its cookie remain operational; new website users are linked on the next enabled startup or central Google sign-in.

Central auth is disabled by default and fails startup if enabled outside Development. Enabling it requires HTTPS loopback issuer/resource URLs. Production certificate handling, host routing, issuer changes, account/provider management, logout/revocation UI, operational cleanup, abuse limits, and website auth cutover are later work. A hostname change changes the issuer and requires reconnecting clients.

## Local setup

Requirements: .NET 10, PostgreSQL, a Google OAuth web application, and a local Codex client. Use a disposable local database or a development copy, not production data.

1. Start local PostgreSQL (for example a PostgreSQL 17 container) and provide its connection string to the **server project**. This phase uses direct server launch; Aspire's dynamically assigned ports do not match this plugin.
2. Trust the ASP.NET HTTPS development certificate:
   `dotnet dev-certs https --trust`.
3. Configure server user secrets. Replace placeholders locally; never commit credentials:

```powershell
dotnet user-secrets set "ConnectionStrings:sprite-tracker" "Host=localhost;Port=5432;Database=sprite_scout_dev;Username=postgres;Password=<local-password>" --project src/FortniteSpriteTracker
dotnet user-secrets set "Authentication:Google:ClientId" "<google-client-id>" --project src/FortniteSpriteTracker
dotnet user-secrets set "Authentication:Google:ClientSecret" "<google-client-secret>" --project src/FortniteSpriteTracker
dotnet user-secrets set "CentralAuth:Enabled" "true" --project src/FortniteSpriteTracker
```

4. In the Google Cloud OAuth client's authorized redirect URIs, add **`https://localhost:7082/identity/signin-google`**. Keep the existing website callback **`https://localhost:7082/signin-google`**. Register test users if Google's consent screen is in testing mode.
5. Start the server:
   `dotnet run --project src/FortniteSpriteTracker --launch-profile https`.

Startup applies website migrations, then auth migrations, pre-registers the Codex client, and backfills existing Google users. The database role needs permission to create the Sprite Scout schema and tables.

### Supabase and the schema name

Central tables and their migration history use **`sprite_scout_auth`**. Supabase owns its existing `auth` schema; do not grant the application additional privileges on it or rename it. Keep `sprite_scout_auth` outside the Supabase Data API's exposed schemas. The server accesses it directly through Npgsql.

If the earlier Phase 1 version failed with `42501: permission denied for schema auth` before creating its central tables, update the code and restart. No managed-schema cleanup is needed.

Defaults:

| Setting | Value |
| --- | --- |
| CentralAuth:Issuer | https://localhost:7082/identity/ |
| CentralAuth:Resource | https://localhost:7082/mcp |
| OAuth public client | sprite-scout-codex |
| Allowed callback | http://127.0.0.1/callback (variable loopback port) |
| Read scope | account:read |
| Access token lifetime | 10 minutes |
| Maximum connection lifetime | 30 days |
| OAuth grant types | authorization_code, refresh_token |

The public client has **no client secret**. Authorization codes require S256 PKCE; plain/missing PKCE is rejected. Only the literal loopback IP and callback path are allowed; OpenIddict's native-client matching permits a dynamically chosen port. Authorization responses return the exact issuer in `iss` and discovery advertises support for it. Tokens must target the MCP resource and carry `account:read`; website cookies cannot authorize MCP.

OpenIddict development signing/encryption certificates persist in the current OS user's certificate store, independently of the HTTPS certificate. Restart under the same OS user and preserve the PostgreSQL database to retain tokens and signing keys. The website's existing data-protection keys persist in its database. These development credentials are not the production deployment strategy.

## Connect Codex

The repository marketplace at `.agents/plugins/marketplace.json` exposes `plugins/sprite-scout-local`. Open this repository in the desktop client and install **sprite-scout-local** from **Sprite Scout Development**. If the marketplace is not visible, add the repository root with `codex plugin marketplace add <absolute-repository-path>`, then refresh/restart the client. CLI installation is `codex plugin add sprite-scout-local@sprite-scout-development`.

The package supplies the HTTPS server URL, public client ID, and `http://127.0.0.1/callback`. Codex chooses the callback port. Connect with Google in the browser and approve the account-read consent screen. Ask Codex to call `who_am_i`; it should return the account UUID, website public profile UUID, and display name. The tool cannot edit collection progress.

If configuring MCP directly instead of installing the plugin, use:

```toml
[mcp_servers.sprite-scout-local]
url = "https://localhost:7082/mcp"
oauth.client_id = "sprite-scout-codex"
oauth.callback_url = "http://127.0.0.1/callback"
```

Check discovery at `/identity/.well-known/openid-configuration` or `/.well-known/oauth-authorization-server/identity`, and resource metadata at `/.well-known/oauth-protected-resource/mcp`. A request to MCP without a bearer token returns 401 with resource metadata in `WWW-Authenticate`. Grant/token records are validated on every request, so revocation is enforced.

Manual browser checklist: connect an existing Google account and compare its profile/progress; repeat consent; connect a second account and compare IDs; restart the server and call the tool again; confirm existing website sign-in still works. Do not share authorization codes, access/refresh tokens, or callback URLs in logs/screenshots.

## Automated verification

```powershell
dotnet build FortniteSpriteTracker.slnx -c Release
dotnet run --project tests/FortniteSpriteTracker.Tests -c Release --no-build
dotnet test tests/SpriteScout.Auth.Tests/SpriteScout.Auth.Tests.csproj -c Release --no-build
dotnet publish src/FortniteSpriteTracker/FortniteSpriteTracker.csproj -c Release --no-build
```

Auth tests normally start disposable PostgreSQL 17 through Testcontainers (Docker required). Alternatively set `SPRITESCOUT_TEST_DATABASE` to a local PostgreSQL admin connection; tests create/drop a uniquely named database, leaving other databases untouched. This role needs CREATEDB.

Tests substitute only the central browser session inside the test host. They exercise real PostgreSQL migrations/backfill, OpenIddict request validation, consent antiforgery, code exchange, refresh tokens, discovery, and HTTP MCP calls. They do not substitute bearer validation or expose a test login route in the application.

See [recorded verification](central-auth-phase-1-verification.md).

## References

- [OpenIddict ASP.NET integration](https://documentation.openiddict.com/integrations/aspnet-core.html)
- [OpenIddict encryption and signing credentials](https://documentation.openiddict.com/configuration/encryption-and-signing-credentials.html)
- [Codex MCP authentication](https://developers.openai.com/codex/mcp)
- [Plugin packaging and local marketplaces](https://developers.openai.com/plugins/build/plugins)
