# Sprite Scout authentication

Auth and the MCP endpoint are always registered; no enable flag is required. Website Google authentication remains operational alongside the separate OAuth session.

## Development

Configure the database connection and `Authentication:Google:ClientId` / `ClientSecret` in the existing server user-secrets store. Default URLs are `https://localhost:7082/identity/` and `https://localhost:7082/mcp`. OpenIddict persists development certificates in the current user's certificate store. Google callbacks are `/signin-google` for the website and `/identity/signin-google` for OAuth.

## Production configuration

Set these configuration values before deploying. URLs must be publicly reachable HTTPS URLs; the issuer must include a path and end with `/`.

| Setting | Purpose |
| --- | --- |
| `SpriteScoutAuth:Issuer` | Canonical OAuth issuer, for example `https://auth.example.com/identity/` |
| `SpriteScoutAuth:Resource` | Canonical MCP endpoint, for example `https://example.com/mcp` |
| `SpriteScoutAuth:Certificates:SigningBase64` | Base64-encoded signing PFX, containing its private key; supply through secrets |
| `SpriteScoutAuth:Certificates:SigningPassword` | PFX password, when required |
| `SpriteScoutAuth:Certificates:EncryptionBase64` | Base64-encoded encryption PFX, containing its private key; supply through secrets |
| `SpriteScoutAuth:Certificates:EncryptionPassword` | PFX password, when required |
| `SpriteScoutAuth:Clients` | Public OAuth clients and their exact registered callbacks |
| `Authentication:Google:ClientId` / `ClientSecret` | Google OAuth provider credentials |
| `ConnectionStrings:sprite-tracker` | Existing website database connection |

Azure environment variables use double underscores instead of colons, for example `SpriteScoutAuth__Certificates__SigningBase64`. Base64 is encoding, not encryption: treat the PFX values and passwords as secrets. The app decodes PFX bytes and loads keys with `EphemeralKeySet`, without writing certificate files or persisting private keys to the operating system key store. File-based hosting remains supported through `SigningPath` / `EncryptionPath`; configure exactly one source per certificate. Retain the same credentials across deployments, restarts, and replicas; development certificate generation is never used in production. Renew before expiry and coordinate rotation with existing tokens. These certificates are for OAuth tokens and are separate from HTTPS certificates.

The local Codex registration is defined only in `appsettings.Development.json`. Register production clients separately through `SpriteScoutAuth:Clients`, with their own IDs and callbacks, `ApplicationType` (`native` or `web`), and a `DisplayName`. Public clients use authorization code with mandatory S256 PKCE, without client secrets. Native loopback callbacks permit variable ports; web callbacks require HTTPS. Client self-registration is not implemented.

Register both Google callback URLs using the actual deployed website origin and issuer: `<website-origin>/signin-google` and `<issuer>signin-google`. Login/error UI pages remain at `/auth/login`, `/auth/error`, `/identity/login`, and `/identity/error`; a custom issuer path redirects its login/error aliases to the UI pages. OAuth protocol endpoints remain relative to the configured issuer.

Ensure the host routes the issuer and MCP hostnames to this application. Behind a TLS-terminating proxy, set `ReverseProxy:KnownProxies` to the trusted immediate proxy IP addresses (`ReverseProxy__KnownProxies__0`, etc. in Azure). The app processes `X-Forwarded-Proto` only from trusted proxies, before HTTPS redirects and authentication. Configure the proxy to preserve the public Host header and send the external HTTPS scheme. Loopback proxies are trusted by the framework defaults. This change does not provision Azure hostnames, certificates, routes, or Key Vault secrets.

The production account migration has been completed. New databases use the normal EF migrations; website profiles are linked on Google sign-in.

### Azure Container Apps setup

The release Bicep sets `https://spritescout.com/identity/` as the issuer and `https://spritescout.com/mcp` as the resource using the configured apex domain. GitHub production secrets supply the two Base64 PFX values and passwords to Azure Container App secrets. The application reads them through secret environment references. No storage account, file share, or volume mount is required. The release reuses credentials; it does not generate new keys.

On Windows with PowerShell 7, install GitHub CLI and sign in with `gh auth login`. Run the setup script from the repository root:

```powershell
./infra/Initialize-AuthCertificates.ps1
```

The script generates separate RSA 3072 signing/encryption certificates valid for two years, then saves these in the repository's GitHub **production** environment:

| Kind | Name |
| --- | --- |
| Secret | `AUTH_SIGNING_PFX` |
| Secret | `AUTH_ENCRYPTION_PFX` |
| Secret | `AUTH_SIGNING_PASSWORD` |
| Secret | `AUTH_ENCRYPTION_PASSWORD` |
| Variable | `AUTH_SIGNING_THUMBPRINT` (public identifier for repeat-run checks) |
| Variable | `AUTH_ENCRYPTION_THUMBPRINT` (public identifier for repeat-run checks) |

The GitHub user needs access to update production environment secrets/variables. The release identity continues to use its existing Azure deployment permissions. Certificates never enter the repository, container image, workflow artifacts, or logs. Workflow parameter files containing secrets exist temporarily on the runner and are removed after deployment.

