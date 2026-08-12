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

## Repository layout

- `FortniteSpriteTracker.sln` - root solution
- `src/FortniteSpriteTracker` - Blazor WebAssembly client
- `src/FortniteSpriteTracker.Server` - ASP.NET Core host, authentication, and persistence API
- `src/FortniteSpriteTracker.Shared` - client/server API contracts
- `src/FortniteSpriteTracker.AppHost` - Aspire orchestration for the server and PostgreSQL
- `src/FortniteSpriteTracker.ServiceDefaults` - shared health checks, telemetry, and service discovery

## Notes

This is an unofficial fan-made companion. Sprite artwork is checked into `wwwroot/images/sprites`, and its original source attribution is retained in `wwwroot/images/sprites/manifest.json`.
