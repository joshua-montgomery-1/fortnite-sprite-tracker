using FortniteSpriteTracker.Services;
using FortniteSpriteTracker.Shared.Profiles;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FortniteSpriteTracker.Pages;

public partial class Account
{
    [Inject] private AccountClient AccountApi { get; set; } = null!;
    [Inject] private AccountState AccountState { get; set; } = null!;
    [Inject] private ThemeService Theme { get; set; } = null!;
    [Inject] private CollectionClient CollectionApi { get; set; } = null!;
    [Inject] private AuthenticationNavigation AuthenticationNavigation { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private IJSRuntime JavaScript { get; set; } = null!;

    private UserProfileDto? profile;
    private string displayName = "";
    private string epicDisplayName = "";
    private string deleteConfirmation = "";
    private string? saveMessage;
    private string? deleteError;
    private bool isCollectionPublic;
    private ThemePreference themePreference;
    private bool loading = true;
    private bool saving;
    private bool deleting;
    private bool busy;
    private bool copied;
    private bool saveSucceeded;
    private int ownedCount;
    private int masteredCount;

    private string ProfileUrl => profile is null ? "" : new Uri(new Uri(Navigation.BaseUri), $"players/{profile.PublicId:D}").AbsoluteUri;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            await AccountState.RefreshAsync();
            profile = AccountState.Profile;
            if (profile is null)
            {
                AuthenticationNavigation.SignIn("/account");
                return;
            }

            displayName = profile.DisplayName;
            epicDisplayName = profile.EpicDisplayName ?? "";
            isCollectionPublic = profile.IsCollectionPublic;
            themePreference = profile.ThemePreference;
            var collection = await CollectionApi.GetAsync();
            ownedCount = collection.Count(item => item.IsOwned);
            masteredCount = collection.Count(item => item.IsMastered);
        }
        catch (HttpRequestException)
        {
            saveMessage = "We couldn't load your account. Refresh and try again.";
        }
        finally
        {
            loading = false;
        }
    }

    private async Task SaveAsync()
    {
        var trimmedDisplayName = displayName.Trim();
        var trimmedEpicName = epicDisplayName.Trim();
        if (trimmedDisplayName.Length is < 1 or > 80 || (trimmedEpicName.Length > 0 && trimmedEpicName.Length is < 3 or > 16))
        {
            saveSucceeded = false;
            saveMessage = "Enter a display name and an Epic name between 3 and 16 characters.";
            return;
        }

        saving = true;
        saveMessage = null;
        try
        {
            profile = await AccountApi.UpdateProfileAsync(new(
                trimmedDisplayName,
                string.IsNullOrWhiteSpace(trimmedEpicName) ? null : trimmedEpicName,
                isCollectionPublic,
                themePreference));
            AccountState.SetProfile(profile);
            await Theme.SetPreferenceAsync(profile.ThemePreference);
            saveSucceeded = true;
            saveMessage = "Account settings saved.";
        }
        catch (HttpRequestException)
        {
            saveSucceeded = false;
            saveMessage = "Your changes couldn't be saved. Try again.";
        }
        finally
        {
            saving = false;
        }
    }

    private Task PreviewThemeAsync() => Theme.PreviewAsync(themePreference);

    private async Task CopyLinkAsync()
    {
        await JavaScript.InvokeVoidAsync("navigator.clipboard.writeText", ProfileUrl);
        copied = true;
    }

    private async Task LogoutAsync()
    {
        busy = true;
        try
        {
            await AccountApi.LogoutAsync();
            Navigation.NavigateTo("/", true);
        }
        finally
        {
            busy = false;
        }
    }

    private async Task DeleteAsync()
    {
        if (deleteConfirmation != "DELETE")
        {
            return;
        }

        busy = deleting = true;
        deleteError = null;
        try
        {
            await AccountApi.DeleteAccountAsync();
            Navigation.NavigateTo("/", true);
        }
        catch (HttpRequestException)
        {
            deleteError = "Your account couldn't be deleted. Try again.";
        }
        finally
        {
            busy = deleting = false;
        }
    }
}
