# Sprite Scout

A Fortnite Sprite collection tracker built as a .NET 10 Blazor Web App with server prerendering and WebAssembly interactivity.

## What it does

- Tracks Sprite families and variants across multiple Fortnite seasons.
- Records separate `Owned` and `Mastered` progress for every Sprite variant, with season filters and styles such as Normal, Gold, Gummy, Galaxy, Holofoil, Gem, Cube, and Quack.
- Provides checklist and collectible Field Guide views, including season-aware progress totals.
- Includes a Lobby Hack cheat-code tracker organized around rewards such as Cosmetics, Sprites, Sprite Dust, Gizmos & Supplies, XP, and Lobby Effects.
- Lets users mark one-time codes as used while identifying repeatable codes and their rewards, which can include Sprites, Sprite Dust, cosmetics, XP, supplies, and temporary lobby effects.
- Keeps unauthenticated progress in browser storage and syncs signed-in Sprite and cheat-code progress to PostgreSQL.
- Provides public player profiles, collection comparisons, permanent profile links, private tracked-player lists, and season-filtered player totals.
- Supports profile privacy, Epic display names, light/dark/system theme preferences, responsive layouts, and populated or blank landscape PDF checklist exports.

## Run locally

The complete development stack is orchestrated by .NET Aspire and requires a Docker-compatible container runtime for PostgreSQL.

```powershell
dotnet tool restore
dotnet user-secrets set "Authentication:Google:ClientId" "your-client-id" --project src/FortniteSpriteTracker
dotnet user-secrets set "Authentication:Google:ClientSecret" "your-client-secret" --project src/FortniteSpriteTracker
dotnet run --project src/FortniteSpriteTracker.AppHost
```

Aspire starts PostgreSQL, waits for it to become healthy, injects its connection string into the server, applies EF Core migrations, and starts the hosted Blazor application. Configure Google OAuth with the server's `/signin-google` callback URL. Google credentials are optional for startup but required to sign in.

The Sprite catalog is intentionally not populated during normal startup. Seed or reconcile the committed catalog explicitly after configuring the target connection string:

```powershell
dotnet run --project src/FortniteSpriteTracker -- --seed-catalog
```

The command applies pending migrations, reconciles the committed multi-season catalog, variant styles, and Lobby Hack code definitions, reports inserted and updated records, and exits without starting the web server. Running it repeatedly is safe.

The committed seed establishes Chapter 7 Season 3 and Chapter 7 Season 4 metadata. Sprite families and variants are associated with individual seasons, so new seasons can be added without changing the meaning of earlier collection progress. The season cheat-code catalog includes both trackable one-time rewards and repeatable effects and can grow as new codes are introduced.

This catalog revision establishes a clean-slate database schema and replaces the earlier migration history. Its initial migration removes the known legacy application tables before recreating them, so existing accounts, authentication keys, and collection progress are intentionally discarded.

The application stores Google's immutable subject identifier, the user's chosen display name, and an optional Epic Games Display Name. Email addresses and Google avatar URLs are not persisted.

## Use Supabase PostgreSQL

The application talks to Supabase through the server using EF Core and Npgsql. Database credentials are never sent to the Blazor WebAssembly client.

1. Create a Supabase project and open **Connect** in its dashboard.
2. Copy the .NET/Npgsql connection details. Prefer the direct connection when the host supports IPv6; otherwise use the Supavisor **session pooler** connection for an IPv4 host. Do not use transaction mode for EF Core migrations.
3. Store the connection string in the Aspire AppHost's user secrets:

```powershell
dotnet user-secrets set "ConnectionStrings:sprite-tracker" "Host=YOUR_HOST;Port=5432;Database=postgres;Username=YOUR_USER;Password=YOUR_PASSWORD;SSL Mode=Require" --project src/FortniteSpriteTracker.AppHost
dotnet run --project src/FortniteSpriteTracker.AppHost
```

When that secret is present, Aspire uses Supabase instead of starting the local PostgreSQL container. Remove it to return to the local database:

```powershell
dotnet user-secrets remove "ConnectionStrings:sprite-tracker" --project src/FortniteSpriteTracker.AppHost
```

On Azure or another production host, set the same secret as the environment variable `ConnectionStrings__sprite-tracker`. Also provide `Authentication__Google__ClientId` and `Authentication__Google__ClientSecret`. The server applies committed EF Core migrations during startup, so the Supabase database user must be allowed to create and alter tables.

## Build and publish

```powershell
dotnet build FortniteSpriteTracker.slnx -c Release
dotnet publish src/FortniteSpriteTracker/FortniteSpriteTracker.csproj -c Release
```

The server hosts the Blazor WebAssembly output and authenticated API from the same origin. Supply the PostgreSQL connection string and Google credentials through environment variables or the hosting platform's secret store.

## Free-first Azure hosting

The `infra` Bicep templates deploy one Azure Container App with conservative cost defaults:

- Consumption workload profile only.
- Scale to zero with a maximum of one replica.
- 0.5 vCPU and 1 GiB memory.
- No Log Analytics workspace, Application Insights, or persisted Azure platform logs.
- An optional $5 resource-group budget with alerts at 50%, forecasted 100%, and actual 100%.
- Secrets stored in the Container App configuration.

Azure budgets send alerts; they do not stop resources or enforce a hard spending limit. The replica ceiling and omission of paid supporting resources are the primary cost controls.

