using System.Text.Json;
using Microsoft.JSInterop;

namespace FortniteSpriteTracker.Services;

public sealed class BrowserStorage(IJSRuntime js)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async ValueTask<T> GetAsync<T>(string key, T fallback)
    {
        var json = await js.InvokeAsync<string?>("localStorage.getItem", key);
        if (string.IsNullOrWhiteSpace(json)) return fallback;
        try { return JsonSerializer.Deserialize<T>(json, JsonOptions) ?? fallback; }
        catch (JsonException) { return fallback; }
    }

    public ValueTask SetAsync<T>(string key, T value) =>
        js.InvokeVoidAsync("localStorage.setItem", key, JsonSerializer.Serialize(value, JsonOptions));
}
