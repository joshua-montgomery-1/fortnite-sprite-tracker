# Sprite Scout

A Fortnite Sprite collection tracker built with .NET 10, Blazor WebAssembly, and an ASP.NET Core backend.

## Features

- Tracks 25 Sprite families and 117 valid variants.
- Separate Owned and Mastered status for every variant.
- Compact checklist and collectible Field Guide views.
- Local browser persistence with a Google-account persistence backend in development.
- Populated and blank landscape PDF checklist exports.
- Responsive layout and locally hosted community artwork.

## Run locally

The complete development stack is orchestrated by .NET Aspire and requires a Docker-compatible container runtime for PostgreSQL.

```powershell
dotnet tool restore
dotnet user-secrets set "Authentication:Google:ClientId" "your-client-id" --project src/FortniteSpriteTracker.Server
dotnet user-secrets set "Authentication:Google:ClientSecret" "your-client-secret" --project src/FortniteSpriteTracker.Server
dotnet run --project src/FortniteSpriteTracker.AppHost
```

Aspire starts PostgreSQL, waits for it to become healthy, injects its connection string into the server, applies EF Core migrations, and starts the hosted Blazor application. Configure Google OAuth with the server's `/signin-google` callback URL. Google credentials are optional for startup but required to sign in.

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
dotnet build FortniteSpriteTracker.sln -c Release
dotnet publish src/FortniteSpriteTracker.Server/FortniteSpriteTracker.Server.csproj -c Release
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
dotnet publish src/FortniteSpriteTracker.Server/FortniteSpriteTracker.Server.csproj `
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

Container Apps retains live log streaming even though historical platform logs are disabled. Application authentication keys are persisted in Supabase, so scaling to zero or replacing the container does not invalidate every login cookie.

## Repository layout

- `FortniteSpriteTracker.sln` - root solution
- `src/FortniteSpriteTracker` - Blazor WebAssembly client
- `src/FortniteSpriteTracker.Server` - ASP.NET Core host, authentication, and persistence API
- `src/FortniteSpriteTracker.Shared` - client/server API contracts
- `src/FortniteSpriteTracker.AppHost` - Aspire orchestration for the server and PostgreSQL
- `src/FortniteSpriteTracker.ServiceDefaults` - shared health checks, telemetry, and service discovery

## Notes

This is an unofficial fan-made companion. Sprite artwork is checked into `wwwroot/images/sprites`, and its original source attribution is retained in `wwwroot/images/sprites/manifest.json`.
