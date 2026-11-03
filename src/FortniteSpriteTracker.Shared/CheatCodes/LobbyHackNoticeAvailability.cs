using FortniteSpriteTracker.Shared.Catalog;

namespace FortniteSpriteTracker.Shared.CheatCodes;

public static class LobbyHackNoticeAvailability
{
    public static bool IsAvailable(IEnumerable<SeasonDto> seasons, DateTimeOffset now)
    {
        // A newer season supersedes an open-ended previous season.
        var current = seasons.Where(season => season.StartAt <= now)
            .OrderByDescending(season => season.StartAt)
            .ThenByDescending(season => season.Chapter)
            .ThenByDescending(season => season.Number)
            .FirstOrDefault();

        return current is { Chapter: 7, Number: 4 } &&
            (current.EndAt is null || now < current.EndAt);
    }
}
