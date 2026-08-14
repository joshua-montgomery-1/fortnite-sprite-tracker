using System.Text.Json;
using Microsoft.JSInterop;

namespace FortniteSpriteTracker.Services;

public sealed class BrowserStorage(IJSRuntime js)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async ValueTask<T> GetAsync<T>(string key, T fallback)
    {
        try
        {
            var json = await js.InvokeAsync<string?>("localStorage.getItem", key);
            if (string.IsNullOrWhiteSpace(json))
            {
                return fallback;
            }

            return JsonSerializer.Deserialize<T>(json, JsonOptions) ?? fallback;
        }
        catch (JsonException)
        {
            return fallback;
        }
        catch (JSException)
        {
            return fallback;
        }
    }

    public async ValueTask SetAsync<T>(string key, T value)
    {
        try
        {
            await js.InvokeVoidAsync("localStorage.setItem", key, JsonSerializer.Serialize(value, JsonOptions));
        }
        catch (JSException)
        {
            // The tracker remains usable when browser storage is unavailable.
        }
    }
}
