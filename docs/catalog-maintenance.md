# Catalog maintenance

This guide explains how to update the season catalog and Lobby Hack definitions without changing the application model.

## Catalog model

Season data is deliberately separate from reusable Sprite data:

- `Season` identifies a Fortnite chapter and season, including its active dates.
- `SpriteFamily` is the reusable family identity and slug.
- `SpriteVariant` combines a family with a `VariantStyle`.
- `SeasonSpriteFamily` stores season-specific family details such as rarity, ability, colors, artwork, and display order.
- `SeasonSpriteVariant` controls which variants appear in a season.
- `CheatCode` belongs to a season and a `CheatCodeCategory`.
- `CheatCodeProgress` stores a signed-in user's used-code state.

This structure allows a new season to reuse existing families and styles while keeping each season's collection and totals distinct.

## Add or update Sprite content

The committed catalog definitions live in `src/FortniteSpriteTracker.DataAccess/Data/Seeding/CatalogSeedData.cs` and `CatalogSeedModels.cs`.

When adding a season:

1. Add or update the season metadata in `CatalogSeeder.cs`.
2. Add the season's family definitions and season-specific properties to the seed data.
3. Reuse an existing `SpriteFamily` when the family identity is unchanged; create a new stable family ID only for a genuinely new family.
4. Use a unique, stable `SpriteVariant` ID for each family/style combination.
5. Add the appropriate season-family and season-variant relationships.
6. Add the artwork path used by the application. Sprite assets are served from the configured CDN; keep source attribution in `src/FortniteSpriteTracker.Client/wwwroot/images/sprites/manifest.json`.
7. Keep display order intentional and verify every image path.

The seeder validates positive, unique IDs and the committed catalog shape before writing data. Do not silently repurpose an existing ID, because user progress references these IDs.

## Add or update Lobby Hack codes

Lobby Hack definitions live in `src/FortniteSpriteTracker.DataAccess/Data/Seeding/CheatCodeSeedData.cs`.

Each code should have a stable ID, season, uppercase code string, reward category, display order, description, and optional requirement. Set `IsTrackable = true` for one-time rewards whose used state should be saved; use `false` for repeatable or reference-only effects.

Keep code definitions season-specific. New seasonal codes should be added to the appropriate season rather than replacing an older season's definitions. The UI and API support category filtering, used/unused progress, repeatable effects, and future categories.

## Seed and verify

Configure the target PostgreSQL connection first, then run:

```powershell
dotnet run --project src/FortniteSpriteTracker -- --seed-catalog
```

The command applies pending EF Core migrations, reconciles the committed seed, reports inserted and updated rows, and exits. It is idempotent and does not start the web server.

Before committing catalog changes, verify:

```powershell
dotnet build FortniteSpriteTracker.slnx -c Release
git diff --check
```

For production-only imports, prefer an explicit SQL script that resolves live season, family, style, and sequence values rather than guessing IDs. Do not run import scripts against production until their transaction and idempotency behavior has been reviewed.

## Schema changes

Use an EF Core migration for changes to entities, relationships, or indexes. The server applies committed migrations during startup, so production database credentials must be able to create and alter tables. Treat destructive migrations as releases requiring an explicit backup and rollback plan.
