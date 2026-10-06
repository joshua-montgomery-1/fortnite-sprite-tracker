# Phase 1 verification record

Verified locally on 2026-10-06, Windows, .NET 10, PostgreSQL 17.6.

| Check | Result |
| --- | --- |
| Solution Release build | Passed, 0 warnings / 0 errors |
| Existing console test suite | Passed (`Sprite catalog route tests passed.`) |
| Auth integration suite | 21 passed, 0 failed, 0 skipped |
| Server Release publish | Passed |
| Website/auth model snapshots | No pending model changes in PostgreSQL fixture |
| Plugin, MCP, and marketplace JSON | Parsed successfully |

## Integration coverage

- Create a reserved `auth` schema containing a platform marker, revoke public schema privileges, and verify central migrations use `sprite_scout_auth` without adding to or modifying the reserved namespace. This simulates the namespace collision locally; it is not a live Supabase project test.

- Populate website users/progress, return the test database to its pre-Phase-1 schema, then start the application to upgrade and backfill automatically.
- Re-run backfill; preserve website primary IDs, public IDs, display names, sprite foreign keys, ownership/mastery; reuse the same Google mapping.
- Link new central Google accounts to website profiles without overwriting existing names.
- Read issuer, PKCE S256, RFC 9207 support, and protected-resource discovery; require bearer auth.
- Reject missing/plain PKCE, unknown clients/scopes, missing/wrong resources, external callbacks, and incorrect loopback host/path.
- Accept two different native callback ports; return matching issuer and state on successful authorization.
- Challenge Google using the distinct central callback while the legacy website login retains its callback.
- Require a valid consent antiforgery token; declining returns no authorization code.
- Reject wrong code verifiers and replayed authorization codes.
- Exchange real OAuth codes for tokens and call `who_am_i` over HTTP MCP for two independent accounts.
- Reject invalid tokens, tokens signed with the server key but targeting another audience/issuer, expired token records, and missing scope.
- Revoke grants through the OpenIddict manager and confirm bearer access is denied.
- Create a new application host against the same database/certificate store; previously issued tokens still work. Refresh succeeds, rotates its token, and reuse fails.
- With the feature disabled, auth discovery is unavailable; reject requests with an unexpected hostname.

Expiry testing updates the authoritative OpenIddict token record: local token-entry validation deliberately restores creation/expiration metadata from PostgreSQL. Simply altering a JWT's expiry while leaving its valid stored record unchanged would not test this mode accurately.

## Environment and limits

Docker did not become available locally. The suite used a standalone, loopback-only PostgreSQL process on a dedicated port. Each run created and dropped its own uniquely named database. CI uses the suite's Testcontainers PostgreSQL 17 fallback on Ubuntu.

The test host substitutes the central browser session to exercise consent and OAuth without real Google credentials. Production application code contains no test header/login mechanism. Audience, scope, signatures, token/grant records, PostgreSQL, and MCP dispatch are real.

**Not exercised:** real Google browser sign-in and invoking the installed plugin through Codex. No server Google credentials were configured locally. Plugin JSON is checked, but installation/OAuth interoperability still needs the manual checklist in [local setup](central-auth-phase-1.md). The automated restart check creates a fresh application host in the test process; it does not claim a deployed-container restart.

No production hostname, deployment, auth cutover, or collection-edit tools are included.
