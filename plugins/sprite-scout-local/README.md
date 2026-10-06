# Sprite Scout local plugin

Development-only OAuth/MCP proof. Start the HTTPS server and configure Google before installing. Configure the server project with `ConnectionStrings:sprite-tracker`, `Authentication:Google:ClientId`, `Authentication:Google:ClientSecret`, and `SpriteScoutAuth:Enabled=true` using user secrets. Register Google callbacks `https://localhost:7082/signin-google` and `https://localhost:7082/identity/signin-google`. Auth requires Development and an HTTPS loopback issuer. Existing accounts are imported using `scripts/migrate-auth-accounts.sql` before deployment; auth startup does not backfill website users. Local databases may apply schema migrations normally and link website profiles on auth sign-in. Use a fresh development database after this unreleased migration cleanup.

Server: `https://localhost:7082/mcp`. Public client: `sprite-scout-codex`. No client secret. Codex chooses a loopback port for `http://127.0.0.1/callback`. The only tool is `who_am_i`; it returns your account ID, website public profile ID, and display name.

The repository marketplace exposes this plugin without automatically enabling it. Install from **Sprite Scout Development** in the plugin directory. This package is for local clients on the server's computer; it is not a cloud ChatGPT connector.

## Production database preparation

Run the one-time script before deploying this release. Use a database role that owns the new tables, or grant the application's database role the required access afterward. For psql, pass `-v ON_ERROR_STOP=1 -f scripts/migrate-auth-accounts.sql` along with your normal production connection options. The script runs in one transaction and reports website and linked-user counts after committing. The schema portion is intended to run once; only the account import block is repeatable.

## Other MCP clients

The OAuth server and MCP tools are independent of Codex. Register public OAuth clients in `SpriteScoutAuth:Clients`; each entry requires `ClientId`, `DisplayName`, `ApplicationType` (`native` or `web`), and `RedirectUris`. The Codex entry lives in the website's `appsettings.Development.json`, rather than C# code. Configure the other client with its own ID and matching callback; it must support authorization code with S256 PKCE and request the MCP resource and `account:read` scope. `offline_access` is optional.

Example additional registration (merge into the `Clients` array):

```json
{
  "ClientId": "sprite-scout-other-desktop",
  "DisplayName": "My MCP client",
  "ApplicationType": "native",
  "RedirectUris": ["http://127.0.0.1/oauth/callback"]
}
```

Native loopback callbacks allow variable ports while retaining the registered host and path, following [RFC 8252](https://www.rfc-editor.org/rfc/rfc8252.html#section-7.3). Web callbacks use HTTPS and match the registered URL. These are public clients with no client secret. Client configuration is trusted server configuration; clients cannot register themselves dynamically. Removing a configured entry does not delete its existing database registration or revoke grants.

The Phase 1 issuer remains Development-only on localhost. Cloud MCP clients require a reachable production deployment in a later phase, plus their own supported registration method and callback.
