using FortniteSpriteTracker.Shared.Profiles;
using Microsoft.AspNetCore.Components.Authorization;

namespace FortniteSpriteTracker.Services;

public sealed class AccountState(
    AccountClient accountClient,
    AuthenticationStateProvider authenticationStateProvider)
{
    private Task? loadTask;

    public UserProfileDto? Profile { get; private set; }
    public bool IsLoaded { get; private set; }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsBrowser())
        {
            return;
        }

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

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsBrowser())
        {
            return;
        }

        loadTask = LoadCoreAsync(cancellationToken);
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
        var authenticationState = await authenticationStateProvider.GetAuthenticationStateAsync();
        Profile = authenticationState.User.Identity?.IsAuthenticated == true
            ? await accountClient.GetProfileAsync(cancellationToken)
            : null;
        IsLoaded = true;
    }
}
