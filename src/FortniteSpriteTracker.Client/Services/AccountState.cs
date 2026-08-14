using FortniteSpriteTracker.Shared.Profiles;

namespace FortniteSpriteTracker.Services;

public sealed class AccountState(AccountClient accountClient)
{
    private Task? loadTask;

    public UserProfileDto? Profile { get; private set; }
    public bool IsLoaded { get; private set; }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (IsLoaded)
        {
            return;
        }

        loadTask ??= LoadCoreAsync(cancellationToken);
        try
        {
            await loadTask;
        }
        catch
        {
            loadTask = null;
            throw;
        }
    }

    public void SetProfile(UserProfileDto profile)
    {
        Profile = profile;
        IsLoaded = true;
    }

    private async Task LoadCoreAsync(CancellationToken cancellationToken)
    {
        Profile = await accountClient.GetProfileAsync(cancellationToken);
        IsLoaded = true;
    }
}
