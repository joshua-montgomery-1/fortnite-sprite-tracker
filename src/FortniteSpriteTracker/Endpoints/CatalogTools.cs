using System.ComponentModel;
using FortniteSpriteTracker.DataAccess;
using FortniteSpriteTracker.Shared.Catalog;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace FortniteSpriteTracker.Server.Endpoints;

[McpServerToolType]
public sealed class CatalogTools(SpriteTrackerDbContext database)
{
    [McpServerTool(Name = "list_seasons", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("List Sprite Scout seasons, newest first, including season IDs, chapter/number, dates and catalog availability. All MCP tools require a free Sprite Scout account.")]
    public async Task<SeasonList> ListSeasons(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var items = await database.Seasons.AsNoTracking().OrderByDescending(item => item.Chapter)
            .ThenByDescending(item => item.Number).ThenBy(item => item.Id)
            .Select(item => new SeasonDto
            {
                Id = item.Id, Name = item.Name, Chapter = item.Chapter, Number = item.Number,
                StartAt = item.StartAt, EndAt = item.EndAt,
                IsActive = item.StartAt <= now && (item.EndAt == null || now < item.EndAt),
                HasCatalog = item.SpriteFamilies.Any() && item.SpriteVariants.Any(),
                HasCheatCodes = item.CheatCodes.Any()
            }).ToArrayAsync(cancellationToken);
        return new(items);
    }

    [McpServerTool(Name = "list_sprites", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("List Sprite variants across seasons, with exact variant IDs for collection tools, family/style names, images, release status and season memberships. Results are paginated; follow HasMore using Offset + Limit. Ownership is shared across seasons.")]
    public Task<SpritePage> ListSprites(CancellationToken cancellationToken,
        [Description("Optional text matching a sprite family name or style name.")] string? search = null,
        [Description("Number of results to skip; zero or greater.")] int offset = 0,
        [Description("Page size, from 1 to 100.")] int limit = 50) =>
        ListAsync(null, search, offset, limit, cancellationToken);

    [McpServerTool(Name = "list_sprites_by_season", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("List Sprite variants belonging to one season. Includes exact variant IDs, family/style names, images and release status for that season. Unreleased entries can be viewed but cannot be marked owned or mastered.")]
    public Task<SpritePage> ListSpritesBySeason(
        [Description("Season ID from list_seasons.")] int seasonId, CancellationToken cancellationToken,
        [Description("Optional text matching a sprite family name or style name.")] string? search = null,
        [Description("Number of results to skip; zero or greater.")] int offset = 0,
        [Description("Page size, from 1 to 100.")] int limit = 50) =>
        ListAsync(seasonId, search, offset, limit, cancellationToken);

    private async Task<SpritePage> ListAsync(int? seasonId, string? search, int offset, int limit,
        CancellationToken cancellationToken)
    {
        ValidatePage(offset, limit);
        search = search?.Trim();
        if (search?.Length > 100) throw new McpException("Search text must be 100 characters or fewer.");
        if (seasonId is not null && !await database.Seasons.AnyAsync(item => item.Id == seasonId, cancellationToken))
            throw new McpException("Season not found. Use list_seasons to choose a season ID.");
        var now = DateTimeOffset.UtcNow;
        var query = database.SpriteVariants.AsNoTracking().Where(item => item.Seasons.Any());
        if (seasonId is not null) query = query.Where(item => item.Seasons.Any(season => season.SeasonId == seasonId));
        if (!string.IsNullOrEmpty(search))
        {
            var term = search.ToLowerInvariant();
            query = query.Where(item => item.SpriteFamily.Name.ToLower().Contains(term) ||
                item.VariantStyle.Name.ToLower().Contains(term));
        }
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(item => item.SpriteFamily.Name).ThenBy(item => item.VariantStyle.DisplayOrder)
            .ThenBy(item => item.Id).Skip(offset).Take(limit)
            .Select(item => new SpriteEntry(item.Id, item.SpriteFamily.Name, item.SpriteFamily.Slug,
                item.VariantStyle.Name, item.VariantStyle.Slug, item.ImagePath,
                item.Seasons.Any(season => (seasonId == null || season.SeasonId == seasonId) &&
                    season.Season.StartAt <= now && (season.ReleasedAt == null || season.ReleasedAt <= now)),
                item.Seasons.Where(season => seasonId == null || season.SeasonId == seasonId)
                    .OrderByDescending(season => season.Season.Chapter).ThenByDescending(season => season.Season.Number)
                    .Select(season => new SpriteSeason(season.SeasonId, season.Season.Name, season.ReleasedAt)).ToArray()))
            .ToArrayAsync(cancellationToken);
        return new(items, total, offset, limit, (long)offset + items.Length < total);
    }

    internal static void ValidatePage(int offset, int limit)
    {
        if (offset < 0 || limit is < 1 or > 100)
            throw new McpException("Offset must be zero or greater, and limit must be between 1 and 100.");
    }

    public sealed record SeasonList(IReadOnlyList<SeasonDto> Items);
    public sealed record SpritePage(IReadOnlyList<SpriteEntry> Items, int TotalCount, int Offset, int Limit, bool HasMore);
    public sealed record SpriteEntry(int SpriteVariantId, string FamilyName, string FamilySlug, string StyleName,
        string StyleSlug, string ImagePath, bool IsReleased, IReadOnlyList<SpriteSeason> Seasons);
    public sealed record SpriteSeason(int SeasonId, string Name, DateTimeOffset? ReleasedAt);
}
