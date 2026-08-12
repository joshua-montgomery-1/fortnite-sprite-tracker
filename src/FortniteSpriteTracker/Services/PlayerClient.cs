using System.Net;
using System.Net.Http.Json;
using FortniteSpriteTracker.Shared.Players;

namespace FortniteSpriteTracker.Services;

public sealed class PlayerClient(HttpClient httpClient)
{
    public async Task<PlayerSummaryDto?> FindByEpicUsernameAsync(string epicUsername, CancellationToken cancellationToken = default)
    {
        var url = $"api/players/search?epicUsername={Uri.EscapeDataString(epicUsername.Trim())}";
        using var response = await httpClient.GetAsync(url, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PlayerSummaryDto>(cancellationToken);
    }

    public async Task<PlayerCollectionDto?> GetAsync(Guid publicId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"api/players/{publicId:D}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PlayerCollectionDto>(cancellationToken);
    }
}
