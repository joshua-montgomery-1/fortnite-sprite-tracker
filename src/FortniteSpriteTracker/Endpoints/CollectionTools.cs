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

    [McpServerTool(Name = "set_sprite_mastered", ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Mark a Sprite variant mastered and owned in your collection. Requires collection:write. Use the exact variant ID from list_sprites.")]
    public Task<SpriteProgressDto> SetMastered([Description("Sprite variant ID, not a family ID.")] int spriteVariantId,
        ClaimsPrincipal user, CancellationToken cancellationToken) => SetAsync(spriteVariantId, null, true, user, cancellationToken);

    [McpServerTool(Name = "set_sprite_unmastered", ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Clear mastery for a Sprite variant, preserving whether it is owned. Requires collection:write.")]
    public Task<SpriteProgressDto> SetUnmastered([Description("Sprite variant ID from list_sprites.")] int spriteVariantId,
        ClaimsPrincipal user, CancellationToken cancellationToken) => SetAsync(spriteVariantId, null, false, user, cancellationToken);

    [McpServerTool(Name = "set_sprite_owned", ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Mark a Sprite variant owned, preserving its mastery status. Requires collection:write.")]
    public Task<SpriteProgressDto> SetOwned([Description("Sprite variant ID from list_sprites.")] int spriteVariantId,
        ClaimsPrincipal user, CancellationToken cancellationToken) => SetAsync(spriteVariantId, true, null, user, cancellationToken);

    [McpServerTool(Name = "set_sprite_unowned", ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Remove a Sprite variant from your collection, clearing both ownership and mastery. Requires collection:write.")]
    public Task<SpriteProgressDto> SetUnowned([Description("Sprite variant ID from list_sprites.")] int spriteVariantId,
        ClaimsPrincipal user, CancellationToken cancellationToken) => SetAsync(spriteVariantId, false, null, user, cancellationToken);

    private async Task<SpriteProgressDto> SetAsync(int variantId, bool? owned, bool? mastered,
        ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var userId = await accounts.GetUserIdAsync(user, AuthDefaults.CollectionWriteScope, cancellationToken);
        try { return await collection.SetAsync(userId, variantId, owned, mastered, cancellationToken); }
        catch (InvalidSpriteVariantException exception) { throw new McpException(exception.Message); }
    }

    public sealed record CollectionPage(IReadOnlyList<SpriteProgressDto> Items, int TotalCount, int Offset, int Limit, bool HasMore);
}
