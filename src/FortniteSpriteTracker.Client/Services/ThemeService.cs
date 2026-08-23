using FortniteSpriteTracker.Shared.Profiles;
using Microsoft.JSInterop;

namespace FortniteSpriteTracker.Services;

public sealed class ThemeService(IJSRuntime js, BrowserStorage storage) : IAsyncDisposable
{
    public const string StorageKey = "sprite-scout-theme-preference";

    private readonly Lazy<Task<IJSObjectReference>> moduleTask = new(() => js.InvokeAsync<IJSObjectReference>(
        "import", "./js/theme.js").AsTask());
    private DotNetObjectReference<ThemeService>? reference;
    private bool initialized;

    public ThemePreference Preference { get; private set; } = ThemePreference.System;

    public async Task InitializeAsync(ThemePreference? accountPreference = null)
    {
        if (!OperatingSystem.IsBrowser())
        {
            return;
        }

        Preference = accountPreference ?? await storage.GetAsync(StorageKey, ThemePreference.System);
        if (accountPreference.HasValue)
        {
            await storage.SetAsync(StorageKey, Preference);
        }
        await ApplyAsync();
        initialized = true;
    }

    public async Task SetPreferenceAsync(ThemePreference preference)
    {
        Preference = preference;
        await storage.SetAsync(StorageKey, preference);

        if (initialized)
        {
            await ApplyAsync();
        }
    }

    public async Task PreviewAsync(ThemePreference preference)
    {
        Preference = preference;
        if (initialized)
        {
            await ApplyAsync();
        }
    }

    private async Task ApplyAsync()
    {
        reference ??= DotNetObjectReference.Create(this);
        var module = await moduleTask.Value;
        await module.InvokeVoidAsync("apply", Preference.ToStorageValue(), reference);
    }

    [JSInvokable]
    public Task SystemThemeChangedAsync(bool isDark)
    {
        // The CSS data attribute is updated by JavaScript before this callback.
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        reference?.Dispose();
        if (moduleTask.IsValueCreated)
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("dispose");
            await module.DisposeAsync();
        }
    }
}