Build and publish the image to a public OCI registry such as GitHub Container Registry. A public GHCR image avoids an Azure Container Registry charge and does not require registry credentials in Container Apps:

```powershell
docker build -t ghcr.io/YOUR_GITHUB_USER/fortnite-sprite-tracker:latest .
docker push ghcr.io/YOUR_GITHUB_USER/fortnite-sprite-tracker:latest
```

GHCR publishes new packages as private by default. After the first push, open the package's **Package settings**, choose **Change visibility**, and make it **Public**. Container Apps intentionally has no registry credentials in this free-first configuration, so the image must allow anonymous pulls. Verify that before deploying:

```powershell
docker logout ghcr.io
docker manifest inspect ghcr.io/YOUR_GITHUB_USER/fortnite-sprite-tracker:latest
```

If the push itself requires authentication, create a GitHub personal access token with `write:packages`, then use it without putting the token in shell history:

```powershell
$env:GHCR_TOKEN | docker login ghcr.io --username YOUR_GITHUB_USER --password-stdin
docker push ghcr.io/YOUR_GITHUB_USER/fortnite-sprite-tracker:latest
```

If Docker containers cannot reach NuGet (`NU1301: Network is unreachable`), publish with the host SDK and use the network-independent local Dockerfile:

```powershell
dotnet publish src/FortniteSpriteTracker/FortniteSpriteTracker.csproj `
  --configuration Release `
  --output container-publish

docker build --file Dockerfile.local `
  --tag ghcr.io/YOUR_GITHUB_USER/fortnite-sprite-tracker:latest .
```

This produces the same ASP.NET Core runtime image without restoring packages inside Docker. The `container-publish` directory is excluded from Git. To repair Docker itself, verify Docker Desktop's proxy/VPN/firewall settings and confirm that `docker run --rm mcr.microsoft.com/dotnet/sdk:10.0 curl -I https://api.nuget.org/v3/index.json` succeeds.

After installing and signing in with the Azure CLI, deploy at subscription scope:

```powershell
az deployment sub create `
  --name sprite-scout-production `
  --location eastus2 `
  --template-file infra/main.bicep `
  --parameters `
    location=eastus2 `
    containerImage=ghcr.io/YOUR_GITHUB_USER/fortnite-sprite-tracker:latest `
    databaseConnectionString="YOUR_SUPABASE_CONNECTION_STRING" `
    googleClientId="YOUR_GOOGLE_CLIENT_ID" `
    googleClientSecret="YOUR_GOOGLE_CLIENT_SECRET" `
    budgetContactEmail="YOUR_EMAIL"
```

Choose an Azure region close to the Supabase project. Use the Supabase session pooler on port 5432 unless the selected Azure environment can reach the direct IPv6 endpoint. Update Google OAuth with the deployed `/signin-google` callback URL.

Each Bicep deployment stamps the Container App template with a new deployment version. This forces Azure to create a revision and re-pull mutable tags such as `latest`. For reproducible production releases, prefer an immutable version tag or image digest in `containerImage`.

## Automatic production releases

The `Release Sprite Scout` GitHub Actions workflow builds and pushes immutable and `latest` GHCR images, then deploys the immutable image to Azure Container Apps on every push to `main`. It can also be run manually from the Actions tab.

Complete these one-time setup steps before the first release:

1. Create a GitHub environment named `production` (optional, but recommended for deployment protection rules).
2. Configure Azure workload identity federation for this repository and its `production` environment. Grant that identity permission to create subscription deployments and manage the production resource group.
3. Add these GitHub environment secrets:
   - `AZURE_CLIENT_ID`
   - `AZURE_TENANT_ID`
   - `AZURE_SUBSCRIPTION_ID`
   - `DATABASE_CONNECTION_STRING`
   - `GOOGLE_CLIENT_ID`
   - `GOOGLE_CLIENT_SECRET`
4. Optionally add these GitHub environment variables:
   - `AZURE_LOCATION` (defaults to `eastus2`)
   - `AZURE_RESOURCE_GROUP` (defaults to `rg-sprite-scout-prod`)
   - `BUDGET_CONTACT_EMAIL` (omitting it disables budget email alerts)
5. After the first image is published, make the repository's GHCR package public so Azure Container Apps can pull it without registry credentials.

The federated credential's GitHub subject must be `repo:OWNER/REPOSITORY:environment:production`. No long-lived Azure password is stored in GitHub.

Container Apps retains live log streaming even though historical platform logs are disabled. Application authentication keys are persisted in Supabase, so scaling to zero or replacing the container does not invalidate every login cookie.

## Repository layout

- `FortniteSpriteTracker.slnx` - root solution
- `src/FortniteSpriteTracker` - ASP.NET Core Blazor Web App host, prerendering, authentication, and persistence API
- `src/FortniteSpriteTracker.Client` - components and services compiled for WebAssembly interactivity
- `src/FortniteSpriteTracker.Shared` - client/server API contracts
- `src/FortniteSpriteTracker.AppHost` - Aspire orchestration for the server and PostgreSQL
- `src/FortniteSpriteTracker.ServiceDefaults` - shared health checks, telemetry, and service discovery

## Notes

This is an unofficial fan-made companion. Sprite artwork is served from the CDN, and its original source attribution is retained in `src/FortniteSpriteTracker.Client/wwwroot/images/sprites/manifest.json`.
