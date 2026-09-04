# Deployment

Sprite Scout runs as an ASP.NET Core container with PostgreSQL. The Blazor WebAssembly client and authenticated API are served from the same origin.

## Configuration

Set these values through .NET user secrets locally or the hosting platform's secret store:

| Purpose | .NET configuration key | Environment variable |
| --- | --- | --- |
| PostgreSQL | `ConnectionStrings:sprite-tracker` | `ConnectionStrings__sprite-tracker` |
| Google OAuth client ID | `Authentication:Google:ClientId` | `Authentication__Google__ClientId` |
| Google OAuth client secret | `Authentication:Google:ClientSecret` | `Authentication__Google__ClientSecret` |

Never commit credentials or put them in `appsettings.json`.

## Supabase PostgreSQL

Create a Supabase project and copy its .NET/Npgsql connection details. Prefer the direct connection when IPv6 is available. Otherwise use the Supavisor session pooler on port 5432. Do not use transaction mode for EF Core migrations.

For local use, store the connection string in AppHost user secrets:

```powershell
dotnet user-secrets set "ConnectionStrings:sprite-tracker" "Host=YOUR_HOST;Port=5432;Database=postgres;Username=YOUR_USER;Password=YOUR_PASSWORD;SSL Mode=Require" --project src/FortniteSpriteTracker.AppHost
dotnet run --project src/FortniteSpriteTracker.AppHost
```

When the secret is absent, Aspire starts the local PostgreSQL container. Remove the secret to return to that mode:

```powershell
dotnet user-secrets remove "ConnectionStrings:sprite-tracker" --project src/FortniteSpriteTracker.AppHost
```

The server applies migrations during startup, so the database user must be allowed to create and alter tables. Authentication keys are persisted in the database so container replacement does not invalidate every login cookie.

## Build the container

The default `Dockerfile` restores packages inside the SDK image:

```powershell
docker build -t ghcr.io/YOUR_GITHUB_USER/fortnite-sprite-tracker:latest .
docker push ghcr.io/YOUR_GITHUB_USER/fortnite-sprite-tracker:latest
```

The image listens on port `8080`. If Docker cannot reach NuGet, publish with the host SDK and use `Dockerfile.local`:

```powershell
dotnet publish src/FortniteSpriteTracker/FortniteSpriteTracker.csproj `
  --configuration Release `
  --output container-publish

docker build --file Dockerfile.local `
  --tag ghcr.io/YOUR_GITHUB_USER/fortnite-sprite-tracker:latest .
```

For anonymous Azure pulls, make the GHCR package public after its first push. Prefer an immutable image tag or digest for production.

## Azure Container Apps

The `infra` Bicep templates deploy a free-first Container App with scale-to-zero, one maximum replica, 0.25 vCPU, and 0.5 GiB memory. Log Analytics and Application Insights are intentionally omitted. An optional budget sends alerts but does not enforce a spending limit.

Deploy at subscription scope after signing in with Azure CLI:

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

Use an Azure region close to Supabase and update Google OAuth with the deployed `/signin-google` callback URL. Container App deployments create a new revision so mutable tags are re-pulled, but immutable tags or digests are preferred.

## GitHub Actions release

The `Release Sprite Scout` workflow runs on pushes to `main` and manual dispatches. It publishes a commit-SHA image and `latest` to GHCR, then deploys the commit-SHA image to Azure Container Apps.

Configure a `production` environment with:

- Secrets: `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`, `DATABASE_CONNECTION_STRING`, `GOOGLE_CLIENT_ID`, and `GOOGLE_CLIENT_SECRET`.
- Optional variables: `AZURE_LOCATION`, `AZURE_RESOURCE_GROUP`, and `BUDGET_CONTACT_EMAIL`.
- Azure workload identity federation scoped to `repo:OWNER/REPOSITORY:environment:production`.

Grant the federated identity permission to create subscription deployments and manage the production resource group. Make the GHCR package public after the first image is published unless registry credentials are added to the Container App.