Local encrypted PFX/password backups are restricted to your Windows user and SYSTEM under `%LOCALAPPDATA%/SpriteScout/AuthCertificates/<owner>/<repository>/`. The password backups use Windows DPAPI and are decryptable only by the same Windows user. Preserve a secure, recoverable backup of these credentials. Reruns reuse local files and check the recorded GitHub thumbprints; the script refuses missing backups for existing secrets, mismatched thumbprints, or expired credentials instead of silently rotating keys. It does not change the running application or register Google callbacks. Each Base64 value must fit GitHub's 48 KB secret limit, which the script checks.

Set the optional GitHub production variable `AUTH_CLIENTS` to a JSON array of explicitly allowed clients before deployment. An empty array permits no new client registrations; removing an entry does not delete an existing database registration. For a production Codex connection:

```json
[{"ClientId":"sprite-scout-codex","DisplayName":"Codex — Sprite Scout","ApplicationType":"native","RedirectUris":["http://127.0.0.1/callback"]}]
```

Register `https://spritescout.com/identity/signin-google` in the same Google OAuth client used by the website, retaining `https://spritescout.com/signin-google`. After review and merge, the release workflow supplies the secrets before starting the application. Check `https://spritescout.com/identity/.well-known/openid-configuration`, then complete a Google sign-in and an authenticated MCP call. Google callback registration and a production sign-in cannot be verified by a local build.

## MCP tools and permissions

Every MCP connection requires a free account and `account:read`; authorization requests omitting account access are rejected. Clients may request `account:write`, `collection:read`, `collection:write`, and `offline_access`. Consent groups permissions under **Collection** (Read/Write), **Profile & account** (required Read/optional Write), and **Connection** (Keep me signed in). It shows only requested permissions, with required account access fixed on and every optional permission checked by default. Users can uncheck any optional permission. The server grants only the approved subset, rejects unrequested/unknown selections, and uses the approved scopes for the authorization grant and tokens. Configured public clients are permitted these scopes when auth startup registers them. Previously issued tokens retain their original scopes: reconnect to approve additional access.

The default HTTP authentication challenge requests `account:read account:write collection:read collection:write offline_access`, so clients following MCP scope selection offer all optional permissions during initial sign-in. Clients intentionally requesting fewer scopes or users deselecting permissions can use only the corresponding tools. Deselecting "Keep me signed in" prevents issuance of a refresh token. Old connections must reconnect and approve the new scope set; a tool error does not automatically upgrade an existing token.

| Tool | Additional permission | Behavior |
| --- | --- | --- |
| `who_am_i` | None | Connected account and website profile identity |
| `get_profile` | None | Display/Epic names and profile settings |
| `update_profile` | `account:write` | Edit display name and Epic Games display name |
| `list_seasons` | None | Season IDs, dates and catalog availability |
| `list_sprites` | None | Searchable, paginated variants across seasons |
| `list_sprites_by_season` | None | Searchable, paginated variants for one season |
| `list_collection` | `collection:read` | Paginated personal progress, optionally filtered by season |
| `update_collection` | `collection:write` | Apply 1–1,000 ownership/mastery updates atomically |

Collection updates require an exact `spriteVariantId` from catalog tools, never a family ID or target user ID. The OAuth subject resolves the website user through `AccountId`. Unknown/unreleased variants are rejected using the website's availability rules. A variant's collection state is shared across seasons. Listings default to 50 entries and permit up to 100 per page; use `offset` / `limit` and `hasMore` for pagination. Mutations are idempotent state setters, marked as write/destructive tools for client confirmation handling. The normal single-variant website endpoint shares the same EF Core update service. Only requested flags are marked modified, preserving mastery for ownership-only writes. Concurrent inserts/removals retry up to three attempts using fresh EF entity state; no raw SQL is used for collection updates.

Profile updates resolve the signed-in user's account and use EF Core. `displayName` accepts 1–80 characters and `epicDisplayName` accepts 3–16 characters, after trimming. Omitted names stay unchanged. Set `clearEpicDisplayName` to remove the Epic name; it cannot be combined with a new Epic name. Epic-name normalization is updated alongside the name. Account identity, profile privacy, and theme settings are preserved.

## UI structure

Login/error pages use normal Blazor routes with `ExcludeFromInteractiveRouting`, static server rendering, and `AuthLayout`. The application shell owns document markup and theme setup. OAuth consent and failed form posts render through that same shell and layout using `IAuthPageRenderer`. Auth forms submit normal HTTP POSTs with antiforgery tokens. Sprite artwork comes from the database through the host's five-minute cache.

### Batch collection updates

`update_collection` replaces the four individual collection mutation tools. Pass `updates` containing 1–1,000 items with unique `spriteVariantId` values and at least one of `isOwned` or `isMastered` per item. Omitted/null flags preserve existing state. Mastery implies ownership, unmastering preserves ownership, and setting ownership false clears mastery. Combining `isOwned: false` with `isMastered: true` is rejected. Unknown/unreleased variants, duplicate IDs, and invalid items reject the entire batch before saving. EF Core saves all changes in one transaction and retries concurrent insert/removal conflicts up to three attempts. Results contain `items` in input order. Use a one-item batch for a single Sprite; split larger requests into separate batches, each with its own transaction.

```json
{
  "updates": [
    { "spriteVariantId": 1, "isMastered": true },
    { "spriteVariantId": 2, "isOwned": false },
    { "spriteVariantId": 3, "isMastered": false }
  ]
}
```

IDs above are examples; resolve real IDs with the catalog tools. The website collection endpoints remain available.
