namespace SpriteScout.Auth;

public sealed record AuthArtwork(int SeasonId, string ImagePath);

// The host supplies catalog artwork; the auth module owns only its presentation.
public interface IAuthArtworkSource
{
    Task<IReadOnlyList<AuthArtwork>> GetAsync(CancellationToken cancellationToken);
}
