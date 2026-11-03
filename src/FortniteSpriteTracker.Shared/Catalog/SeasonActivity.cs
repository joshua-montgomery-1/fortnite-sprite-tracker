namespace FortniteSpriteTracker.Shared.Catalog;

public static class SeasonActivity
{
    public static SeasonDto? GetActiveSeason(IEnumerable<SeasonDto> seasons, DateTimeOffset now)
    {
        // A newer started season supersedes an open-ended previous season.
        var current = seasons.Where(season => season.StartAt <= now)
            .OrderByDescending(season => season.StartAt)
            .ThenByDescending(season => season.Chapter)
            .ThenByDescending(season => season.Number)
            .FirstOrDefault();

        return current is not null && (current.EndAt is null || now < current.EndAt)
            ? current
            : null;
    }
}
