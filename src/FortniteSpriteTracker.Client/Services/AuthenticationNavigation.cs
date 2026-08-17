using Microsoft.AspNetCore.Components;

namespace FortniteSpriteTracker.Services;

public sealed class AuthenticationNavigation(NavigationManager navigation)
{
    private bool signInStarted;

    public void SignIn(string? returnUrl = null)
    {
        if (signInStarted)
        {
            return;
        }

        signInStarted = true;
        var destination = returnUrl ?? CurrentPathAndQuery();
        navigation.NavigateTo(
            $"auth/login?returnUrl={Uri.EscapeDataString(destination)}",
            forceLoad: true);
    }

    private string CurrentPathAndQuery()
    {
        var relativeUrl = navigation.ToBaseRelativePath(navigation.Uri);
        return string.IsNullOrEmpty(relativeUrl) ? "/" : $"/{relativeUrl}";
    }
}
