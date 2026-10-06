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
| `SpriteScoutAuth:Certificates:SigningPath` | Absolute path to a PFX containing the signing certificate and private key |
| `SpriteScoutAuth:Certificates:SigningPassword` | PFX password, when required |
| `SpriteScoutAuth:Certificates:EncryptionPath` | Absolute path to a separate PFX containing the encryption certificate and private key |
| `SpriteScoutAuth:Certificates:EncryptionPassword` | PFX password, when required |
| `SpriteScoutAuth:Clients` | Public OAuth clients and their exact registered callbacks |
| `Authentication:Google:ClientId` / `ClientSecret` | Google OAuth provider credentials |
| `ConnectionStrings:sprite-tracker` | Existing website database connection |

Azure environment variables use double underscores instead of colons, for example `SpriteScoutAuth__Certificates__SigningPath`. Store certificate files outside the repository on a protected persistent mount, and provide passwords through secret configuration such as Azure Key Vault. Provision valid RSA certificates with private keys for signing and encryption. Retain the same files across restarts and replicas; development certificate generation is never used in production. Renew them before expiry and coordinate rotation with existing tokens. These certificates are for OAuth tokens; the web host still needs HTTPS separately.

The local Codex registration is defined only in `appsettings.Development.json`. Register production clients separately through `SpriteScoutAuth:Clients`, with their own IDs and callbacks, `ApplicationType` (`native` or `web`), and a `DisplayName`. Public clients use authorization code with mandatory S256 PKCE, without client secrets. Native loopback callbacks permit variable ports; web callbacks require HTTPS. Client self-registration is not implemented.

Register both Google callback URLs using the actual deployed website origin and issuer: `<website-origin>/signin-google` and `<issuer>signin-google`. Login/error UI pages remain at `/auth/login`, `/auth/error`, `/identity/login`, and `/identity/error`; a custom issuer path redirects its login/error aliases to the UI pages. OAuth protocol endpoints remain relative to the configured issuer.

Ensure the host routes the issuer and MCP hostnames to this application. Behind a TLS-terminating proxy, set `ReverseProxy:KnownProxies` to the trusted immediate proxy IP addresses (`ReverseProxy__KnownProxies__0`, etc. in Azure). The app processes `X-Forwarded-Proto` only from trusted proxies, before HTTPS redirects and authentication. Configure the proxy to preserve the public Host header and send the external HTTPS scheme. Loopback proxies are trusted by the framework defaults. This change does not provision Azure hostnames, certificates, routes, or Key Vault secrets.

The production account migration has been completed. New databases use the normal EF migrations; website profiles are linked on Google sign-in.

### Azure Container Apps setup

The release Bicep sets `https://spritescout.com/identity/` as the issuer and `https://spritescout.com/mcp` as the resource using the configured apex domain. It mounts an existing Azure Files share at `/auth-certificates` with read-only access and provides the PFX passwords through Container App secrets. These files persist across revisions and replicas. The release does not generate or replace certificates.

On Windows with PowerShell 7, install Azure CLI and GitHub CLI, then sign in with `az login` and `gh auth login`. Run the setup script from the repository root:

```powershell
./infra/Initialize-AuthCertificates.ps1 `
  -SubscriptionId '<production-subscription-id>' `
  -StorageAccountName '<globally-unique-lowercase-storage-name>'
```

The script creates a Standard LRS storage account and a one-GiB-quota `auth-certificates` share in the existing production resource group. Azure Files incurs storage charges. It generates separate RSA 3072 signing/encryption certificates valid for two years, uploads their encrypted PFX files, and sets the following in the repository's GitHub **production** environment:

| Kind | Name |
| --- | --- |
| Variable | `AUTH_STORAGE_ACCOUNT` |
| Secret | `AUTH_STORAGE_KEY` |
| Secret | `AUTH_SIGNING_PASSWORD` |
| Secret | `AUTH_ENCRYPTION_PASSWORD` |

The signed-in Azure user needs permission to create storage accounts and read their keys in that resource group. The GitHub user needs access to update production environment secrets/variables. The release identity continues to use its existing resource-group deployment permissions.

Local encrypted PFX/password backups are restricted to your Windows user and SYSTEM under `%LOCALAPPDATA%/SpriteScout/AuthCertificates/<subscription>/<storage-account>/`. The password backups use Windows DPAPI and are decryptable only by the same Windows user. Preserve a secure, recoverable backup of these credentials. Reruns reuse the local files and compare remote certificate thumbprints; the script refuses missing backups, mismatched remote certificates, or expired credentials instead of silently rotating keys. It does not change the running application or register Google callbacks.

Set the optional GitHub production variable `AUTH_CLIENTS` to a JSON array of explicitly allowed clients before deployment. An empty array permits no new client registrations; removing an entry does not delete an existing database registration. For a production Codex connection:

```json
[{"ClientId":"sprite-scout-codex","DisplayName":"Codex — Sprite Scout","ApplicationType":"native","RedirectUris":["http://127.0.0.1/callback"]}]
```

Register `https://spritescout.com/identity/signin-google` in the same Google OAuth client used by the website, retaining `https://spritescout.com/signin-google`. After review and merge, the release workflow supplies the new secrets and mounts the share before starting the application. Check `https://spritescout.com/identity/.well-known/openid-configuration`, then complete a Google sign-in and an authenticated MCP call. Certificate provisioning, Google callback registration, and a production sign-in cannot be verified by a local build.

## UI structure

Login/error pages use normal Blazor routes with `ExcludeFromInteractiveRouting`, static server rendering, and `AuthLayout`. The application shell owns document markup and theme setup. OAuth consent and failed form posts render through that same shell and layout using `IAuthPageRenderer`. Auth forms submit normal HTTP POSTs with antiforgery tokens. Sprite artwork comes from the database through the host's five-minute cache.
