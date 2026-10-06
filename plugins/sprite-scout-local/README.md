# Sprite Scout local plugin

Local OAuth/MCP plugin. Start the HTTPS server and configure Google before installing. Configure the server project with `ConnectionStrings:sprite-tracker`, `Authentication:Google:ClientId`, `Authentication:Google:ClientSecret` using user secrets. Register Google callbacks `https://localhost:7082/signin-google` and `https://localhost:7082/identity/signin-google`. Auth is always registered. Local development uses the default HTTPS loopback issuer. The production account migration is complete; auth startup does not backfill website users. Local databases may apply schema migrations normally and link website profiles on auth sign-in. Use a fresh development database after this unreleased migration cleanup.

Server: `https://localhost:7082/mcp`. Public client: `sprite-scout-mcp`. No client secret. A native MCP client chooses a loopback port for `http://127.0.0.1/callback`. Tools include `who_am_i`, `list_seasons`, `list_sprites`, `list_sprites_by_season`, `list_collection`, and `update_collection`. All tools require a free account. No client configuration change is needed to discover the new tools after restarting the server.

The repository marketplace exposes this plugin without automatically enabling it. Install from **Sprite Scout Development** in the plugin directory. This package is for local clients on the server's computer; it is not a cloud ChatGPT connector.

## Other MCP clients

The OAuth server and MCP tools are independent of Codex. Register public OAuth clients in `SpriteScoutAuth:Clients`; each entry requires `ClientId`, `DisplayName`, `ApplicationType` (`native` or `web`), and `RedirectUris`. The Codex entry lives in the website's `appsettings.Development.json`, rather than C# code. Configure the other client with its own ID and matching callback; it must support authorization code with S256 PKCE and request the MCP resource and `account:read` scope. Request `collection:read` to list personal collection progress and `collection:write` to change ownership/mastery. `offline_access` is optional. Existing connections must reconnect to approve additional permissions; refreshing an old token does not add scopes.

Use `list_seasons` to resolve a season ID and `list_sprites` / `list_sprites_by_season` to resolve an exact Sprite variant ID. Sprite and collection listings accept `offset` and `limit` (default 50, maximum 100); follow `hasMore` to retrieve additional pages. Sprite listings support family/style name search and include upcoming variants with release status. Collection updates reject variants that have not been released in any started season. Ownership and mastery are shared across seasons for the same variant.

`update_collection` accepts an `updates` array of 1–1,000 items with unique `spriteVariantId` values and optional `isOwned` / `isMastered` flags. Omitted flags preserve existing state; mastered implies owned, and unowned clears mastery. Each item must provide at least one flag. Contradictory unowned/mastered flags, duplicate IDs, or unknown/unreleased variants reject the entire batch. Updates save atomically with EF Core and return resulting progress in input order. A one-item batch handles a single Sprite. All changes apply only to the signed-in user. Restart the server and refresh client tool discovery to replace the old individual actions.

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

This plugin targets localhost. Cloud MCP clients require a reachable production deployment and their own supported registration method and callback. See the auth module README for production settings.

Consent groups **Collection** Read/Write, **Profile & account** Read/Write, and **Connection** Keep me signed in. Optional permissions are checked by default when requested. `account:read` allows `get_profile` as well as `who_am_i`; `account:write` allows `update_profile` to edit the display name and Epic Games display name. Omit a name to preserve it, or use `clearEpicDisplayName: true` to remove the Epic name. Profile privacy and theme settings remain unchanged. Reconnect to approve the new profile write permission; existing tokens do not gain it automatically.
