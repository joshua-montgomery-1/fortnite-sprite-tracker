# Sprite Scout

A static-first Fortnite Sprite collection tracker built with .NET 10 and standalone Blazor WebAssembly.

## Features

- Tracks 25 Sprite families and 117 valid variants.
- Separate Owned and Mastered status for every variant.
- Compact checklist and collectible Field Guide views.
- Local browser persistence; no server or account required.
- Populated and blank landscape PDF checklist exports.
- Responsive layout and locally hosted community artwork.

## Run locally

```powershell
dotnet run --project src/FortniteSpriteTracker
```

## Build and publish

```powershell
dotnet build FortniteSpriteTracker.sln -c Release
dotnet publish src/FortniteSpriteTracker/FortniteSpriteTracker.csproj -c Release
```

The static site is emitted to `src/FortniteSpriteTracker/bin/Release/net10.0/publish/wwwroot` and can be hosted by any static file provider. Configure the host to serve `index.html` for unknown routes if additional client routes are added.

## Repository layout

- `FortniteSpriteTracker.sln` - root solution
- `src/FortniteSpriteTracker` - standalone Blazor WebAssembly project

## Notes

This is an unofficial fan-made companion. Sprite artwork is checked into `wwwroot/images/sprites`, and its original source attribution is retained in `wwwroot/images/sprites/manifest.json`.
