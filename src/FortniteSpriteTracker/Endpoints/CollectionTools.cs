using System.ComponentModel;
using System.Security.Claims;
using FortniteSpriteTracker.DataAccess;
using FortniteSpriteTracker.Server.Services;
using FortniteSpriteTracker.Shared.Collections;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using SpriteScout.Auth;

namespace FortniteSpriteTracker.Server.Endpoints;

[McpServerToolType]
public sealed class CollectionTools(SpriteTrackerDbContext database, McpAccountService accounts, CollectionService collection)
{
    [McpServerTool(Name = "list_collection", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("List your saved owned/mastered Sprite variants. Missing variants are unowned. Requires collection:read. Use list_sprites to resolve variant IDs.")]
    public async Task<CollectionPage> ListCollection(ClaimsPrincipal user, CancellationToken cancellationToken,
        [Description("Optional season ID from list_seasons.")] int? seasonId = null,
        [Description("Number of results to skip; zero or greater.")] int offset = 0,
        [Description("Page size, from 1 to 100.")] int limit = 50)
    {
        CatalogTools.ValidatePage(offset, limit);
        var userId = await accounts.GetUserIdAsync(user, AuthDefaults.CollectionReadScope, cancellationToken);
        if (seasonId is not null && !await database.Seasons.AnyAsync(item => item.Id == seasonId, cancellationToken))
            throw new McpException("Season not found. Use list_seasons to choose a season ID.");
        var query = database.SpriteProgress.AsNoTracking().Where(item => item.UserId == userId);
        if (seasonId is not null) query = query.Where(item => item.SpriteVariant.Seasons.Any(season => season.SeasonId == seasonId));
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(item => item.SpriteVariantId).Skip(offset).Take(limit)
            .Select(item => new SpriteProgressDto
            {
                SpriteVariantId = item.SpriteVariantId, IsOwned = item.IsOwned,
                IsMastered = item.IsMastered, UpdatedAtUtc = item.UpdatedAtUtc
            }).ToArrayAsync(cancellationToken);
        return new(items, total, offset, limit, (long)offset + items.Length < total);
    }

    [McpServerTool(Name = "update_collection", ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Update 1 to 1,000 Sprite variants in your collection atomically. Requires collection:write. Use variant IDs from list_sprites. Omitted flags preserve existing state. Mastered implies owned; unowned clears mastery. Invalid or duplicate IDs, empty updates, and isOwned=false with isMastered=true reject the entire batch. Returns resulting progress in input order. Use a one-item batch for a single Sprite.")]
    public async Task<CollectionUpdateResult> UpdateCollection(
        [Description("Updates with unique spriteVariantId values and at least one of isOwned or isMastered per item.")] CollectionUpdate[] updates,
        ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var userId = await accounts.GetUserIdAsync(user, AuthDefaults.CollectionWriteScope, cancellationToken);
        if (updates is null || updates.Length is < 1 or > 1000)
            throw new McpException("Provide between 1 and 1,000 collection updates.");
        if (updates.Any(item => item is null || item.SpriteVariantId <= 0 ||
            (item.IsOwned is null && item.IsMastered is null) ||
            (item.IsOwned == false && item.IsMastered == true)))
            throw new McpException("Each update needs a positive variant ID and at least one state flag. An unowned Sprite cannot be mastered.");
        if (updates.Select(item => item.SpriteVariantId).Distinct().Count() != updates.Length)
            throw new McpException("Provide each Sprite variant ID only once per batch.");
        try { return new(await collection.SetBatchAsync(userId, updates, cancellationToken)); }
        catch (InvalidSpriteVariantException exception) { throw new McpException(exception.Message); }
    }

    public sealed record CollectionUpdateResult(IReadOnlyList<SpriteProgressDto> Items);
    public sealed record CollectionPage(IReadOnlyList<SpriteProgressDto> Items, int TotalCount, int Offset, int Limit, bool HasMore);
}
