namespace SpriteScout.Auth;

// The composing host provisions application profiles; the auth module owns identity only.
public interface ICentralAccountObserver
{
    Task GoogleSignedInAsync(Guid accountId, string googleSubject, string? displayName,
        CancellationToken cancellationToken);
}
